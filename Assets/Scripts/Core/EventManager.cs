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
    public event Action<int> SoulCollected;                  // amount
    public event Action CargoFull;
    public event Action<int> CargoDeposited;                 // totalAmount
    public event Action<int, int, int, int> StorageUpdated;  // meat, wood, gold, souls

    // Army events
    public event Action<int, int> ArmyUpdated;               // count, limit
    public event Action<string> UnitSpawnRequested;          // unitType
    public event Action<GameObject> UnitSpawned;             // unit
    public event Action<GameObject> UnitDied;                // unit

    // Enemy events
    public event Action<GameObject> EnemyDied;               // enemy
    public event Action<GameObject, int> EnemyDamaged;       // enemy, damage

    // Building events
    public event Action<string, bool> BuildingInRange;       // buildingType, inRange
    public event Action<string> BuildingInteract;            // buildingType

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

    public void OnSoulCollected(int amount)
    {
        SoulCollected?.Invoke(amount);
    }

    public void OnCargoFull()
    {
        CargoFull?.Invoke();
    }

    public void OnCargoDeposited(int totalAmount)
    {
        CargoDeposited?.Invoke(totalAmount);
    }

    public void OnStorageUpdated(int meat, int wood, int gold, int souls)
    {
        StorageUpdated?.Invoke(meat, wood, gold, souls);
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

    #region Building Event Triggers

    public void OnBuildingInRange(string buildingType, bool inRange)
    {
        BuildingInRange?.Invoke(buildingType, inRange);
    }

    public void OnBuildingInteract(string buildingType)
    {
        BuildingInteract?.Invoke(buildingType);
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
