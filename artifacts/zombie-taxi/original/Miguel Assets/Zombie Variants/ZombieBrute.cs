using UnityEngine;

public class ZombieBrute : MonoBehaviour
{
    [SerializeField] private float throwForce = 18f;
    [SerializeField] private float upwardForce = 5f;
    [SerializeField] private float cooldown = 3f;

    private float nextThrowTime;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextThrowTime) return;

        S_TaxiPlayer_Test taxi = other.GetComponentInParent<S_TaxiPlayer_Test>();
        if (!taxi) return;

        Rigidbody taxiRb = taxi.GetComponent<Rigidbody>();
        if (!taxiRb) return;

        nextThrowTime = Time.time + cooldown;

        Vector3 direction = (taxi.transform.position - transform.position).normalized;
        taxiRb.AddForce(direction * throwForce + Vector3.up * upwardForce, ForceMode.Impulse);
    }
}
