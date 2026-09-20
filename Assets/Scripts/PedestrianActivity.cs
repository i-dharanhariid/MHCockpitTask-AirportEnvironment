using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ambient pedestrian: walks to a point of interest, stands there for a while (occasionally shuffling),
/// then picks another one. Some trips deliberately cross floors, so the agent rides an escalator
/// (a NavMeshLink) instead of walking over it.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PedestrianActivity : MonoBehaviour
{
    [Header("Behaviour")]
    public float minStandTime = 8f;
    public float maxStandTime = 25f;
    [Range(0f, 1f)] public float otherFloorChance = 0.3f;
    public float shuffleRadius = 2.5f;

    [Tooltip("Walk to one post, then stay there for good.")]
    public bool stationary;

    [Tooltip("Only visit points of interest of these types. Leave empty for the general public.")]
    public string[] allowedTypes;

    [Header("Escalator")]
    public float escalatorRideSeconds = 7f;

    [Header("LOD")]
    public float animatorCullDistance = 70f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private NpcPoi _target;
    private float _standTimer;
    private bool _riding;
    private Transform _cam;
    private float _lodTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.autoTraverseOffMeshLink = false;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            // The locomotion controller's walk state uses MotionSpeed as its speed multiplier (default 0 = frozen).
            _animator.SetFloat(MotionSpeedHash, 1f);
            _animator.SetBool(GroundedHash, true);
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
        if (Camera.main != null) _cam = Camera.main.transform;

        _lodTimer = Random.Range(0f, 0.5f);
        _standTimer = Random.Range(0.5f, 4f);
        ChooseNextTarget(false);
    }

    private void Update()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        UpdateLod();

        if (_animator != null && _animator.enabled)
        {
            float speed = _riding ? 0f : _agent.velocity.magnitude;
            _animator.SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
        }

        if (_agent.isOnOffMeshLink && !_riding)
        {
            StartCoroutine(RideLink());
            return;
        }
        if (_riding || _agent.pathPending) return;

        bool arrived = !_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
        if (!arrived) return;

        if (stationary) return;
        _standTimer -= Time.deltaTime;
        if (_standTimer > 0f) return;

        ChooseNextTarget(Random.value < 0.35f);
    }

    private void UpdateLod()
    {
        _lodTimer -= Time.deltaTime;
        if (_lodTimer > 0f || _animator == null) return;
        _lodTimer = 0.5f;
        if (_cam == null) return;
        bool far = (transform.position - _cam.position).sqrMagnitude > animatorCullDistance * animatorCullDistance;
        if (_animator.enabled == far) _animator.enabled = !far;
    }

    private IEnumerator RideLink()
    {
        _riding = true;
        OffMeshLinkData data = _agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        Vector3 end = data.endPos + Vector3.up * _agent.baseOffset;

        Vector3 flat = end - start; flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(1f, escalatorRideSeconds);
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }
        _agent.CompleteOffMeshLink();
        _riding = false;
        _standTimer = 0.2f;
    }

    private void ChooseNextTarget(bool shuffleOnly)
    {
        if (shuffleOnly && _target != null)
        {
            Vector2 o = Random.insideUnitCircle * shuffleRadius;
            MoveTo(_target.transform.position + new Vector3(o.x, 0f, o.y));
            _standTimer = Random.Range(minStandTime, maxStandTime);
            return;
        }

        int myFloor = transform.position.y > 4f ? 1 : 0;
        bool crossFloor = Random.value < otherFloorChance;
        NpcPoi pick = null;
        int tries = 0;
        var all = NpcPoi.All;
        while (all.Count > 0 && tries++ < 20)
        {
            var p = all[Random.Range(0, all.Count)];
            if (p == _target) continue;
            if (!TypeAllowed(p)) continue;
            if ((p.Floor != myFloor) != crossFloor) continue;
            if (new Vector2(p.transform.position.x - transform.position.x, p.transform.position.z - transform.position.z).magnitude > 90f) continue;
            pick = p;
            break;
        }
        if (pick == null) { _standTimer = Random.Range(3f, 8f); return; }

        _target = pick;
        Vector2 off = Random.insideUnitCircle * pick.radius;
        MoveTo(pick.transform.position + new Vector3(off.x, 0f, off.y));
        _standTimer = Random.Range(minStandTime, maxStandTime);
    }

    private bool TypeAllowed(NpcPoi p)
    {
        if (allowedTypes != null && allowedTypes.Length > 0)
        {
            foreach (var t in allowedTypes) if (t == p.poiType) return true;
            return false;
        }
        return !p.poiType.StartsWith("Police") && !p.poiType.StartsWith("Pilot");
    }

    /// <summary>Send this pedestrian to a specific spot and keep them there for a while (used by the trailer director).</summary>
    public void ForceGoTo(Vector3 world, float standSeconds)
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        _target = null;
        MoveTo(world);
        _standTimer = standSeconds;
    }

    private void MoveTo(Vector3 world)
    {
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
