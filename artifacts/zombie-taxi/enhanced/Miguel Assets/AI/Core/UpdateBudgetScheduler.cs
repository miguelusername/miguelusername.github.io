namespace ZombieTaxi.Core
{
    /// <summary>
    /// Round-robin scheduler that spreads work for a population across frames.
    ///
    /// Hands out a fixed-size slice of the population each tick and remembers
    /// where it stopped, so per-frame cost is bounded by the budget rather than
    /// by n. The trade-off is latency: each agent is serviced once every
    /// ceil(n / budget) ticks instead of every tick. That is invisible for
    /// slow-moving agents but would not be acceptable for something like
    /// projectile collision.
    ///
    /// The cursor wraps and survives population changes, so no agent is starved
    /// and none is serviced twice in the same pass.
    /// </summary>
    public sealed class UpdateBudgetScheduler
    {
        private int cursor;

        /// <summary>Index that the next slice will start from.</summary>
        public int Cursor => cursor;

        /// <summary>
        /// Produces the next slice as a start index and a length.
        /// Indices are taken modulo <paramref name="count"/>, so a slice may wrap
        /// past the end of the collection back to the start.
        /// </summary>
        /// <param name="count">Current population size.</param>
        /// <param name="budget">Maximum agents to service this tick.</param>
        /// <param name="start">First index to service.</param>
        /// <param name="length">How many indices to service, never more than count.</param>
        /// <returns>False when there is nothing to do.</returns>
        public bool TryGetSlice(int count, int budget, out int start, out int length)
        {
            start = 0;
            length = 0;

            if (count <= 0 || budget <= 0) return false;

            // Keep the cursor valid when agents have been removed since last tick.
            if (cursor >= count) cursor = 0;

            start = cursor;
            length = budget < count ? budget : count;

            cursor = (cursor + length) % count;
            return true;
        }

        /// <summary>
        /// Maps a slice offset onto a real index, wrapping around the end of the
        /// collection. Callers iterate 0..length-1 and pass each offset here.
        /// </summary>
        public static int IndexAt(int start, int offset, int count)
        {
            return (start + offset) % count;
        }

        /// <summary>Number of ticks needed to service the whole population once.</summary>
        public static int TicksPerFullPass(int count, int budget)
        {
            if (count <= 0 || budget <= 0) return 0;
            return (count + budget - 1) / budget;
        }

        public void Reset()
        {
            cursor = 0;
        }
    }
}
