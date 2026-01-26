using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Factory for spawning player army units.
/// Subscribes to EventManager.UnitSpawnRequested to handle spawn requests.
/// </summary>
public class UnitFactory : MonoBehaviour
{
    public static UnitFactory Instance { get; private set; }

    [Header("Unit Configurations")]
    [SerializeField] private UnitConfig skullConfig;
    [SerializeField] private UnitConfig gnollConfig;
    [SerializeField] private UnitConfig gnomeConfig;
    [SerializeField] private UnitConfig tntGoblinConfig;
    [SerializeField] private UnitConfig shamanConfig;

    [Header("Default Prefabs")]
    [SerializeField] private GameObject defaultUnitPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 2f;
    [SerializeField] private float minSpawnDistance = 1f;
    [SerializeField] private Transform spawnParent;

    // Track spawned units
    private List<UnitBase> activeUnits = new List<UnitBase>();
    private int formationCounter = 0;

    // Dictionary for quick config lookup
    private Dictionary<string, UnitConfig> configLookup;

    // Properties
    public List<UnitBase> ActiveUnits => activeUnits;
    public int ActiveUnitCount => activeUnits.Count;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeConfigLookup();
    }

    private void Start()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.UnitSpawnRequested += OnUnitSpawnRequested;
            EventManager.Instance.UnitDied += OnUnitDied;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.UnitSpawnRequested -= OnUnitSpawnRequested;
            EventManager.Instance.UnitDied -= OnUnitDied;
        }
    }

    /// <summary>
    /// Initialize the configuration lookup dictionary.
    /// </summary>
    private void InitializeConfigLookup()
    {
        configLookup = new Dictionary<string, UnitConfig>
        {
            { "skull", skullConfig },
            { "gnoll", gnollConfig },
            { "gnome", gnomeConfig },
            { "tntgoblin", tntGoblinConfig },
            { "shaman", shamanConfig }
        };
    }

    #region Event Handlers

    /// <summary>
    /// Handle unit spawn request from EventManager.
    /// </summary>
    private void OnUnitSpawnRequested(string unitType)
    {
        SpawnUnit(unitType);
    }

    /// <summary>
    /// Handle unit death - remove from active units list.
    /// </summary>
    private void OnUnitDied(GameObject unit)
    {
        UnitBase unitBase = unit.GetComponent<UnitBase>();
        if (unitBase != null && activeUnits.Contains(unitBase))
        {
            activeUnits.Remove(unitBase);
            Debug.Log($"[UnitFactory] Unit died. Active units: {activeUnits.Count}");
        }
    }

    #endregion

    #region Spawning

    /// <summary>
    /// Spawn a unit by type name.
    /// </summary>
    public UnitBase SpawnUnit(string unitType)
    {
        string type = unitType.ToLower();

        // Check if we can add more units
        if (GameManager.Instance != null && !GameManager.Instance.CanAddUnit())
        {
            Debug.LogWarning("[UnitFactory] Army limit reached. Cannot spawn more units.");
            return null;
        }

        // Get configuration
        UnitConfig config = GetConfig(type);
        if (config == null)
        {
            Debug.LogWarning($"[UnitFactory] Unknown unit type: {unitType}");
            return null;
        }

        // Check and spend resources
        if (GameManager.Instance != null)
        {
            bool canAfford = GameManager.Instance.SpendResources(
                config.skullCost,
                config.meatCost,
                config.woodCost,
                config.goldCost
            );

            if (!canAfford)
            {
                Debug.LogWarning($"[UnitFactory] Not enough resources to spawn {unitType}");
                return null;
            }
        }

        // Get prefab
        GameObject prefab = config.prefab != null ? config.prefab : defaultUnitPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[UnitFactory] No prefab assigned for unit type: {unitType}");
            return null;
        }

        // Calculate spawn position
        Vector3 spawnPos = GetSpawnPosition();

        // Spawn the unit
        GameObject unitObj = Instantiate(prefab, spawnPos, Quaternion.identity, spawnParent);
        unitObj.name = $"{config.displayName}_{activeUnits.Count}";

        // Initialize unit
        UnitBase unit = unitObj.GetComponent<UnitBase>();
        if (unit == null)
        {
            unit = unitObj.AddComponent<UnitBase>();
        }
        unit.Initialize(config);

        // Ensure it has AI
        UnitAI ai = unitObj.GetComponent<UnitAI>();
        if (ai == null)
        {
            ai = unitObj.AddComponent<UnitAI>();
        }
        ai.SetFormationIndex(formationCounter);
        formationCounter++;

        // Add to active units
        activeUnits.Add(unit);

        // Update army count
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateArmyCount(GameManager.Instance.ArmyCount + 1);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitSpawned(unitObj);
        }

        Debug.Log($"[UnitFactory] Spawned {config.displayName} at {spawnPos}. Active units: {activeUnits.Count}");

        return unit;
    }

    /// <summary>
    /// Spawn a unit without resource cost (used by Soul Drain).
    /// </summary>
    public UnitBase SpawnUnitFree(string unitType, Vector3? position = null)
    {
        string type = unitType.ToLower();

        // Check if we can add more units
        if (GameManager.Instance != null && !GameManager.Instance.CanAddUnit())
        {
            Debug.LogWarning("[UnitFactory] Army limit reached. Cannot spawn more units.");
            return null;
        }

        // Get configuration
        UnitConfig config = GetConfig(type);
        if (config == null)
        {
            Debug.LogWarning($"[UnitFactory] Unknown unit type: {unitType}");
            return null;
        }

        // Get prefab
        GameObject prefab = config.prefab != null ? config.prefab : defaultUnitPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[UnitFactory] No prefab assigned for unit type: {unitType}");
            return null;
        }

        // Calculate spawn position
        Vector3 spawnPos = position ?? GetSpawnPosition();

        // Spawn the unit
        GameObject unitObj = Instantiate(prefab, spawnPos, Quaternion.identity, spawnParent);
        unitObj.name = $"{config.displayName}_{activeUnits.Count}";

        // Initialize unit
        UnitBase unit = unitObj.GetComponent<UnitBase>();
        if (unit == null)
        {
            unit = unitObj.AddComponent<UnitBase>();
        }
        unit.Initialize(config);

        // Ensure it has AI
        UnitAI ai = unitObj.GetComponent<UnitAI>();
        if (ai == null)
        {
            ai = unitObj.AddComponent<UnitAI>();
        }
        ai.SetFormationIndex(formationCounter);
        formationCounter++;

        // Add to active units
        activeUnits.Add(unit);

        // Update army count
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateArmyCount(GameManager.Instance.ArmyCount + 1);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitSpawned(unitObj);
        }

        Debug.Log($"[UnitFactory] Spawned {config.displayName} (FREE via Soul Drain) at {spawnPos}. Active units: {activeUnits.Count}");

        return unit;
    }

    /// <summary>
    /// Spawn a unit with a specific configuration.
    /// </summary>
    public UnitBase SpawnUnit(UnitConfig config, Vector3? position = null)
    {
        if (config == null)
        {
            Debug.LogError("[UnitFactory] Cannot spawn unit with null config.");
            return null;
        }

        // Check army limit
        if (GameManager.Instance != null && !GameManager.Instance.CanAddUnit())
        {
            Debug.LogWarning("[UnitFactory] Army limit reached.");
            return null;
        }

        // Get prefab
        GameObject prefab = config.prefab != null ? config.prefab : defaultUnitPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[UnitFactory] No prefab assigned for unit: {config.displayName}");
            return null;
        }

        // Calculate spawn position
        Vector3 spawnPos = position ?? GetSpawnPosition();

        // Spawn the unit
        GameObject unitObj = Instantiate(prefab, spawnPos, Quaternion.identity, spawnParent);
        unitObj.name = $"{config.displayName}_{activeUnits.Count}";

        // Initialize unit
        UnitBase unit = unitObj.GetComponent<UnitBase>();
        if (unit == null)
        {
            unit = unitObj.AddComponent<UnitBase>();
        }
        unit.Initialize(config);

        // Ensure it has AI
        UnitAI ai = unitObj.GetComponent<UnitAI>();
        if (ai == null)
        {
            ai = unitObj.AddComponent<UnitAI>();
        }
        ai.SetFormationIndex(formationCounter);
        formationCounter++;

        // Add to active units
        activeUnits.Add(unit);

        // Update army count
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateArmyCount(GameManager.Instance.ArmyCount + 1);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitSpawned(unitObj);
        }

        return unit;
    }

    /// <summary>
    /// Get a valid spawn position near the player.
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;

        // Try to find a valid position
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            if (randomOffset.magnitude < minSpawnDistance)
            {
                randomOffset = randomOffset.normalized * minSpawnDistance;
            }

            Vector3 spawnPos = basePos + new Vector3(randomOffset.x, randomOffset.y, 0);

            // Check if position is valid (not overlapping other units)
            Collider2D overlap = Physics2D.OverlapCircle(spawnPos, 0.5f);
            if (overlap == null)
            {
                return spawnPos;
            }
        }

        // Fallback: just spawn at offset position
        return basePos + new Vector3(Random.Range(-spawnRadius, spawnRadius), Random.Range(-spawnRadius, spawnRadius), 0);
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Get unit configuration by type name.
    /// </summary>
    public UnitConfig GetConfig(string unitType)
    {
        string type = unitType.ToLower();

        if (configLookup != null && configLookup.TryGetValue(type, out UnitConfig config))
        {
            return config;
        }

        // Fallback: create default config if none exists
        return CreateDefaultConfig(type);
    }

    /// <summary>
    /// Create a default configuration for a unit type.
    /// </summary>
    private UnitConfig CreateDefaultConfig(string unitType)
    {
        switch (unitType.ToLower())
        {
            case "skull":
                return UnitConfig.CreateSkullConfig();
            case "gnoll":
                return UnitConfig.CreateGnollConfig();
            case "gnome":
                return UnitConfig.CreateGnomeConfig();
            case "tntgoblin":
            case "tnt goblin":
            case "goblin":
                return UnitConfig.CreateTNTGoblinConfig();
            case "shaman":
                return UnitConfig.CreateShamanConfig();
            default:
                return UnitConfig.CreateSkullConfig();
        }
    }

    /// <summary>
    /// Register a configuration at runtime.
    /// </summary>
    public void RegisterConfig(string unitType, UnitConfig config)
    {
        if (configLookup == null)
        {
            InitializeConfigLookup();
        }

        configLookup[unitType.ToLower()] = config;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get all units of a specific type.
    /// </summary>
    public List<UnitBase> GetUnitsOfType(string unitType)
    {
        string type = unitType.ToLower();
        return activeUnits.FindAll(u => u.UnitTypeName.ToLower() == type);
    }

    /// <summary>
    /// Get all units of a specific UnitType enum value.
    /// </summary>
    public List<UnitBase> GetUnitsOfType(UnitType unitType)
    {
        return activeUnits.FindAll(u => u.UnitTypeEnum == unitType);
    }

    /// <summary>
    /// Clean up null references from the active units list.
    /// </summary>
    public void CleanupActiveUnits()
    {
        activeUnits.RemoveAll(u => u == null || u.IsDead);
    }

    /// <summary>
    /// Destroy all active units.
    /// </summary>
    public void DestroyAllUnits()
    {
        foreach (var unit in activeUnits)
        {
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }

        activeUnits.Clear();
        formationCounter = 0;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateArmyCount(0);
        }
    }

    #endregion
}
