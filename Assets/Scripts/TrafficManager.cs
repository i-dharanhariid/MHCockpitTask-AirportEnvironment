using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One-way traffic along the -Z axis in front of the terminal. Cars spawn before the entry toll plaza,
/// pause briefly at a toll booth, drive down the road (some pull into the curb-side bay at a random point
/// for a short stop) and leave through the exit toll plaza.
/// </summary>
public class TrafficManager : MonoBehaviour
{
    [Header("Cars")]
    public GameObject[] carPrefabs;
    public int carCount = 16;
    [Tooltip("Rotate prefab if its model does not face +Z.")]
    public float prefabYawOffset = 0f;

    [Header("Road")]
    public float[] throughLanes = { -95.6f, -77f, -56f, -46.5f };
    public float throughLaneX = -46.5f;
    public float bayLaneX = -37f;
    public float roadY = 1.01f;
    public float startZ = 450f;
    public float endZ = -470f;
    public float laneChangeLength = 24f;

    [Header("Toll plazas")]
    public float entryTollZ = 352f;
    public float exitTollZ = -355f;
    public float[] entryLanes = { -92.2f, -76.8f, -61.7f, -46.9f, -31.8f, -16.9f };
    public float[] exitLanes = { -92.2f, -77.5f, -62.4f, -47.4f, -32.3f, -17f };
    public float tollBlendLength = 50f;
    public Vector2 tollPauseSeconds = new Vector2(1f, 1.8f);

    [Header("Roadside stops")]
    [Tooltip("Random stops happen between these Z values.")]
    public float stopRangeMaxZ = 290f;
    public float stopRangeMinZ = -290f;

    [Header("Driving")]
    public float cruiseSpeed = 9f;
    public float acceleration = 3.5f;
    public float deceleration = 5f;
    [Range(0f, 1f)] public float stopChance = 0.4f;
    public Vector2 stopSeconds = new Vector2(2f, 3f);
    public Vector2 respawnSeconds = new Vector2(1f, 4f);

    private class Car
    {
        public Transform t;
        public List<Transform> tires = new List<Transform>();
        public float z, v, cruise, length;
        public float entryX, exitX, laneA, laneB, changeZ;
        public float bayZ;
        public bool hasBay;
        public bool entryDone, bayDone, exitDone;
        public float pause;
        public bool pausing;
        public float respawn;
        public bool active;
    }

    private readonly List<Car> _cars = new List<Car>();

    private void Start()
    {
        for (int i = 0; i < carCount; i++)
        {
            var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            var go = Instantiate(prefab, transform);
            go.name = "TrafficCar_" + i;
            var c = new Car { t = go.transform };
            foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (var tr in go.GetComponentsInChildren<Transform>())
                if (tr != go.transform && tr.name.ToLower().Contains("tire")) c.tires.Add(tr);
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                c.length = Mathf.Max(b.size.x, b.size.z);
            }
            else c.length = 5f;
            c.respawn = i * 3f + Random.Range(0f, 2f);
            go.SetActive(false);
            _cars.Add(c);
        }

