using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Player upgrade system per GDD.
/// Handles progression of player stats:
/// - HP, Damage, Cargo Capacity, Move Speed
/// - Cocooning Time (skull collection), Aggro Radius
/// - Army Limit, Armor, HP Regen, XP Bonus, Drop Bonus
///
/// Formulas from GDD:
/// NewValue = Floor(InitialValue + IncrementValue * CurrentStatLevel)
/// or
/// NewValue = Floor(InitialValue * IncrementValue ^ CurrentStatLevel)
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    [Header("Upgrade Definitions")]
    [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private SkullCollector skullCollector;

    // Current upgrade levels
    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();

    // Events
    public event Action<UpgradeType, int> OnUpgradePurchased;  // type, newLevel
    public event Action<UpgradeType, float> OnStatChanged;     // type, newValue

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeUpgradeLevels();
    }

    private void Start()
    {
        FindPlayerComponents();
        ApplyAllUpgrades();
    }

    private void InitializeUpgradeLevels()
    {
        // Initialize all upgrade types to level 0
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            upgradeLevels[type] = 0;
        }
    }

    private void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
            if (playerController == null) playerController = player.GetComponent<PlayerController>();
            if (playerCombat == null) playerCombat = player.GetComponent<PlayerCombat>();
            if (skullCollector == null) skullCollector = player.GetComponent<SkullCollector>();
        }
    }

    /// <summary>
    /// Get upgrade definition by type.
    /// </summary>
    public UpgradeDefinition GetUpgradeDefinition(UpgradeType type)
    {
        return upgrades.Find(u => u.upgradeType == type);
    }

    /// <summary>
    /// Get current level of an upgrade.
    /// </summary>
    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels.TryGetValue(type, out int level) ? level : 0;
    }

    /// <summary>
    /// Get current value of a stat based on upgrade level.
    /// </summary>
    public float GetStatValue(UpgradeType type)
    {
        UpgradeDefinition def = GetUpgradeDefinition(type);
        if (def == null) return 0f;

        int level = GetUpgradeLevel(type);
        return CalculateValue(def, level);
    }

    /// <summary>
    /// Get value at next level.
    /// </summary>
    public float GetNextStatValue(UpgradeType type)
    {
        UpgradeDefinition def = GetUpgradeDefinition(type);
        if (def == null) return 0f;

        int level = GetUpgradeLevel(type);
        if (level >= def.maxLevel) return GetStatValue(type);

        return CalculateValue(def, level + 1);
    }

    /// <summary>
    /// Get cost to upgrade to next level.
    /// </summary>
    public UpgradeCost GetUpgradeCost(UpgradeType type)
    {
        UpgradeDefinition def = GetUpgradeDefinition(type);
        if (def == null) return new UpgradeCost();

        int level = GetUpgradeLevel(type);
        if (level >= def.maxLevel) return new UpgradeCost();

        return CalculateCost(def, level + 1);
    }

    /// <summary>
    /// Check if player can afford upgrade.
    /// </summary>
    public bool CanAffordUpgrade(UpgradeType type)
    {
        if (GameManager.Instance == null) return false;

        UpgradeCost cost = GetUpgradeCost(type);
        return GameManager.Instance.Skulls >= cost.skulls &&
               GameManager.Instance.Meat >= cost.meat &&
               GameManager.Instance.Wood >= cost.wood &&
               GameManager.Instance.Gold >= cost.gold;
    }

    /// <summary>
    /// Check if upgrade is at max level.
    /// </summary>
    public bool IsMaxLevel(UpgradeType type)
    {
        UpgradeDefinition def = GetUpgradeDefinition(type);
        if (def == null) return true;

        return GetUpgradeLevel(type) >= def.maxLevel;
    }

    /// <summary>
    /// Purchase an upgrade.
    /// </summary>
    public bool PurchaseUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type))
        {
            Debug.Log($"[UpgradeSystem] {type} is at max level");
            return false;
        }

        if (!CanAffordUpgrade(type))
        {
            Debug.Log($"[UpgradeSystem] Cannot afford {type} upgrade");
            return false;
        }

        // Deduct cost
        UpgradeCost cost = GetUpgradeCost(type);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpendResources(cost.skulls, cost.meat, cost.wood, cost.gold);
        }

        // Increase level
        upgradeLevels[type]++;
        int newLevel = upgradeLevels[type];

        // Apply the upgrade
        ApplyUpgrade(type);

        // Fire event
        OnUpgradePurchased?.Invoke(type, newLevel);

        Debug.Log($"[UpgradeSystem] Purchased {type} upgrade, now level {newLevel}");
        return true;
    }

    /// <summary>
    /// Apply a specific upgrade to player.
    /// </summary>
    private void ApplyUpgrade(UpgradeType type)
    {
        float value = GetStatValue(type);

        switch (type)
        {
            case UpgradeType.MaxHP:
                if (playerHealth != null)
                {
                    playerHealth.SetMaxHealth(Mathf.RoundToInt(value));
                }
                break;

            case UpgradeType.Damage:
                if (playerCombat != null)
                {
                    playerCombat.SetDamage(value);
                }
                break;

            case UpgradeType.CargoCapacity:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetCargoCapacity(Mathf.RoundToInt(value));
                }
                break;

            case UpgradeType.MoveSpeed:
                if (playerController != null)
                {
                    playerController.MoveSpeed = value;
                }
                break;

            case UpgradeType.CocooningTime:
                // Skull collection duration - handled by SkullCollector
                // Lower is better, so we might want inverse
                break;

            case UpgradeType.AggroRadius:
                if (playerController != null)
                {
                    playerController.AutoAttackRange = value;
                }
                break;

            case UpgradeType.ArmyLimit:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetArmyLimit(Mathf.RoundToInt(value));
                }
                break;

            case UpgradeType.Armor:
                if (playerHealth != null)
                {
                    playerHealth.SetArmor(value);
                }
                break;

            case UpgradeType.HPRegen:
                if (playerHealth != null)
                {
                    playerHealth.SetRegenRate(value);
                }
                break;

            case UpgradeType.XPBonus:
                // Applied through GameManager when XP is gained
                break;

            case UpgradeType.DropBonus:
                // Applied when enemies drop loot
                break;
        }

        OnStatChanged?.Invoke(type, value);
    }

    /// <summary>
    /// Apply all upgrades (call on game load).
    /// </summary>
    public void ApplyAllUpgrades()
    {
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            if (upgradeLevels[type] > 0)
            {
                ApplyUpgrade(type);
            }
        }
    }

    /// <summary>
    /// Calculate stat value based on formula.
    /// </summary>
    private float CalculateValue(UpgradeDefinition def, int level)
    {
        if (def.useMultiplicativeFormula)
        {
            // NewValue = Floor(InitialValue * IncrementValue ^ CurrentStatLevel)
            return Mathf.Floor(def.baseValue * Mathf.Pow(def.incrementValue, level));
        }
        else
        {
            // NewValue = Floor(InitialValue + IncrementValue * CurrentStatLevel)
            return Mathf.Floor(def.baseValue + def.incrementValue * level);
        }
    }

    /// <summary>
    /// Calculate cost for a level.
    /// </summary>
    private UpgradeCost CalculateCost(UpgradeDefinition def, int targetLevel)
    {
        UpgradeCost cost = new UpgradeCost();

        // Cost increases with level
        float costMultiplier = 1f + (targetLevel - 1) * def.costScaling;

        cost.skulls = Mathf.RoundToInt(def.baseCost.skulls * costMultiplier);
        cost.meat = Mathf.RoundToInt(def.baseCost.meat * costMultiplier);
        cost.wood = Mathf.RoundToInt(def.baseCost.wood * costMultiplier);
        cost.gold = Mathf.RoundToInt(def.baseCost.gold * costMultiplier);

        return cost;
    }

    /// <summary>
    /// Get XP bonus multiplier.
    /// </summary>
    public float GetXPBonusMultiplier()
    {
        float bonus = GetStatValue(UpgradeType.XPBonus);
        return 1f + (bonus / 100f); // bonus is percentage
    }

    /// <summary>
    /// Get drop bonus multiplier.
    /// </summary>
    public float GetDropBonusMultiplier()
    {
        float bonus = GetStatValue(UpgradeType.DropBonus);
        return 1f + (bonus / 100f); // bonus is percentage
    }

    /// <summary>
    /// Save upgrade levels (for persistence).
    /// </summary>
    public Dictionary<UpgradeType, int> GetSaveData()
    {
        return new Dictionary<UpgradeType, int>(upgradeLevels);
    }

    /// <summary>
    /// Load upgrade levels (for persistence).
    /// </summary>
    public void LoadSaveData(Dictionary<UpgradeType, int> data)
    {
        if (data != null)
        {
            upgradeLevels = new Dictionary<UpgradeType, int>(data);
            ApplyAllUpgrades();
        }
    }
}

