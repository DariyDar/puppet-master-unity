using System.Collections.Generic;

/// <summary>
/// Static mapping of enemy types to army unit types for Soul Drain conversion.
/// Defines which unit type is spawned when draining different enemy corpses.
/// </summary>
public static class EnemyToUnitMapping
{
    // Mapping dictionary: EnemyType -> UnitType string
    private static readonly Dictionary<EnemyType, string> enemyToUnitMap = new Dictionary<EnemyType, string>
    {
        // Basic pawns become basic Skull units
        { EnemyType.PawnUnarmed, "skull" },
        { EnemyType.PawnAxe, "skull" },
        { EnemyType.PeasantUnarmed, "skull" },
        { EnemyType.PeasantAxe, "skull" },
        { EnemyType.Peasant, "skull" },

        // Military units become stronger army units
        { EnemyType.Lancer, "gnoll" },      // Defensive -> Tank
        { EnemyType.Warrior, "gnoll" },     // Heavy melee -> Tank
        { EnemyType.Archer, "gnome" },      // Ranged -> DPS with Stun
        { EnemyType.Monk, "shaman" },       // Support -> Support with Mind Control

        // Special enemies
        { EnemyType.Miner, "gnoll" },       // Strong worker -> Tank
        { EnemyType.PawnMiner, "gnoll" },   // Alias

        // Bosses - could give special units or multiple units
        { EnemyType.Minotaur, "gnoll" }     // Boss -> Tank (could be enhanced)
    };

    // UnitType enum mapping for type-safe access
    private static readonly Dictionary<EnemyType, UnitType> enemyToUnitTypeEnumMap = new Dictionary<EnemyType, UnitType>
    {
        { EnemyType.PawnUnarmed, UnitType.Skull },
        { EnemyType.PawnAxe, UnitType.Skull },
        { EnemyType.PeasantUnarmed, UnitType.Skull },
        { EnemyType.PeasantAxe, UnitType.Skull },
        { EnemyType.Peasant, UnitType.Skull },
        { EnemyType.Lancer, UnitType.Gnoll },
        { EnemyType.Warrior, UnitType.Gnoll },
        { EnemyType.Archer, UnitType.Gnome },
        { EnemyType.Monk, UnitType.Shaman },
        { EnemyType.Miner, UnitType.Gnoll },
        { EnemyType.PawnMiner, UnitType.Gnoll },
        { EnemyType.Minotaur, UnitType.Gnoll }
    };

    /// <summary>
    /// Get the unit type string for a given enemy type.
    /// </summary>
    /// <param name="enemyType">The enemy type to convert</param>
    /// <returns>Unit type string (lowercase) for UnitFactory</returns>
    public static string GetUnitType(EnemyType enemyType)
    {
        if (enemyToUnitMap.TryGetValue(enemyType, out string unitType))
        {
            return unitType;
        }

        // Default to skull for unknown enemy types
        return "skull";
    }

    /// <summary>
    /// Get the UnitType enum for a given enemy type.
    /// </summary>
    /// <param name="enemyType">The enemy type to convert</param>
    /// <returns>UnitType enum value</returns>
    public static UnitType GetUnitTypeEnum(EnemyType enemyType)
    {
        if (enemyToUnitTypeEnumMap.TryGetValue(enemyType, out UnitType unitType))
        {
            return unitType;
        }

        // Default to Skull for unknown enemy types
        return UnitType.Skull;
    }

    /// <summary>
    /// Get display name for the unit type that would be spawned.
    /// </summary>
    /// <param name="enemyType">The enemy type</param>
    /// <returns>Human-readable unit name</returns>
    public static string GetUnitDisplayName(EnemyType enemyType)
    {
        UnitType unitType = GetUnitTypeEnum(enemyType);

        return unitType switch
        {
            UnitType.Skull => "Skull",
            UnitType.Gnoll => "Gnoll",
            UnitType.Gnome => "Gnome",
            UnitType.TNTGoblin => "TNT Goblin",
            UnitType.Shaman => "Shaman",
            _ => "Skull"
        };
    }

    /// <summary>
    /// Check if an enemy type can be drained (some might be excluded).
    /// </summary>
    /// <param name="enemyType">The enemy type to check</param>
    /// <returns>True if the enemy can be drained for souls</returns>
    public static bool CanBeDrained(EnemyType enemyType)
    {
        // All human enemies can be drained
        // Could exclude certain types if needed
        return enemyToUnitMap.ContainsKey(enemyType);
    }

    /// <summary>
    /// Get bonus info for draining special enemies (e.g., bosses might give bonuses).
    /// </summary>
    /// <param name="enemyType">The enemy type</param>
    /// <returns>Tuple of (bonusUnits, bonusXp)</returns>
    public static (int bonusUnits, int bonusXp) GetDrainBonus(EnemyType enemyType)
    {
        return enemyType switch
        {
            // Bosses could give extra rewards
            EnemyType.Minotaur => (1, 50),  // Extra unit + XP bonus

            // Regular enemies - no bonus
            _ => (0, 0)
        };
    }
}
