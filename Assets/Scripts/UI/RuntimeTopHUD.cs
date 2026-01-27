using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime version of TopHUD that finds UI elements by name.
/// Works with UIAutoBuilder generated UI.
/// Subscribes to EventManager for updates.
/// </summary>
public class RuntimeTopHUD : MonoBehaviour
{
    [Header("Auto-find by name")]
    [SerializeField] private bool autoFindElements = true;

    // Resource texts (found by name)
    private TextMeshProUGUI skullsText;
    private TextMeshProUGUI meatText;
    private TextMeshProUGUI woodText;
    private TextMeshProUGUI goldText;

    // HP/XP texts
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI xpText;
    private TextMeshProUGUI lvText;

    // HP/XP bars (if exist)
    private Image hpBarFill;
    private Image xpBarFill;

    private void Awake()
    {
        if (autoFindElements)
        {
            FindUIElements();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.PlayerHealthChanged += OnHealthChanged;
            EventManager.Instance.StorageUpdated += OnStorageUpdated;
            EventManager.Instance.LevelUp += OnLevelUp;
            EventManager.Instance.XpGained += OnXpGained;
            EventManager.Instance.SkullCollected += OnSkullCollected;
            EventManager.Instance.ResourceCollected += OnResourceCollected;
            EventManager.Instance.CargoDeposited += OnCargoDeposited;
        }
        else
        {
            // Try again later
            Invoke(nameof(TrySubscribeToEvents), 0.5f);
        }
    }

