using UnityEngine;

public class DropoffCollider : MonoBehaviour
{
    private RideManager manager;

    private void Awake()
    {
        manager = FindFirstObjectByType<RideManager>();

        if (!manager)
            Debug.LogError("RideManager not found in scene!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (manager != null)
            manager.TryDropoff();
    }
}
