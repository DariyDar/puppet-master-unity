using UnityEngine;

/// <summary>
/// ScriptableObject defining unit stats and configuration.
/// Create instances via Assets > Create > Puppet Master > Unit Config.
/// </summary>
[CreateAssetMenu(fileName = "NewUnitConfig", menuName = "Puppet Master/Unit Config")]
public class UnitConfig : ScriptableObject
{
    [Header("Identity")]
    public UnitType unitType = UnitType.Skull;
    public string displayName = "Skull";
    [TextArea] public string description = "Basic skeleton warrior.";

    [Header("Combat Stats")]
    public float maxHealth = 80f;
    public float damage = 15f;
    public float attackSpeed = 1.2f;      // attacks per second
    public float attackRange = 45f;       // units
    public float moveSpeed = 85f;         // units per second
    public bool isRanged = false;

    [Header("Behavior")]
    public float followDistance = 2f;
    public float returnDistance = 8f;
    public float detectionRange = 5f;

    [Header("Cost - Per GDD v5")]
    public int skullCost = 7;        // Skulls required
    public int meatCost = 0;         // Meat required
    public int woodCost = 0;         // Wood required
    public int goldCost = 0;         // Gold required
    public float productionTime = 30f;
    public int unlockLevel = 1;

    [Header("Special Ability")]
    public UnitAbilityType abilityType = UnitAbilityType.None;
    [Tooltip("Lifesteal: % of damage. Stun: duration in seconds. AoE: damage % to nearby. MindControl: duration.")]
    public float abilityValue = 0f;
    public float abilityCooldown = 0f;
    [Tooltip("For Gnome: triggers on every Nth attack")]
    public int abilityTriggerCount = 0;

    [Header("Visuals")]
    public GameObject prefab;
    public RuntimeAnimatorController animatorController;
    public Sprite icon;

