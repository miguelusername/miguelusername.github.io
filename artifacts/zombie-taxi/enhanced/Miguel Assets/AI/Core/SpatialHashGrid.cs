using System.Collections.Generic;
using UnityEngine;

namespace ZombieTaxi.Core
{
    /// <summary>
    /// A uniform spatial hash grid over the XZ plane.
    ///
    /// Answers "which entities are within radius r of this point?" by bucketing
    /// entities into fixed-size cells, so a query only visits the cells the
    /// search circle overlaps: O(c + k), where c is the number of overlapped
    /// cells and k is the number of entities inside them. With the cell size
    /// tuned to roughly the query radius, c is a small constant and k is
    /// proportional to local crowding rather than to the total population.
    ///
    /// Rebuilding costs O(n), so the structure pays for itself once there is
    /// more than one query per rebuild, or when queries happen more often than
    /// entities move far enough to matter.
    /// </summary>
    public sealed class SpatialHashGrid<T>
    {
        private readonly struct Entry
        {
            public readonly T Item;
            public readonly Vector3 Position;

            public Entry(T item, Vector3 position)
            {
                Item = item;
                Position = position;
            }
        }

        private readonly float cellSize;
        private readonly float inverseCellSize;
        private readonly Dictionary<long, List<Entry>> cells;

        /// <summary>Number of entities currently stored.</summary>
        public int Count { get; private set; }

        /// <summary>Number of non-empty cells. Useful for diagnostics and tests.</summary>
        public int CellCount => cells.Count;

        public float CellSize => cellSize;

        public SpatialHashGrid(float cellSize)
        {
            // A non-positive cell size would make the hash meaningless and divide by zero.
            this.cellSize = Mathf.Max(0.0001f, cellSize);
            inverseCellSize = 1f / this.cellSize;
            cells = new Dictionary<long, List<Entry>>();
        }

        /// <summary>
        /// Removes every entity but keeps the allocated cell lists, so a
        /// per-frame rebuild does not generate garbage after the first frame.
        /// </summary>
        public void Clear()
        {
            foreach (KeyValuePair<long, List<Entry>> cell in cells)
                cell.Value.Clear();

            Count = 0;
        }

        public void Insert(T item, Vector3 position)
        {
            long key = CellKey(CellCoordinate(position.x), CellCoordinate(position.z));

            if (!cells.TryGetValue(key, out List<Entry> bucket))
            {
                bucket = new List<Entry>();
                cells[key] = bucket;
            }

            bucket.Add(new Entry(item, position));
            Count++;
        }

        /// <summary>
        /// Appends every entity within <paramref name="radius"/> of
        /// <paramref name="center"/> to <paramref name="results"/>.
        /// The caller owns the list, so it can be reused across frames to avoid
        /// allocating. Comparisons use squared distance, so no square roots.
        /// </summary>
        public void Query(Vector3 center, float radius, List<T> results)
        {
            if (results == null) return;

            float sqrRadius = radius * radius;

            int minX = CellCoordinate(center.x - radius);
            int maxX = CellCoordinate(center.x + radius);
            int minZ = CellCoordinate(center.z - radius);
            int maxZ = CellCoordinate(center.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!cells.TryGetValue(CellKey(x, z), out List<Entry> bucket)) continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        Entry entry = bucket[i];

                        float dx = entry.Position.x - center.x;
                        float dz = entry.Position.z - center.z;

                        if (dx * dx + dz * dz <= sqrRadius)
                            results.Add(entry.Item);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the closest entity within <paramref name="radius"/>, or
        /// <c>default</c> when the search area is empty. Searches the same
        /// cells as <see cref="Query"/> but never builds a result list.
        /// </summary>
        public bool TryFindNearest(Vector3 center, float radius, out T nearest)
        {
            nearest = default;

            float bestSqrDistance = radius * radius;
            bool found = false;

            int minX = CellCoordinate(center.x - radius);
            int maxX = CellCoordinate(center.x + radius);
            int minZ = CellCoordinate(center.z - radius);
            int maxZ = CellCoordinate(center.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!cells.TryGetValue(CellKey(x, z), out List<Entry> bucket)) continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        Entry entry = bucket[i];

                        float dx = entry.Position.x - center.x;
                        float dz = entry.Position.z - center.z;
                        float sqrDistance = dx * dx + dz * dz;

                        if (sqrDistance > bestSqrDistance) continue;

                        bestSqrDistance = sqrDistance;
                        nearest = entry.Item;
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>Floor division so negative world coordinates map to distinct cells.</summary>
        private int CellCoordinate(float worldValue)
        {
            return Mathf.FloorToInt(worldValue * inverseCellSize);
        }

        /// <summary>Packs two cell coordinates into one key so a single dictionary lookup finds the bucket.</summary>
        private static long CellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
