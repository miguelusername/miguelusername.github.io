using UnityEngine;

public class AntiZombieZone : MonoBehaviour
{
    ZombieSpawner zombieSpawner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if( zombieSpawner == null )
            zombieSpawner = FindFirstObjectByType<ZombieSpawner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<S_TaxiPlayer_Test>() != null)
        {
            foreach(GameObject zombie in zombieSpawner.zombies)
            {
                //Debug.Log("Told a zombie to avoid player");
                zombie.GetComponent<Zombie>().ToggleIsPlayerInAntiZombieZone(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<S_TaxiPlayer_Test>() != null)
        {
            foreach (GameObject zombie in zombieSpawner.zombies)
            {
                //Debug.Log("Told a zombie to NOT avoid player");
                zombie.GetComponent<Zombie>().ToggleIsPlayerInAntiZombieZone(false);
            }
        }
    }
}
