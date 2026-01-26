using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top HUD panel displaying health, XP, level, and resources.
/// Tiny Swords style UI with animated resource changes.
/// </summary>
public class TopHUD : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthFullColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color healthLowColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("XP")]
    [SerializeField] private Image xpBarFill;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Color xpColor = new Color(0.4f, 0.6f, 1f);

    [Header("Resources - Per GDD v5")]
    [SerializeField] private ResourceDisplay skullsDisplay;
    [SerializeField] private ResourceDisplay meatDisplay;
    [SerializeField] private ResourceDisplay woodDisplay;
    [SerializeField] private ResourceDisplay goldDisplay;

    [Header("Resource Texts (Fallback)")]
    [SerializeField] private TextMeshProUGUI skullsText;
    [SerializeField] private TextMeshProUGUI meatText;
    [SerializeField] private TextMeshProUGUI woodText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Army Display")]
    [SerializeField] private TextMeshProUGUI armyText;

    [Header("Quest Display")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questProgressText;
    [SerializeField] private Image questProgressBar;

    [Header("Animation")]
    [SerializeField] private float barAnimationSpeed = 5f;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeDuration = 0.2f;

    // Cached values for animation
    private float targetHealthFill;
    private float currentHealthFill;
    private float targetXpFill;
    private float currentXpFill;
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
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
            EventManager.Instance.ArmyUpdated += OnArmyUpdated;
        }

        // Subscribe to quest events
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestStarted += OnQuestStarted;
            QuestSystem.Instance.OnQuestProgress += OnQuestProgress;
            QuestSystem.Instance.OnQuestCompleted += OnQuestCompleted;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.PlayerHealthChanged -= OnHealthChanged;
            EventManager.Instance.StorageUpdated -= OnStorageUpdated;
            EventManager.Instance.LevelUp -= OnLevelUp;
            EventManager.Instance.XpGained -= OnXpGained;
            EventManager.Instance.SkullCollected -= OnSkullCollected;
            EventManager.Instance.ResourceCollected -= OnResourceCollected;
            EventManager.Instance.ArmyUpdated -= OnArmyUpdated;
        }

        // Unsubscribe from quest events
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestStarted -= OnQuestStarted;
            QuestSystem.Instance.OnQuestProgress -= OnQuestProgress;
            QuestSystem.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    private void Start()
    {
        // Initial update from GameManager
        UpdateAllFromGameManager();
    }

    private void Update()
    {
        // Smooth bar animations
        AnimateBars();
    }

    private void AnimateBars()
    {
        // Smooth health bar
        if (healthBarFill != null && Mathf.Abs(currentHealthFill - targetHealthFill) > 0.001f)
        {
            currentHealthFill = Mathf.Lerp(currentHealthFill, targetHealthFill, Time.deltaTime * barAnimationSpeed);
            healthBarFill.fillAmount = currentHealthFill;

            // Update color based on health
            healthBarFill.color = Color.Lerp(healthLowColor, healthFullColor, currentHealthFill);
        }

        // Smooth XP bar
        if (xpBarFill != null && Mathf.Abs(currentXpFill - targetXpFill) > 0.001f)
        {
            currentXpFill = Mathf.Lerp(currentXpFill, targetXpFill, Time.deltaTime * barAnimationSpeed);
            xpBarFill.fillAmount = currentXpFill;
        }
    }

    #region Data Updates

    public void UpdateAllFromGameManager()
    {
        if (GameManager.Instance == null) return;

        // Update health
        OnHealthChanged(GameManager.Instance.CurrentHealth, GameManager.Instance.MaxHealth);

        // Update XP
        UpdateXP(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);

        // Update level
        UpdateLevel(GameManager.Instance.Level);

        // Update resources - GDD v5 (4 resources)
        OnStorageUpdated(
            GameManager.Instance.Skulls,
            GameManager.Instance.Meat,
            GameManager.Instance.Wood,
            GameManager.Instance.Gold
        );

        // Update army
        OnArmyUpdated(GameManager.Instance.ArmyCount, GameManager.Instance.ArmyLimit);

        // Update quest
        if (QuestSystem.Instance != null && QuestSystem.Instance.CurrentQuest != null)
        {
            OnQuestStarted(QuestSystem.Instance.CurrentQuest);
            OnQuestProgress(QuestSystem.Instance.CurrentProgress, QuestSystem.Instance.CurrentQuest.targetAmount);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        targetHealthFill = max > 0 ? (float)current / max : 0f;

        // If damage taken, trigger shake
        if (current < max && currentHealthFill > targetHealthFill)
        {
            StartCoroutine(ShakeEffect());
        }

        // Instant update if first time
        if (currentHealthFill == 0 && current > 0)
        {
            currentHealthFill = targetHealthFill;
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentHealthFill;
            }
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }

        // Update color for low health warning
        if (healthBarFill != null && targetHealthFill <= lowHealthThreshold)
        {
            StartCoroutine(PulseHealthBar());
        }
    }

    private void OnStorageUpdated(int skulls, int meat, int wood, int gold)
    {
        // Update ResourceDisplay components if available - Per GDD v5 (4 resources)
        if (skullsDisplay != null)
            skullsDisplay.UpdateAmount(skulls);
        else if (skullsText != null)
            skullsText.text = skulls.ToString();

        if (meatDisplay != null)
            meatDisplay.UpdateAmount(meat);
        else if (meatText != null)
            meatText.text = meat.ToString();

        if (woodDisplay != null)
            woodDisplay.UpdateAmount(wood);
        else if (woodText != null)
            woodText.text = wood.ToString();

        if (goldDisplay != null)
            goldDisplay.UpdateAmount(gold);
        else if (goldText != null)
            goldText.text = gold.ToString();
    }

    private void OnArmyUpdated(int count, int limit)
    {
        if (armyText != null)
        {
            armyText.text = $"{count}/{limit}";
        }
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevel(newLevel);

        // Level up effect
        StartCoroutine(LevelUpEffect());
    }

    private void OnXpGained(int amount)
    {
        if (GameManager.Instance != null)
        {
            UpdateXP(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);
        }
    }

    private void OnSkullCollected(int amount)
    {
        if (skullsDisplay != null)
        {
            skullsDisplay.AnimateDelta(amount);
        }
    }

    private void OnResourceCollected(string type, int amount)
    {
        // Animate the specific resource display - Per GDD v5 (4 resources)
        switch (type.ToLower())
        {
            case "meat":
                if (meatDisplay != null) meatDisplay.AnimateDelta(amount);
                break;
            case "wood":
                if (woodDisplay != null) woodDisplay.AnimateDelta(amount);
                break;
            case "gold":
                if (goldDisplay != null) goldDisplay.AnimateDelta(amount);
                break;
        }
    }

    #region Quest Display

    private void OnQuestStarted(QuestDefinition quest)
    {
        if (questTitleText != null)
        {
            questTitleText.text = quest.title;
        }

        if (questProgressText != null)
        {
            questProgressText.text = $"0/{quest.targetAmount}";
        }

        if (questProgressBar != null)
        {
            questProgressBar.fillAmount = 0f;
        }
    }

    private void OnQuestProgress(int current, int target)
    {
        if (questProgressText != null)
        {
            questProgressText.text = $"{current}/{target}";
        }

        if (questProgressBar != null)
        {
            questProgressBar.fillAmount = target > 0 ? (float)current / target : 0f;
        }
    }

    private void OnQuestCompleted(QuestDefinition quest, int xpReward)
    {
        // Show completion effect
        StartCoroutine(QuestCompleteEffect(xpReward));
    }

    private IEnumerator QuestCompleteEffect(int xpReward)
    {
        if (questTitleText != null)
        {
            string originalText = questTitleText.text;
            questTitleText.text = $"COMPLETE! +{xpReward} XP";
            questTitleText.color = Color.green;

            yield return new WaitForSeconds(2f);

            questTitleText.color = Color.white;
        }
    }

    #endregion

    private void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }

        // Reset XP bar on level up
        if (xpBarFill != null)
        {
            currentXpFill = 0f;
            xpBarFill.fillAmount = 0f;
        }
    }

    private void UpdateXP(int current, int max)
    {
        targetXpFill = max > 0 ? (float)current / max : 0f;
    }

    #endregion

    #region Visual Effects

    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    private IEnumerator PulseHealthBar()
    {
        if (healthBarFill == null) yield break;

        Color original = healthBarFill.color;

        for (int i = 0; i < 3; i++)
        {
            healthBarFill.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            healthBarFill.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator LevelUpEffect()
    {
        if (levelText == null) yield break;

        // Scale animation
        Vector3 originalScale = levelText.transform.localScale;

        levelText.transform.localScale = originalScale * 1.5f;
        yield return new WaitForSeconds(0.15f);

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            levelText.transform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        levelText.transform.localScale = originalScale;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Force refresh all displays
    /// </summary>
    public void RefreshAll()
    {
        UpdateAllFromGameManager();
    }

    /// <summary>
    /// Set health bar colors
    /// </summary>
    public void SetHealthColors(Color fullColor, Color lowColor)
    {
        healthFullColor = fullColor;
        healthLowColor = lowColor;
    }

    /// <summary>
    /// Set XP bar color
    /// </summary>
    public void SetXPColor(Color color)
    {
        xpColor = color;
        if (xpBarFill != null)
        {
            xpBarFill.color = xpColor;
        }
    }

    #endregion
}
