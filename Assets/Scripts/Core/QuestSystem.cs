using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Quest system per GDD pages 46-47.
/// Quests run sequentially (one at a time).
/// Starts as tutorial, progresses through game.
///
/// Quest types from GDD:
/// 1. Collect N resource (bring to base)
/// 2. Accumulate N resource (in storage)
/// 3. Reach player level N
/// 4. Destroy N objects (deposits, towers, houses)
/// 5. Destroy N objects at specific locations
/// 6. Kill N any enemies
/// 7. Kill N specific enemies
/// 8. Build N any units
/// 9. Build N specific units
/// 10. Build base structure
/// 11. Upgrade player stat to N
/// 12. Upgrade unit to N
/// 13. Upgrade base object to N
/// </summary>
public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }

    [Header("Quest Data")]
    [SerializeField] private List<QuestDefinition> allQuests = new List<QuestDefinition>();
    [SerializeField] private int currentQuestIndex = 0;

    [Header("UI")]
    [SerializeField] private bool showDirectionArrow = true;

    // Current quest state
    private QuestDefinition currentQuest;
    private int currentProgress;
    private bool questCompleted;

    // Events
    public event Action<QuestDefinition> OnQuestStarted;
    public event Action<int, int> OnQuestProgress;        // current, target
    public event Action<QuestDefinition, int> OnQuestCompleted; // quest, xpReward
    public event Action OnAllQuestsCompleted;

    // Properties
    public QuestDefinition CurrentQuest => currentQuest;
    public int CurrentProgress => currentProgress;
    public bool HasActiveQuest => currentQuest != null && !questCompleted;
    public float ProgressPercent => currentQuest != null ? (float)currentProgress / currentQuest.targetAmount : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SubscribeToEvents();
        StartNextQuest();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ResourceCollected += OnResourceCollected;
            EventManager.Instance.StorageUpdated += OnStorageUpdated;
            EventManager.Instance.LevelUp += OnLevelUp;
            EventManager.Instance.BuildingDestroyed += OnBuildingDestroyed;
            EventManager.Instance.EnemyDied += OnEnemyDied;
            EventManager.Instance.UnitSpawned += OnUnitSpawned;
        }

        if (UpgradeSystem.Instance != null)
        {
            UpgradeSystem.Instance.OnUpgradePurchased += OnUpgradePurchased;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ResourceCollected -= OnResourceCollected;
            EventManager.Instance.StorageUpdated -= OnStorageUpdated;
            EventManager.Instance.LevelUp -= OnLevelUp;
            EventManager.Instance.BuildingDestroyed -= OnBuildingDestroyed;
            EventManager.Instance.EnemyDied -= OnEnemyDied;
            EventManager.Instance.UnitSpawned -= OnUnitSpawned;
        }

        if (UpgradeSystem.Instance != null)
        {
            UpgradeSystem.Instance.OnUpgradePurchased -= OnUpgradePurchased;
        }
    }

    /// <summary>
    /// Start the next quest in sequence.
    /// </summary>
    public void StartNextQuest()
    {
        if (currentQuestIndex >= allQuests.Count)
        {
            Debug.Log("[QuestSystem] All quests completed!");
            OnAllQuestsCompleted?.Invoke();
            return;
        }

        currentQuest = allQuests[currentQuestIndex];
        currentProgress = 0;
        questCompleted = false;

        // Check if quest is already complete (e.g., "reach level 2" when already level 5)
        CheckInitialProgress();

        OnQuestStarted?.Invoke(currentQuest);
        Debug.Log($"[QuestSystem] Started quest: {currentQuest.title} - {currentQuest.description}");
    }

    /// <summary>
    /// Check initial progress for quests that might already be satisfied.
    /// </summary>
    private void CheckInitialProgress()
    {
        if (currentQuest == null) return;

        switch (currentQuest.questType)
        {
            case QuestType.AccumulateResource:
                if (GameManager.Instance != null)
                {
                    currentProgress = GetResourceAmount(currentQuest.targetResource);
                }
                break;

            case QuestType.ReachLevel:
                if (GameManager.Instance != null)
                {
                    currentProgress = GameManager.Instance.PlayerLevel;
                }
                break;

            case QuestType.UpgradeStat:
                if (UpgradeSystem.Instance != null)
                {
                    currentProgress = UpgradeSystem.Instance.GetUpgradeLevel(currentQuest.targetUpgrade);
                }
                break;
        }

        CheckQuestCompletion();
    }

    /// <summary>
    /// Add progress to current quest.
    /// </summary>
    public void AddProgress(int amount = 1)
    {
        if (currentQuest == null || questCompleted) return;

        currentProgress += amount;
        currentProgress = Mathf.Min(currentProgress, currentQuest.targetAmount);

        OnQuestProgress?.Invoke(currentProgress, currentQuest.targetAmount);
        Debug.Log($"[QuestSystem] Progress: {currentProgress}/{currentQuest.targetAmount}");

        CheckQuestCompletion();
    }

    /// <summary>
    /// Set progress directly (for resource-based quests).
    /// </summary>
    public void SetProgress(int amount)
    {
        if (currentQuest == null || questCompleted) return;

        currentProgress = Mathf.Min(amount, currentQuest.targetAmount);
        OnQuestProgress?.Invoke(currentProgress, currentQuest.targetAmount);

        CheckQuestCompletion();
    }

    /// <summary>
    /// Check if quest is complete.
    /// </summary>
    private void CheckQuestCompletion()
    {
        if (currentQuest == null || questCompleted) return;

        if (currentProgress >= currentQuest.targetAmount)
        {
            CompleteQuest();
        }
    }

    /// <summary>
    /// Complete the current quest.
    /// </summary>
    private void CompleteQuest()
    {
        if (currentQuest == null || questCompleted) return;

        questCompleted = true;

        // Award XP
        int xpReward = currentQuest.xpReward;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddXP(xpReward);
        }

        Debug.Log($"[QuestSystem] Quest completed: {currentQuest.title}! Earned {xpReward} XP");
        OnQuestCompleted?.Invoke(currentQuest, xpReward);

        // Move to next quest
        currentQuestIndex++;
        StartNextQuest();
    }

    /// <summary>
    /// Get direction to quest target (for UI arrow).
    /// </summary>
    public Vector3? GetQuestTargetDirection()
    {
        if (currentQuest == null || !showDirectionArrow) return null;

        // If quest has a specific target position
        if (currentQuest.hasTargetPosition)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                return (currentQuest.targetPosition - player.transform.position).normalized;
            }
        }

        // If quest has a target tag
        if (!string.IsNullOrEmpty(currentQuest.targetTag))
        {
            GameObject target = GameObject.FindGameObjectWithTag(currentQuest.targetTag);
            if (target != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    return (target.transform.position - player.transform.position).normalized;
                }
            }
        }

        return null;
    }

    #region Event Handlers

    private void OnResourceCollected(string resourceType, int amount)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.CollectResource)
        {
            if (currentQuest.targetResource.ToString().ToLower() == resourceType.ToLower())
            {
                AddProgress(amount);
            }
        }
    }

    private void OnStorageUpdated(int skulls, int meat, int wood, int gold)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.AccumulateResource)
        {
            int currentAmount = GetResourceAmount(currentQuest.targetResource);
            SetProgress(currentAmount);
        }
    }

    private int GetResourceAmount(ResourceType resource)
    {
        if (GameManager.Instance == null) return 0;

        return resource switch
        {
            ResourceType.Skull => GameManager.Instance.Skulls,
            ResourceType.Meat => GameManager.Instance.Meat,
            ResourceType.Wood => GameManager.Instance.Wood,
            ResourceType.Gold => GameManager.Instance.Gold,
            _ => 0
        };
    }

    private void OnLevelUp(int newLevel)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.ReachLevel)
        {
            SetProgress(newLevel);
        }
    }

    private void OnBuildingDestroyed(GameObject building)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.DestroyObjects ||
            currentQuest.questType == QuestType.DestroyObjectsAtLocation)
        {
            // Check if matches target type
            bool matches = false;

            if (string.IsNullOrEmpty(currentQuest.targetObjectType))
            {
                matches = true; // Any building
            }
            else
            {
                matches = building.name.Contains(currentQuest.targetObjectType);
            }

            // Check location if required
            if (currentQuest.questType == QuestType.DestroyObjectsAtLocation && currentQuest.hasTargetPosition)
            {
                float dist = Vector2.Distance(building.transform.position, currentQuest.targetPosition);
                matches = matches && dist <= currentQuest.targetRadius;
            }

            if (matches)
            {
                AddProgress();
            }
        }
    }

    private void OnEnemyDied(GameObject enemy)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.KillEnemies ||
            currentQuest.questType == QuestType.KillSpecificEnemies)
        {
            bool matches = false;

            if (currentQuest.questType == QuestType.KillEnemies)
            {
                matches = true; // Any enemy
            }
            else
            {
                // Check specific enemy type
                HumanEnemy humanEnemy = enemy.GetComponent<HumanEnemy>();
                if (humanEnemy != null && humanEnemy.Config != null)
                {
                    matches = humanEnemy.Config.enemyType.ToString() == currentQuest.targetEnemyType;
                }
            }

            if (matches)
            {
                AddProgress();
            }
        }
    }

    private void OnUnitSpawned(GameObject unit)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.BuildUnits ||
            currentQuest.questType == QuestType.BuildSpecificUnits)
        {
            bool matches = false;

            if (currentQuest.questType == QuestType.BuildUnits)
            {
                matches = true; // Any unit
            }
            else
            {
                // Check specific unit type
                UnitBase unitBase = unit.GetComponent<UnitBase>();
                if (unitBase != null)
                {
                    matches = unit.name.Contains(currentQuest.targetUnitType);
                }
            }

            if (matches)
            {
                AddProgress();
            }
        }
    }

    private void OnUpgradePurchased(UpgradeType type, int newLevel)
    {
        if (currentQuest == null || questCompleted) return;

        if (currentQuest.questType == QuestType.UpgradeStat)
        {
            if (type == currentQuest.targetUpgrade)
            {
                SetProgress(newLevel);
            }
        }
    }

    #endregion

    /// <summary>
    /// Skip current quest (for debugging).
    /// </summary>
    [ContextMenu("Skip Current Quest")]
    public void SkipCurrentQuest()
    {
        if (currentQuest != null && !questCompleted)
        {
            currentProgress = currentQuest.targetAmount;
            CheckQuestCompletion();
        }
    }

    /// <summary>
    /// Reset all quests (for debugging).
    /// </summary>
    [ContextMenu("Reset All Quests")]
    public void ResetAllQuests()
    {
        currentQuestIndex = 0;
        currentQuest = null;
        currentProgress = 0;
        questCompleted = false;
        StartNextQuest();
    }
}