/// <summary>
/// Types of player upgrades per GDD.
/// </summary>
public enum UpgradeType
{
    MaxHP,          // Health points
    Damage,         // Attack damage
    CargoCapacity,  // Max cargo load
    MoveSpeed,      // Movement speed
    CocooningTime,  // Skull collection speed
    AggroRadius,    // Auto-attack range
    ArmyLimit,      // Max army units
    Armor,          // Damage reduction
    HPRegen,        // HP regeneration per second
    XPBonus,        // % bonus XP from enemies
    DropBonus       // % bonus drop from enemies
}

/// <summary>
/// Cost to purchase an upgrade.
/// </summary>
[System.Serializable]
public struct UpgradeCost
{
    public int skulls;
    public int meat;
    public int wood;
    public int gold;

    public override string ToString()
    {
        var parts = new List<string>();
        if (skulls > 0) parts.Add($"{skulls} Skulls");
        if (meat > 0) parts.Add($"{meat} Meat");
        if (wood > 0) parts.Add($"{wood} Wood");
        if (gold > 0) parts.Add($"{gold} Gold");
        return string.Join(", ", parts);
    }
}

/// <summary>
/// Definition for a single upgrade type.
/// </summary>
[System.Serializable]
public class UpgradeDefinition
{
    public UpgradeType upgradeType;
    public string displayName;
    [TextArea] public string description;

    [Header("Value Calculation")]
    public float baseValue;          // Starting value at level 0
    public float incrementValue;     // How much to add/multiply per level
    public bool useMultiplicativeFormula; // True = multiply, False = add
    public int maxLevel = 10;

    [Header("Cost")]
    public UpgradeCost baseCost;     // Cost for level 1
    public float costScaling = 0.5f; // Cost increase per level (0.5 = 50% more each level)

    [Header("UI")]
    public Sprite icon;
}