    private void TrySubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.PlayerHealthChanged += OnHealthChanged;
            EventManager.Instance.StorageUpdated += OnStorageUpdated;
            EventManager.Instance.LevelUp += OnLevelUp;
            EventManager.Instance.XpGained += OnXpGained;
            EventManager.Instance.SkullCollected += OnSkullCollected;
            EventManager.Instance.ResourceCollected += OnResourceCollected;
            EventManager.Instance.CargoDeposited += OnCargoDeposited;
            Debug.Log("[RuntimeTopHUD] Subscribed to events (delayed)");
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.PlayerHealthChanged -= OnHealthChanged;
            EventManager.Instance.StorageUpdated -= OnStorageUpdated;
            EventManager.Instance.LevelUp -= OnLevelUp;
            EventManager.Instance.XpGained -= OnXpGained;
            EventManager.Instance.SkullCollected -= OnSkullCollected;
            EventManager.Instance.ResourceCollected -= OnResourceCollected;
            EventManager.Instance.CargoDeposited -= OnCargoDeposited;
        }
    }

    private void Start()
    {
        // Initial update from GameManager
        UpdateAllFromGameManager();
    }

    private void FindUIElements()
    {
        // Find Canvas root to search ALL UI elements (not just children of this object)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("[RuntimeTopHUD] No Canvas found!");
            return;
        }

        // Find all TextMeshProUGUI in entire Canvas
        TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var text in texts)
        {
            string name = text.gameObject.name.ToLower();

            if (name.Contains("skull"))
                skullsText = text;
            else if (name.Contains("meat"))
                meatText = text;
            else if (name.Contains("wood"))
                woodText = text;
            else if (name.Contains("gold"))
                goldText = text;
            else if (name.Contains("hp") && !name.Contains("bar"))
                hpText = text;
            else if (name.Contains("xp") && !name.Contains("bar"))
                xpText = text;
            else if (name.Contains("lv") || name.Contains("level"))
                lvText = text;
        }

        // Find bars - look for Fill images
        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string name = img.gameObject.name.ToLower();
            if (name.Contains("hpbar") && name.Contains("fill"))
                hpBarFill = img;
            else if (name.Contains("xpbar") && name.Contains("fill"))
                xpBarFill = img;
            else if ((name.Contains("hp") || name.Contains("health")) && name.Contains("fill"))
                hpBarFill = img;
            else if ((name.Contains("xp") || name.Contains("exp")) && name.Contains("fill"))
                xpBarFill = img;
        }

        Debug.Log($"[RuntimeTopHUD] Found elements - Skulls:{skullsText != null}, Meat:{meatText != null}, Wood:{woodText != null}, Gold:{goldText != null}, HP:{hpText != null}, HPBar:{hpBarFill != null}, XPBar:{xpBarFill != null}");
    }

    public void UpdateAllFromGameManager()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[RuntimeTopHUD] GameManager.Instance is null");
            return;
        }

        // Update storage (skulls, meat, wood, gold)
        OnStorageUpdated(
            GameManager.Instance.Skulls,
            GameManager.Instance.Meat,
            GameManager.Instance.Wood,
            GameManager.Instance.Gold
        );

        // Update HP
        OnHealthChanged(GameManager.Instance.CurrentHealth, GameManager.Instance.MaxHealth);

        // Update XP - use direct values from GameManager
        UpdateXpDisplay(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);

        // Update Level
        OnLevelUp(GameManager.Instance.Level);

        Debug.Log("[RuntimeTopHUD] Updated all from GameManager");
    }

    #region Event Handlers

    private void OnHealthChanged(int current, int max)
    {
        if (hpText != null)
        {
            hpText.text = $"{current}/{max}";
        }

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = max > 0 ? (float)current / max : 0;
        }
    }

    private void OnStorageUpdated(int skulls, int meat, int wood, int gold)
    {
        // Show combined values: permanent storage + temporary cargo
        int totalMeat = meat;
        int totalWood = wood;
        int totalGold = gold;

        if (GameManager.Instance != null)
        {
            totalMeat += GameManager.Instance.CargoMeat;
            totalWood += GameManager.Instance.CargoWood;
            totalGold += GameManager.Instance.CargoGold;
        }

        if (skullsText != null)
            skullsText.text = skulls.ToString();

        if (meatText != null)
            meatText.text = totalMeat.ToString();

        if (woodText != null)
            woodText.text = totalWood.ToString();

        if (goldText != null)
            goldText.text = totalGold.ToString();

        Debug.Log($"[RuntimeTopHUD] Storage updated - Skulls:{skulls}, Total Meat:{totalMeat}, Total Wood:{totalWood}, Total Gold:{totalGold}");
    }

    private void OnLevelUp(int newLevel)
    {
        if (lvText != null)
        {
            lvText.text = newLevel.ToString();
        }
    }

    private void OnXpGained(int amount)
    {
        // XpGained event only gives us the amount gained, need to get current values from GameManager
        if (GameManager.Instance != null)
        {
            UpdateXpDisplay(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);
        }
    }

    private void UpdateXpDisplay(int currentXp, int xpToNext)
    {
        if (xpText != null)
        {
            xpText.text = $"{currentXp}/{xpToNext}";
        }

        if (xpBarFill != null)
        {
            xpBarFill.fillAmount = xpToNext > 0 ? (float)currentXp / xpToNext : 0;
        }
    }

    private void OnSkullCollected(int amount)
    {
        // Skulls update will come through StorageUpdated
        // This is for animation/effects if needed
    }

    private void OnResourceCollected(string resourceType, int amount)
    {
        // When resources are collected, they go to cargo
        // Update display to show storage + cargo combined
        if (GameManager.Instance != null)
        {
            // Show combined values: permanent storage + temporary cargo
            int totalMeat = GameManager.Instance.Meat + GameManager.Instance.CargoMeat;
            int totalWood = GameManager.Instance.Wood + GameManager.Instance.CargoWood;
            int totalGold = GameManager.Instance.Gold + GameManager.Instance.CargoGold;

            if (meatText != null)
                meatText.text = totalMeat.ToString();

            if (woodText != null)
                woodText.text = totalWood.ToString();

            if (goldText != null)
                goldText.text = totalGold.ToString();

            Debug.Log($"[RuntimeTopHUD] Resource collected: {amount} {resourceType}. Displayed totals - Meat:{totalMeat}, Wood:{totalWood}, Gold:{totalGold}");
        }
    }

    private void OnCargoDeposited(int totalAmount)
    {
        // Could show cargo indicator here
        // totalAmount is the amount deposited
    }

    #endregion
}
