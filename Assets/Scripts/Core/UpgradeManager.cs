using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UpgradeManager - manages all player upgrades.
/// Singleton that stores upgrade data and applies bonuses.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Definitions")]
    [SerializeField] private List<UpgradeManagerDefinition> upgradeDefinitions = new List<UpgradeManagerDefinition>();

    // Runtime upgrade data
    private Dictionary<string, UpgradeData> upgrades = new Dictionary<string, UpgradeData>();

    // Cached bonus values
    private float spiderHealthBonus = 0f;
    private float spiderDamageBonus = 0f;
    private float spiderSpeedBonus = 0f;
    private float spiderRangeBonus = 0f;
    private int spiderCargoBonus = 0;
    private int armyLimitBonus = 0;
    private float armyHPBonusPercent = 0f;
    private float armyDamageBonusPercent = 0f;
    private int baseStorageBonus = 0;
    private float baseFarmEfficiencyBonus = 0f;

    // Properties for accessing bonuses
    public float SpiderHealthBonus => spiderHealthBonus;
    public float SpiderDamageBonus => spiderDamageBonus;
    public float SpiderSpeedBonus => spiderSpeedBonus;
    public float SpiderRangeBonus => spiderRangeBonus;
    public int SpiderCargoBonus => spiderCargoBonus;
    public int ArmyLimitBonus => armyLimitBonus;
    public float ArmyHPBonusPercent => armyHPBonusPercent;
    public float ArmyDamageBonusPercent => armyDamageBonusPercent;
    public int BaseStorageBonus => baseStorageBonus;
    public float BaseFarmEfficiencyBonus => baseFarmEfficiencyBonus;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUpgrades();
    }

    private void InitializeUpgrades()
    {
        // Initialize from definitions or create defaults
        if (upgradeDefinitions.Count > 0)
        {
            foreach (var def in upgradeDefinitions)
            {
                upgrades[def.id] = def.ToUpgradeData();
            }
        }
        else
        {
            // Create default upgrades
            CreateDefaultUpgrades();
        }

        RecalculateBonuses();
    }

    private void CreateDefaultUpgrades()
    {
        // Spider upgrades
        upgrades["spider_health"] = new UpgradeData
        {
            id = "spider_health",
            displayName = "Spider HP",
            descriptionFormat = "Max HP +{0}",
            maxLevel = 10,
            baseCostSkulls = 5,
            baseCostMeat = 10,
            baseValue = 0,
            valuePerLevel = 20
        };

        upgrades["spider_damage"] = new UpgradeData
        {
            id = "spider_damage",
            displayName = "Spider Damage",
            descriptionFormat = "Attack +{0}",
            maxLevel = 10,
            baseCostSkulls = 5,
            baseCostGold = 5,
            baseValue = 0,
            valuePerLevel = 2
        };

        upgrades["spider_speed"] = new UpgradeData
        {
            id = "spider_speed",
            displayName = "Spider Speed",
            descriptionFormat = "Move Speed +{0}",
            maxLevel = 5,
            baseCostSkulls = 10,
            baseCostMeat = 20,
            baseValue = 0,
            valuePerLevel = 0.5f
        };

        upgrades["spider_range"] = new UpgradeData
        {
            id = "spider_range",
            displayName = "Attack Range",
            descriptionFormat = "Range +{0}",
            maxLevel = 5,
            baseCostSkulls = 8,
            baseCostWood = 15,
            baseValue = 0,
            valuePerLevel = 0.3f
        };

        upgrades["spider_cargo"] = new UpgradeData
        {
            id = "spider_cargo",
            displayName = "Cargo Capacity",
            descriptionFormat = "Cargo +{0}",
            maxLevel = 10,
            baseCostSkulls = 3,
            baseCostWood = 10,
            baseValue = 0,
            valuePerLevel = 5
        };

        // Army upgrades
        upgrades["army_limit"] = new UpgradeData
        {
            id = "army_limit",
            displayName = "Army Size",
            descriptionFormat = "Max Army +{0}",
            maxLevel = 10,
            baseCostSkulls = 10,
            baseCostMeat = 20,
            baseCostGold = 5,
            baseValue = 0,
            valuePerLevel = 2
        };

        upgrades["army_hp_bonus"] = new UpgradeData
        {
            id = "army_hp_bonus",
            displayName = "Unit HP",
            descriptionFormat = "Unit HP +{0}%",
            maxLevel = 10,
            baseCostSkulls = 5,
            baseCostMeat = 15,
            baseValue = 0,
            valuePerLevel = 5
        };

        upgrades["army_damage_bonus"] = new UpgradeData
        {
            id = "army_damage_bonus",
            displayName = "Unit Damage",
            descriptionFormat = "Unit Damage +{0}%",
            maxLevel = 10,
            baseCostSkulls = 5,
            baseCostGold = 10,
            baseValue = 0,
            valuePerLevel = 5
        };

        // Base upgrades
        upgrades["base_storage"] = new UpgradeData
        {
            id = "base_storage",
            displayName = "Storage",
            descriptionFormat = "Storage +{0}",
            maxLevel = 10,
            baseCostWood = 20,
            baseCostGold = 5,
            baseValue = 0,
            valuePerLevel = 50
        };

        upgrades["base_farm_efficiency"] = new UpgradeData
        {
            id = "base_farm_efficiency",
            displayName = "Farm Efficiency",
            descriptionFormat = "Farm Output +{0}%",
            maxLevel = 10,
            baseCostWood = 15,
            baseCostMeat = 10,
            baseValue = 0,
            valuePerLevel = 10
        };
    }

    public UpgradeData GetUpgradeData(string upgradeId)
    {
        return upgrades.TryGetValue(upgradeId, out var data) ? data : null;
    }

    public bool TryPurchaseUpgrade(string upgradeId)
    {
        if (!upgrades.TryGetValue(upgradeId, out var data))
        {
            Debug.LogWarning($"[UpgradeManager] Unknown upgrade: {upgradeId}");
            return false;
        }

        if (data.IsMaxLevel)
        {
            Debug.Log($"[UpgradeManager] {upgradeId} is already at max level");
            return false;
        }

        if (!data.CanAfford())
        {
            Debug.Log($"[UpgradeManager] Cannot afford {upgradeId}");
            return false;
        }

        // Deduct resources
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpendResources(
                data.GetCostSkulls(),
                data.GetCostMeat(),
                data.GetCostWood(),
                data.GetCostGold()
            );
        }

        // Increase level
        data.currentLevel++;
        Debug.Log($"[UpgradeManager] Upgraded {upgradeId} to level {data.currentLevel}");

        // Recalculate bonuses
        RecalculateBonuses();

        // Notify systems
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUpgradePurchased(upgradeId, data.currentLevel);
        }

        return true;
    }

    private void RecalculateBonuses()
    {
        // Spider bonuses
        spiderHealthBonus = GetUpgradeValue("spider_health");
        spiderDamageBonus = GetUpgradeValue("spider_damage");
        spiderSpeedBonus = GetUpgradeValue("spider_speed");
        spiderRangeBonus = GetUpgradeValue("spider_range");
        spiderCargoBonus = Mathf.RoundToInt(GetUpgradeValue("spider_cargo"));

        // Army bonuses
        armyLimitBonus = Mathf.RoundToInt(GetUpgradeValue("army_limit"));
        armyHPBonusPercent = GetUpgradeValue("army_hp_bonus") / 100f;
        armyDamageBonusPercent = GetUpgradeValue("army_damage_bonus") / 100f;

        // Base bonuses
        baseStorageBonus = Mathf.RoundToInt(GetUpgradeValue("base_storage"));
        baseFarmEfficiencyBonus = GetUpgradeValue("base_farm_efficiency") / 100f;

        Debug.Log($"[UpgradeManager] Bonuses recalculated - ArmyLimit: +{armyLimitBonus}, SpiderHP: +{spiderHealthBonus}");
    }

    private float GetUpgradeValue(string upgradeId)
    {
        if (upgrades.TryGetValue(upgradeId, out var data))
        {
            return data.GetCurrentValue();
        }
        return 0f;
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        if (upgrades.TryGetValue(upgradeId, out var data))
        {
            return data.currentLevel;
        }
        return 0;
    }
}

/// <summary>
/// Serializable upgrade definition for Inspector setup (UpgradeManager version).
/// </summary>
[System.Serializable]
public class UpgradeManagerDefinition
{
    public string id;
    public string displayName;
    public string descriptionFormat;
    public Sprite icon;
    public int maxLevel = 10;
    public int baseCostSkulls;
    public int baseCostMeat;
    public int baseCostWood;
    public int baseCostGold;
    public float costMultiplierPerLevel = 1.5f;
    public float baseValue;
    public float valuePerLevel;

    public UpgradeData ToUpgradeData()
    {
        return new UpgradeData
        {
            id = id,
            displayName = displayName,
            descriptionFormat = descriptionFormat,
            icon = icon,
            maxLevel = maxLevel,
            currentLevel = 0,
            baseCostSkulls = baseCostSkulls,
            baseCostMeat = baseCostMeat,
            baseCostWood = baseCostWood,
            baseCostGold = baseCostGold,
            costMultiplierPerLevel = costMultiplierPerLevel,
            baseValue = baseValue,
            valuePerLevel = valuePerLevel
        };
    }
}
