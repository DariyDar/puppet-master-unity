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
    [SerializeField] private int meat = 0;
    [SerializeField] private int wood = 0;
    [SerializeField] private int gold = 0;
    [SerializeField] private int souls = 0;

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

    public int Meat => meat;
    public int Wood => wood;
    public int Gold => gold;
    public int Souls => souls;

    public int CargoMeat => cargoMeat;
    public int CargoWood => cargoWood;
    public int CargoGold => cargoGold;
    public int CargoCapacity => cargoCapacity;
    public int CurrentCargo => cargoMeat + cargoWood + cargoGold;

    public int ArmyCount => armyCount;
    public int ArmyLimit => armyLimit;

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

        LoadGame();
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
        return true;
    }

    public void DepositCargo()
    {
        if (CurrentCargo <= 0) return;

        meat += cargoMeat;
        wood += cargoWood;
        gold += cargoGold;

        int totalDeposited = CurrentCargo;

        cargoMeat = 0;
        cargoWood = 0;
        cargoGold = 0;

        EventManager.Instance?.OnCargoDeposited(totalDeposited);
        EventManager.Instance?.OnStorageUpdated(meat, wood, gold, souls);
    }

    public void AddSoul(int amount = 1)
    {
        souls += amount;
        EventManager.Instance?.OnSoulCollected(amount);
    }

    public bool SpendResources(int meatCost, int woodCost, int goldCost = 0, int soulsCost = 0)
    {
        if (meat < meatCost || wood < woodCost || gold < goldCost || souls < soulsCost)
        {
            return false;
        }

        meat -= meatCost;
        wood -= woodCost;
        gold -= goldCost;
        souls -= soulsCost;

        EventManager.Instance?.OnStorageUpdated(meat, wood, gold, souls);
        return true;
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

    #endregion

    #region Save/Load

    public void SaveGame()
    {
        PlayerPrefs.SetInt("MaxHealth", maxHealth);
        PlayerPrefs.SetInt("CurrentHealth", currentHealth);
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("CurrentXp", currentXp);
        PlayerPrefs.SetInt("XpToNextLevel", xpToNextLevel);

        PlayerPrefs.SetInt("Meat", meat);
        PlayerPrefs.SetInt("Wood", wood);
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.SetInt("Souls", souls);

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

        meat = PlayerPrefs.GetInt("Meat", 0);
        wood = PlayerPrefs.GetInt("Wood", 0);
        gold = PlayerPrefs.GetInt("Gold", 0);
        souls = PlayerPrefs.GetInt("Souls", 0);

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

        meat = 0;
        wood = 0;
        gold = 0;
        souls = 0;

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
