using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ZombieTaxi.Core;

/// <summary>
/// A single zombie.
///
/// AI decisions are driven by <see cref="ZombieManager"/> through
/// <see cref="ManagedTick"/> rather than by this component's own Update, so the
/// cost of the horde is bounded by the manager's per-frame budget instead of
/// scaling with the number of zombies alive. Update is reserved for cheap
/// per-frame presentation work, and that is skipped once a zombie is far enough
/// away to be imperceptible.
/// </summary>
public class Zombie : MonoBehaviour
{
    [Header("Variant")]
    public ZombieType zombieType = ZombieType.Normal;

    [Header("Components")]
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody rb;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] groanSounds;

    [SerializeField] private float groanHearDistance = 25f;

    [SerializeField] private float groanIntervalMin = 6f;
    [SerializeField] private float groanIntervalMax = 14f;
    [SerializeField] private float groanStartOffsetMax = 8f;
    [SerializeField] private float pitchMin = 0.75f;
    [SerializeField] private float pitchMax = 1.35f;
    [SerializeField] private float volumeMin = 0.6f;
    [SerializeField] private float volumeMax = 0.7f;
    private float individualPitch = 1f;
    [SerializeField][Range(0f, 1f)] private float groanChance = 0.75f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Movement")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float waitTime = 3f;
    [Tooltip("Distance at which this zombie abandons wandering and chases the taxi.")]
    [SerializeField] private float chaseRadius = 31.6f;
    [Tooltip("Randomized walk speed range, rolled once per zombie so the horde does not move as one block.")]
    [SerializeField] private float speedMin = 1f;
    [SerializeField] private float speedMax = 5f;

    [Header("Animator")]
    [SerializeField] private string velocityParam = "Velocity";
    [SerializeField] private float animDamp = 8f;

    [Header("Bumper Hit Reaction")]
    [SerializeField] private float launchedSeconds = 5.0f;
    [SerializeField] private float navmeshSnapRadius = 3f;
    private bool isRecovering = false;

    private bool isWaiting;
    private bool isLaunched;
    private Coroutine waitCoroutine;
    private Coroutine groanCoroutine;

    public int zombieHealth = 100;
    public bool pleaseApplyForce;

    // --- Pathing throttle state -------------------------------------------------
    // Squared so the per-tick distance check needs no square root.
    private float sqrChaseRadius;
    // Decides when a chase path has gone stale enough to be worth re-solving.
    private ChaseRepathPolicy repathPolicy;
    private bool isChasing;

    // Detail level from the most recent managed tick. Update reads it to decide
    // whether this zombie is close enough to be worth animating.
    private AiDetailLevel detailLevel = AiDetailLevel.Near;

    public AiDetailLevel DetailLevel => detailLevel;
    public bool IsAlive => zombieHealth > 0;

    public enum ZombieType
    {
        Normal,
        Exploder,
        Brute,
        Clinger,
        Gas,
        Shooter
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (agent != null)
            agent.updateRotation = false;

        zombieHealth = 100;
        sqrChaseRadius = chaseRadius * chaseRadius;

        // Guarded: setting a destination before the agent is on the NavMesh throws.
        if (AgentReady())
            SetRandomDestination();

        if (groanCoroutine == null)
            groanCoroutine = StartCoroutine(RandomGroans());

        // Randomize walk speed and voice so the horde is not uniform.
        if (agent != null)
            agent.speed = Random.Range(speedMin, speedMax);

        individualPitch = Random.Range(pitchMin, pitchMax);
        if (audioSource) audioSource.pitch = individualPitch;

        ZombieManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (ZombieManager.Instance != null)
            ZombieManager.Instance.Unregister(this);
    }

    /// <summary>
    /// AI decision step, called by <see cref="ZombieManager"/> on its own budget
    /// rather than every frame. All the expensive work lives here: distance
    /// classification, chase/wander choice and NavMesh path requests.
    /// </summary>
    public void ManagedTick(Transform player)
    {
        if (agent == null || isLaunched || !AgentReady()) return;

        ZombieManager manager = ZombieManager.Instance;

        if (player == null)
        {
            detailLevel = AiDetailLevel.Far;
            return;
        }

        // Squared comparison throughout, so no square root per zombie per tick.
        Vector3 toPlayer = player.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;

        detailLevel = manager.DetailBands.Classify(sqrDistance);

        // Far zombies keep whatever destination they had and cost nothing further.
        if (detailLevel == AiDetailLevel.Far)
        {
            isChasing = false;
            return;
        }

        bool shouldChase = sqrDistance < sqrChaseRadius && !manager.PlayerInAntiZombieZone;

        if (shouldChase)
        {
            ChaseTaxi(player.position, manager);
            return;
        }

        if (isChasing)
        {
            // Just lost the player: fall back to wandering from where we are.
            isChasing = false;
            SetRandomDestination();
            return;
        }

        // Reached the wander destination, so stand still briefly then pick a new one.
        if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            waitCoroutine = StartCoroutine(StandAndWaitThenMove());
    }

    private void Update()
    {
        if (agent == null) return;

        if (isLaunched)
        {
            if (AgentReady() && !isRecovering)
                StartCoroutine(RecoverAfterLaunch());

            return;
        }

        if (zombieHealth <= 0)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        if (pleaseApplyForce && rb != null)
            rb.AddForce(Vector3.up * 10, ForceMode.Impulse);

        // Presentation only. A zombie the player cannot make out does not need
        // its blend tree or facing updated every frame.
        if (detailLevel == AiDetailLevel.Far || !AgentReady()) return;

        UpdateAnimation();

        if (agent.desiredVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.desiredVelocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private IEnumerator StandAndWaitThenMove()
    {
        isWaiting = true;

        if (AgentReady())
            agent.isStopped = true;

        yield return new WaitForSeconds(waitTime);

        if (isLaunched || !AgentReady())
        {
            isWaiting = false;
            waitCoroutine = null;
            yield break;
        }

        agent.isStopped = false;
        SetRandomDestination();
        isWaiting = false;
        waitCoroutine = null;
    }

    private void SetRandomDestination()
    {
        if (!AgentReady()) return;

        Vector3 randomOffset = new(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius)
        );

        Vector3 targetPos = transform.position + randomOffset;

        // Snap to NavMesh so it never picks an unreachable point
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(targetPos);
    }

    /// <summary>
    /// Chases the taxi, asking the NavMesh for a new path only when the current
    /// one is actually stale.
    ///
    /// Every SetDestination call queues a full path solve, so two gates gate it:
    /// a cooldown that scales with the detail band, and a check that the target
    /// has moved at least the manager's threshold. Close zombies have no
    /// cooldown — the update budget already limits how often they are serviced,
    /// and adding a second delay makes them steer at a stale point and trail a
    /// moving taxi.
    /// </summary>
    private void ChaseTaxi(Vector3 playerPosition, ZombieManager manager)
    {
        if (isWaiting && waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            isWaiting = false;
            if (AgentReady()) agent.isStopped = false;
        }

        if (!isChasing)
        {
            // Entering a chase: drop any wander path so the first decision repaths.
            repathPolicy.Reset();
            isChasing = true;
        }

        bool repath = repathPolicy.ShouldRepath(
            playerPosition,
            Time.time,
            detailLevel,
            manager.BaseRepathInterval,
            manager.RepathMoveThreshold);

        if (repath)
            agent.SetDestination(playerPosition);
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        // Convert real speed to 0-1 for blend tree
        float speed01 = Mathf.InverseLerp(0f, agent.speed, agent.velocity.magnitude);

        // Smooth it (prevents jitter)
        float current = animator.GetFloat(velocityParam);
        float smooth = Mathf.Lerp(current, speed01, animDamp * Time.deltaTime);

        animator.SetFloat(velocityParam, smooth);
    }

    public void OnHitByTaxi(Vector3 taxiForward, float taxiSpeedKmh, float horizontalForce, float verticalForce)
    {
        if (isLaunched) return;

        if (!rb)
            rb = GetComponent<Rigidbody>();

        if (!rb)
        {
            Debug.LogWarning("Zombie has no Rigidbody, cannot launch.");
            return;
        }

        isLaunched = true;
        isChasing = false;
        PlayRandomHitSound();

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        isWaiting = false;

        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();

            agent.enabled = false;
        }

        // Disable animator so physics can take over (simple ragdoll-ish)
        if (animator != null)
            animator.enabled = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        // Launch force scales with speed
        float speed01 = Mathf.InverseLerp(10f, 80f, taxiSpeedKmh);
        float multiplier = Mathf.Lerp(0.8f, 2.0f, speed01);

        Vector3 force =
            taxiForward.normalized * horizontalForce +
            Vector3.up * verticalForce;

        rb.AddForce(force * multiplier, ForceMode.Impulse);

        // Guarded so Update cannot start a second recovery coroutine alongside this one.
        if (!isRecovering)
            StartCoroutine(RecoverAfterLaunch());
    }

    private IEnumerator RecoverAfterLaunch()
    {
        isRecovering = true;
        yield return new WaitForSeconds(launchedSeconds);

        if (animator != null)
            animator.enabled = true;

        // Snap back to navmesh & re-enable agent
        if (agent != null)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                navmeshSnapRadius,
                NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.isStopped = false;
                SetRandomDestination();
            }
            else
            {
                // No navmesh nearby, so stay "launched" rather than spamming errors.
                agent.enabled = false;
            }
        }

        isLaunched = false;
        isRecovering = false;

        // The agent was warped, so any chase path it held is meaningless.
        isChasing = false;
        repathPolicy.Reset();
    }

    private IEnumerator RandomGroans()
    {
        if (groanStartOffsetMax > 0f)
            yield return new WaitForSeconds(Random.Range(0f, groanStartOffsetMax));

        while (true)
        {
            float wait = Random.Range(groanIntervalMin, groanIntervalMax);
            yield return new WaitForSeconds(wait);

            if (isLaunched) continue;
            if (Random.value > groanChance) continue;

            PlayRandomGroan();
        }
    }

    private void PlayRandomHitSound()
    {
        if (!audioSource) return;
        if (hitSounds == null || hitSounds.Length == 0) return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        PlayOneShotVaried(clip);
    }

    private void PlayRandomGroan()
    {
        if (!audioSource) return;
        if (groanSounds == null || groanSounds.Length == 0) return;

        // Groans are only audible near the player, so skip the clip entirely otherwise.
        Transform player = ZombieManager.Instance.PlayerTransform;
        if (player == null) return;

        float sqrDist = (player.position - transform.position).sqrMagnitude;
        if (sqrDist > groanHearDistance * groanHearDistance) return;

        AudioClip clip = groanSounds[Random.Range(0, groanSounds.Length)];
        PlayOneShotVaried(clip);
    }

    private void PlayOneShotVaried(AudioClip clip)
    {
        if (!clip) return;
        if (!audioSource) return;

        audioSource.volume = Random.Range(volumeMin, volumeMax);
        audioSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Zombie can be on root or child collider
        S_TaxiPlayer_Test taxi = other.GetComponentInParent<S_TaxiPlayer_Test>();
        if (taxi == null) return;

        taxi.LoseFuel(1f);
    }

    /// <summary>
    /// Forwards to the manager, where the flag is stored for the whole horde.
    /// Exists so callers holding a Zombie reference can still toggle the zone
    /// without knowing about the manager.
    /// </summary>
    public void ToggleIsPlayerInAntiZombieZone(bool isInZone)
    {
        ZombieManager.Instance.SetPlayerInAntiZombieZone(isInZone);
    }
}
