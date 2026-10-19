using UnityEngine;

public class UpgradeShop : MonoBehaviour
{
    [SerializeField] S_TaxiPlayer_Test taxi;
    [SerializeField] GameObject shopCanvas;
    public UpgradeManager manager;
    public GameManager gm;

    int upgradePoints = 0;

    bool isRefueling = false;

    [Header("Audio")]
    [SerializeField] private AudioSource refuelAudioSource;
    [SerializeField] private AudioClip refuelClip;
    [SerializeField] private AudioClip refuelCompleteClip;
    private bool refuelSoundPlaying = false;
    private bool playedCompleteSound = false;

    //Miguel note
    /*
     * I overcomplicated the sound of this script like crazy. It was all because it kept playing
     * the clicking sound and I was trying to stop it from looping. Turns out it was because I had
     * the refueling sound swapped with the refueling complete sound, and the complete clip field was empty.
     * 
     * I'm tired. Remind me to clean this up. It's very ugly. But I need shut eye for tomorrow's big showcase. G'night.
     */

    private void Awake()
    {
        if(gm == null)
            gm = FindFirstObjectByType<GameManager>();
        if (manager == null)
            manager = GetComponent<UpgradeManager>();
        if(taxi == null)
            taxi = FindFirstObjectByType<S_TaxiPlayer_Test>();
        if (refuelAudioSource == null)
            refuelAudioSource = GetComponent<AudioSource>();

        if (refuelAudioSource != null)
        {
            refuelAudioSource.playOnAwake = false;
            refuelAudioSource.loop = true;
        }
        shopCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<S_TaxiPlayer_Test>())
        {
            playedCompleteSound = false;
            isRefueling = true;

            if (taxi != null && taxi.fuelTank < taxi.fuelTankMax)
                StartRefuelSound();

            if (upgradePoints > 0)
            {
                shopCanvas.SetActive(true);
                manager.RandomizeUpgrades();
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<S_TaxiPlayer_Test>())
        {
            StopRefuelSound();
            shopCanvas.SetActive(false);
            isRefueling = false;
            playedCompleteSound = false;
        }
    }

    private void StartRefuelSound()
    {
        if (refuelSoundPlaying) return;
        if (refuelAudioSource == null || refuelClip == null) return;

        refuelAudioSource.clip = refuelClip;
        refuelAudioSource.loop = true;
        refuelAudioSource.Play();
        refuelSoundPlaying = true;
    }

    private void StopRefuelSound()
    {
        if (!refuelSoundPlaying) return;
        if (refuelAudioSource == null) return;

        //Shouldn't be here, but this is just for now
        PlayRefuelCompleteSound();

        refuelAudioSource.Stop();
        refuelSoundPlaying = false;

    }

    //Lazy Refuel
    public void RefillFuelToMax()
    {
        if (!taxi) return;
        taxi.fuelTank = taxi.fuelTankMax;
    }

    public void UpdateUpgradePoints(int points)
    {
        upgradePoints += points;
        gm.UpgradeCounterText.text = "Upgrade Points: " + upgradePoints.ToString();
    }

    private void PlayRefuelCompleteSound()
    {
        if (refuelAudioSource == null || refuelCompleteClip == null) return;

        refuelAudioSource.PlayOneShot(refuelCompleteClip);
    }

    private void Update()
    {
        if (manager.upgradeChosen)
        {
            if (upgradePoints > 0)
                manager.RandomizeUpgrades();

            manager.upgradeChosen = false;
            RefillFuelToMax();
            upgradePoints -= 1; //Could be more depending on the upgrade
            gm.UpgradeCounterText.text = "Upgrade Points: " + upgradePoints.ToString();
        }

        if (isRefueling && taxi != null)
        {
            if (taxi.fuelTank < taxi.fuelTankMax)
            {
                taxi.fuelTank += 10f * Time.deltaTime;

                if (!refuelSoundPlaying)
                    StartRefuelSound();

                if (taxi.fuelTank >= taxi.fuelTankMax)
                {
                    taxi.fuelTank = taxi.fuelTankMax;
                    StopRefuelSound();

                    if (!playedCompleteSound)
                    {
                        PlayRefuelCompleteSound();
                        playedCompleteSound = true;
                    }
                }
            }
        }
    }
}
