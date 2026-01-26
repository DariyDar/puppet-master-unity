using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Singleton UI manager for controlling all UI panels.
/// Handles panel visibility, pause state, and event subscriptions.
/// Tiny Swords style UI system.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private TopHUD topHUD;
    [SerializeField] private BottomHUD bottomHUD;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private WorkbenchUI workbenchUI;

    [Header("Canvas")]
    [SerializeField] private Canvas mainCanvas;

    [Header("State")]
    [SerializeField] private bool isPaused;

    // Panel dictionary for easy access
    private Dictionary<string, GameObject> panelDictionary = new Dictionary<string, GameObject>();

    // Input System
    private InputAction cancelAction;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup canvas
        SetupCanvas();

        // Register panels
        RegisterPanels();

        // Setup Input System - use Keyboard directly for Escape
        cancelAction = new InputAction("Cancel", InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Gamepad>/start");
    }

    private void OnDestroy()
    {
        if (cancelAction != null)
        {
            cancelAction.Disable();
            cancelAction.Dispose();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OpenModal += OnOpenModal;
            EventManager.Instance.CloseModal += OnCloseModal;
            EventManager.Instance.GamePaused += OnGamePaused;
        }

        // Enable input action
        if (cancelAction != null)
        {
            cancelAction.Enable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OpenModal -= OnOpenModal;
            EventManager.Instance.CloseModal -= OnCloseModal;
            EventManager.Instance.GamePaused -= OnGamePaused;
        }

        // Disable input action
        if (cancelAction != null)
        {
            cancelAction.Disable();
        }
    }

    private void Start()
    {
        // Re-subscribe if EventManager wasn't ready in OnEnable
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OpenModal -= OnOpenModal;
            EventManager.Instance.CloseModal -= OnCloseModal;
            EventManager.Instance.GamePaused -= OnGamePaused;

            EventManager.Instance.OpenModal += OnOpenModal;
            EventManager.Instance.CloseModal += OnCloseModal;
            EventManager.Instance.GamePaused += OnGamePaused;
        }

        // Initial panel states
        HidePanel("pauseMenu");
        HidePanel("workbenchUI");
    }

    private void Update()
    {
        // Handle pause toggle with Escape key (using new Input System)
        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            TogglePause();
        }
    }

    private void SetupCanvas()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
        }

        if (mainCanvas != null)
        {
            // Set to Screen Space - Overlay for HUD
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
        }
    }

    private void RegisterPanels()
    {
        panelDictionary.Clear();

        if (topHUD != null)
            panelDictionary.Add("topHUD", topHUD.gameObject);

        if (bottomHUD != null)
            panelDictionary.Add("bottomHUD", bottomHUD.gameObject);

        if (pauseMenu != null)
            panelDictionary.Add("pauseMenu", pauseMenu.gameObject);

        if (workbenchUI != null)
            panelDictionary.Add("workbenchUI", workbenchUI.gameObject);
    }

    #region Panel Management

    /// <summary>
    /// Show a panel by name
    /// </summary>
    public void ShowPanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(true);
            Debug.Log($"[UIManager] Showing panel: {panelName}");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Panel not found: {panelName}");
        }
    }

    /// <summary>
    /// Hide a panel by name
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(false);
            Debug.Log($"[UIManager] Hiding panel: {panelName}");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Panel not found: {panelName}");
        }
    }

    /// <summary>
    /// Toggle a panel's visibility
    /// </summary>
    public void TogglePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    /// <summary>
    /// Check if a panel is visible
    /// </summary>
    public bool IsPanelVisible(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out GameObject panel))
        {
            return panel.activeSelf;
        }
        return false;
    }

    /// <summary>
    /// Hide all modal panels (workbench, pause, etc.)
    /// </summary>
    public void HideAllModals()
    {
        HidePanel("pauseMenu");
        HidePanel("workbenchUI");
    }

    #endregion

    #region Pause Management

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;
        ShowPanel("pauseMenu");

        EventManager.Instance?.OnGamePaused(true);
        Debug.Log("[UIManager] Game paused");
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        HidePanel("pauseMenu");

        EventManager.Instance?.OnGamePaused(false);
        Debug.Log("[UIManager] Game resumed");
    }

    /// <summary>
    /// Check if game is paused
    /// </summary>
    public bool IsPaused => isPaused;

    #endregion

    #region Event Handlers

    private void OnOpenModal(string modalType, object data)
    {
        switch (modalType.ToLower())
        {
            case "workbench":
                ShowPanel("workbenchUI");
                if (workbenchUI != null && data is Workbench workbench)
                {
                    workbenchUI.ShowRecipes(workbench);
                }
                break;

            case "pause":
                PauseGame();
                break;

            default:
                Debug.LogWarning($"[UIManager] Unknown modal type: {modalType}");
                break;
        }
    }

    private void OnCloseModal()
    {
        HideAllModals();
    }

    private void OnGamePaused(bool paused)
    {
        // Sync state if changed externally
        if (isPaused != paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (paused)
            {
                ShowPanel("pauseMenu");
            }
            else
            {
                HidePanel("pauseMenu");
            }
        }
    }

    #endregion

    #region Panel References

    /// <summary>
    /// Get TopHUD reference
    /// </summary>
    public TopHUD TopHUD => topHUD;

    /// <summary>
    /// Get BottomHUD reference
    /// </summary>
    public BottomHUD BottomHUD => bottomHUD;

    /// <summary>
    /// Get PauseMenu reference
    /// </summary>
    public PauseMenu PauseMenu => pauseMenu;

    /// <summary>
    /// Get WorkbenchUI reference
    /// </summary>
    public WorkbenchUI WorkbenchUI => workbenchUI;

    #endregion
}
