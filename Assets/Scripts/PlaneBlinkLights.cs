using UnityEngine;

/// <summary>Blinking anti-collision beacons (red) and wing-tip / tail strobes (white) for a plane.</summary>
public class PlaneBlinkLights : MonoBehaviour
{
    public Renderer[] beaconRenderers;
    public Light[] beaconLights;
    public Renderer[] strobeRenderers;
    public Light[] strobeLights;
    public float beaconPeriod = 1.1f;
    public float strobePeriod = 1.4f;
    public float phase;

    private void Update()
    {
        float t = Time.time + phase;

        float b = t % beaconPeriod;
        bool beaconOn = b < 0.14f;
        float s = t % strobePeriod;
        bool strobeOn = s < 0.06f || (s > 0.20f && s < 0.26f);

        Set(beaconRenderers, beaconLights, beaconOn);
        Set(strobeRenderers, strobeLights, strobeOn);
    }

    private static void Set(Renderer[] rs, Light[] ls, bool on)
    {
        if (rs != null) foreach (var r in rs) if (r != null && r.enabled != on) r.enabled = on;
        if (ls != null) foreach (var l in ls) if (l != null && l.enabled != on) l.enabled = on;
    }
}
