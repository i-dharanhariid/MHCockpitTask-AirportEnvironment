using UnityEngine;

/// <summary>
/// Drives a baggage tractor and its carts along a fixed route. Every unit follows exactly the path
/// the leader drives, so the train bends around corners like a real one.
/// </summary>
public class BaggageTrain : MonoBehaviour
{
    [Tooltip("Index 0 is the tractor (leader), the rest follow in order.")]
    public Transform[] units;
    public float[] yawOffsets;
    [Tooltip("Distance of each unit behind the leader at the start.")]
    public float[] unitBehind;
    [Tooltip("Full path: the train's own initial line (tail to leader) followed by the route.")]
    public Vector3[] trail;
    public float leaderStartArc;

    [Tooltip("Fixed rotation applied to each unit's own axes (the FBX pivots are lying on their side).")]
    public Vector3 localTilt = new Vector3(-90f, 0f, 0f);

    public float speed = 4f;
    public float acceleration = 1.2f;
    public float brakeDistance = 12f;
    public float startDelay = 3f;

    private float[] _cum;
    private float _s;
    private float _v;
    private float[] _y;

    private void Start()
    {
        if (trail == null || trail.Length < 2 || units == null) { enabled = false; return; }
        _cum = new float[trail.Length];
        for (int i = 1; i < trail.Length; i++) _cum[i] = _cum[i - 1] + Vector3.Distance(Flat(trail[i]), Flat(trail[i - 1]));
        _s = leaderStartArc;
        _y = new float[units.Length];
        for (int i = 0; i < units.Length; i++) _y[i] = units[i].position.y;
        Place();
    }

    private void Update()
    {
        if (startDelay > 0f) { startDelay -= Time.deltaTime; return; }
        float end = _cum[_cum.Length - 1];
        float remaining = end - _s;
        float target = remaining <= 0.05f ? 0f : speed * Mathf.Clamp01(remaining / brakeDistance);
        _v = Mathf.MoveTowards(_v, Mathf.Max(target, remaining > 0.5f ? 0.4f : 0f), acceleration * Time.deltaTime);
        _s = Mathf.Min(end, _s + _v * Time.deltaTime);
        Place();
    }

    private static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    private Vector3 Sample(float s)
    {
        s = Mathf.Clamp(s, 0f, _cum[_cum.Length - 1]);
        int lo = 0, hi = _cum.Length - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (_cum[mid] <= s) lo = mid; else hi = mid;
        }
        float seg = _cum[hi] - _cum[lo];
        float t = seg > 1e-4f ? (s - _cum[lo]) / seg : 0f;
        return Vector3.Lerp(trail[lo], trail[hi], t);
    }

    private void Place()
    {
        for (int i = 0; i < units.Length; i++)
        {
            float s = _s - unitBehind[i];
            Vector3 p = Sample(s);
            Vector3 fwd = Sample(s + 0.6f) - Sample(s - 0.6f);
            fwd.y = 0f;
            units[i].position = new Vector3(p.x, _y[i], p.z);
            if (fwd.sqrMagnitude > 1e-4f)
            {
                float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg + yawOffsets[i];
                units[i].rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(localTilt);
            }
        }
    }
}
