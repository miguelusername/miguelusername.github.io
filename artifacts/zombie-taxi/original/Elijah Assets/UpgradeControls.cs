using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeControls : MonoBehaviour
{
    public InputActionReference nitro, machineGun, rocketLauncher;

    [Header("Nitro")]
    public bool nitroUpgrade;
    public Slider nitroFuelGuage;
    public float nitroFuel;
    public float nitroFuelMax = 5f;
    public float nitroForce = 10f;
    public float nitroCooldown = 3f;
    float nitroCooldownTemp;

    [Header("Machine Gun")]
    public bool gunUpgrade;
    public float machineGunHeat;
    public float machinegunOverheat = 30f;
    public bool gunOverheated = false;
    public float machineGunFiringDelay = 0.5f;
    public float machineGunDelayTemp;
    public float machineGunBulletSpeed = 3f;
    public Transform machineGunBulletSpawn;
    public Slider machineGunHeatGauge;

    [Header("Rocket Launcher")]
    public bool rocketUpgrade;
    public float rocketHeat;
    public float rocketOverheat = 5f;
    public bool rocketOverheated = false;
    public float rocketFiringDelay = 3f;
    public float rocketDelayTemp;
    public float rocketBulletSpeed = 5f;
    public Transform rocketBulletSpawn;
    public Slider rocketLauncherHeatGauge;
    public float heatIncrease = 0;

    [Header("Audio")]
    public AudioSource audioSource;
    [SerializeField] private AudioSource machineGunAudioSource; //Todo: have on separate sources so they don't interfere with eachother.
    [SerializeField] private AudioSource rocketAudioSource;
    public AudioClip gunShots;
    public AudioClip rocketShots;
    private bool machineGunLoopPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        /*
        if (machineGunBulletSpawn == null)
        {
            MachineGunSpawn mgSpawn = GetComponentInChildren<MachineGunSpawn>();
            if (mgSpawn != null)
                machineGunBulletSpawn = mgSpawn.transform;
        }

        if(rocketBulletSpawn == null)
        {
            RocketLauncherSpawn rocketSpawn = GetComponentInChildren<RocketLauncherSpawn>();
            if (rocketSpawn != null)
                rocketBulletSpawn = rocketSpawn.transform;
        }
        */

        nitroFuel = nitroFuelMax;
        nitroFuelGuage.gameObject.SetActive(false);
        nitroCooldownTemp = nitroCooldown;

        machineGunHeatGauge.gameObject.SetActive(false);
        rocketLauncherHeatGauge.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (nitroUpgrade)
        {
            nitroFuelGuage.gameObject.SetActive(true);
            nitroFuelGuage.value = nitroFuel;

            if (nitro.action.IsPressed() && nitroFuel > 0 && GetComponent<S_TaxiPlayer_Test>().isGrounded)
            {
                nitroCooldownTemp = nitroCooldown;
                nitroFuel -= 1 * Time.deltaTime;

                GetComponent<Rigidbody>().AddForce(transform.forward * nitroForce, ForceMode.Acceleration);
            }
            else
            {
                nitroCooldownTemp -= 1 * Time.deltaTime;
                if (nitroCooldownTemp <= 0)
                {
                    if (nitroFuel < nitroFuelMax)
                    {
                        nitroFuel += 1 * Time.deltaTime;
                    }
                    else nitroCooldownTemp = nitroCooldown;
                }
            }
        }

        if (gunUpgrade)
        {
            machineGunBulletSpawn = GetComponentInChildren<MachineGunSpawn>().transform;

            machineGunHeatGauge.gameObject.SetActive(true);
            Image gaugeColor = machineGunHeatGauge.GetComponentInChildren<Image>();
            machineGunHeatGauge.value = machineGunHeat;

            if (machineGunHeat <= 0)
                machineGunHeatGauge.gameObject.SetActive(false);
            else machineGunHeatGauge.gameObject.SetActive(true);
            if (!gunOverheated)
            {
                if (machineGunHeat < 10)
                    gaugeColor.color = Color.white;
                else if (machineGunHeat >= 10 && machineGunHeat < 20)
                    gaugeColor.color = Color.yellow;
                else if (machineGunHeat >= 20 && machineGunHeat < 30)
                    gaugeColor.color = Color.orange;
            }
            if (machineGunHeat >= machinegunOverheat)
            {
                gunOverheated = true;
                gaugeColor.color = Color.red;
            }
            if (!gunOverheated)
            {
                //Miguel note: fixed this section up a bit. Those bullets get so freakin' loud when they stack. So I have it loop instead :D
                if(machineGun.action.IsPressed())
                {
                    machineGunDelayTemp -= Time.deltaTime;

                    if(!machineGunLoopPlaying && audioSource != null && gunShots != null)
                    {
                        audioSource.clip = gunShots;
                        audioSource.loop = true;
                        audioSource.Play();
                        machineGunLoopPlaying = true;
                    }

                    if(machineGunDelayTemp <= 0)
                    {
                        machineGunDelayTemp = machineGunFiringDelay;
                        ShootProjectile((GameObject)Resources.Load("BulletPrefab"), machineGunBulletSpawn, machineGunBulletSpeed);
                        machineGunHeat++;
                    }
                }
                else
                {
                    machineGunDelayTemp = 0;

                    if(machineGunHeat >= 0)
                        machineGunHeat -= 5 * Time.deltaTime;

                    if(machineGunLoopPlaying && audioSource != null)
                    {
                        audioSource.Stop();
                        machineGunLoopPlaying = false;
                    }
                }
            }
            else
            {
                //Miguel note: fixed this part up too. Had to have it like this otherwise the loop could kee going while the gun isn't available
                machineGunDelayTemp = 0;
                machineGunHeat -= 8 * Time.deltaTime;

                if (machineGunLoopPlaying && audioSource != null)
                {
                    audioSource.Stop();
                    machineGunLoopPlaying = false;
                }

                if (machineGunHeat <= 0)
                    gunOverheated = false;
            }
        }

        if (rocketUpgrade)
        {
            rocketBulletSpawn = GetComponentInChildren<RocketLauncherSpawn>().transform;

            rocketLauncherHeatGauge.gameObject.SetActive(true);
            Image gaugeColor = rocketLauncherHeatGauge.GetComponentInChildren<Image>();
            rocketLauncherHeatGauge.value = rocketHeat;

            if (rocketHeat <= 0)
                rocketLauncherHeatGauge.gameObject.SetActive(false);
            else rocketLauncherHeatGauge.gameObject.SetActive(true);
            if (!rocketOverheated)
            {
                if (rocketHeat < 2)
                    gaugeColor.color = Color.white;
                else if (rocketHeat >= 2 && rocketHeat < 4)
                    gaugeColor.color = Color.yellow;
                else if (rocketHeat >= 4 && rocketHeat < 5)
                    gaugeColor.color = Color.orange;
            }
            if (rocketHeat >= rocketOverheat)
            {
                rocketOverheated = true;
                gaugeColor.color = Color.red;
            }
            if (!rocketOverheated)
            {
                if (rocketLauncher.action.IsPressed())
                {
                    rocketDelayTemp -= Time.deltaTime;

                    if (rocketDelayTemp <= 0)
                    {
                        rocketDelayTemp = rocketFiringDelay;
                        ShootProjectile((GameObject)Resources.Load("MissilePrefab"), rocketBulletSpawn, rocketBulletSpeed);
                        audioSource.PlayOneShot(rocketShots, 1f);
                        rocketHeat++;
                    }

                }
                else if (!rocketLauncher.action.IsPressed())
                {
                    rocketDelayTemp = 0;
                    if (rocketHeat >= 0)
                        rocketHeat -= 5 * Time.deltaTime;
                }
            }
            else
            {
                rocketDelayTemp = 0;
                rocketHeat -= 3 * Time.deltaTime;
                if (rocketHeat <= 0)
                    rocketOverheated = false;
            }
        }
    }

    void ShootProjectile(GameObject bulletType, Transform spawnLocation, float bulletSpeed)
    {
        var projectile = Instantiate(bulletType, spawnLocation.position, spawnLocation.transform.rotation);

        projectile.GetComponent<Rigidbody>().AddForce(spawnLocation.transform.forward * bulletSpeed + new Vector3(GetComponent<S_TaxiPlayer_Test>().speedKmh, 0, 0), ForceMode.Force);
    }

    private void OnEnable()
    {
        if (nitro != null) nitro.action.Enable();
        if (machineGun != null) machineGun.action.Enable();
        if (rocketLauncher != null) rocketLauncher.action.Enable();
    }
    private void OnDisable()
    {
        if (nitro != null) nitro.action.Disable();
        if (machineGun != null) machineGun.action.Disable();
        if (rocketLauncher != null) rocketLauncher.action.Disable();

        if(machineGunLoopPlaying && audioSource != null)
        {
            audioSource.Stop();
            machineGunLoopPlaying = false;
        }
    }
}
