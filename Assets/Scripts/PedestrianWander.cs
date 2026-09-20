using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple ambient-crowd behaviour: picks a random reachable point on the NavMesh
/// within range, walks to it, waits a bit, then picks another. Drives the
/// Animator's "Speed" parameter so the walk animation blends correctly.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PedestrianWander : MonoBehaviour
{
    public float wanderRadius = 25f;
    public float minWaitTime = 1.5f;
    public float maxWaitTime = 6f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Vector3 _origin;
    private float _waitTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _origin = transform.position;
        PickNewDestination();
    }

    private void Update()
    {
        if (_animator != null)
        {
            // StarterAssetsThirdPerson's blend tree uses raw speed (0 = Idle, 2 = Walk, 6 = Run),
            // not a normalized 0-1 value, so feed actual velocity magnitude directly.
            _animator.SetFloat(SpeedHash, _agent.velocity.magnitude, 0.15f, Time.deltaTime);
        }

        if (_agent.pathPending) return;

        if (!_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                PickNewDestination();
            }
        }
    }

    private void PickNewDestination()
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = _origin + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _waitTimer = Random.Range(minWaitTime, maxWaitTime);
                return;
            }
        }

        // fall back: try again shortly
        _waitTimer = 0.5f;
    }
}
