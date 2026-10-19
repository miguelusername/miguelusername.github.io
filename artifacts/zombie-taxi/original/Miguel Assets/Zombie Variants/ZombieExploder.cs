using UnityEngine;

public class ZombieExploder : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float fuelDamage = 10f;
    [SerializeField] private GameObject explosionEffect;

    private bool exploded;

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;

        S_TaxiPlayer_Test taxi = other.GetComponentInParent<S_TaxiPlayer_Test>();
        if (!taxi) return;

        Explode(taxi);
    }

    private void Explode(S_TaxiPlayer_Test taxi)
    {
        exploded = true;

        taxi.LoseFuel(fuelDamage);

        Rigidbody taxiRb = taxi.GetComponent<Rigidbody>();
        if (taxiRb)
        {
            Vector3 direction = (taxi.transform.position - transform.position).normalized;
            taxiRb.AddForce(direction * explosionForce + Vector3.up * 4f, ForceMode.Impulse);
        }

        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