/// <summary>
/// Types of quests per GDD.
/// </summary>
public enum QuestType
{
    CollectResource,          // Collect N resource (bring to base)
    AccumulateResource,       // Have N resource in storage
    ReachLevel,               // Reach player level N
    DestroyObjects,           // Destroy N buildings/objects
    DestroyObjectsAtLocation, // Destroy N objects at specific location
    KillEnemies,              // Kill N any enemies
    KillSpecificEnemies,      // Kill N specific enemy type
    BuildUnits,               // Build N any units
    BuildSpecificUnits,       // Build N specific unit type
    BuildBaseStructure,       // Build a base structure
    UpgradeStat,              // Upgrade player stat to level N
    UpgradeUnit,              // Upgrade unit to level N
    UpgradeBaseObject         // Upgrade base object to level N
}

/// <summary>
/// Resource types for quest targeting.
/// </summary>
public enum ResourceType
{
    Skull,
    Meat,
    Wood,
    Gold
}

/// <summary>
/// Definition for a single quest.
/// </summary>
[System.Serializable]
public class QuestDefinition
{
    [Header("Basic Info")]
    public string title;
    [TextArea] public string description;
    public QuestType questType;

    [Header("Target")]
    public int targetAmount = 1;

    [Header("Resource Quests")]
    public ResourceType targetResource;

    [Header("Kill/Destroy Quests")]
    public string targetEnemyType;    // For KillSpecificEnemies
    public string targetObjectType;   // For DestroyObjects
    public string targetTag;          // For finding target

    [Header("Unit Quests")]
    public string targetUnitType;     // For BuildSpecificUnits

    [Header("Upgrade Quests")]
    public UpgradeType targetUpgrade; // For UpgradeStat

    [Header("Location")]
    public bool hasTargetPosition;
    public Vector3 targetPosition;
    public float targetRadius = 5f;

    [Header("Rewards")]
    public int xpReward = 50;

    [Header("UI")]
    public Sprite icon;
}
