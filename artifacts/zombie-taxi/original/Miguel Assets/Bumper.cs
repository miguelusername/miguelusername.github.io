using UnityEngine;

public class Bumper : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody taxiRb;
    [SerializeField] private GameManager gm;
    [SerializeField] private CameraShake cameraShake;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] thudSounds; // assign 3 clips here
    [SerializeField] private float minVolume = 0.1f;
    [SerializeField] private float maxVolume = 0.4f;
    private float lastHitTime;
    [SerializeField] private float hitCooldown = 0.1f;

    [Header("Hit Tuning")]
    [SerializeField] private float minHitSpeedKmh = 10f;   //ignore tiny bumps
    [SerializeField] private float horizontalForce = 12f;  //forward shove
    [SerializeField] private float verticalForce = 7f;     //pop upward

    [Header("Upgrade Variables")]
    public bool spikeUpgrade;
    public int spikeDamage = 20;
    public bool pushBumperUpgrade;
    public int pushDamage = 20;

    private void Awake()
    {
        if (!taxiRb)
        {
            taxiRb = GetComponentInParent<Rigidbody>();
            if (!taxiRb)
                taxiRb = FindFirstObjectByType<S_TaxiPlayer_Test>().GetComponent<Rigidbody>();
        }

        if (!gm) 
            gm = FindFirstObjectByType<GameManager>();

        if (!cameraShake)
            cameraShake = FindFirstObjectByType<CameraShake>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collided with: " + other);

        float speedKmh = taxiRb ? taxiRb.linearVelocity.magnitude * 3.6f : 0f;
        if(speedKmh < minHitSpeedKmh) return;

        //Camera shake
        float shakeStrength = Mathf.InverseLerp(minHitSpeedKmh, 60f, speedKmh);

        //Hit Direction (for zombies)
        Vector3 taxiForward = taxiRb ? taxiRb.transform.forward : transform.forward;

        //--- Zombie hit ---
        Zombie zombie = other.GetComponentInParent<Zombie>();
        if(zombie != null)
        {
            zombie.OnHitByTaxi(taxiForward, speedKmh, horizontalForce, verticalForce);

            if (spikeUpgrade && speedKmh < 20)
            {
                zombie.zombieHealth -= spikeDamage;
                Debug.Log("UpgradeManager: Spike Bumpers attack!");
            }
            if (pushBumperUpgrade && speedKmh > 50)
            {
                zombie.zombieHealth -= pushDamage;
                Debug.Log("UpgradeManager: Side Bumpers attack!");
            }

            gm.UpdateScore(1);
            PlayThud(speedKmh);
            cameraShake?.Shake(0.15f, 0.2f * shakeStrength);
            return;
        }

        //--- Environment hit (buildings, etc.) ---
        if (!other.isTrigger && other.CompareTag("Building"))
        {
            PlayThud(speedKmh);
            cameraShake?.Shake(0.15f, 0.2f * shakeStrength);
        }
    }

    private void PlayThud(float speedKmh)
    {
        if (audioSource == null) return;
        if (thudSounds == null || thudSounds.Length == 0) return;

        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        AudioClip clip = thudSounds[Random.Range(0, thudSounds.Length)];

        float speed01 = Mathf.InverseLerp(minHitSpeedKmh, 60f, speedKmh);
        float volume = Mathf.Lerp(minVolume, maxVolume, speed01);

        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(clip, volume);
        //audioSource.pitch = 1f;
    }
}
