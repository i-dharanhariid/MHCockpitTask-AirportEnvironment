using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Camera-side level of detail. Keeps the frame rate up by:
///  - per-layer cull distances for small props and pedestrians,
///  - switching off point/spot lights that are far from the camera,
///  - adapting all of these (plus lodBias and shadow distance) to the measured frame time.
/// Attach to the main camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraLODController : MonoBehaviour
{
    [Header("Target")]
    public float targetFrameMs = 16.7f;

    [Header("Layer cull distances (metres, at full quality)")]
    public string[] cullLayers = { "AirportComponents", "Pedestrians" };
    public float[] cullDistances = { 90f, 80f };

    [Header("Lights")]
    public float lightExtraDistance = 45f;
    public float lightCheckInterval = 0.25f;

    [Header("Adaptive quality")]
    [Range(0.4f, 1f)] public float minScale = 0.5f;
    public float shadowDistanceAtFull = 50f;
    public float lodBiasAtFull = 1.0f;

    private Camera _cam;
    private readonly List<Light> _lights = new List<Light>();
    private float _scale = 1f;
    private float _smoothedMs = 16f;
    private float _lightTimer;
    private float _adaptTimer;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        _cam.layerCullSpherical = true;
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l.type != LightType.Directional) _lights.Add(l);
        ApplyScale();
    }

    private void Update()
    {
        _smoothedMs = Mathf.Lerp(_smoothedMs, Time.unscaledDeltaTime * 1000f, 0.05f);

        _adaptTimer -= Time.unscaledDeltaTime;
        if (_adaptTimer <= 0f)
        {
            _adaptTimer = 0.5f;
            if (_smoothedMs > targetFrameMs * 1.25f) _scale = Mathf.Max(minScale, _scale - 0.05f);
            else if (_smoothedMs < targetFrameMs * 0.85f) _scale = Mathf.Min(1f, _scale + 0.02f);
            ApplyScale();
        }

        _lightTimer -= Time.unscaledDeltaTime;
        if (_lightTimer <= 0f)
        {
            _lightTimer = lightCheckInterval;
            CullLights();
        }
    }

    private void ApplyScale()
    {
        var distances = new float[32];
        for (int i = 0; i < cullLayers.Length && i < cullDistances.Length; i++)
        {
            int layer = LayerMask.NameToLayer(cullLayers[i]);
            if (layer >= 0) distances[layer] = cullDistances[i] * _scale;
        }
        _cam.layerCullDistances = distances;

        QualitySettings.lodBias = lodBiasAtFull * Mathf.Lerp(0.6f, 1f, (_scale - minScale) / (1f - minScale));
        var urp = UniversalRenderPipeline.asset;
        if (urp != null) urp.shadowDistance = shadowDistanceAtFull * _scale;
    }

    private void CullLights()
    {
        Vector3 p = transform.position;
        float extra = lightExtraDistance * _scale;
        for (int i = 0; i < _lights.Count; i++)
        {
            var l = _lights[i];
            if (l == null) continue;
            float limit = l.range + extra;
            bool near = (l.transform.position - p).sqrMagnitude < limit * limit;
            if (l.enabled != near) l.enabled = near;
        }
    }
}
