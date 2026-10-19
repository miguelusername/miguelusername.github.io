using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private List<ZombieSpawnOption> zombieOptions = new();
    public List<GameObject> zombies = new();
    private GameObject zombiePrefab;
    //[SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private int zombieCount = 100;

    [SerializeField] private Renderer roadsRenderer;

    GameObject player;

    private int roadAreaMask;

    private void Awake()
    {
        int roadAreaIndex = NavMesh.GetAreaFromName("Road");
        roadAreaMask = 1 << roadAreaIndex;
        Debug.Log("Road area index: " + roadAreaIndex);

        if (roadAreaIndex < 0)
        {
            Debug.LogError("Road area name not found.");
            roadAreaMask = NavMesh.AllAreas;
        }
        else roadAreaMask = 1 << roadAreaIndex;


    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(zombiePrefab == null)
        {
            //Fallback for zombies
            zombiePrefab = (GameObject)Resources.Load("Zombie");
        }

        Bounds b = roadsRenderer.bounds;


        for (int i = 0; i < zombieCount; i++)
        {
            if(TryGetRandomPointOnRoad(b, out Vector3 spawnPos))
            {
                GameObject prefab = PickZombiePrefab();
                var z = Instantiate(prefab, spawnPos, Quaternion.identity);
                zombies.Add(z);
            }
            else
            {
                Debug.LogWarning("Failed to find road spawn point for zombie #" + i);
            }
        }
        
    }
    GameObject PickZombiePrefab()
    {
        float totalWeight = 0f;

        foreach (var option in zombieOptions)
            totalWeight += option.weight;

        float roll = Random.Range(0f, totalWeight);

        foreach (var option in zombieOptions)
        {
            if (roll < option.weight)
                return option.prefab;

            roll -= option.weight;
        }

        return zombieOptions[0].prefab;
    }

    bool TryGetRandomPointOnRoad(Bounds b, out Vector3 result)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            Vector3 randomPoint = new Vector3(x, b.max.y + 200f, z);

            //Snap to ANY navmesh first
            if (!NavMesh.SamplePosition(randomPoint, out var anyHit, 200f, NavMesh.AllAreas))
                continue;

            //From that snapped point, look nearby for ROAD specifically
            if (NavMesh.SamplePosition(anyHit.position, out var roadHit, 200f, roadAreaMask))
            {
                result = roadHit.position;
                return true;
            }
        }

        result = default;
        return false;
    }
}
