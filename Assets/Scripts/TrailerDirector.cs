using System;
using UnityEngine;

/// <summary>
/// Drives one camera through a list of timed shots (a poor-man's Timeline). Time starts at scene start,
/// which is also when the plane animations, traffic, NPCs and the baggage train start, so shots line up with them.
/// Also handles per-shot fog, fade-to-black and NPC "cues" (send a few pedestrians somewhere at a given time).
/// </summary>
public class TrailerDirector : MonoBehaviour
{
    public enum Mode { Fixed, TrackTarget, ChaseTarget, CockpitTarget, PanFixed, YawTrack }

    [Serializable]
    public class Shot
    {
        public string name;
        public float start;
        public float duration = 8f;
        public Mode mode;
        public Transform target;

        [Header("Camera path (Fixed / Track / PanFixed / YawTrack use posA -> posB)")]
        public Vector3 posA, posB;
        [Header("Fixed: look-at path")]
        public Vector3 lookA, lookB;

        [Header("Chase / Cockpit: offsets in heading space")]
        public Vector3 offsetA, offsetB;
        [Tooltip("Look-at offset in world space added to the target position (Track / Chase).")]
        public Vector3 lookOffset;

        [Header("PanFixed / YawTrack (degrees; yaw 0 = +Z, 90 = +X; pitch positive = down)")]
        public float yawA, yawB, pitch;

        public float fovA = 50f, fovB = 50f;
        [Range(0f, 1f)] public float handheld = 0.15f;
        public bool ease = true;

        [Header("Look")]
        [Tooltip("Exponential-squared fog density while this shot is on. 0 = no fog.")]
        public float fogDensity;
        public float fadeIn, fadeOut;
    }

    [Serializable]
    public class Cue
    {
        public string name;
        public float time;
        public Vector3 center;
        public float radius = 25f;
        public int count = 3;
        public Vector3 dest;
        public float destRadius = 1.5f;
        public float stand = 12f;
        [NonSerialized] public bool done;
    }

    [Serializable]
    public class LinkBlock
    {
        public string[] linkNames;
        public float from, to;
    }

    public LinkBlock[] linkBlocks;
    private Unity.AI.Navigation.NavMeshLink[] _links;

    public Camera cam;
    public CanvasGroup fade;
    public Shot[] shots;
    public Cue[] cues;
    public bool loop;
    public float totalLength = 190f;
    public float timeOffset;
    public Color fogColor = new Color(0.16f, 0.11f, 0.13f);

    private float _t;
    private Vector3 _lastTargetPos;
    private Vector3 _heading = Vector3.forward;   // smoothed 3D velocity direction of the current target
    private Transform _lastTarget;
    private int _active = -1;
    private float _fog;

    private void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        _t = timeOffset;
        if (fade != null) fade.alpha = 1f;
        _links = FindObjectsByType<Unity.AI.Navigation.NavMeshLink>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        _t += Time.deltaTime;
        if (loop && _t > totalLength) _t -= totalLength;
        RunCues();
        RunLinkBlocks();

        int idx = -1;
        for (int i = 0; i < shots.Length; i++)
            if (_t >= shots[i].start && _t < shots[i].start + shots[i].duration) { idx = i; break; }
        if (idx < 0) { if (fade != null) fade.alpha = 1f; return; }
        var s = shots[idx];
        if (idx != _active) { _active = idx; _lastTarget = null; }

        Vector3 pos, look; float fov;
        if (s.target != null && (s.mode == Mode.ChaseTarget || s.mode == Mode.CockpitTarget)) UpdateHeading(s.target);
        ComputePose(s, _t, s.target != null ? s.target.position : Vector3.zero, _heading, out pos, out look, out fov);

        if (s.handheld > 0f)
        {
            float n = Time.time * 0.9f;
            pos += new Vector3(Mathf.PerlinNoise(n, 1f) - 0.5f, Mathf.PerlinNoise(n, 5f) - 0.5f, Mathf.PerlinNoise(n, 9f) - 0.5f) * s.handheld;
        }
        cam.transform.position = pos;
        Vector3 fwd = look - pos;
        if (fwd.sqrMagnitude > 1e-4f) cam.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        cam.fieldOfView = fov;

