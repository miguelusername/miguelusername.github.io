using UnityEngine;

/// <summary>
/// Aims a mounted gun at the closest living zombie.
///
/// Targets come from <see cref="ZombieManager"/>'s spatial index, which only
/// inspects the grid cells the search radius overlaps rather than testing every
/// zombie. Re-acquisition runs on an interval, and the current target is only
/// revalidated per frame, which is a squared-distance comparison.
/// </summary>
public class GunZombieTracking : MonoBehaviour
{
    [SerializeField] private float maximumDistanceCheck = 1500f;
    [Tooltip("Seconds between attempts to acquire a new target.")]
    [SerializeField] private float retargetInterval = 0.15f;

    [SerializeField] private Zombie trackingTarget;

    private Transform playerTransform;
    private float nextRetargetTime;

    private void Update()
    {
        ZombieManager manager = ZombieManager.Instance;

        // Cached so facing the taxi does not cost a scene search per frame.
        if (playerTransform == null) playerTransform = manager.PlayerTransform;

        if (!IsTargetValid(manager))
        {
            trackingTarget = null;

            if (Time.time >= nextRetargetTime)
            {
                nextRetargetTime = Time.time + retargetInterval;

                if (manager.TryFindNearestZombie(transform.position, maximumDistanceCheck, out Zombie nearest)
                    && nearest != null && nearest.IsAlive)
                {
                    trackingTarget = nearest;
                }
            }
        }

        if (trackingTarget != null)
        {
            Vector3 aimPoint = trackingTarget.transform.position + Vector3.up;
            Debug.DrawLine(transform.position, aimPoint, Color.red);
            transform.parent.LookAt(aimPoint);
            return;
        }

        // Nothing to shoot, so fall back to facing the same way as the taxi.
        if (playerTransform != null)
            transform.parent.rotation = playerTransform.rotation;
    }

    /// <summary>Squared comparison so the per-frame validity check needs no square root.</summary>
    private bool IsTargetValid(ZombieManager manager)
    {
        if (trackingTarget == null || !trackingTarget.IsAlive) return false;

        float sqrRange = maximumDistanceCheck * maximumDistanceCheck;
        return (trackingTarget.transform.position - transform.position).sqrMagnitude <= sqrRange;
    }
}
