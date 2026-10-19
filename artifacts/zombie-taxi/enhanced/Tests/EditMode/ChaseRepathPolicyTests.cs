using NUnit.Framework;
using UnityEngine;
using ZombieTaxi.Core;

/// <summary>
/// Behavioural tests for chase pathing.
///
/// The structural tests elsewhere prove the data structures return correct
/// answers. These prove the optimisation does not change what the game does:
/// a chasing zombie must still end up on top of a moving taxi. The regression
/// these were written for passed every structural test, because nothing that
/// tested a grid or a scheduler knew how fast a taxi drives.
/// </summary>
public class ChaseRepathPolicyTests
{
    // Taxi top speed is 70 km/h in S_TaxiPlayer_Test, which is ~19.4 m/s.
    private const float TaxiTopSpeed = 19.4f;

    // 100 zombies serviced 25 per frame at 60 fps = every 4th frame.
    private const float ServiceInterval = 4f / 60f;

    private const float BaseInterval = 0.25f;
    private const float MoveThreshold = 0.75f;

    private static Vector3 At(float x) => new(x, 0f, 0f);

    [Test]
    public void FirstDecisionAlwaysRepaths()
    {
        ChaseRepathPolicy policy = default;

        Assert.IsTrue(policy.ShouldRepath(At(0f), 0f, AiDetailLevel.Near, BaseInterval, MoveThreshold));
        Assert.IsTrue(policy.HasPath);
    }

    [Test]
    public void StationaryTargetDoesNotTriggerRepaths()
    {
        ChaseRepathPolicy policy = default;
        policy.ShouldRepath(At(0f), 0f, AiDetailLevel.Near, BaseInterval, MoveThreshold);

        int repaths = 0;
        for (float t = 0f; t < 5f; t += ServiceInterval)
        {
            if (policy.ShouldRepath(At(0f), t, AiDetailLevel.Near, BaseInterval, MoveThreshold))
                repaths++;
        }

        Assert.AreEqual(0, repaths, "A parked taxi should never invalidate the path.");
    }

    [Test]
    public void TargetMovingPastThresholdTriggersARepath()
    {
        ChaseRepathPolicy policy = default;
        policy.ShouldRepath(At(0f), 0f, AiDetailLevel.Near, BaseInterval, MoveThreshold);

        Assert.IsFalse(policy.ShouldRepath(At(MoveThreshold * 0.5f), 1f, AiDetailLevel.Near, BaseInterval, MoveThreshold));
        Assert.IsTrue(policy.ShouldRepath(At(MoveThreshold * 2f), 2f, AiDetailLevel.Near, BaseInterval, MoveThreshold));
    }

    [Test]
    public void ResetForcesTheNextDecisionToRepath()
    {
        ChaseRepathPolicy policy = default;
        policy.ShouldRepath(At(0f), 0f, AiDetailLevel.Near, BaseInterval, MoveThreshold);

        policy.Reset();

        Assert.IsFalse(policy.HasPath);
        Assert.IsTrue(policy.ShouldRepath(At(0f), 0.01f, AiDetailLevel.Near, BaseInterval, MoveThreshold));
    }

    /// <summary>
    /// Drives a target away in a straight line at taxi speed and measures how
    /// far behind the chaser's aim point drifts. This is the regression test:
    /// with a quarter-second cooldown on the Near band the error exceeded five
    /// metres and zombies trailed the taxi instead of touching it.
    /// </summary>
    [Test]
    public void ChaserTracksATaxiMovingAtTopSpeed()
    {
        ChaseRepathPolicy policy = default;

        float worstError = 0f;

        for (float t = 0f; t < 10f; t += ServiceInterval)
        {
            Vector3 taxi = At(TaxiTopSpeed * t);
            policy.ShouldRepath(taxi, t, AiDetailLevel.Near, BaseInterval, MoveThreshold);

            float error = (taxi - policy.LastPathTarget).magnitude;
            if (error > worstError) worstError = error;
        }

        // One service interval of travel plus the move threshold is the floor;
        // anything beyond that means a second gate is adding avoidable lag.
        float allowed = TaxiTopSpeed * ServiceInterval + MoveThreshold;

        Assert.LessOrEqual(worstError, allowed,
            $"Chaser aim point drifted {worstError:F2} m behind the taxi, budget {allowed:F2} m.");
    }

    [Test]
    public void NearBandAddsNoLagBeyondTheServiceInterval()
    {
        float near = ChaseRepathPolicy.WorstCaseTrackingError(
            AiDetailLevel.Near, BaseInterval, MoveThreshold, TaxiTopSpeed, ServiceInterval);

        float floor = TaxiTopSpeed * ServiceInterval + MoveThreshold;

        Assert.AreEqual(floor, near, 0.001f,
            "Close chasers must be limited only by how often they are serviced.");
    }

    [Test]
    public void DistantBandsAreAllowedMoreLag()
    {
        float near = ChaseRepathPolicy.WorstCaseTrackingError(
            AiDetailLevel.Near, BaseInterval, MoveThreshold, TaxiTopSpeed, ServiceInterval);
        float mid = ChaseRepathPolicy.WorstCaseTrackingError(
            AiDetailLevel.Mid, BaseInterval, MoveThreshold, TaxiTopSpeed, ServiceInterval);
        float far = ChaseRepathPolicy.WorstCaseTrackingError(
            AiDetailLevel.Far, BaseInterval, MoveThreshold, TaxiTopSpeed, ServiceInterval);

        Assert.Less(near, mid);
        Assert.Less(mid, far);
    }

    /// <summary>
    /// The throttle still has to earn its place: a chaser must not request a
    /// path on every single service tick when the taxi is crawling.
    /// </summary>
    [Test]
    public void ThrottleStillSuppressesRepathsForASlowTarget()
    {
        ChaseRepathPolicy policy = default;
        const float crawlSpeed = 1f;

        int repaths = 0;
        int ticks = 0;

        for (float t = 0f; t < 10f; t += ServiceInterval)
        {
            ticks++;
            if (policy.ShouldRepath(At(crawlSpeed * t), t, AiDetailLevel.Near, BaseInterval, MoveThreshold))
                repaths++;
        }

        Assert.Less(repaths, ticks / 2,
            "The move threshold should still cut path solves for a slow-moving target.");
    }
}
