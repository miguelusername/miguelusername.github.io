using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RideManager : MonoBehaviour
{
    [Header("Refs")]
    public PassengerSpawner passengerSpawner;
    public DropoffPointSpawner dropoffPointSpawner;
    public Upgrades upgrades;
    public GameObject dropoffMarkerPrefab;
    public GameManager gm;
    public UpgradeShop upgradeShop;

    private Passenger _targetPassenger;
    private GameObject _activeDropoffMarker;
    private bool _hasPassenger;

    private Passenger _activePassenger;
    private GameObject _activeDropoff;

    public PassengerPointerUI pointerUI;

    [Header("Audio")]
    public AudioClip doorCloseAudio;
    public AudioClip doorOpenAudio;
    public AudioSource carAudioSource;
    public AudioClip pickupSound;
    public AudioClip dropoffSound;

    private bool isStart = false;

    IEnumerator Start()
    {
        if (!passengerSpawner) passengerSpawner = FindFirstObjectByType<PassengerSpawner>();
        if (!dropoffPointSpawner) dropoffPointSpawner = FindFirstObjectByType<DropoffPointSpawner>();
        if (!pointerUI) pointerUI = FindFirstObjectByType<PassengerPointerUI>();
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        if (!upgradeShop) FindFirstObjectByType(typeof(UpgradeShop));


        yield return null; //wait one frame so spawners can run Start()

        if (!upgrades) upgrades = FindFirstObjectByType<Upgrades>();
        isStart = true;
        PickNewPassenger();
        isStart = false;
    }


    void PickNewPassenger()
    {
        _hasPassenger = false;

        //off previous active passenger marker + passenger
        if (_activePassenger != null)
        {
            _activePassenger.SetAsTarget(false);
            _activePassenger.gameObject.SetActive(false);
            _activePassenger = null;
        }

        //make sure no dropoff is active at this moment
        if (_activeDropoff != null)
        {
            _activeDropoff.SetActive(false);
            _activeDropoff = null;
        }

        _targetPassenger = GetRandomPassenger();
        if (_targetPassenger == null)
        {
            Debug.LogWarning("No passengers available.");
            return;
        }

        _activePassenger = _targetPassenger;
        _activePassenger.gameObject.SetActive(true);
        _activePassenger.SetAsTarget(true);

        if (pointerUI != null)
        {
            pointerUI.target = _activePassenger.transform;
            pointerUI.gameObject.SetActive(true);
            if (pointerUI.arrow) pointerUI.arrow.gameObject.SetActive(true);
        }

        if (doorOpenAudio != null && !isStart)
            StartCoroutine(PlayDoorOpenSoundDelayed(0.1f));

        //Debug.Log("Target passenger: " + _activePassenger.name);
    }


    Passenger GetRandomPassenger()
    {
        if (passengerSpawner == null || passengerSpawner.passengers.Count == 0) return null;

        for (int tries = 0; tries < 50; tries++)
        {
            var go = passengerSpawner.passengers[Random.Range(0, passengerSpawner.passengers.Count)];
            if (go != null)
                return go.GetComponent<Passenger>();  //or GetComponentInChildren<Passenger>()
        }

        return null;
    }

    //Called by passenger pickup trigger
    public void TryPickupPassenger(Passenger p)
    {
        if (_hasPassenger) return;
        if (p == null) return;
        if (p != _targetPassenger) return;

        _hasPassenger = true;

        if (doorCloseAudio != null)
            StartCoroutine(PlayDoorCloseSoundDelayed(0.1f));

        //remove passenger
        p.SetAsTarget(false);
        p.gameObject.SetActive(false);

        //choose a dropoff and activate it
        Transform point = dropoffPointSpawner.GetDropoffPoint(p.transform.position);
        if (point == null) return;

        _activeDropoff = point.gameObject;
        _activeDropoff.SetActive(true);

        //point arrow to dropoff
        if (pointerUI != null)
            pointerUI.target = _activeDropoff.transform;

        //Grant player some score
        gm.UpdateScore(100);

        Debug.Log("Picked up passenger");
    }


    //Called by DropoffCollider
    public void TryDropoff()
    {
        if (!_hasPassenger) return;

        _hasPassenger = false;

        if (_activeDropoff != null)
        {
            _activeDropoff.SetActive(false);
            _activeDropoff = null;
        }

        if (upgradeShop != null)
        {
            //Old upgrade system
            //upgrades.RefillFuelToMax();
            //upgrades.OnSuccessfulDropoff();
            upgradeShop.UpdateUpgradePoints(1);
        }

        Debug.Log("Dropped off passenger!");
        PickNewPassenger();
    }


    IEnumerator PlayDoorCloseSoundDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (doorCloseAudio != null)
            carAudioSource.PlayOneShot(doorCloseAudio);
        if (pickupSound != null)
            carAudioSource.PlayOneShot(pickupSound);
    }

    IEnumerator PlayDoorOpenSoundDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (doorOpenAudio != null)
            carAudioSource.PlayOneShot(doorOpenAudio);
        if (dropoffSound != null)
            carAudioSource.PlayOneShot(dropoffSound);
    }
}
