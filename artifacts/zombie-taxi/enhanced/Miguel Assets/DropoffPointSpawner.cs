using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DropoffPointSpawner : MonoBehaviour
{
    [Header("Dropoff Settings")]
    [SerializeField] private int dropoffCount = 15;
    [SerializeField] private float sampleRadius = 500f;
    [SerializeField] private float minSpacing = 15f;

    [SerializeField] private Renderer roadsRenderer;

    [Header("Prefabs")]
    [SerializeField] private GameObject dropoffPrefab;

    [Header("Road/Sidewalk")]
    [SerializeField] private float maxDistanceToRoad = 8f;  
    [SerializeField] private float pushAwayFromRoad = 1.0f;  

    [Header("Selection Rules")]
    public float minDistanceFromPickup = 60f;
    public float maxDistanceFromPickup = 300f;

    public List<GameObject> dropoffPoints = new();

    int roadAreaMask;
    int walkMask;

    private void Awake()
    {
        int roadIndex = NavMesh.GetAreaFromName("Road");

        if (roadIndex < 0)
        {
            Debug.LogError("Road NavMesh area not found!");
            roadAreaMask = NavMesh.AllAreas;
        }
        else
        {
            roadAreaMask = 1 << roadIndex;
        }

        //everything except road
        walkMask = NavMesh.AllAreas & ~roadAreaMask;
    }

    void Start()
    {
        if (!roadsRenderer)
        {
            Debug.LogError("DropoffSpawner: Roads renderer missing.");
            return;
        }

        if (!dropoffPrefab)
            dropoffPrefab = Resources.Load<GameObject>("DropoffPoint");

        if (!dropoffPrefab)
        {
            Debug.LogError("Dropoff prefab not found in Resources.");
            return;
        }

        Bounds b = roadsRenderer.bounds;

        for (int i = 0; i < dropoffCount; i++)
        {
            if (TryGetDropoffPoint(b, out Vector3 pos))
            {
                GameObject d = Instantiate(dropoffPrefab, pos, Quaternion.identity);
                d.SetActive(false);              // <- IMPORTANT
                dropoffPoints.Add(d);

            }
        }
    }

    //API for RideManager
    public Transform GetDropoffPoint(Vector3 pickupPos)
    {
        if (dropoffPoints.Count == 0)
        {
            Debug.LogError("No dropoff points available!");
            return null;
        }

        List<Transform> candidates = new();
        float minSqr = minDistanceFromPickup * minDistanceFromPickup;
        float maxSqr = maxDistanceFromPickup * maxDistanceFromPickup;

        Transform farthest = dropoffPoints[0].transform;
        float farthestSqr = -1f;

        foreach (var d in dropoffPoints)
        {
            if (!d) continue;

            float distSqr = (d.transform.position - pickupPos).sqrMagnitude;

            if (distSqr > farthestSqr)
            {
                farthestSqr = distSqr;
                farthest = d.transform;
            }

            if (distSqr >= minSqr && distSqr <= maxSqr)
                candidates.Add(d.transform);
        }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        return farthest; // fallback
    }

    bool TryGetDropoffPoint(Bounds b, out Vector3 result)
    {
        for (int i = 0; i < 300; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);

            Vector3 random = new(x, b.max.y + 200f, z);

            //Find nearest road
            if (!NavMesh.SamplePosition(random, out var roadHit, sampleRadius, roadAreaMask))
                continue;

            //Find walkable near that road (NOT on road)
            if (!NavMesh.SamplePosition(roadHit.position, out var walkHit, maxDistanceToRoad, NavMesh.AllAreas))
                continue;

            bool isRoad = (walkHit.mask & roadAreaMask) != 0;
            if (isRoad) continue;

            //Push away from road a little (more “sidewalk” feel)
            Vector3 away = (walkHit.position - roadHit.position);
            if (away.sqrMagnitude > 0.001f)
            {
                away.Normalize();
                Vector3 pushed = walkHit.position + away * pushAwayFromRoad;

                //Re-snap to non-road using walkMask
                if (NavMesh.SamplePosition(pushed, out var finalHit, 2f, walkMask) &&
                    (finalHit.mask & roadAreaMask) == 0)
                {
                    if (!IsFarEnough(finalHit.position))
                        continue;

                    result = finalHit.position;
                    return true;
                }
            }

            //fallback: walkHit position (still not road)
            if (!IsFarEnough(walkHit.position))
                continue;

            result = walkHit.position;
            return true;
        }

        result = default;
        return false;
    }

    bool IsFarEnough(Vector3 pos)
    {
        float minSqr = minSpacing * minSpacing;

        foreach (var d in dropoffPoints)
        {
            if (!d) continue;
            if ((d.transform.position - pos).sqrMagnitude < minSqr)
                return false;
        }

        return true;
    }
}
