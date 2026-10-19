using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieTaxi.Core;

public class SpatialHashGridTests
{
    private static Vector3 At(float x, float z) => new(x, 0f, z);

    [Test]
    public void Query_ReturnsOnlyItemsInsideRadius()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("origin", At(0f, 0f));
        grid.Insert("near", At(3f, 4f));    // distance 5
        grid.Insert("far", At(50f, 50f));

        List<string> results = new();
        grid.Query(At(0f, 0f), 6f, results);

        CollectionAssert.AreEquivalent(new[] { "origin", "near" }, results);
    }

    [Test]
    public void Query_IsInclusiveOfTheBoundary()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("edge", At(5f, 0f));

        List<string> results = new();
        grid.Query(At(0f, 0f), 5f, results);

        Assert.AreEqual(1, results.Count);
    }

    [Test]
    public void Query_FindsItemsAcrossCellBoundaries()
    {
        // Points either side of a cell edge must still both be found.
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("leftOfEdge", At(9.5f, 0f));
        grid.Insert("rightOfEdge", At(10.5f, 0f));

        List<string> results = new();
        grid.Query(At(10f, 0f), 2f, results);

        Assert.AreEqual(2, results.Count);
    }

    [Test]
    public void Query_HandlesNegativeCoordinates()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("negative", At(-15f, -25f));

        List<string> results = new();
        grid.Query(At(-14f, -24f), 3f, results);

        CollectionAssert.AreEqual(new[] { "negative" }, results);
    }

    [Test]
    public void Query_AppendsToCallerList_SoItCanBeReused()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("a", At(0f, 0f));

        List<string> results = new() { "preexisting" };
        grid.Query(At(0f, 0f), 1f, results);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("preexisting", results[0]);
    }

    [Test]
    public void Clear_RemovesEveryItemButKeepsCellsAllocated()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("a", At(0f, 0f));
        grid.Insert("b", At(100f, 100f));

        int cellsBefore = grid.CellCount;
        grid.Clear();

        Assert.AreEqual(0, grid.Count);
        Assert.AreEqual(cellsBefore, grid.CellCount, "Buckets should be reused to avoid per-frame allocation.");

        List<string> results = new();
        grid.Query(At(0f, 0f), 1000f, results);
        Assert.IsEmpty(results);
    }

    [Test]
    public void TryFindNearest_ReturnsClosestItem()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("far", At(20f, 0f));
        grid.Insert("closest", At(2f, 0f));
        grid.Insert("middle", At(8f, 0f));

        bool found = grid.TryFindNearest(At(0f, 0f), 100f, out string nearest);

        Assert.IsTrue(found);
        Assert.AreEqual("closest", nearest);
    }

    [Test]
    public void TryFindNearest_FailsWhenNothingIsInRange()
    {
        SpatialHashGrid<string> grid = new(10f);
        grid.Insert("far", At(500f, 500f));

        bool found = grid.TryFindNearest(At(0f, 0f), 10f, out string nearest);

        Assert.IsFalse(found);
        Assert.IsNull(nearest);
    }

    [Test]
    public void TryFindNearest_OnEmptyGrid_ReturnsFalse()
    {
        SpatialHashGrid<string> grid = new(10f);

        Assert.IsFalse(grid.TryFindNearest(At(0f, 0f), 100f, out _));
    }

    [Test]
    public void GridAgreesWithBruteForceSearch()
    {
        // The grid is only useful if it returns exactly what an O(n) scan would.
        // Cross-check the two against each other on pseudo-random data.
        Random.InitState(1234);

        SpatialHashGrid<int> grid = new(25f);
        List<Vector3> positions = new();

        for (int i = 0; i < 500; i++)
        {
            Vector3 p = At(Random.Range(-400f, 400f), Random.Range(-400f, 400f));
            positions.Add(p);
            grid.Insert(i, p);
        }

        const float radius = 40f;

        for (int trial = 0; trial < 25; trial++)
        {
            Vector3 center = At(Random.Range(-400f, 400f), Random.Range(-400f, 400f));

            List<int> expected = new();
            for (int i = 0; i < positions.Count; i++)
            {
                float dx = positions[i].x - center.x;
                float dz = positions[i].z - center.z;
                if (dx * dx + dz * dz <= radius * radius) expected.Add(i);
            }

            List<int> actual = new();
            grid.Query(center, radius, actual);

            CollectionAssert.AreEquivalent(expected, actual, $"Mismatch on trial {trial}.");
        }
    }

    [Test]
    public void ConstructorRejectsNonPositiveCellSize()
    {
        // Must not divide by zero or hash everything into one cell.
        SpatialHashGrid<string> grid = new(0f);
        Assert.Greater(grid.CellSize, 0f);

        grid.Insert("a", At(0f, 0f));
        grid.Insert("b", At(1000f, 1000f));

        List<string> results = new();
        grid.Query(At(0f, 0f), 1f, results);

        CollectionAssert.AreEqual(new[] { "a" }, results);
    }
}
