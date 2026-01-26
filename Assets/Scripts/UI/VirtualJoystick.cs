using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Virtual joystick for mobile touch input.
/// Implements drag handlers for touch control.
/// Sends input to PlayerController.SetMoveInput().
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private bool hideWhenIdle = false;
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Visual")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float idleAlpha = 0.5f;

    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    // Input state
    private Vector2 inputDirection = Vector2.zero;
    private bool isActive = false;
    private Canvas parentCanvas;
    private Camera uiCamera;

    // Properties
    public Vector2 InputDirection => inputDirection;
    public bool IsActive => isActive;

    private void Awake()
    {
        // Get components if not assigned
        if (background == null)
            background = GetComponent<RectTransform>();

        if (handle == null)
        {
            Transform handleTransform = transform.Find("Handle");
            if (handleTransform != null)
                handle = handleTransform as RectTransform;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Get parent canvas for coordinate conversion
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }
    }

    private void Start()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        // Set initial visual state
        if (hideWhenIdle && canvasGroup != null)
        {
            canvasGroup.alpha = idleAlpha;
        }

        // Reset handle position
        ResetHandle();
    }

    private void Update()
    {
        // Send input to player controller
        if (playerController != null && isActive)
        {
            playerController.SetMoveInput(inputDirection);
        }

        // Fade visual
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (canvasGroup == null || !hideWhenIdle) return;

        float targetAlpha = isActive ? activeAlpha : idleAlpha;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    #region Pointer Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        isActive = true;

        // Process initial touch position
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        // Convert screen position to local position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out localPoint
        );

        // Calculate input direction
        Vector2 direction = localPoint;

        // Clamp to handle range
        float magnitude = direction.magnitude;
        if (magnitude > handleRange)
        {
            direction = direction.normalized * handleRange;
        }

        // Move handle
        handle.anchoredPosition = direction;

        // Calculate normalized input direction (0-1 range)
        inputDirection = direction / handleRange;

        // Apply dead zone
        if (inputDirection.magnitude < deadZone)
        {
            inputDirection = Vector2.zero;
        }
        else
        {
            // Rescale input to account for dead zone
            inputDirection = inputDirection.normalized *
                ((inputDirection.magnitude - deadZone) / (1f - deadZone));
        }

        // Clamp magnitude
        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isActive = false;
        inputDirection = Vector2.zero;

        // Reset handle position
        ResetHandle();

        // Clear player input
        if (playerController != null)
        {
            playerController.SetMoveInput(Vector2.zero);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reset handle to center position
    /// </summary>
    public void ResetHandle()
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Set the player controller reference
    /// </summary>
    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }

    /// <summary>
    /// Set handle movement range
    /// </summary>
    public void SetHandleRange(float range)
    {
        handleRange = Mathf.Max(1f, range);
    }

    /// <summary>
    /// Set dead zone (0-1)
    /// </summary>
    public void SetDeadZone(float zone)
    {
        deadZone = Mathf.Clamp01(zone);
    }

    /// <summary>
    /// Enable/disable joystick
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        gameObject.SetActive(enabled);

        if (!enabled)
        {
            isActive = false;
            inputDirection = Vector2.zero;
            ResetHandle();
        }
    }

    /// <summary>
    /// Set visibility when idle
    /// </summary>
    public void SetHideWhenIdle(bool hide)
    {
        hideWhenIdle = hide;
    }

    /// <summary>
    /// Get raw input value (before dead zone)
    /// </summary>
    public Vector2 GetRawInput()
    {
        if (handle == null || background == null) return Vector2.zero;
        return handle.anchoredPosition / handleRange;
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Create a virtual joystick at runtime
    /// </summary>
    public static VirtualJoystick Create(Canvas canvas, Vector2 position, float size = 150f, float handleSize = 50f)
    {
        // Create background
        GameObject joystickObj = new GameObject("VirtualJoystick");
        joystickObj.transform.SetParent(canvas.transform, false);

        RectTransform bgRect = joystickObj.AddComponent<RectTransform>();
        bgRect.anchoredPosition = position;
        bgRect.sizeDelta = new Vector2(size, size);

        Image bgImage = joystickObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Create handle
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(joystickObj.transform, false);

        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(handleSize, handleSize);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        // Add joystick component
        VirtualJoystick joystick = joystickObj.AddComponent<VirtualJoystick>();
        joystick.background = bgRect;
        joystick.handle = handleRect;
        joystick.handleRange = (size - handleSize) * 0.5f;

        // Add canvas group for fading
        CanvasGroup group = joystickObj.AddComponent<CanvasGroup>();
        joystick.canvasGroup = group;

        return joystick;
    }

    #endregion

    #region Editor Helpers

    private void OnValidate()
    {
        handleRange = Mathf.Max(1f, handleRange);
        deadZone = Mathf.Clamp01(deadZone);
    }

    private void OnDrawGizmosSelected()
    {
        if (background == null) return;

        // Draw handle range
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Gizmos.DrawWireSphere(center, handleRange * 0.01f); // Scale for visibility

        // Draw dead zone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, handleRange * deadZone * 0.01f);
    }

    #endregion
}
