using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieRagdoll : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Ragdoll Bodies")]
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private Collider[] ragdollColliders;

    [Header("Ragdoll Tuning")]
    public float ragdollTime = 3.0f;
    public float minLaunchSpeed = 3f;

    private bool _ragdolled;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (ragdollBodies == null || ragdollBodies.Length == 0)
            ragdollBodies = GetComponentsInChildren<Rigidbody>(true);

        if (ragdollColliders == null || ragdollColliders.Length == 0)
            ragdollColliders = GetComponentsInChildren<Collider>(true);

        SetRagdoll(false);
    }

    public void HitAndLaunch(Vector3 impulse, Vector3 hitPoint, Vector3 torqueAxis, float torqueStrength)
    {
        if (_ragdolled) return;

        float approxSpeed = impulse.magnitude; //not exact speed, but works as a gate
        if (approxSpeed < minLaunchSpeed) return;

        _ragdolled = true;

        //stop AI/anim
        if (agent) { agent.enabled = false; }
        if (animator) { animator.enabled = false; }

        //enable ragdoll physics
        SetRagdoll(true);

        //Apply impulse to the closest ragdoll body to hit point
        Rigidbody rb = GetClosestBody(hitPoint);
        if (rb != null)
        {
            rb.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
            rb.AddTorque(torqueAxis * torqueStrength, ForceMode.Impulse);
        }
        else
        {
            //fallback: blast all bodies a bit
            foreach (var b in ragdollBodies)
                b.AddForce(impulse / Mathf.Max(1, ragdollBodies.Length), ForceMode.Impulse);
        }

        StartCoroutine(RecoverAfterTime());
    }

    Rigidbody GetClosestBody(Vector3 point)
    {
        Rigidbody best = null;
        float bestDist = float.MaxValue;

        foreach (var b in ragdollBodies)
        {
            if (!b) continue;
            float d = (b.worldCenterOfMass - point).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }

        return best;
    }

    void SetRagdoll(bool enabled)
    {
        foreach (var b in ragdollBodies)
        {
            if (!b) continue;
            if (b.gameObject == gameObject) continue;
            b.isKinematic = !enabled;
            b.detectCollisions = enabled;
        }
    }

    IEnumerator RecoverAfterTime()
    {
        yield return new WaitForSeconds(ragdollTime);

        //Destroy(gameObject); yield break;

        //Recover
        SetRagdoll(false);

        if (animator) animator.enabled = true;
        if (agent) agent.enabled = true;

        _ragdolled = false;
    }
}
