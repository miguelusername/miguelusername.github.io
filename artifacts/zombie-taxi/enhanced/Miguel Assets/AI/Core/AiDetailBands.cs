namespace ZombieTaxi.Core
{
    /// <summary>How much work an agent deserves this tick, based on how far away it is.</summary>
    public enum AiDetailLevel
    {
        /// <summary>Close to the player: chase, repath, animate.</summary>
        Near = 0,

        /// <summary>Visible but not a threat: wander and animate, repath rarely.</summary>
        Mid = 1,

        /// <summary>Too far to matter: skip the expensive work entirely.</summary>
        Far = 2
    }

    /// <summary>
    /// Level-of-detail selector for AI work.
    ///
    /// Classifying agents by distance lets expensive work scale with what the
    /// player can actually perceive, rather than every agent costing the same
    /// whether it is under the bumper or on the far side of the map.
    ///
    /// Thresholds are stored squared so callers can compare against squared
    /// distances and avoid a square root per agent per tick.
    /// </summary>
    public readonly struct AiDetailBands
    {
        private readonly float nearSqrDistance;
        private readonly float midSqrDistance;

        public AiDetailBands(float nearDistance, float midDistance)
        {
            // Guard against inverted thresholds so Mid can never be unreachable.
            if (midDistance < nearDistance) midDistance = nearDistance;

            nearSqrDistance = nearDistance * nearDistance;
            midSqrDistance = midDistance * midDistance;
        }

        public float NearSqrDistance => nearSqrDistance;
        public float MidSqrDistance => midSqrDistance;

        /// <param name="sqrDistanceToPlayer">Squared distance, so no square root is needed.</param>
        public AiDetailLevel Classify(float sqrDistanceToPlayer)
        {
            if (sqrDistanceToPlayer <= nearSqrDistance) return AiDetailLevel.Near;
            if (sqrDistanceToPlayer <= midSqrDistance) return AiDetailLevel.Mid;
            return AiDetailLevel.Far;
        }

        /// <summary>
        /// Seconds an agent at this detail level should wait between path
        /// recalculations.
        ///
        /// Near agents get zero extra cooldown: they repath every time the
        /// scheduler services them, which the update budget already limits. A
        /// second delay on top of that would leave a chaser steering at a stale
        /// point, trailing a moving target instead of catching it, so the only
        /// remaining gate for Near is the move threshold.
        /// </summary>
        public static float RepathInterval(AiDetailLevel level, float baseInterval)
        {
            switch (level)
            {
                case AiDetailLevel.Near: return 0f;
                case AiDetailLevel.Mid: return baseInterval * 4f;
                default: return baseInterval * 12f;
            }
        }
    }
}
