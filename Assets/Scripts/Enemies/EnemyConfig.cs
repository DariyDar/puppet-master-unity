using UnityEngine;

/// <summary>
/// Types of human enemies the player faces.
/// All enemies are from Blue Units faction.
/// </summary>
public enum EnemyType
{
    // Regular enemies (Blue Units)
    PeasantUnarmed,     // Coward, runs away, switches to knife when attacking
    PeasantAxe,         // Basic melee with axe
    PawnUnarmed,        // Alias for PeasantUnarmed
    PawnAxe,            // Alias for PeasantAxe
    Lancer,             // Defensive, spear
    Archer,             // Ranged, bow with arrows
    Warrior,            // Heavy melee (Knight)
    Monk,               // Support, heals allies

    // Special
    Miner,              // Guards gold mines, collects dropped gold
    PawnMiner,          // Alias for Miner
    Peasant,            // Grabs gold and runs

    // Boss
    Minotaur            // Boss enemy (for future use)
}

/// <summary>
/// Enemy behavior patterns.
/// </summary>
public enum EnemyBehavior
{
    Aggressive,     // Attacks player and units on sight
    Defensive,      // Stays near position, attacks if approached
    Coward,         // Runs away when player is near
    Guard,          // Patrols area, attacks intruders
    Ranged          // Keeps distance, shoots from afar, flees when too close
}

