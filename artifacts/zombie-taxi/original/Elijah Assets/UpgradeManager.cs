using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class UpgradeManager : MonoBehaviour
{
    S_TaxiPlayer_Test taxi;
    UpgradeBase uBase;
    UpgradeControls controls;
    Bumper bumper;

    public bool upgradeChosen = false;
    public TextMeshProUGUI upgradeOneName;
    public TextMeshProUGUI upgradeTwoName;
    public TextMeshProUGUI upgradeThreeName;
    public TextMeshProUGUI upgradeDescription;

    public GameObject shopButtonOne, shopButtonTwo, shopButtonThree;

    public Sprite tailpipesIcon, maxSpeedIcon, fuelCapacityIcon, steeringIcon, nitroIcon, gunIcon, rocketIcon, spikesIcon, bumperIcon, nitroFuelIcon, gunHeatIcon, fortificationIcon;

    public List<Upgrade> allUpgrades;
    public List<Upgrade> shopSelection;
    public GameObject[] sideBumperHitboxes;
    private void Awake()
    {
        taxi = FindFirstObjectByType<S_TaxiPlayer_Test>().GetComponent<S_TaxiPlayer_Test>();
        uBase = FindFirstObjectByType<UpgradeBase>().GetComponent<UpgradeBase>();
        controls = FindFirstObjectByType<UpgradeControls>().GetComponent<UpgradeControls>();
        bumper = FindFirstObjectByType<Bumper>().GetComponent<Bumper>();
        allUpgrades = new List<Upgrade>();
        shopSelection = new List<Upgrade>();

        // Acceleration Rate, Machine Gun, Rocket Launcher, Spikes, Push Bumper 
        // Nitro, Nitro Upgrade, Ammo Upgrade, Fortification Upgrade, Donut Upgrade

        Upgrade tailpipes = new Upgrade(taxi.burnUpgrade, 1, 2, "Tailpipes ", "Decrease idle fuel burn, but increase burn while moving", tailpipesIcon);
        allUpgrades.Add(tailpipes);
        Upgrade maxSpeed = new Upgrade(taxi.maxSpeedKmh, 5, 4, "Max Speed ", "Increase your maximum Speed!", maxSpeedIcon);
        allUpgrades.Add(maxSpeed);
        Upgrade fuelCapacity = new Upgrade(taxi.fuelTankMax, 30, 4, "Fuel Increase ", "Increase your maximum Fuel Tank", fuelCapacityIcon);
        allUpgrades.Add(fuelCapacity);
        Upgrade steering = new Upgrade(taxi.steerAtMaxKmh, 10, 3, "Steering Increase ", "Increase your turning angle at Max Speed", steeringIcon);
        allUpgrades.Add(steering);
        Upgrade nitroAbility = new Upgrade(controls.nitroUpgrade, "Nitro Ability ", "Use a Nitro Boost with SHIFT!", nitroIcon);
        allUpgrades.Add(nitroAbility);
        Upgrade machineGun = new Upgrade(controls.gunUpgrade, "Machine Gun Turret ", "Add a Machine Gun to your car! Fire with LEFT CLICK!", gunIcon);
        allUpgrades.Add(machineGun);
        Upgrade rocketLauncher = new Upgrade(controls.rocketUpgrade, "Rocket Launcher Turret ", "Add a Rocket Launcher to your car! Fire with RIGHT CLICK!", rocketIcon);
        allUpgrades.Add(rocketLauncher);
        Upgrade spikeBumper = new Upgrade(bumper.spikeUpgrade, "Spike Bumper ", "Attach Spikes to your car and deal more damage at lower speeds!", spikesIcon);
        allUpgrades.Add(spikeBumper);
        Upgrade pushBumper = new Upgrade(bumper.pushBumperUpgrade, "Side Bumpers ", "Add more Bumpers to you car and deal more damage at higher speeds!", bumperIcon);
        allUpgrades.Add(pushBumper);
        Upgrade fortification = new Upgrade(taxi.zombieDamageChance, 15, 4, "Fortification ", "Increase your defenses against the zombies!", fortificationIcon);
        allUpgrades.Add(fortification);
    }

    public void RandomizeUpgrades()
    {
        List<Upgrade> tempList = new List<Upgrade>();
        tempList.AddRange(allUpgrades);

        shopSelection.Clear();
        int amountOfUpgradesInShop = 0;
        if (tempList.Count > 2)
            amountOfUpgradesInShop = 3;
        else amountOfUpgradesInShop = tempList.Count;
        Debug.Log("Upgrades in Shop: " + amountOfUpgradesInShop);

        for (int count = 0; count < amountOfUpgradesInShop; count++)
        {
            int rand = UnityEngine.Random.Range(0, amountOfUpgradesInShop - 1);

            if (tempList[rand].maxLevel == tempList[rand].currentLevel) // There's an error here when less than 3 upgrades
            {
                tempList.Remove(tempList[rand]);
                rand = UnityEngine.Random.Range(0, tempList.Count);
            }
            Debug.Log("Selected Upgrade: " + tempList[rand].nameOfUpgrade); // There's an error here when less than 3 upgrades
            shopSelection.Add(tempList[rand]);
            tempList.Remove(tempList[rand]);
        }

        if (shopSelection[0] != null)
            shopButtonOne.SetActive(true);
        else shopButtonOne.SetActive(false);
        if (shopSelection[1] != null)
            shopButtonOne.SetActive(true);
        else shopButtonOne.SetActive(false);
        if (shopSelection[2] != null)
            shopButtonOne.SetActive(true);
        else shopButtonOne.SetActive(false);

        upgradeOneName.text = shopSelection[0]?.nameOfUpgrade + (shopSelection[0]?.currentLevel + 1);
        if (shopSelection[0]?.icon != null) shopButtonOne.GetComponentInChildren<Image>().sprite = shopSelection[0]?.icon;
        upgradeTwoName.text = shopSelection[1]?.nameOfUpgrade + (shopSelection[1]?.currentLevel + 1);
        if (shopSelection[1]?.icon != null) shopButtonTwo.GetComponentInChildren<Image>().sprite = shopSelection[1]?.icon;
        upgradeThreeName.text = shopSelection[2]?.nameOfUpgrade + (shopSelection[2]?.currentLevel + 1);
        if (shopSelection[2]?.icon != null) shopButtonThree.GetComponentInChildren<Image>().sprite = shopSelection[2]?.icon;
    }

    public void ShopUpgradeOneClick()
    {
        CheckStat(shopSelection[0]);
    }
    public void ShopUpgradeTwoClick()
    {
        CheckStat(shopSelection[1]);
    }
    public void ShopUpgradeThreeClick()
    {
        CheckStat(shopSelection[2]);
    }

    private void CheckStat(Upgrade shopSelection)
    {
        if (shopSelection.nameOfUpgrade == "Max Speed ")
            taxi.maxSpeedKmh = shopSelection.AddUpgrade();
        if (shopSelection.nameOfUpgrade == "Fuel Increase ")
            taxi.fuelTankMax = shopSelection.AddUpgrade();
        if (shopSelection.nameOfUpgrade == "Tailpipes ")
            taxi.burnUpgrade = shopSelection.AddUpgrade((GameObject)Resources.Load("UpgradeParts/Boost"), uBase.tailpipesLocation);
        if (shopSelection.nameOfUpgrade == "Steering Increase ")
            taxi.steerAtMaxKmh = shopSelection.AddUpgrade();
        if (shopSelection.nameOfUpgrade == "Nitro Ability ")
        {
            controls.nitroUpgrade = shopSelection.ActivateUpgrade();

            Upgrade nitroFuel = new Upgrade(controls.nitroFuelMax, 5, 3, "Nitro Fuel Increase ", "Increase the length of your nitro!", nitroFuelIcon);
            allUpgrades.Add(nitroFuel);
        }
        if (shopSelection.nameOfUpgrade == "Machine Gun Turret ")
        {
            controls.gunUpgrade = shopSelection.ActivateUpgrade((GameObject)Resources.Load("UpgradeParts/MachineGunTurret"), uBase.minigunLocation);

            Upgrade gunHeat = new Upgrade(controls.heatIncrease, 5, 5, "Increased Gun Cooling ", "Your guns overheat slower!", gunHeatIcon);
            allUpgrades.Add(gunHeat);
        }
        if (shopSelection.nameOfUpgrade == "Rocket Launcher Turret ")
        {
            controls.rocketUpgrade = shopSelection.ActivateUpgrade((GameObject)Resources.Load("UpgradeParts/RocketLauncherTurret"), uBase.rocketLocation);
        }
        if (shopSelection.nameOfUpgrade == "Spike Bumper ")
            bumper.spikeUpgrade = shopSelection.ActivateUpgrade((GameObject)Resources.Load("UpgradeParts/Bumber spikes"), uBase.spikesLocation);
        if (shopSelection.nameOfUpgrade == "Side Bumpers ")
        {
            bumper.pushBumperUpgrade = shopSelection.ActivateUpgrade((GameObject)Resources.Load("UpgradeParts/SideBumpers"), uBase.bumpersLocation);
            foreach (GameObject hitbox in sideBumperHitboxes)
                hitbox.SetActive(true);
        }
        if (shopSelection.nameOfUpgrade == "Fortification ")
            taxi.zombieDamageChance = shopSelection.AddUpgrade();
        if (shopSelection.nameOfUpgrade == "Increased Gun Cooling ")
        {
            controls.heatIncrease = shopSelection.AddUpgrade();

            controls.machinegunOverheat += controls.heatIncrease;
            controls.rocketOverheat += controls.heatIncrease;
            controls.machineGunHeatGauge.maxValue = controls.machinegunOverheat;
            controls.rocketLauncherHeatGauge.maxValue = controls.rocketOverheat;
        }
        if (shopSelection.nameOfUpgrade == "Nitro Fuel Increase ")
        {
            controls.nitroFuelMax = shopSelection.AddUpgrade();
            controls.nitroFuelGuage.maxValue = controls.nitroFuelMax;
        }

        if (shopSelection.currentLevel == shopSelection.maxLevel || shopSelection.boolToChange == true)
            allUpgrades.Remove(shopSelection);

        Debug.Log("UpgradeManager: Upgrade Chosen: " + shopSelection.nameOfUpgrade + shopSelection.currentLevel);

        upgradeChosen = true; // will turn off shop canvas in UpgradeShop
    }


    public void ShopUpgradeOneHover()
    {
        upgradeDescription.text = shopSelection[0].description;
    }
    public void ShopUpgradeTwoHover()
    {
        upgradeDescription.text = shopSelection[1].description;
    }
    public void ShopUpgradeThreeHover()
    {
        upgradeDescription.text = shopSelection[2].description;
    }
    public void HideDesc()
    {
        upgradeDescription.text = "";
    }
}

