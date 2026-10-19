using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

public class Zombie : MonoBehaviour
{
    [Header("Variant")]
    public ZombieType zombieType = ZombieType.Normal;

    [Header("Refs")]
    private S_TaxiPlayer_Test player;

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

    //private static int activeGroans = 0;
    //[SerializeField] private int maxSimultaneousGroans = 20;


    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Movement")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float waitTime = 3f;

    [Header("Animator")]
    [SerializeField] private string velocityParam = "Velocity";
    [SerializeField] private float animDamp = 8f;

    [Header("Bumper Hit Reaction")]
    [SerializeField] private float launchedSeconds = 5.0f;
    [SerializeField] private float navmeshSnapRadius = 3f;
    private bool isRecovering = false;

    private bool isWaiting;
    private bool isLaunched;
    private bool isChasing;
    private bool isPlayerInAntiZombieZone = false;
    private Coroutine waitCoroutine;
    private Coroutine groanCoroutine;

    [SerializeField] float distanceToPlayer;

    public int zombieHealth = 100;
    public bool pleaseApplyForce;

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
        player = FindFirstObjectByType<S_TaxiPlayer_Test>();

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (agent != null)
            agent.updateRotation = false;

        zombieHealth = 100;

        //AI setup only when agent is actually usable
        if (AgentReady())
            SetRandomDestination();

        SetRandomDestination();

        //Start groans (desynced)
        if (groanCoroutine == null)
            groanCoroutine = StartCoroutine(RandomGroans());

        //Randomize the zombies speed for the duration of their existence
        agent.speed = UnityEngine.Random.Range(1,5);

        //Randomize the zombie's voice
        individualPitch = Random.Range(pitchMin, pitchMax);
        audioSource.pitch = individualPitch;
    }

    private void Update()
    {
        //Debug.Log("isPlayerInAntiZombieZOne "+ isPlayerInAntiZombieZone);

        if (agent == null) return;
        if(isLaunched && AgentReady() && !isRecovering)
        {
            StartCoroutine(RecoverAfterLaunch());
        }
        if (isLaunched) return;
        if (!AgentReady()) return;

        UpdateAnimation();

        //distanceToPlayer = (player.transform.position - transform.position).sqrMagnitude;
        //Ignore whatever zombie is doing (unless flying in the sky) and chase that car!
        if ((player.transform.position - transform.position).sqrMagnitude < 1000f)
        {
            //Debug.Log((player.transform.position - transform.position).sqrMagnitude);
            //StopAllCoroutines();
            if (!isPlayerInAntiZombieZone)
                ChaseTaxi();
        }
        else if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) //If we reached our destination, wait then pick a new one
        {
            waitCoroutine = StartCoroutine(StandAndWaitThenMove());
        }

        //Smoothly rotate toward movement direction
        if (agent.desiredVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.desiredVelocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (zombieHealth <= 0)
            Destroy(transform.root.gameObject);
        if (pleaseApplyForce)
            rb.AddForce(Vector3.up * 10, ForceMode.Impulse);
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
    }

    private void SetRandomDestination()
    {
        if (agent == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius)
        );

        Vector3 targetPos = transform.position + randomOffset;

        //Snap to NavMesh so it never picks an unreachable point
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(targetPos);
    }

    private void ChaseTaxi()
    {
        agent.SetDestination(FindFirstObjectByType<S_TaxiPlayer_Test>().transform.position);
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        //Convert real speed to 0�1 for blend tree
        float speed01 = Mathf.InverseLerp(0f, agent.speed, agent.velocity.magnitude);

        //Smooth it (prevents jitter)
        float current = animator.GetFloat(velocityParam);
        float smooth = Mathf.Lerp(current, speed01, animDamp * Time.deltaTime);

        animator.SetFloat(velocityParam, smooth);
    }

    public void OnHitByTaxi(Vector3 taxiForward, float taxiSpeedKmh, float horizontalForce, float verticalForce)
    {
        if (isLaunched) return;

        // Make sure we actually have a rigidbody reference
        if (!rb)
            rb = GetComponent<Rigidbody>();

        if (!rb)
        {
            Debug.LogWarning("Zombie has no Rigidbody, cannot launch.");
            return;
        }

        isLaunched = true;
        PlayRandomHitSound();

        //Stop any AI coroutine so it doesn't keep calling agent stuff
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        isWaiting = false;

        //Disable agent safely
        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();

            agent.enabled = false;
        }

        //Disable animator so physics can take over (simple ragdoll-ish)
        if (animator != null)
            animator.enabled = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        //Launch force scales with speed
        float speed01 = Mathf.InverseLerp(10f, 80f, taxiSpeedKmh);
        float multiplier = Mathf.Lerp(0.8f, 2.0f, speed01);

        Vector3 force =
            taxiForward.normalized * horizontalForce +
            Vector3.up * verticalForce;

        rb.AddForce(force * multiplier, ForceMode.Impulse);

        StartCoroutine(RecoverAfterLaunch());
    }

    private IEnumerator RecoverAfterLaunch()
    {
        isRecovering = true;
        yield return new WaitForSeconds(launchedSeconds);

        //Re-enable animator
        if (animator != null)
            animator.enabled = true;

        //Snap back to navmesh & re-enable agent
        if (agent != null)
        {
            //Find closest navmesh point to where physics left us
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
                //If we couldn't find navmesh nearby, just keep him "launched" (no AI) instead of error-spamming
                agent.enabled = false;
            }
        }

        isLaunched = false;
        isRecovering = false;
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
        if (!player) return;

        float sqrDist = (player.transform.position - transform.position).sqrMagnitude;
        if (sqrDist > groanHearDistance * groanHearDistance) return;

        AudioClip clip = groanSounds[Random.Range(0, groanSounds.Length)];
        PlayOneShotVaried(clip);
    }

    /*
    private IEnumerator PlayGroanLimited(AudioClip clip)
    {
        activeGroans++;
        PlayOneShotVaried(clip);
        yield return new WaitForSeconds(clip.length);
        activeGroans--;
    }
    */

    private void PlayOneShotVaried(AudioClip clip)
    {
        if (!clip) return;
        if (!audioSource) return;

        audioSource.volume = Random.Range(volumeMin, volumeMax);
        audioSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collided with: " + other);

        //Zombie can be on root or child collider
        S_TaxiPlayer_Test taxi = other.GetComponentInParent<S_TaxiPlayer_Test>();
        if (taxi == null) return;

        //Call into player
        taxi.LoseFuel(1f);
    }
    public void ToggleIsPlayerInAntiZombieZone(bool isInZone)
    {
        isPlayerInAntiZombieZone = isInZone;
    }
}
