using UnityEngine;

/// <summary>Flashing amber vehicle siren: a small emissive lens plus a point light that pulse together.</summary>
public class SirenLight : MonoBehaviour
{
    public Renderer lens;
    public Light glow;
    public float frequency = 1.6f;      // flashes per second
    public float maxIntensity = 160f;
    public float phase;

    private void Update()
    {
        float t = Mathf.Repeat(Time.time * frequency + phase, 1f);
        // sharp flash with a short fade: bright for ~45% of the cycle
        float k = t < 0.45f ? Mathf.Sin(t / 0.45f * Mathf.PI) : 0f;
        k = Mathf.Clamp01(k * 1.3f);
        if (glow != null) glow.intensity = maxIntensity * k;
        if (lens != null) lens.enabled = k > 0.15f;
    }
}
