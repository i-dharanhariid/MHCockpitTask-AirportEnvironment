using UnityEngine;

/// <summary>
/// Flashes a chain of approach lights one after another toward the runway
/// (the "rabbit" sequence pilots see on an approach lighting system).
/// </summary>
public class RunwayLightSequencer : MonoBehaviour
{
    [Tooltip("Stations ordered from the outermost light toward the runway threshold.")]
    public Renderer[] stations;
    public Color flashColor = new Color(1f, 0.95f, 0.85f) * 14f;
    public Color idleColor = new Color(1f, 0.95f, 0.85f) * 0.6f;
    [Tooltip("Seconds for the flash to travel the whole chain.")]
    public float sweepDuration = 0.5f;
    [Tooltip("Seconds between sweeps.")]
    public float cycleDuration = 1f;

    private MaterialPropertyBlock _block;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (stations == null || stations.Length == 0) return;

        float t = Time.time % cycleDuration;
        float step = sweepDuration / stations.Length;
        int active = t < sweepDuration ? Mathf.FloorToInt(t / step) : -1;

        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] == null) continue;
            stations[i].GetPropertyBlock(_block);
            _block.SetColor(EmissionId, i == active ? flashColor : idleColor);
            stations[i].SetPropertyBlock(_block);
        }
    }
}
