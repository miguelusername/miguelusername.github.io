using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using ZombieTaxi.Core;
using Debug = UnityEngine.Debug;

/// <summary>
/// Compares the spatial index against a brute-force linear scan.
///
/// The assertions are deliberately loose because wall-clock timing on a shared
/// machine is noisy; the point is to prove the grid is not slower and to print
/// measured figures. The correctness guarantee, that both approaches return the
/// same set, lives in SpatialHashGridTests.
/// </summary>
public class SpatialHashGridBenchmark
{
    private const int ZombieCount = 500;
    private const int QueryCount = 2000;
    private const float WorldExtent = 400f;
    private const float SearchRadius = 40f;

    private static List<Vector3> BuildPositions()
    {
        Random.InitState(20260927);

        List<Vector3> positions = new(ZombieCount);
        for (int i = 0; i < ZombieCount; i++)
        {
            positions.Add(new Vector3(
                Random.Range(-WorldExtent, WorldExtent),
                0f,
                Random.Range(-WorldExtent, WorldExtent)));
        }

        return positions;
    }

    private static List<Vector3> BuildQueryPoints()
    {
        Random.InitState(987654);

        List<Vector3> queries = new(QueryCount);
        for (int i = 0; i < QueryCount; i++)
        {
            queries.Add(new Vector3(
                Random.Range(-WorldExtent, WorldExtent),
                0f,
                Random.Range(-WorldExtent, WorldExtent)));
        }

        return queries;
    }

    /// <summary>Baseline: test every zombie, using Vector3.Distance (a square root) each time.</summary>
    private static int BruteForceNearest(List<Vector3> positions, Vector3 center, float radius)
    {
        int best = -1;
        float bestDistance = radius;

        for (int i = 0; i < positions.Count; i++)
        {
            float distance = Vector3.Distance(center, positions[i]);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = i;
        }

        return best;
    }

    [Test]
    public void NearestQuery_GridBeatsLinearScan()
    {
        List<Vector3> positions = BuildPositions();
        List<Vector3> queries = BuildQueryPoints();

        SpatialHashGrid<int> grid = new(SearchRadius);
        for (int i = 0; i < positions.Count; i++)
            grid.Insert(i, positions[i]);

        // Warm up so JIT cost is not attributed to whichever runs first.
        for (int i = 0; i < 50; i++)
        {
            BruteForceNearest(positions, queries[i], SearchRadius);
            grid.TryFindNearest(queries[i], SearchRadius, out _);
        }

        Stopwatch bruteForceTimer = Stopwatch.StartNew();
        int bruteForceHits = 0;
        for (int i = 0; i < queries.Count; i++)
        {
            if (BruteForceNearest(positions, queries[i], SearchRadius) >= 0) bruteForceHits++;
        }
        bruteForceTimer.Stop();

        Stopwatch gridTimer = Stopwatch.StartNew();
        int gridHits = 0;
        for (int i = 0; i < queries.Count; i++)
        {
            if (grid.TryFindNearest(queries[i], SearchRadius, out _)) gridHits++;
        }
        gridTimer.Stop();

        double bruteForceMs = bruteForceTimer.Elapsed.TotalMilliseconds;
        double gridMs = gridTimer.Elapsed.TotalMilliseconds;

        Debug.Log(
            $"Nearest-zombie query over {ZombieCount} zombies, {QueryCount} queries:\n" +
            $"  linear scan : {bruteForceMs:F2} ms ({bruteForceMs / QueryCount * 1000.0:F2} us per query)\n" +
            $"  spatial grid: {gridMs:F2} ms ({gridMs / QueryCount * 1000.0:F2} us per query)\n" +
            $"  speedup     : {bruteForceMs / gridMs:F1}x");

        // Both must agree on whether anything was in range at all.
        Assert.AreEqual(bruteForceHits, gridHits, "Grid and linear scan disagree on how many queries found a target.");
        Assert.Less(gridMs, bruteForceMs, "The spatial index should not be slower than the scan it replaces.");
    }

    [Test]
    public void RebuildCost_IsSmallRelativeToTheQueriesItSaves()
    {
        List<Vector3> positions = BuildPositions();
        SpatialHashGrid<int> grid = new(SearchRadius);

        // Rebuilding is O(n) and happens on an interval, not per query.
        Stopwatch rebuildTimer = Stopwatch.StartNew();
        const int rebuilds = 500;
        for (int r = 0; r < rebuilds; r++)
        {
            grid.Clear();
            for (int i = 0; i < positions.Count; i++)
                grid.Insert(i, positions[i]);
        }
        rebuildTimer.Stop();

        double perRebuildMs = rebuildTimer.Elapsed.TotalMilliseconds / rebuilds;
        Debug.Log($"Spatial index rebuild of {ZombieCount} zombies: {perRebuildMs * 1000.0:F1} us per rebuild");

        Assert.AreEqual(positions.Count, grid.Count);
        Assert.Less(perRebuildMs, 5.0, "A rebuild should be well under a frame budget.");
    }
}
