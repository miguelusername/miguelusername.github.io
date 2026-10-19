using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class Upgrades : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private S_TaxiPlayer_Test taxi;

    [Header("Components")]
    [SerializeField] private GameManager gm;

    [Header("Upgrade Amounts")]
    [SerializeField] private float speedIncreaseKmh = 5f;
    [SerializeField] private float fuelMaxIncrease = 5f;

    [Header("UI")]
    [SerializeField] private Button speedButton;
    [SerializeField] private Button fuelButton;

    [Header("Stats UI")]
    [SerializeField] private TextMeshProUGUI statsText;

    private bool _canUpgrade;

    private int _speedUpgrades;
    private int _fuelUpgrades;

    private void Awake()
    {
        if (!taxi) taxi = FindFirstObjectByType<S_TaxiPlayer_Test>();
        if (!taxi) Debug.LogError("Upgrades: Could not find S_TaxiPlayer_Test in scene.");
        if (!gm) gm = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        //Player shouldn't be able to upgrade from the get go
        SetUpgradeButtons(false);
        UpdateStatsUI();
    }

    //We call this from RideManager when the player successfully drops off
    public void OnSuccessfulDropoff()
    {
        SetUpgradeButtons(true);
    }

    void SetUpgradeButtons(bool enabled)
    {
        _canUpgrade = enabled;

        if (speedButton) speedButton.interactable = enabled;
        if (fuelButton) fuelButton.interactable = enabled;
    }

    //Upgrades
    public void UpgradeSpeed()
    {
        if (!_canUpgrade || !taxi) return;

        taxi.maxSpeedKmh += speedIncreaseKmh;

        _speedUpgrades++;
        UpdateStatsUI();

        SetUpgradeButtons(false);
    }

    public void UpgradeFuelMax()
    {
        if (!_canUpgrade || !taxi) return;

        taxi.fuelTankMax += fuelMaxIncrease;
        taxi.fuelTank = Mathf.Clamp(taxi.fuelTank, 0f, taxi.fuelTankMax);

        _fuelUpgrades++;
        UpdateStatsUI();

        SetUpgradeButtons(false);
    }

    //Refuel
    public void RefillFuelToMax()
    {
        if (!taxi) return;
        taxi.fuelTank = taxi.fuelTankMax;
    }

    //Stats
    void UpdateStatsUI()
    {
        if (!statsText) return;

        statsText.text =
            $"Speed: {_speedUpgrades}\n" +
            $"Fuel: {_fuelUpgrades}";

        //Grant player some score
        gm.UpdateScore(50);
    }
}
