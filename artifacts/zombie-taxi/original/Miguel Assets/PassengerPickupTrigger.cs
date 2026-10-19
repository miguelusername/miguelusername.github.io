using UnityEngine;

public class PassengerPickupTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var manager = FindFirstObjectByType<RideManager>();
        if (manager != null)
            manager.TryPickupPassenger(GetComponentInParent<Passenger>());
    }
}
