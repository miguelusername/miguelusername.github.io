using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PassengerSpawner : MonoBehaviour
{
    public List<GameObject> passengers = new();
    private GameObject passengerPrefab;
    //[SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private int passengerCount = 10;

    //How far from a random pointw e search for nearest navmesh point
    [SerializeField] private float sampleRadius = 500f;

    [SerializeField] private Renderer roadsRenderer;

    private int roadAreaMask;
    private int walkNotRoadMask;

    [SerializeField] private float maxDistanceToRoad = 8f;

    [SerializeField] private float minPassengerSpacing = 6f; 


    private void Awake()
    {
        int roadAreaIndex = NavMesh.GetAreaFromName("Road");
        roadAreaMask = 1 << roadAreaIndex;
        //Debug.Log("Road area index: " + roadAreaIndex);

        if (roadAreaIndex < 0)
        {
            Debug.LogError("Road area name not found.");
            roadAreaMask = NavMesh.AllAreas;
        }
        else
        {
            roadAreaMask = 1 << roadAreaIndex;
        }

        walkNotRoadMask = NavMesh.AllAreas & ~roadAreaMask;

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!roadsRenderer)
        {
            Debug.LogError("roadsRenderer not assigned.");
            return;
        }

        if (passengerPrefab == null)
        {
            //Fallback for passengers
            passengerPrefab = (GameObject)Resources.Load("Passenger");
        }

        if (!passengerPrefab)
        {
            Debug.LogError("Passenger prefab not found in resources.");
            return;
        }

        Bounds b = roadsRenderer.bounds;


        for (int i = 0; i < passengerCount; i++)
        {
            if (TryGetPassegengerPointNearRoad(b, out Vector3 spawnPos))
            {
                var p = Instantiate(passengerPrefab, spawnPos, Quaternion.identity);
                p.SetActive(false);              // <- IMPORTANT
                passengers.Add(p);

            }
            else
            {
                Debug.LogWarning("Failed to find road spawn point for Passenger #" + i);
            }
        }

    }

    bool TryGetPassegengerPointNearRoad(Bounds b, out Vector3 result)
    {
        for (int attempt = 0; attempt < 300; attempt++)
        {
            //Pick random point that's within bounds
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            Vector3 randomPoint = new Vector3(x, b.max.y + 200f, z);

            //Find nearest road point
            if (!NavMesh.SamplePosition(randomPoint, out var roadHit, sampleRadius, roadAreaMask))
                continue;

            //Now find a walkable point near that road point (sidewalk/ground)
            //    Using AllAreas here so it can land on walkable regions; if you have a "Sidewalk" area, use that instead.
            if (!NavMesh.SamplePosition(roadHit.position, out var walkHit, maxDistanceToRoad, NavMesh.AllAreas))
                continue;

            bool isRoad = (walkHit.mask & roadAreaMask) != 0;
            if (isRoad) continue;

            //Push slightly away from the road (toward sidewalk) so their feet don't get crushed from incoming traffic
            Vector3 away = (walkHit.position - roadHit.position);
            if (away.sqrMagnitude > 0.001f)
            {
                away.Normalize();
                Vector3 pushed = walkHit.position + away * 1.0f;

                //re-snap to non-road
                if (NavMesh.SamplePosition(pushed, out var finalHit, 2f, walkNotRoadMask) &&
                    (finalHit.mask & roadAreaMask) == 0)
                {
                    result = finalHit.position;
                    return true;
                }
            }


            Vector3 finalPos = walkHit.position;

            //spacing check
            if (!IsFarEnoughFromOthers(finalPos))
                continue;

            result = finalPos;
            return true;

        }

        result = default;
        return false;
    }

    bool IsFarEnoughFromOthers(Vector3 pos)
    {
        float minSqr = minPassengerSpacing * minPassengerSpacing;

        foreach (var p in passengers)
        {
            if (!p) continue;

            float sqrDist = (p.transform.position - pos).sqrMagnitude;
            if (sqrDist < minSqr)
                return false;
        }

        return true;
    }

}
