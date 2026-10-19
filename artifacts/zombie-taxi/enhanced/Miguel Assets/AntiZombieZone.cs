using UnityEngine;

/// <summary>
/// Marks a region the horde will not chase the player into.
///
/// The flag lives on <see cref="ZombieManager"/> rather than on each zombie, so
/// crossing the boundary is a single assignment that the whole horde reads
/// instead of a loop over every zombie.
/// </summary>
public class AntiZombieZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<S_TaxiPlayer_Test>() == null) return;

        ZombieManager.Instance.SetPlayerInAntiZombieZone(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<S_TaxiPlayer_Test>() == null) return;

        ZombieManager.Instance.SetPlayerInAntiZombieZone(false);
    }
}