/// <summary>
/// ScriptableObject defining enemy stats and configuration.
/// Create instances via Assets > Create > Puppet Master > Enemy Config.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Puppet Master/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identity")]
    public EnemyType enemyType = EnemyType.PawnUnarmed;
    public string displayName = "Peasant";
    [TextArea] public string description = "A frightened villager.";

    [Header("Combat Stats")]
    public int maxHealth = 30;
    public int damage = 2;
    public float attackSpeed = 1f;          // attacks per second
    public float attackRange = 1.5f;        // Unity units (not pixels!)
    public float moveSpeed = 2f;            // Unity units per second
    public bool isRanged = false;

    [Header("Behavior")]
    public EnemyBehavior behavior = EnemyBehavior.Coward;
    public float detectionRange = 6f;       // Unity units - range to detect player
    public float chaseRange = 10f;          // Unity units - max chase distance
    public float fleeRange = 4f;            // Unity units - for cowards
    public float preferredRange = 6f;       // Unity units - for ranged units, preferred attack distance
    public float fearRange = 3f;            // Unity units - for ranged units, flee if player closer than this

    [Header("Rewards")]
    public int xpReward = 10;
    // Note: All enemies drop exactly 1 Skull on death (handled in HumanEnemy)

    [Header("Loot")]
    public int meatMin = 5;
    public int meatMax = 10;
    public int woodMin = 0;
    public int woodMax = 3;
    [Range(0f, 1f)] public float woodChance = 0.2f;
    public int goldMin = 0;
    public int goldMax = 0;
    [Range(0f, 1f)] public float goldChance = 0f;

    [Header("Visuals")]
    public GameObject prefab;
    public RuntimeAnimatorController animatorController;
    public Sprite icon;
    public Vector2 frameSize = new Vector2(192, 192);

    [Header("Projectile (for ranged)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 200f;

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    /// <summary>
    /// Get attack cooldown from attack speed.
    /// </summary>
    public float AttackCooldown => attackSpeed > 0 ? 1f / attackSpeed : 1f;

    #region Static Factory Methods

    public static EnemyConfig CreatePawnUnarmedConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.PawnUnarmed;
        config.displayName = "Pawn";
        config.description = "A frightened villager who runs away. Switches to knife when forced to attack.";
        config.maxHealth = 20;  // GDD: 20 HP
        config.damage = 5;      // GDD: 5 Damage
        config.attackSpeed = 1f;
        config.attackRange = 1.2f;   // Unity units
        config.moveSpeed = 2f;       // Unity units/sec
        config.isRanged = false;
        config.behavior = EnemyBehavior.Coward;
        config.detectionRange = 16f;  // Unity units - see player from far away to flee (doubled)
        config.chaseRange = 16f;
        config.fleeRange = 16f;       // Unity units - flee when player within this range (doubled)
        config.xpReward = 10;
        config.meatMin = 5;
        config.meatMax = 10;
        config.woodMin = 1;
        config.woodMax = 3;
        config.woodChance = 0.2f;
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    public static EnemyConfig CreatePawnAxeConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.PawnAxe;
        config.displayName = "Pawn (Axe)";
        config.description = "A villager armed with an axe, willing to fight back.";
        config.maxHealth = 30;  // GDD: 30 HP
        config.damage = 10;     // GDD: 10 Damage
        config.attackSpeed = 0.8f;
        config.attackRange = 1.5f;   // Unity units
        config.moveSpeed = 2.5f;     // Unity units/sec
        config.isRanged = false;
        config.behavior = EnemyBehavior.Aggressive;
        config.detectionRange = 6f;  // Unity units
        config.chaseRange = 10f;
        config.xpReward = 20;
        config.meatMin = 8;
        config.meatMax = 15;
        config.woodMin = 2;
        config.woodMax = 5;
        config.woodChance = 0.3f;
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    public static EnemyConfig CreateLancerConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.Lancer;
        config.displayName = "Lancer";
        config.description = "A guard armed with a spear. Defends key positions.";
        config.maxHealth = 50;  // GDD: 50 HP
        config.damage = 15;     // GDD: 15 Damage
        config.attackSpeed = 0.8f;
        config.attackRange = 2f;     // Unity units (spear has longer reach)
        config.moveSpeed = 2.5f;     // Unity units/sec
        config.isRanged = false;
        config.behavior = EnemyBehavior.Defensive;
        config.detectionRange = 5f;  // Unity units
        config.chaseRange = 8f;
        config.xpReward = 35;
        config.meatMin = 12;
        config.meatMax = 20;
        config.woodMin = 5;
        config.woodMax = 10;
        config.woodChance = 0.5f;
        config.goldMin = 1;
        config.goldMax = 1;
        config.goldChance = 0.05f;
        config.frameSize = new Vector2(320, 320);
        return config;
    }

    public static EnemyConfig CreateArcherConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.Archer;
        config.displayName = "Archer";
        config.description = "A skilled marksman with a bow. Fires arrows in an arc. Flees when spider gets too close.";
        config.maxHealth = 40;  // GDD: 40 HP
        config.damage = 10;     // GDD: 10 Damage
        config.attackSpeed = 0.4f;  // Slow attack - one shot every 2.5 seconds
        config.attackRange = 30f;    // Unity units (ranged) - max shooting distance (3x original)
        config.moveSpeed = 2.5f;     // Unity units/sec - slightly faster to escape
        config.isRanged = true;
        config.behavior = EnemyBehavior.Ranged;  // New behavior for ranged units
        config.detectionRange = 35f; // Unity units - sees far (beyond attack range)
        config.chaseRange = 35f;
        config.preferredRange = 20f;  // Unity units - wants to stay at this distance
        config.fearRange = 10f;       // Unity units - runs away if player closer than this (old attackRange)
        config.xpReward = 40;
        config.meatMin = 10;
        config.meatMax = 18;
        config.woodMin = 8;
        config.woodMax = 15;
        config.woodChance = 0.6f;
        config.goldMin = 1;
        config.goldMax = 1;
        config.goldChance = 0.1f;
        config.projectileSpeed = 10f; // Unity units/sec - arrow speed
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    public static EnemyConfig CreateWarriorConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.Warrior;
        config.displayName = "Knight";
        config.description = "A heavily armored knight. Slow but devastating.";
        config.maxHealth = 80;  // GDD: 80 HP
        config.damage = 20;     // GDD: 20 Damage
        config.attackSpeed = 0.6f;
        config.attackRange = 1.5f;   // Unity units
        config.moveSpeed = 1.8f;     // Unity units/sec (slow but heavy)
        config.isRanged = false;
        config.behavior = EnemyBehavior.Aggressive;
        config.detectionRange = 6f;  // Unity units
        config.chaseRange = 10f;
        config.xpReward = 60;
        config.meatMin = 20;
        config.meatMax = 35;
        config.woodMin = 15;
        config.woodMax = 25;
        config.woodChance = 0.8f;
        config.goldMin = 1;
        config.goldMax = 2;
        config.goldChance = 0.25f;
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    public static EnemyConfig CreateMonkConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.Monk;
        config.displayName = "Monk";
        config.description = "A healer who supports allies in combat.";
        config.maxHealth = 40;  // GDD: 40 HP
        config.damage = 8;      // GDD: 8 Damage
        config.attackSpeed = 0.73f;
        config.attackRange = 5f;     // Unity units (healing range)
        config.moveSpeed = 2f;       // Unity units/sec
        config.isRanged = false;     // Monk heals, not ranged attack
        config.behavior = EnemyBehavior.Defensive;
        config.detectionRange = 8f;  // Unity units
        config.chaseRange = 10f;
        config.xpReward = 75;
        config.meatMin = 15;
        config.meatMax = 25;
        config.woodMin = 20;
        config.woodMax = 30;
        config.woodChance = 0.9f;
        config.goldMin = 2;
        config.goldMax = 3;
        config.goldChance = 0.35f;
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    public static EnemyConfig CreatePawnMinerConfig()
    {
        var config = CreateInstance<EnemyConfig>();
        config.enemyType = EnemyType.PawnMiner;
        config.displayName = "Miner";
        config.description = "A miner guarding the gold mine. Will collect dropped gold.";
        config.maxHealth = 30;  // Same as Pawn with Axe
        config.damage = 10;     // Same as Pawn with Axe
        config.attackSpeed = 0.8f;
        config.attackRange = 1.5f;   // Unity units
        config.moveSpeed = 2.5f;     // Unity units/sec
        config.isRanged = false;
        config.behavior = EnemyBehavior.Guard;
        config.detectionRange = 6f;  // Unity units
        config.chaseRange = 8f;
        config.xpReward = 25;
        config.meatMin = 5;
        config.meatMax = 10;
        config.woodMin = 0;
        config.woodMax = 2;
        config.woodChance = 0.2f;
        config.goldMin = 1;
        config.goldMax = 3;
        config.goldChance = 0.5f;  // Higher gold drop chance
        config.frameSize = new Vector2(192, 192);
        return config;
    }

    #endregion
}
