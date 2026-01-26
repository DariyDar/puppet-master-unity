using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quest display UI - shows current quest, progress, and direction arrow.
/// Per GDD pages 46-47.
/// </summary>
public class QuestUI : MonoBehaviour
{
    [Header("Quest Panel")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questProgressText;
    [SerializeField] private Image progressBar;

    [Header("Direction Arrow")]
    [SerializeField] private GameObject directionArrow;
    [SerializeField] private float arrowDistance = 100f;  // Distance from center of screen

    [Header("Completion Popup")]
    [SerializeField] private GameObject completionPopup;
    [SerializeField] private TextMeshProUGUI completionTitleText;
    [SerializeField] private TextMeshProUGUI completionRewardText;
    [SerializeField] private float popupDuration = 3f;

    [Header("Animation")]
    [SerializeField] private float progressAnimSpeed = 5f;

    // State
    private QuestDefinition currentQuest;
    private float targetProgress;
    private float currentProgress;
    private RectTransform arrowRect;

    private void Awake()
    {
        if (directionArrow != null)
        {
            arrowRect = directionArrow.GetComponent<RectTransform>();
        }

        // Hide completion popup initially
        if (completionPopup != null)
        {
            completionPopup.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Subscribe to quest events
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestStarted += OnQuestStarted;
            QuestSystem.Instance.OnQuestProgress += OnQuestProgress;
            QuestSystem.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestSystem.Instance.OnAllQuestsCompleted += OnAllQuestsCompleted;

            // Initialize with current quest if any
            if (QuestSystem.Instance.CurrentQuest != null)
            {
                OnQuestStarted(QuestSystem.Instance.CurrentQuest);
                OnQuestProgress(QuestSystem.Instance.CurrentProgress, QuestSystem.Instance.CurrentQuest.targetAmount);
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from quest events
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestStarted -= OnQuestStarted;
            QuestSystem.Instance.OnQuestProgress -= OnQuestProgress;
            QuestSystem.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestSystem.Instance.OnAllQuestsCompleted -= OnAllQuestsCompleted;
        }
    }

    private void Update()
    {
        // Animate progress bar
        if (progressBar != null && Mathf.Abs(currentProgress - targetProgress) > 0.001f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressAnimSpeed);
            progressBar.fillAmount = currentProgress;
        }

        // Update direction arrow
        UpdateDirectionArrow();
    }

    #region Event Handlers

    private void OnQuestStarted(QuestDefinition quest)
    {
        currentQuest = quest;
        currentProgress = 0f;
        targetProgress = 0f;

        // Show quest panel
        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }

        // Update texts
        if (questTitleText != null)
        {
            questTitleText.text = quest.title;
        }

        if (questDescriptionText != null)
        {
            questDescriptionText.text = quest.description;
        }

        if (questProgressText != null)
        {
            questProgressText.text = $"0/{quest.targetAmount}";
        }

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }

        // Show direction arrow if quest has target
        UpdateArrowVisibility();

        Debug.Log($"[QuestUI] Started quest: {quest.title}");
    }

    private void OnQuestProgress(int current, int target)
    {
        targetProgress = target > 0 ? (float)current / target : 0f;

        if (questProgressText != null)
        {
            questProgressText.text = $"{current}/{target}";
        }
    }

    private void OnQuestCompleted(QuestDefinition quest, int xpReward)
    {
        // Show completion popup
        StartCoroutine(ShowCompletionPopup(quest, xpReward));
    }

    private void OnAllQuestsCompleted()
    {
        // Hide quest panel
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        // Hide arrow
        if (directionArrow != null)
        {
            directionArrow.SetActive(false);
        }

        // Show "All quests complete" message
        if (questTitleText != null)
        {
            questTitleText.text = "All Quests Complete!";
        }
    }

    #endregion

    #region Direction Arrow

    private void UpdateArrowVisibility()
    {
        if (directionArrow == null) return;

        bool showArrow = currentQuest != null &&
                        (currentQuest.hasTargetPosition || !string.IsNullOrEmpty(currentQuest.targetTag));

        directionArrow.SetActive(showArrow);
    }

    private void UpdateDirectionArrow()
    {
        if (directionArrow == null || arrowRect == null) return;
        if (!directionArrow.activeSelf) return;

        // Get direction from QuestSystem
        if (QuestSystem.Instance == null) return;

        Vector3? direction = QuestSystem.Instance.GetQuestTargetDirection();
        if (!direction.HasValue)
        {
            directionArrow.SetActive(false);
            return;
        }

        // Position arrow around screen center
        Vector2 dir2D = new Vector2(direction.Value.x, direction.Value.y).normalized;
        arrowRect.anchoredPosition = dir2D * arrowDistance;

        // Rotate arrow to point in direction
        float angle = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
    }

    #endregion

    #region Completion Popup

    private IEnumerator ShowCompletionPopup(QuestDefinition quest, int xpReward)
    {
        if (completionPopup == null) yield break;

        // Set popup texts
        if (completionTitleText != null)
        {
            completionTitleText.text = $"Quest Complete: {quest.title}";
        }

        if (completionRewardText != null)
        {
            completionRewardText.text = $"+{xpReward} XP";
        }

        // Show popup with animation
        completionPopup.SetActive(true);
        completionPopup.transform.localScale = Vector3.zero;

        // Scale in
        float elapsed = 0f;
        float scaleInDuration = 0.3f;
        while (elapsed < scaleInDuration)
        {
            float t = elapsed / scaleInDuration;
            completionPopup.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, EaseOutBack(t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        completionPopup.transform.localScale = Vector3.one;

        // Wait
        yield return new WaitForSecondsRealtime(popupDuration);

        // Fade out
        elapsed = 0f;
        float fadeOutDuration = 0.5f;
        CanvasGroup cg = completionPopup.GetComponent<CanvasGroup>();
        if (cg == null) cg = completionPopup.AddComponent<CanvasGroup>();

        while (elapsed < fadeOutDuration)
        {
            float t = elapsed / fadeOutDuration;
            cg.alpha = 1f - t;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Hide
        completionPopup.SetActive(false);
        cg.alpha = 1f;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually refresh quest display.
    /// </summary>
    public void Refresh()
    {
        if (QuestSystem.Instance != null && QuestSystem.Instance.CurrentQuest != null)
        {
            OnQuestStarted(QuestSystem.Instance.CurrentQuest);
            OnQuestProgress(QuestSystem.Instance.CurrentProgress, QuestSystem.Instance.CurrentQuest.targetAmount);
        }
    }

    /// <summary>
    /// Toggle quest panel visibility.
    /// </summary>
    public void TogglePanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(!questPanel.activeSelf);
        }
    }

    #endregion
}
