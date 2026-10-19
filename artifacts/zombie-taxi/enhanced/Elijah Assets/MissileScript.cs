using UnityEditor;
using UnityEngine;

public class MissileScript : MonoBehaviour
{
    public int damage;
    public float explosionRange;
    public float force = 10f;
    float timer = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {

            Debug.Log("Missile: Hit " + other.gameObject);
            Collider[] objects = Physics.OverlapSphere(other.transform.position, explosionRange);

            foreach (Collider z in objects)
            {
                if (z.CompareTag("Player"))
                {
                    Debug.Log("Missile: Not attacking Player");
                    continue;
                }
                else
                {
                    GetComponentInChildren<ParticleSystem>().Play();

                    Debug.Log("Missile: Found object");
                    if (z.GetComponent<Zombie>())
                        z.GetComponent<Zombie>().zombieHealth -= damage;
/*
                    if (z.GetComponent<Rigidbody>() != null)
                    {
                        Debug.Log("Missile: KNOCK BACK");
                        //z.GetComponent<Rigidbody>().AddExplosionForce(force, transform.position, explosionRange);
                    }*/
                    if (z.GetComponent<Zombie>() != null)
                    {
                        Debug.Log("PLEASE KNOCK BACK");
                        Vector3 direction = (transform.position - z.transform.position).normalized;
                        z.GetComponent<Zombie>().OnHitByTaxi(direction, 1f, force, force);
                    }
                }
            }
            //Destroy(gameObject);
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
            Destroy(gameObject);
    }
}