public class Upgrade
{
    public float statToChange;
    public bool boolToChange;
    public Sprite icon;

    public string nameOfUpgrade;
    public string description;

    public float increase;
    public float maxLevel;
    public float currentLevel = 0;

    public int costToUpgrade;

    public Upgrade(float statChange, float increaseBy, float max, string upgradeName, string desc, Sprite upgradeIcon)
    {
        statToChange = statChange;
        increase = increaseBy;
        maxLevel = max;
        nameOfUpgrade = upgradeName;
        description = desc;
        icon = upgradeIcon;
    }
    public Upgrade(bool statChange, string upgradeName, string desc, Sprite upgradeIcon)
    {
        boolToChange = statChange;
        nameOfUpgrade = upgradeName;
        description = desc;
        icon = upgradeIcon;
    }

    public float AddUpgrade()
    {
        if (currentLevel != maxLevel)
        {
            statToChange += increase;
            currentLevel++;
            costToUpgrade += (int)(currentLevel * increase * 10);
        }
        return statToChange;
    }
    public float AddUpgrade(GameObject addition, Transform location)
    {
        if (currentLevel != maxLevel)
        {
            AddAddition(addition, location);
            statToChange += increase;
            currentLevel++;
            costToUpgrade += (int)(currentLevel * increase * 10);
        }
        return statToChange;
    }
    public bool ActivateUpgrade()
    {
        if (!boolToChange)
        {
            boolToChange = true;
        }
        return boolToChange;
    }
    public bool ActivateUpgrade(GameObject addition, Transform location)
    {
        if (!boolToChange)
        {
            boolToChange = true;
            AddAddition(addition, location);
        }
        return boolToChange;
    }
    public bool CheckPrice(float tokens)
    {
        if (tokens - costToUpgrade >= 0)
        {
            return true;
        }
        return false;
    }
    public void AddAddition(GameObject prefab, Transform location)
    {
        GameObject addition = GameObject.Instantiate(prefab, location.position, location.rotation);
        addition.transform.parent = GameObject.FindFirstObjectByType<S_TaxiPlayer_Test>().transform;
    }

    public void AddButton(Sprite sprite, TextMeshProUGUI name, TextMeshProUGUI desc)
    {
        sprite = icon;
        name.text = nameOfUpgrade;
        desc.text = description;
    }
}
