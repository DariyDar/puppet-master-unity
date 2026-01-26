using UnityEngine;
using System;

/// <summary>
/// Central event system for game-wide communication.
/// Replaces Phaser's EventManager pattern.
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // Player events
    public event Action<int, int> PlayerHealthChanged;      // current, max
    public event Action<int> XpGained;                       // amount
    public event Action<int> LevelUp;                        // newLevel
    public event Action PlayerDied;

    // Resource events
    public event Action<string, int> ResourceCollected;      // type, amount
    public event Action<int> SkullCollected;                 // amount
    public event Action CargoFull;
    public event Action<int> CargoDeposited;                 // totalAmount
    public event Action<int, int, int, int> StorageUpdated;  // skulls, meat, wood, gold
    public event Action<int, int, int> ResourceDeposited;    // meat, wood, gold

    // Army events
    public event Action<int, int> ArmyUpdated;               // count, limit
    public event Action<string> UnitSpawnRequested;          // unitType
    public event Action<GameObject> UnitSpawned;             // unit
    public event Action<GameObject> UnitDied;                // unit

    // Enemy events
    public event Action<GameObject> EnemyDied;               // enemy
    public event Action<GameObject, int> EnemyDamaged;       // enemy, damage

    // Skull Collection events
    public event Action<HumanEnemy> SkullCollectionStarted;  // target corpse
    public event Action<float> SkullCollectionProgress;      // progress 0-1
    public event Action<int> SkullCollectionCompleted;       // skulls collected
    public event Action SkullCollectionCanceled;

    // Building events
    public event Action<string, bool> BuildingInRange;       // buildingType, inRange
    public event Action<string> BuildingInteract;            // buildingType
    public event Action<GameObject> BuildingDestroyed;       // building
    public event Action<GameObject> CastleDestroyed;         // castle (zone liberation)

    // Farm events
    public event Action<string> FarmPurchased;               // farmType: "meat", "wood", "gold"

    // UI events
    public event Action<string, object> OpenModal;           // modalType, data
    public event Action CloseModal;

    // Game state events
    public event Action<bool> GamePaused;                    // isPaused
    public event Action<string> LocationChanged;             // location

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Player Event Triggers

    public void OnPlayerHealthChanged(int current, int max)
    {
        PlayerHealthChanged?.Invoke(current, max);
    }

    public void OnXpGained(int amount)
    {
        XpGained?.Invoke(amount);
    }

    public void OnLevelUp(int newLevel)
    {
        LevelUp?.Invoke(newLevel);
    }

    public void OnPlayerDied()
    {
        PlayerDied?.Invoke();
    }

    #endregion

    #region Resource Event Triggers

    public void OnResourceCollected(string type, int amount)
    {
        ResourceCollected?.Invoke(type, amount);
    }

    public void OnSkullCollected(int amount)
    {
        SkullCollected?.Invoke(amount);
    }

    public void OnCargoFull()
    {
        CargoFull?.Invoke();
    }

    public void OnCargoDeposited(int totalAmount)
    {
        CargoDeposited?.Invoke(totalAmount);
    }

    public void OnStorageUpdated(int skulls, int meat, int wood, int gold)
    {
        StorageUpdated?.Invoke(skulls, meat, wood, gold);
    }

    public void OnResourceDeposited(int meat, int wood, int gold)
    {
        ResourceDeposited?.Invoke(meat, wood, gold);
    }

    #endregion

    #region Army Event Triggers

    public void OnArmyUpdated(int count, int limit)
    {
        ArmyUpdated?.Invoke(count, limit);
    }

    public void OnUnitSpawnRequested(string unitType)
    {
        UnitSpawnRequested?.Invoke(unitType);
    }

    public void OnUnitSpawned(GameObject unit)
    {
        UnitSpawned?.Invoke(unit);
    }

    public void OnUnitDied(GameObject unit)
    {
        UnitDied?.Invoke(unit);
    }

    #endregion

    #region Enemy Event Triggers

    public void OnEnemyDied(GameObject enemy)
    {
        EnemyDied?.Invoke(enemy);
    }

    public void OnEnemyDamaged(GameObject enemy, int damage)
    {
        EnemyDamaged?.Invoke(enemy, damage);
    }

    #endregion

    #region Skull Collection Event Triggers

    public void OnSkullCollectionStarted(HumanEnemy target)
    {
        SkullCollectionStarted?.Invoke(target);
    }

    public void OnSkullCollectionProgress(float progress)
    {
        SkullCollectionProgress?.Invoke(progress);
    }

    public void OnSkullCollectionCompleted(int skullsCollected)
    {
        SkullCollectionCompleted?.Invoke(skullsCollected);
    }

    public void OnSkullCollectionCanceled()
    {
        SkullCollectionCanceled?.Invoke();
    }

    #endregion

    #region Building Event Triggers

    public void OnBuildingInRange(string buildingType, bool inRange)
    {
        BuildingInRange?.Invoke(buildingType, inRange);
    }

    public void OnBuildingInteract(string buildingType)
    {
        BuildingInteract?.Invoke(buildingType);
    }

    public void OnBuildingDestroyed(GameObject building)
    {
        BuildingDestroyed?.Invoke(building);
    }

    public void OnCastleDestroyed(GameObject castle)
    {
        CastleDestroyed?.Invoke(castle);
        Debug.Log($"[EventManager] Castle destroyed! Zone liberated: {castle.name}");
    }

    public void OnFarmPurchased(string farmType)
    {
        FarmPurchased?.Invoke(farmType);
        Debug.Log($"[EventManager] Farm purchased: {farmType}");
    }

    #endregion

    #region UI Event Triggers

    public void OnOpenModal(string modalType, object data = null)
    {
        OpenModal?.Invoke(modalType, data);
    }

    public void OnCloseModal()
    {
        CloseModal?.Invoke();
    }

    #endregion

    #region Game State Event Triggers

    public void OnGamePaused(bool isPaused)
    {
        GamePaused?.Invoke(isPaused);
    }

    public void OnLocationChanged(string location)
    {
        LocationChanged?.Invoke(location);
    }

    #endregion
}
