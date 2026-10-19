using UnityEngine;

namespace ZombieTaxi.Core
{
    /// <summary>
    /// Decides when a chasing agent should ask for a new path.
    ///
    /// Every path request is a full solve, so requesting one per frame per agent
    /// is the dominant cost of a chasing horde. Two gates reduce that: a cooldown
    /// that scales with the agent's detail level, and a minimum distance the
    /// target must have moved for the existing path to count as stale.
    ///
    /// Both gates have a failure mode in the same direction. Gate too hard and
    /// the agent steers at where the target used to be, falling further behind
    /// the faster the target moves, until it can never make contact. The policy
    /// lives here, separate from the MonoBehaviour, so that behaviour can be
    /// tested against a simulated moving target instead of only in play.
    /// </summary>
    public struct ChaseRepathPolicy
    {
        private Vector3 lastPathTarget;
        private float nextAllowedTime;
        private bool hasPath;

        /// <summary>The position the current path was solved against.</summary>
        public Vector3 LastPathTarget => lastPathTarget;

        /// <summary>False until the first path has been requested.</summary>
        public bool HasPath => hasPath;

        /// <summary>Forgets the current path so the next call repaths immediately.</summary>
        public void Reset()
        {
            hasPath = false;
            nextAllowedTime = 0f;
            lastPathTarget = Vector3.zero;
        }

        /// <summary>
        /// Returns true when the caller should request a path to
        /// <paramref name="targetPosition"/>, and records that it did.
        /// </summary>
        /// <param name="targetPosition">Where the target is right now.</param>
        /// <param name="now">Current time in seconds.</param>
        /// <param name="level">Detail level, which scales the cooldown.</param>
        /// <param name="baseInterval">Cooldown for the slowest band to scale from.</param>
        /// <param name="moveThreshold">How far the target must move to invalidate the path.</param>
        public bool ShouldRepath(
            Vector3 targetPosition,
            float now,
            AiDetailLevel level,
            float baseInterval,
            float moveThreshold)
        {
            // The first path is never gated; the agent has nowhere to walk yet.
            if (!hasPath)
            {
                Commit(targetPosition, now, level, baseInterval);
                return true;
            }

            if (now < nextAllowedTime) return false;

            float dx = targetPosition.x - lastPathTarget.x;
            float dy = targetPosition.y - lastPathTarget.y;
            float dz = targetPosition.z - lastPathTarget.z;

            if (dx * dx + dy * dy + dz * dz < moveThreshold * moveThreshold)
            {
                // Target has barely moved, so the current path still leads to it.
                // Push the next check out rather than retesting every tick.
                nextAllowedTime = now + AiDetailBands.RepathInterval(level, baseInterval);
                return false;
            }

            Commit(targetPosition, now, level, baseInterval);
            return true;
        }

        private void Commit(Vector3 targetPosition, float now, AiDetailLevel level, float baseInterval)
        {
            lastPathTarget = targetPosition;
            nextAllowedTime = now + AiDetailBands.RepathInterval(level, baseInterval);
            hasPath = true;
        }

        /// <summary>
        /// How far the agent's aim point can drift behind a target moving at
        /// <paramref name="targetSpeed"/>, given how often the agent is serviced.
        /// Used by tests and tuning to check a band can actually keep contact.
        /// </summary>
        public static float WorstCaseTrackingError(
            AiDetailLevel level,
            float baseInterval,
            float moveThreshold,
            float targetSpeed,
            float serviceInterval)
        {
            // The agent cannot react faster than it is serviced, and on top of
            // that it waits out whichever gate is longer.
            float gate = Mathf.Max(AiDetailBands.RepathInterval(level, baseInterval), serviceInterval);
            return targetSpeed * gate + moveThreshold;
        }
    }
}
