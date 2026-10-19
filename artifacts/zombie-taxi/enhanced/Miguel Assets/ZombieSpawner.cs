using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Populates the map with zombies on valid road surface.
///
/// The NavMesh triangulation is read once at startup and the centroid of every
/// road triangle is kept in a list, so choosing a spawn point is a single
/// random index: O(m) once, then O(1) per zombie with a bounded cost. Spawns
/// are spread across frames so a hundred instantiations do not land on one.
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private List<ZombieSpawnOption> zombieOptions = new();
    public List<GameObject> zombies = new();
    private GameObject zombiePrefab;
    [SerializeField] private int zombieCount = 100;

    [SerializeField] private Renderer roadsRenderer;

    [Tooltip("How many zombies to instantiate per frame while filling the map.")]
    [SerializeField] private int spawnsPerFrame = 10;

    [Tooltip("Random offset applied around a road centroid so zombies are not all dead centre.")]
    [SerializeField] private float spawnJitter = 2f;

    private int roadAreaMask;

    /// <summary>Candidate spawn points, computed once from the NavMesh triangulation.</summary>
    private readonly List<Vector3> roadPoints = new();

    private void Awake()
    {
        int roadAreaIndex = NavMesh.GetAreaFromName("Road");

        if (roadAreaIndex < 0)
        {
            Debug.LogError("Road area name not found, falling back to all NavMesh areas.");
            roadAreaMask = NavMesh.AllAreas;
        }
        else
        {
            roadAreaMask = 1 << roadAreaIndex;
        }
    }

    private void Start()
    {
        if (zombiePrefab == null)
        {
            // Fallback for zombies
            zombiePrefab = (GameObject)Resources.Load("Zombie");
        }

        BuildRoadPointCache();
        StartCoroutine(SpawnOverTime());
    }

    /// <summary>
    /// Walks the NavMesh triangulation once and records the centroid of every
    /// triangle flagged as road, giving spawning a fixed pool to draw from.
    /// </summary>
    private void BuildRoadPointCache()
    {
        roadPoints.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        int triangleCount = triangulation.indices.Length / 3;

        for (int triangle = 0; triangle < triangleCount; triangle++)
        {
            int areaIndex = triangulation.areas[triangle];

            // areas[] holds the area index; roadAreaMask is a bitmask over those indices.
            if ((roadAreaMask & (1 << areaIndex)) == 0) continue;

            Vector3 a = triangulation.vertices[triangulation.indices[triangle * 3]];
            Vector3 b = triangulation.vertices[triangulation.indices[triangle * 3 + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[triangle * 3 + 2]];

            roadPoints.Add((a + b + c) / 3f);
        }

        if (roadPoints.Count == 0)
            Debug.LogWarning("No road triangles found on the NavMesh; falling back to bounds sampling.");
    }

    private IEnumerator SpawnOverTime()
    {
        int spawnedThisFrame = 0;

        for (int i = 0; i < zombieCount; i++)
        {
            if (TryGetSpawnPoint(out Vector3 spawnPos))
            {
                GameObject prefab = PickZombiePrefab();
                if (prefab == null) continue;

                GameObject z = Instantiate(prefab, spawnPos, Quaternion.identity);
                zombies.Add(z);
            }
            else
            {
                Debug.LogWarning("Failed to find road spawn point for zombie #" + i);
            }

            spawnedThisFrame++;

            if (spawnedThisFrame < spawnsPerFrame) continue;

            spawnedThisFrame = 0;
            yield return null;
        }
    }

    private GameObject PickZombiePrefab()
    {
        if (zombieOptions == null || zombieOptions.Count == 0) return zombiePrefab;

        float totalWeight = 0f;

        foreach (ZombieSpawnOption option in zombieOptions)
            totalWeight += option.weight;

        if (totalWeight <= 0f) return zombieOptions[0].prefab;

        float roll = Random.Range(0f, totalWeight);

        foreach (ZombieSpawnOption option in zombieOptions)
        {
            if (roll < option.weight)
                return option.prefab;

            roll -= option.weight;
        }

        return zombieOptions[0].prefab;
    }

    /// <summary>
    /// O(1) pick from the precomputed road points, with a short NavMesh snap to
    /// keep the jittered position on the surface. Falls back to bounds sampling
    /// if the triangulation yielded no road triangles.
    /// </summary>
    private bool TryGetSpawnPoint(out Vector3 result)
    {
        if (roadPoints.Count > 0)
        {
            Vector3 candidate = roadPoints[Random.Range(0, roadPoints.Count)];

            candidate += new Vector3(
                Random.Range(-spawnJitter, spawnJitter),
                0f,
                Random.Range(-spawnJitter, spawnJitter));

            // Small radius: the candidate is already on the mesh, we are only
            // correcting for the jitter.
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, spawnJitter * 2f, roadAreaMask))
            {
                result = hit.position;
                return true;
            }

            result = roadPoints[Random.Range(0, roadPoints.Count)];
            return true;
        }

        return TryGetRandomPointOnRoad(roadsRenderer.bounds, out result);
    }

    /// <summary>
    /// Rejection sampling over the road bounds. Only used when the NavMesh has
    /// no triangles tagged as road, since cost per zombie here is unbounded.
    /// </summary>
    private bool TryGetRandomPointOnRoad(Bounds b, out Vector3 result)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            Vector3 randomPoint = new(x, b.max.y + 200f, z);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit anyHit, 200f, NavMesh.AllAreas))
                continue;

            if (NavMesh.SamplePosition(anyHit.position, out NavMeshHit roadHit, 200f, roadAreaMask))
            {
                result = roadHit.position;
                return true;
            }
        }

        result = default;
        return false;
    }
}
