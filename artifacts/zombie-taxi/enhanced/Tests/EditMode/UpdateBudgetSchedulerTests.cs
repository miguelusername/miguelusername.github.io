using System.Collections.Generic;
using NUnit.Framework;
using ZombieTaxi.Core;

public class UpdateBudgetSchedulerTests
{
    /// <summary>Runs the scheduler for a number of ticks and records how often each index was serviced.</summary>
    private static int[] ServiceCounts(int count, int budget, int ticks)
    {
        UpdateBudgetScheduler scheduler = new();
        int[] serviced = new int[count];

        for (int tick = 0; tick < ticks; tick++)
        {
            if (!scheduler.TryGetSlice(count, budget, out int start, out int length)) continue;

            for (int offset = 0; offset < length; offset++)
                serviced[UpdateBudgetScheduler.IndexAt(start, offset, count)]++;
        }

        return serviced;
    }

    [Test]
    public void SliceNeverExceedsBudget()
    {
        UpdateBudgetScheduler scheduler = new();

        scheduler.TryGetSlice(100, 25, out _, out int length);

        Assert.AreEqual(25, length);
    }

    [Test]
    public void SliceIsCappedByPopulationWhenBudgetIsLarger()
    {
        UpdateBudgetScheduler scheduler = new();

        scheduler.TryGetSlice(3, 25, out _, out int length);

        Assert.AreEqual(3, length);
    }

    [Test]
    public void EveryAgentIsServicedExactlyOncePerFullPass()
    {
        const int count = 100;
        const int budget = 25;

        int[] serviced = ServiceCounts(count, budget, UpdateBudgetScheduler.TicksPerFullPass(count, budget));

        CollectionAssert.DoesNotContain(serviced, 0, "No agent may be starved.");
        foreach (int times in serviced)
            Assert.AreEqual(1, times, "No agent should be serviced twice in a single pass.");
    }

    [Test]
    public void NoAgentIsStarvedWhenBudgetDoesNotDividePopulation()
    {
        // 7 into 100 does not divide evenly, so slices wrap mid-population.
        const int count = 100;
        const int budget = 7;

        int[] serviced = ServiceCounts(count, budget, 400);

        foreach (int times in serviced)
            Assert.GreaterOrEqual(times, 1);
    }

    [Test]
    public void ServiceCountsStayBalancedOverManyTicks()
    {
        const int count = 37;
        const int budget = 5;

        int[] serviced = ServiceCounts(count, budget, 1000);

        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (int times in serviced)
        {
            if (times < min) min = times;
            if (times > max) max = times;
        }

        // Round-robin should never let one agent fall a full pass behind another.
        Assert.LessOrEqual(max - min, 1);
    }

    [Test]
    public void SliceWrapsAroundEndOfCollection()
    {
        UpdateBudgetScheduler scheduler = new();

        scheduler.TryGetSlice(10, 8, out _, out _);          // services 0..7, cursor now 8
        scheduler.TryGetSlice(10, 8, out int start, out int length);  // should wrap

        Assert.AreEqual(8, start);

        List<int> indices = new();
        for (int offset = 0; offset < length; offset++)
            indices.Add(UpdateBudgetScheduler.IndexAt(start, offset, 10));

        CollectionAssert.Contains(indices, 9);
        CollectionAssert.Contains(indices, 0, "Slice should wrap past the end back to the start.");
    }

    [Test]
    public void CursorRecoversWhenPopulationShrinks()
    {
        UpdateBudgetScheduler scheduler = new();

        scheduler.TryGetSlice(100, 90, out _, out _);   // cursor lands at 90
        Assert.AreEqual(90, scheduler.Cursor);

        // Most of the horde was wiped out; the stale cursor must not index out of range.
        bool ok = scheduler.TryGetSlice(5, 2, out int start, out int length);

        Assert.IsTrue(ok);
        Assert.Less(start, 5);
        Assert.AreEqual(2, length);
    }

    [Test]
    public void EmptyPopulationOrBudgetProducesNoWork()
    {
        UpdateBudgetScheduler scheduler = new();

        Assert.IsFalse(scheduler.TryGetSlice(0, 25, out _, out _));
        Assert.IsFalse(scheduler.TryGetSlice(100, 0, out _, out _));
    }

    [Test]
    public void TicksPerFullPassRoundsUp()
    {
        Assert.AreEqual(4, UpdateBudgetScheduler.TicksPerFullPass(100, 25));
        Assert.AreEqual(5, UpdateBudgetScheduler.TicksPerFullPass(101, 25));
        Assert.AreEqual(1, UpdateBudgetScheduler.TicksPerFullPass(10, 100));
        Assert.AreEqual(0, UpdateBudgetScheduler.TicksPerFullPass(0, 25));
    }
}
