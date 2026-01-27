using UnityEngine;

/// <summary>
/// Central game manager singleton.
/// Manages game state, player stats, and resources.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 150;
    [SerializeField] private int currentHealth = 150;
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXp = 0;
    [SerializeField] private int xpToNextLevel = 100;

    [Header("Resources")]
    [SerializeField] private int skulls = 0;     // From killed humans (1 per enemy)
    [SerializeField] private int meat = 0;       // From sheep
    [SerializeField] private int wood = 0;       // From destroyed buildings
    [SerializeField] private int gold = 0;       // From gold mines

    [Header("Cargo")]
    [SerializeField] private int cargoMeat = 0;
    [SerializeField] private int cargoWood = 0;
    [SerializeField] private int cargoGold = 0;
    [SerializeField] private int cargoCapacity = 50;

    [Header("Army")]
    [SerializeField] private int armyCount = 0;
    [SerializeField] private int armyLimit = 5;

    // Properties for external access
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Level => level;
    public int CurrentXp => currentXp;
    public int XpToNextLevel => xpToNextLevel;

    public int Skulls => skulls;
    public int Meat => meat;
    public int Wood => wood;
    public int Gold => gold;

    public int CargoMeat => cargoMeat;
    public int CargoWood => cargoWood;
    public int CargoGold => cargoGold;
    public int CargoCapacity => cargoCapacity;
    public int CurrentCargo => cargoMeat + cargoWood + cargoGold;

    public int ArmyCount => armyCount;
    public int ArmyLimit => armyLimit;

    [Header("Settings")]
    [SerializeField] private bool loadSaveOnStart = false;  // Set to false for fresh start each time

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadSaveOnStart)
        {
            LoadGame();
        }
        else
        {
            // Start fresh - reset all values to initial state
            ResetToInitialState();
        }
    }

    /// <summary>
    /// Reset to initial state without deleting PlayerPrefs.
    /// </summary>
    private void ResetToInitialState()
    {
        maxHealth = 150;
        currentHealth = 150;
        level = 1;
        currentXp = 0;
        xpToNextLevel = 100;

        skulls = 0;
        meat = 0;
        wood = 0;
        gold = 0;

        cargoMeat = 0;
        cargoWood = 0;
        cargoGold = 0;

        Debug.Log("[GameManager] Reset to initial state (fresh start)");
    }

    private void Start()
    {
        // Broadcast initial state to UI after EventManager has time to initialize
        Invoke(nameof(BroadcastInitialState), 0.1f);
    }

    /// <summary>
    /// Broadcast all state to UI. Called on startup and when UI needs refresh.
    /// </summary>
    public void BroadcastInitialState()
    {
        if (EventManager.Instance == null) return;

        EventManager.Instance.OnPlayerHealthChanged(currentHealth, maxHealth);
        EventManager.Instance.OnStorageUpdated(skulls, meat, wood, gold);
        EventManager.Instance.OnLevelUp(level);
        EventManager.Instance.OnXpGained(0);  // Trigger XP display update
        EventManager.Instance.OnArmyUpdated(armyCount, armyLimit);

        Debug.Log($"[GameManager] Broadcast initial state - Skulls:{skulls}, Meat:{meat}, Wood:{wood}, Gold:{gold}");
    }

    #region Health & XP

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        EventManager.Instance?.OnPlayerHealthChanged(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        EventManager.Instance?.OnPlayerHealthChanged(currentHealth, maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        currentHealth = current;
        maxHealth = max;
        EventManager.Instance?.OnPlayerHealthChanged(currentHealth, maxHealth);
    }

    public void AddXp(int amount)
    {
        currentXp += amount;

        while (currentXp >= xpToNextLevel)
        {
            currentXp -= xpToNextLevel;
            LevelUp();
        }

        EventManager.Instance?.OnXpGained(amount);
    }

    private void LevelUp()
    {
        level++;
        xpToNextLevel = Mathf.FloorToInt(100 * Mathf.Pow(1.2f, level - 1));
        maxHealth += 10;
        currentHealth = maxHealth;

        EventManager.Instance?.OnLevelUp(level);
    }

    private void Die()
    {
        EventManager.Instance?.OnPlayerDied();
        // TODO: Handle death (respawn, game over, etc.)
    }

    #endregion

    #region Resources & Cargo

    public bool AddToCargo(string resourceType, int amount)
    {
        int spaceLeft = cargoCapacity - CurrentCargo;
        if (spaceLeft <= 0)
        {
            EventManager.Instance?.OnCargoFull();
            return false;
        }

        int actualAmount = Mathf.Min(amount, spaceLeft);

        switch (resourceType.ToLower())
        {
            case "meat":
                cargoMeat += actualAmount;
                break;
            case "wood":
                cargoWood += actualAmount;
                break;
            case "gold":
                cargoGold += actualAmount;
                break;
            default:
                Debug.LogWarning($"Unknown resource type: {resourceType}");
                return false;
        }

        EventManager.Instance?.OnResourceCollected(resourceType, actualAmount);

        // Also update cargo display if UI is listening
        // The TopHUD will now show: Skulls (storage) + Cargo values
        Debug.Log($"[GameManager] Added {actualAmount} {resourceType} to cargo. Total cargo: {CurrentCargo}/{cargoCapacity}");
        return true;
    }

    public bool DepositCargo()
    {
        if (CurrentCargo <= 0) return false;

        meat += cargoMeat;
        wood += cargoWood;
        gold += cargoGold;

        int depositedMeat = cargoMeat;
        int depositedWood = cargoWood;
        int depositedGold = cargoGold;
        int totalDeposited = CurrentCargo;

        cargoMeat = 0;
        cargoWood = 0;
        cargoGold = 0;

        EventManager.Instance?.OnCargoDeposited(totalDeposited);
        EventManager.Instance?.OnStorageUpdated(skulls, meat, wood, gold);

        Debug.Log($"[GameManager] Deposited to storage: Meat={depositedMeat}, Wood={depositedWood}, Gold={depositedGold}");
        return true;
    }

    /// <summary>
    /// Deposit a single unit of a specific resource from cargo to storage.
    /// Used by Storage for gradual deposit visual effect.
    /// </summary>
    public bool DepositSingleResource(string resourceType)
    {
        int amount = 1;
        bool success = false;

        switch (resourceType.ToLower())
        {
            case "meat":
                if (cargoMeat >= amount)
                {
                    cargoMeat -= amount;
                    meat += amount;
                    success = true;
                }
                break;
            case "wood":
                if (cargoWood >= amount)
                {
                    cargoWood -= amount;
                    wood += amount;
                    success = true;
                }
                break;
            case "gold":
                if (cargoGold >= amount)
                {
                    cargoGold -= amount;
                    gold += amount;
                    success = true;
                }
                break;
        }

        if (success)
        {
            EventManager.Instance?.OnCargoDeposited(amount);
            EventManager.Instance?.OnStorageUpdated(skulls, meat, wood, gold);
        }

        return success;
    }

    public void AddSkull(int amount = 1)
    {
        skulls += amount;
        EventManager.Instance?.OnSkullCollected(amount);
        // Also update storage display for skulls
        EventManager.Instance?.OnStorageUpdated(skulls, meat, wood, gold);
    }

    /// <summary>
    /// Spend resources. Order: skulls, meat, wood, gold.
    /// </summary>
    public bool SpendResources(int skullsCost, int meatCost, int woodCost, int goldCost = 0)
    {
        if (skulls < skullsCost || meat < meatCost || wood < woodCost || gold < goldCost)
        {
            return false;
        }

        skulls -= skullsCost;
        meat -= meatCost;
        wood -= woodCost;
        gold -= goldCost;

        EventManager.Instance?.OnStorageUpdated(skulls, meat, wood, gold);
        return true;
    }

    /// <summary>
    /// Check if player can afford specified costs.
    /// </summary>
    public bool CanAfford(int skullsCost, int meatCost, int woodCost, int goldCost = 0)
    {
        return skulls >= skullsCost && meat >= meatCost && wood >= woodCost && gold >= goldCost;
    }

    #endregion

    #region Army

    public void UpdateArmyCount(int count)
    {
        armyCount = Mathf.Clamp(count, 0, armyLimit);
        EventManager.Instance?.OnArmyUpdated(armyCount, armyLimit);
    }

    public bool CanAddUnit()
    {
        return armyCount < armyLimit;
    }

    /// <summary>
    /// Set army limit (from UpgradeSystem).
    /// </summary>
    public void SetArmyLimit(int limit)
    {
        armyLimit = limit;
        EventManager.Instance?.OnArmyUpdated(armyCount, armyLimit);
    }

    /// <summary>
    /// Set cargo capacity (from UpgradeSystem).
    /// </summary>
    public void SetCargoCapacity(int capacity)
    {
        cargoCapacity = capacity;
    }

    #endregion

    #region Cargo Methods (per ResourceMagnet)

    /// <summary>
    /// Add meat to cargo.
    /// </summary>
    public void AddCargoMeat(int amount)
    {
        int spaceLeft = cargoCapacity - CurrentCargo;
        if (spaceLeft <= 0)
        {
            EventManager.Instance?.OnCargoFull();
            return;
        }
        cargoMeat += Mathf.Min(amount, spaceLeft);
        EventManager.Instance?.OnResourceCollected("Meat", amount);
    }

    /// <summary>
    /// Add wood to cargo.
    /// </summary>
    public void AddCargoWood(int amount)
    {
        int spaceLeft = cargoCapacity - CurrentCargo;
        if (spaceLeft <= 0)
        {
            EventManager.Instance?.OnCargoFull();
            return;
        }
        cargoWood += Mathf.Min(amount, spaceLeft);
        EventManager.Instance?.OnResourceCollected("Wood", amount);
    }

    /// <summary>
    /// Add gold to cargo.
    /// </summary>
    public void AddCargoGold(int amount)
    {
        int spaceLeft = cargoCapacity - CurrentCargo;
        if (spaceLeft <= 0)
        {
            EventManager.Instance?.OnCargoFull();
            return;
        }
        cargoGold += Mathf.Min(amount, spaceLeft);
        EventManager.Instance?.OnResourceCollected("Gold", amount);
    }

    #endregion

    #region XP Methods (per UpgradeSystem)

    /// <summary>
    /// Add XP with bonus from upgrades.
    /// </summary>
    public void AddXP(int amount)
    {
        // Apply XP bonus from UpgradeSystem if available
        float multiplier = 1f;
        if (UpgradeSystem.Instance != null)
        {
            multiplier = UpgradeSystem.Instance.GetXPBonusMultiplier();
        }

        int finalAmount = Mathf.RoundToInt(amount * multiplier);
        AddXp(finalAmount);
    }

    /// <summary>
    /// Get current player level (alias for QuestSystem compatibility).
    /// </summary>
    public int PlayerLevel => level;

    #endregion

    #region Save/Load

    public void SaveGame()
    {
        PlayerPrefs.SetInt("MaxHealth", maxHealth);
        PlayerPrefs.SetInt("CurrentHealth", currentHealth);
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("CurrentXp", currentXp);
        PlayerPrefs.SetInt("XpToNextLevel", xpToNextLevel);

        PlayerPrefs.SetInt("Skulls", skulls);
        PlayerPrefs.SetInt("Meat", meat);
        PlayerPrefs.SetInt("Wood", wood);
        PlayerPrefs.SetInt("Gold", gold);

        PlayerPrefs.Save();
        Debug.Log("Game saved!");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("Level")) return;

        maxHealth = PlayerPrefs.GetInt("MaxHealth", 150);
        currentHealth = PlayerPrefs.GetInt("CurrentHealth", 150);
        level = PlayerPrefs.GetInt("Level", 1);
        currentXp = PlayerPrefs.GetInt("CurrentXp", 0);
        xpToNextLevel = PlayerPrefs.GetInt("XpToNextLevel", 100);

        skulls = PlayerPrefs.GetInt("Skulls", 0);
        meat = PlayerPrefs.GetInt("Meat", 0);
        wood = PlayerPrefs.GetInt("Wood", 0);
        gold = PlayerPrefs.GetInt("Gold", 0);

        Debug.Log("Game loaded!");
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();

        maxHealth = 150;
        currentHealth = 150;
        level = 1;
        currentXp = 0;
        xpToNextLevel = 100;

        skulls = 0;
        meat = 0;
        wood = 0;
        gold = 0;

        cargoMeat = 0;
        cargoWood = 0;
        cargoGold = 0;

        Debug.Log("Progress reset!");
    }

    #endregion

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }
}