    [Header("Projectile (for ranged)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 200f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public GameObject deathEffectPrefab;
    public GameObject abilityEffectPrefab;

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip abilitySound;

    /// <summary>
    /// Get stats with upgrade bonuses applied
    /// </summary>
    public UnitConfig GetWithUpgrades(int upgradeLevel)
    {
        float bonus = GetUpgradeBonus(upgradeLevel);

        // Create a runtime copy with bonuses
        UnitConfig upgraded = Instantiate(this);
        upgraded.maxHealth = maxHealth * (1f + bonus);
        upgraded.damage = damage * (1f + bonus);

        return upgraded;
    }

    private float GetUpgradeBonus(int level)
    {
        return level switch
        {
            1 => 0f,
            2 => 0.10f,
            3 => 0.20f,
            4 => 0.35f,
            5 => 0.50f,
            _ => 0f
        };
    }

    #region Static Factory Methods

    /// <summary>
    /// Creates Skull (basic melee) configuration - Per GDD v5
    /// Cost: 7 Skulls only
    /// </summary>
    public static UnitConfig CreateSkullConfig()
    {
        var config = CreateInstance<UnitConfig>();
        config.unitType = UnitType.Skull;
        config.displayName = "Skull";
        config.description = "Basic skeleton warrior. Reliable and cheap.";
        config.maxHealth = 80;
        config.damage = 15;
        config.attackSpeed = 1.2f;
        config.attackRange = 45f;
        config.moveSpeed = 85f;
        config.isRanged = false;
        // Cost per GDD: 7 Skulls
        config.skullCost = 7;
        config.meatCost = 0;
        config.woodCost = 0;
        config.goldCost = 0;
        config.productionTime = 30f;
        config.unlockLevel = 1;
        config.abilityType = UnitAbilityType.None;
        return config;
    }

    /// <summary>
    /// Creates Gnoll/Bonnie (tank with lifesteal) configuration - Per GDD v5
    /// Cost: 5 Skulls, 5 Meat, 5 Wood
    /// Ability: Lifesteal 20%
    /// </summary>
    public static UnitConfig CreateGnollConfig()
    {
        var config = CreateInstance<UnitConfig>();
        config.unitType = UnitType.Gnoll;
        config.displayName = "Gnoll";
        config.description = "Tough beast with vampiric attacks. Heals 20% of damage dealt.";
        config.maxHealth = 180;
        config.damage = 12;
        config.attackSpeed = 0.8f;
        config.attackRange = 50f;
        config.moveSpeed = 60f;
        config.isRanged = false;
        // Cost per GDD: 5 Skulls, 5 Meat, 5 Wood
        config.skullCost = 5;
        config.meatCost = 5;
        config.woodCost = 5;
        config.goldCost = 0;
        config.productionTime = 60f;
        config.unlockLevel = 3;
        config.abilityType = UnitAbilityType.Lifesteal;
        config.abilityValue = 0.20f; // 20% lifesteal
        return config;
    }

    /// <summary>
    /// Creates Gnome/Foxy (DPS with stun) configuration - Per GDD v5
    /// Cost: 5 Skulls, 3 Meat, 3 Wood, 2 Gold
    /// Ability: Stun every 5th attack for 1 second
    /// </summary>
    public static UnitConfig CreateGnomeConfig()
    {
        var config = CreateInstance<UnitConfig>();
        config.unitType = UnitType.Gnome;
        config.displayName = "Gnome";
        config.description = "Small but fierce. Every 5th attack stuns the enemy.";
        config.maxHealth = 60;
        config.damage = 30;
        config.attackSpeed = 1.5f;
        config.attackRange = 40f;
        config.moveSpeed = 120f;
        config.isRanged = false;
        // Cost per GDD: 5 Skulls, 3 Meat, 3 Wood, 2 Gold
        config.skullCost = 5;
        config.meatCost = 3;
        config.woodCost = 3;
        config.goldCost = 2;
        config.productionTime = 45f;
        config.unlockLevel = 5;
        config.abilityType = UnitAbilityType.Stun;
        config.abilityValue = 1f; // 1 second stun
        config.abilityTriggerCount = 5; // every 5th attack
        return config;
    }

    /// <summary>
    /// Creates TNT/Chica (ranged AoE) configuration - Per GDD v5
    /// Cost: 5 Skulls, 3 Meat, 5 Wood, 3 Gold
    /// Ability: Dynamite AoE with 2 tile radius
    /// </summary>
    public static UnitConfig CreateTNTGoblinConfig()
    {
        var config = CreateInstance<UnitConfig>();
        config.unitType = UnitType.TNTGoblin;
        config.displayName = "TNT";
        config.description = "Demolitions expert. Throws dynamite that explodes in an area.";
        config.maxHealth = 70;
        config.damage = 20;
        config.attackSpeed = 0.7f;
        config.attackRange = 180f;
        config.moveSpeed = 70f;
        config.isRanged = true;
        config.projectileSpeed = 200f;
        // Cost per GDD: 5 Skulls, 3 Meat, 5 Wood, 3 Gold
        config.skullCost = 5;
        config.meatCost = 3;
        config.woodCost = 5;
        config.goldCost = 3;
        config.productionTime = 75f;
        config.unlockLevel = 7;
        config.abilityType = UnitAbilityType.AoE;
        config.abilityValue = 2f; // 2 tile AoE radius
        return config;
    }

    /// <summary>
    /// Creates Shaman/Marionette (support with mind control) configuration - Per GDD v5
    /// Cost: 5 Skulls, 3 Meat, 5 Wood, 5 Gold
    /// Ability: Mind Control for 10 seconds
    /// </summary>
    public static UnitConfig CreateShamanConfig()
    {
        var config = CreateInstance<UnitConfig>();
        config.unitType = UnitType.Shaman;
        config.displayName = "Shaman";
        config.description = "Dark magic wielder. Can take control of enemy minds for 10 seconds.";
        config.maxHealth = 50;
        config.damage = 8;
        config.attackSpeed = 1f;
        config.attackRange = 120f;
        config.moveSpeed = 100f;
        config.isRanged = true;
        config.projectileSpeed = 150f;
        // Cost per GDD: 5 Skulls, 3 Meat, 5 Wood, 5 Gold
        config.skullCost = 5;
        config.meatCost = 3;
        config.woodCost = 5;
        config.goldCost = 5;
        config.productionTime = 90f;
        config.unlockLevel = 10;
        config.abilityType = UnitAbilityType.MindControl;
        config.abilityValue = 10f; // 10 seconds control
        config.abilityCooldown = 30f;
        return config;
    }

    #endregion
}

/// <summary>
/// Types of special abilities for units per GDD.
/// </summary>
public enum UnitAbilityType
{
    None,           // No special ability (Skull)
    Lifesteal,      // Heals % of damage dealt (Gnoll/Bonnie)
    Stun,           // Stuns target on Nth attack (Gnome/Foxy)
    AoE,            // Area of effect damage (TNT/Chica)
    MindControl     // Converts enemy temporarily (Shaman/Marionette)
}
