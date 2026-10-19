using System.Collections.Generic;
using UnityEngine;

public class GunZombieTracking : MonoBehaviour
{
    ZombieSpawner spawner;

    [SerializeField] float maximumDistanceCheck = 1500f;
    [SerializeField] bool isZombieInLOS;
    [SerializeField] GameObject trackingTarget;

    private void Awake()
    {
        spawner = FindFirstObjectByType<ZombieSpawner>();
    }

    private void Update()
    {
        List<GameObject> allZombies = spawner.zombies;
        float closestZombieDistance = maximumDistanceCheck;

        if (!isZombieInLOS)
        {
            foreach (GameObject z in allZombies)
            {
                if (z != null)
                    if (z.GetComponent<Zombie>().zombieHealth > 0)
                        if (Vector3.Distance(transform.position, z.transform.position) < closestZombieDistance)
                        {
                            Debug.Log("Tracker: Found Zombie");

                            Debug.DrawLine(transform.position, z.transform.position + new Vector3(0, 1, 0), Color.green);

                            trackingTarget = z;
                            closestZombieDistance = Vector3.Distance(transform.position, z.transform.position);
                        }
            }
        }

        if (trackingTarget != null)
        {
            Debug.DrawLine(transform.position, trackingTarget.transform.position + new Vector3(0, 1, 0), Color.red);

            Debug.Log("Tracker: Zombie in sight");
            transform.parent.LookAt(trackingTarget.transform.position + new Vector3(0, 1, 0));
            //transform.LookAt(trackingTarget.transform.position + new Vector3(0, 1, 0));

            if (Vector3.Distance(transform.position, trackingTarget.transform.position) > maximumDistanceCheck)
            {
                trackingTarget = null;
                transform.parent.rotation = FindAnyObjectByType<S_TaxiPlayer_Test>().transform.rotation;
            }
        }
        else
        {
            isZombieInLOS = false;
            transform.parent.rotation = FindAnyObjectByType<S_TaxiPlayer_Test>().transform.rotation;
        }

    }
}