        // populate the whole road from the start so it is not empty at the beginning
        float span = (entryTollZ + 60f) - (exitTollZ + 40f);
        int prefill = Mathf.Min(_cars.Count, Mathf.RoundToInt(_cars.Count * 0.8f));
        for (int i = 0; i < prefill; i++)
        {
            float z0 = (entryTollZ + 60f) - span * (i + Random.Range(0.2f, 0.8f)) / prefill;
            Launch(_cars[i], z0);
        }
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        foreach (var c in _cars)
        {
            if (!c.active)
            {
                c.respawn -= dt;
                if (c.respawn <= 0f) TrySpawn(c);
                continue;
            }
            Step(c, dt);
        }
    }

    private void TrySpawn(Car c)
    {
        foreach (var o in _cars)
            if (o != c && o.active && o.z > startZ - 25f) { c.respawn = 1f; return; }
        Launch(c, startZ);
    }

    private void Launch(Car c, float z0)
    {
        c.z = z0;
        c.cruise = cruiseSpeed * Random.Range(0.85f, 1.1f);
        c.v = c.cruise * 0.7f;
        c.entryX = entryLanes[Random.Range(0, entryLanes.Length)];
        c.exitX = exitLanes[Random.Range(0, exitLanes.Length)];
        c.laneA = throughLanes[Random.Range(0, throughLanes.Length)];
        c.laneB = throughLanes[Random.Range(0, throughLanes.Length)];
        c.changeZ = Random.Range(-250f, 250f);
        c.entryDone = c.exitDone = false;
        c.pausing = false;
        c.pause = 0f;
        c.hasBay = Random.value < stopChance;
        c.bayDone = !c.hasBay;
        c.bayZ = Random.Range(stopRangeMinZ, stopRangeMaxZ);
        if (z0 < entryTollZ - 1f) c.entryDone = true;
        if (c.hasBay && c.bayZ > z0 - 12f) c.bayDone = true;   // already past its stop
        c.active = true;
        c.t.gameObject.SetActive(true);
        Apply(c, 0f);
    }

    private static float Smooth(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

    private float LaneX(Car c, float z)
    {
        float x = Mathf.Lerp(c.laneB, c.laneA, Smooth((z - c.changeZ) / 45f + 0.5f));
        x = Mathf.Lerp(x, c.entryX, Smooth((z - (entryTollZ - tollBlendLength - 10f)) / tollBlendLength));
        x = Mathf.Lerp(x, c.exitX, Smooth((exitTollZ + tollBlendLength + 10f - z) / tollBlendLength));

        if (c.hasBay)
        {
            float zE = c.bayZ;
            float u;
            if (z > zE + 8f) u = Mathf.Clamp01((zE + 8f + laneChangeLength - z) / laneChangeLength);
            else if (z >= zE - 8f) u = 1f;
            else u = 1f - Mathf.Clamp01((zE - 8f - z) / laneChangeLength);
            x = Mathf.Lerp(x, bayLaneX, Smooth(u));
        }
        return x;
    }

    private bool NextStop(Car c, out float stopZ, out int kind)
    {
        if (!c.entryDone) { stopZ = entryTollZ; kind = 0; return true; }
        if (!c.bayDone) { stopZ = c.bayZ; kind = 1; return true; }
        if (!c.exitDone) { stopZ = exitTollZ; kind = 2; return true; }
        stopZ = 0f; kind = -1; return false;
    }

    private void FinishStop(Car c, int kind)
    {
        if (kind == 0) c.entryDone = true; else if (kind == 1) c.bayDone = true; else c.exitDone = true;
    }

    private void Step(Car c, float dt)
    {
        float desired = c.cruise;

        float sz; int kind;
        if (NextStop(c, out sz, out kind))
        {
            float dz = c.z - sz;
            if (dz < -0.5f)
            {
                FinishStop(c, kind);
            }
            else if (dz <= 0.2f && c.v < 0.5f)
            {
                c.v = 0f;
                if (!c.pausing)
                {
                    c.pausing = true;
                    c.pause = kind == 1 ? Random.Range(stopSeconds.x, stopSeconds.y) : Random.Range(tollPauseSeconds.x, tollPauseSeconds.y);
                }
                c.pause -= dt;
                desired = 0f;
                if (c.pause <= 0f) { c.pausing = false; FinishStop(c, kind); desired = c.cruise; }
            }
            else
            {
                desired = Mathf.Min(desired, Mathf.Sqrt(2f * deceleration * Mathf.Max(0f, dz)) + 0.6f);
            }
        }

        // keep a gap to the car ahead in the same lane
        float myX = LaneX(c, c.z);
        foreach (var o in _cars)
        {
            if (o == c || !o.active) continue;
            if (o.z >= c.z) continue;
            if (Mathf.Abs(LaneX(o, o.z) - myX) > 2.4f) continue;
            float gap = c.z - o.z - (c.length + o.length) * 0.5f;
            if (gap > 40f) continue;
            desired = Mathf.Min(desired, Mathf.Sqrt(2f * deceleration * Mathf.Max(0f, gap - 2.5f)));
        }

        float rate = desired > c.v ? acceleration : deceleration;
        c.v = Mathf.MoveTowards(c.v, desired, rate * dt);
        c.z -= c.v * dt;

        Apply(c, c.v * dt);

        if (c.z <= endZ)
        {
            c.active = false;
            c.t.gameObject.SetActive(false);
            c.respawn = Random.Range(respawnSeconds.x, respawnSeconds.y);
        }
    }

    private void Apply(Car c, float moved)
    {
        float x = LaneX(c, c.z);
        float xa = LaneX(c, c.z - 0.75f), xb = LaneX(c, c.z + 0.75f);
        Vector3 fwd = new Vector3(xa - xb, 0f, -1.5f).normalized;
        c.t.position = new Vector3(x, roadY, c.z);
        c.t.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, prefabYawOffset, 0f);
        if (moved > 0f)
        {
            float deg = moved / 0.35f * Mathf.Rad2Deg;
            foreach (var tire in c.tires) tire.Rotate(Vector3.right, deg, Space.Self);
        }
    }
}
