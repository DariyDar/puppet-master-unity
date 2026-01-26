using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pause menu with resume, settings, and quit options.
/// Tiny Swords style UI with animated transitions.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button settingsBackButton;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmDialog;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // State
    private System.Action pendingConfirmAction;
    private Coroutine fadeCoroutine;

    // Input System
    private InputAction escapeAction;

    private void Awake()
    {
        // Setup button listeners
        SetupButtons();

        // Get canvas group
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Setup Input System
        escapeAction = new InputAction("Escape", InputActionType.Button);
        escapeAction.AddBinding("<Keyboard>/escape");
    }

    private void OnDestroy()
    {
        if (escapeAction != null)
        {
            escapeAction.Disable();
            escapeAction.Dispose();
        }
    }

    private void OnEnable()
    {
        // Show main panel, hide others
        ShowMainPanel();

        // Fade in animation
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());

        // Enable input
        if (escapeAction != null)
        {
            escapeAction.Enable();
        }
    }

    private void OnDisable()
    {
        // Disable input
        if (escapeAction != null)
        {
            escapeAction.Disable();
        }
    }

    private void Update()
    {
        // Handle Escape to close (using new Input System)
        if (escapeAction != null && escapeAction.WasPressedThisFrame())
        {
            if (confirmDialog != null && confirmDialog.activeSelf)
            {
                HideConfirmDialog();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                OnSettingsBack();
            }
            else
            {
                OnResume();
            }
        }
    }

    private void SetupButtons()
    {
        // Main buttons
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResume);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSave);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        // Settings buttons
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnSettingsBack);
        }

        // Settings sliders
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        // Confirmation dialog
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
    }

    #region Main Panel Actions

    /// <summary>
    /// Resume game
    /// </summary>
    public void OnResume()
    {
        StartCoroutine(FadeOutAndClose());
    }

    /// <summary>
    /// Open settings panel
    /// </summary>
    public void OnSettings()
    {
        ShowSettingsPanel();
    }

    /// <summary>
    /// Quit to desktop
    /// </summary>
    public void OnQuit()
    {
        ShowConfirmDialog("Are you sure you want to quit?", () =>
        {
            // Save game before quit
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
            }

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        });
    }

    /// <summary>
    /// Save game manually
    /// </summary>
    public void OnSave()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            ShowNotification("Game Saved!");
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void OnMainMenu()
    {
        ShowConfirmDialog("Return to main menu?\nUnsaved progress will be lost.", () =>
        {
            // Resume time before changing scene
            Time.timeScale = 1f;

            // Load main menu scene
            SceneManager.LoadScene("MainMenu");
        });
    }

    #endregion

    #region Settings Panel

    private void ShowSettingsPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        // Load current settings
        LoadSettings();
    }

    private void OnSettingsBack()
    {
        // Save settings
        SaveSettings();

        ShowMainPanel();
    }

    private void LoadSettings()
    {
        // Load from PlayerPrefs
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }
    }

    private void SaveSettings()
    {
        if (musicVolumeSlider != null)
        {
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        }

        PlayerPrefs.Save();
    }

    private void OnMusicVolumeChanged(float value)
    {
        // Apply to audio mixer or manager
        AudioListener.volume = value; // Simple fallback
        Debug.Log($"[PauseMenu] Music volume: {value}");
    }

    private void OnSFXVolumeChanged(float value)
    {
        Debug.Log($"[PauseMenu] SFX volume: {value}");
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    #endregion

    #region Confirmation Dialog

    private void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        pendingConfirmAction = onConfirm;

        if (confirmText != null)
        {
            confirmText.text = message;
        }

        if (confirmDialog != null)
        {
            confirmDialog.SetActive(true);
        }
    }

    private void HideConfirmDialog()
    {
        if (confirmDialog != null)
        {
            confirmDialog.SetActive(false);
        }
        pendingConfirmAction = null;
    }

    private void OnConfirmYes()
    {
        System.Action action = pendingConfirmAction;
        HideConfirmDialog();
        action?.Invoke();
    }

    private void OnConfirmNo()
    {
        HideConfirmDialog();
    }

    #endregion

    #region Panel Management

    private void ShowMainPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }

    private void ShowNotification(string message)
    {
        // Could be expanded to show a toast notification
        Debug.Log($"[PauseMenu] {message}");
    }

    #endregion

    #region Animations

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            float t = elapsed / fadeInDuration;
            canvasGroup.alpha = fadeInCurve.Evaluate(t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        if (canvasGroup == null)
        {
            CloseMenu();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            float t = elapsed / fadeOutDuration;
            canvasGroup.alpha = 1f - fadeOutCurve.Evaluate(t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        CloseMenu();
    }

    private void CloseMenu()
    {
        // Resume game
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResumeGame();
        }
        else
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set resume button interactable state
    /// </summary>
    public void SetResumeInteractable(bool interactable)
    {
        if (resumeButton != null)
        {
            resumeButton.interactable = interactable;
        }
    }

    /// <summary>
    /// Force show/hide specific panel
    /// </summary>
    public void ShowPanel(string panelName)
    {
        switch (panelName.ToLower())
        {
            case "main":
                ShowMainPanel();
                break;
            case "settings":
                ShowSettingsPanel();
                break;
        }
    }

    #endregion
}