        // fog
        _fog = Mathf.MoveTowards(_fog, s.fogDensity, Time.deltaTime * 0.004f);
        RenderSettings.fog = _fog > 0.00001f;
        if (RenderSettings.fog)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = _fog;
        }

        // fade to / from black
        if (fade != null)
        {
            float a = 0f;
            if (s.fadeIn > 0f) a = Mathf.Max(a, 1f - (_t - s.start) / s.fadeIn);
            if (s.fadeOut > 0f) a = Mathf.Max(a, 1f - (s.start + s.duration - _t) / s.fadeOut);
            fade.alpha = Mathf.Clamp01(a);
        }
    }

    // keeps pedestrians off chosen escalators for a window of time (so nobody rides against the camera)
    private void RunLinkBlocks()
    {
        if (linkBlocks == null || _links == null) return;
        foreach (var b in linkBlocks)
        {
            bool blocked = _t >= b.from && _t <= b.to;
            foreach (var l in _links)
            {
                if (l == null) continue;
                if (Array.IndexOf(b.linkNames, l.name) < 0) continue;
                if (l.enabled == blocked) l.enabled = !blocked;
            }
        }
    }

    private void RunCues()
    {
        if (cues == null) return;
        foreach (var c in cues)
        {
            if (c.done || _t < c.time) continue;
            c.done = true;
            var all = FindObjectsByType<PedestrianActivity>(FindObjectsSortMode.None);
            var picks = new System.Collections.Generic.List<PedestrianActivity>();
            foreach (var p in all)
            {
                if (p.allowedTypes != null && p.allowedTypes.Length > 0) continue;   // staff stay put
                if (Mathf.Abs(p.transform.position.y - c.center.y) > 3f) continue;
                if ((p.transform.position - c.center).magnitude <= c.radius) picks.Add(p);
            }
            picks.Sort((a, b) => (a.transform.position - c.center).sqrMagnitude.CompareTo((b.transform.position - c.center).sqrMagnitude));
            for (int i = 0; i < picks.Count && i < c.count; i++)
            {
                Vector2 o = UnityEngine.Random.insideUnitCircle * c.destRadius;
                picks[i].ForceGoTo(c.dest + new Vector3(o.x, 0f, o.y), c.stand);
            }
        }
    }

    /// <summary>Pure pose evaluation (also used by the editor preview).</summary>
    public static void ComputePose(Shot s, float t, Vector3 targetPos, Vector3 heading, out Vector3 pos, out Vector3 look, out float fov)
    {
        float u = Mathf.Clamp01((t - s.start) / Mathf.Max(0.01f, s.duration));
        float k = s.ease ? u * u * (3f - 2f * u) : u;
        pos = Vector3.zero; look = Vector3.zero;
        switch (s.mode)
        {
            case Mode.Fixed:
                pos = Vector3.Lerp(s.posA, s.posB, k);
                look = Vector3.Lerp(s.lookA, s.lookB, k);
                break;
            case Mode.TrackTarget:
                pos = Vector3.Lerp(s.posA, s.posB, k);
                look = targetPos + s.lookOffset;
                break;
            case Mode.PanFixed:
                pos = Vector3.Lerp(s.posA, s.posB, k);
                look = pos + Quaternion.Euler(s.pitch, Mathf.Lerp(s.yawA, s.yawB, k), 0f) * Vector3.forward * 50f;
                break;
            case Mode.YawTrack:
            {
                pos = Vector3.Lerp(s.posA, s.posB, k);
                Vector3 d = targetPos + s.lookOffset - pos;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                look = pos + Quaternion.Euler(s.pitch, yaw, 0f) * Vector3.forward * 50f;
                break;
            }
            default:
            {
                Vector3 flat = new Vector3(heading.x, 0f, heading.z);
                if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
                Quaternion hs = Quaternion.LookRotation(flat.normalized, Vector3.up);
                pos = targetPos + hs * Vector3.Lerp(s.offsetA, s.offsetB, k);
                if (s.mode == Mode.CockpitTarget)
                {
                    Vector3 dir = heading.sqrMagnitude > 1e-4f ? heading.normalized : Vector3.forward;
                    look = pos + dir * 100f;
                }
                else look = targetPos + s.lookOffset;
                break;
            }
        }
        fov = Mathf.Lerp(s.fovA, s.fovB, k);
    }

    private void UpdateHeading(Transform target)
    {
        if (_lastTarget != target) { _lastTarget = target; _lastTargetPos = target.position; }
        Vector3 d = target.position - _lastTargetPos;
        _lastTargetPos = target.position;
        if (d.sqrMagnitude > 1e-6f) _heading = Vector3.Slerp(_heading, d.normalized, 1f - Mathf.Exp(-3f * Time.deltaTime));
    }

    public float CurrentTime { get { return _t; } }
}
