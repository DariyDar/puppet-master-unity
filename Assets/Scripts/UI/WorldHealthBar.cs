using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that appears above enemies and buildings when damaged.
/// Automatically hides after a delay if no damage is taken.
/// Billboard style - always faces camera.
///
/// SPRITE MAPPING (Cryo's Mini GUI - Bars_4x.png):
/// ================================================
/// Small World Bars (right side of spritesheet, x=352):
///   - Bars_4x_6 (y=180, 96x12): Small red frame
///   - Bars_4x_7 (y=132, 96x12): Small blue frame
///   - Bars_4x_12 (y=84, 83x12): Small yellow frame
///
/// Color Strips (bottom of spritesheet, y=0-16, 64x16 each):
///   - Bars_4x_9 (x=0): Blue strip
///   - Bars_4x_13 (x=64): Green strip
///   - Bars_4x_14 (x=128): Yellow strip
///   - Bars_4x_15 (x=192): Orange strip
///   - Bars_4x_16 (x=256): Red strip
///   - Bars_4x_17 (x=320): Purple strip
///   - Bars_4x_18 (x=384): Pink strip
///
/// Dynamic HP colors: Green (>75%), Yellow (25-75%), Red (<25%)
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;

    [Header("Settings")]
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private bool billboardMode = true;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color midHealthColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private float midHealthThreshold = 0.5f;
    [SerializeField] private float lowHealthThreshold = 0.25f;

    // State
    private float lastDamageTime;
    private bool isVisible = false;
    private float targetAlpha = 0f;
    private Transform targetTransform;
    private Camera mainCamera;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>();

        mainCamera = Camera.main;

        // Start hidden
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        isVisible = false;
    }

    private void Start()
    {
        // Setup canvas for world space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
        }
    }

    private void LateUpdate()
    {
        // Billboard - face camera
        if (billboardMode && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        // Follow target with offset
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;
        }

        // Auto-hide after delay
        if (isVisible && Time.time - lastDamageTime > hideDelay)
        {
            Hide();
        }

        // Animate alpha
        if (canvasGroup != null)
        {
            float currentAlpha = canvasGroup.alpha;
            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }
            else
            {
                canvasGroup.alpha = targetAlpha;
            }
        }
    }

    /// <summary>
    /// Initialize the health bar for a target transform.
    /// </summary>
    public void Initialize(Transform target)
    {
        targetTransform = target;
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    /// <summary>
    /// Update health display. Shows the bar and resets hide timer.
    /// </summary>
    public void UpdateHealth(float current, float max)
    {
        if (max <= 0) return;

        float ratio = Mathf.Clamp01(current / max);

        // Update fill using anchor scaling (fill decreases from right)
        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            // Keep left anchor at 0, move right anchor based on health ratio
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillImage.color = GetHealthColor(ratio);
        }

        // Show bar and reset timer
        lastDamageTime = Time.time;
        Show();
    }

    /// <summary>
    /// Update health display with int values.
    /// </summary>
    public void UpdateHealth(int current, int max)
    {
        UpdateHealth((float)current, (float)max);
    }

    /// <summary>
    /// Show the health bar immediately.
    /// </summary>
    public void Show()
    {
        isVisible = true;
        targetAlpha = 1f;
        lastDamageTime = Time.time;
    }

    /// <summary>
    /// Hide the health bar with fade.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        targetAlpha = 0f;
    }

    /// <summary>
    /// Instantly hide without fade.
    /// </summary>
    public void HideInstant()
    {
        isVisible = false;
        targetAlpha = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Get color based on health ratio.
    /// </summary>
    private Color GetHealthColor(float ratio)
    {
        if (ratio <= lowHealthThreshold)
            return lowHealthColor;
        else if (ratio <= midHealthThreshold)
            return Color.Lerp(lowHealthColor, midHealthColor, (ratio - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold));
        else
            return Color.Lerp(midHealthColor, fullHealthColor, (ratio - midHealthThreshold) / (1f - midHealthThreshold));
    }

    /// <summary>
    /// Set the offset from target position.
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>
    /// Set the hide delay.
    /// </summary>
    public void SetHideDelay(float delay)
    {
        hideDelay = delay;
    }

    /// <summary>
    /// Check if currently visible.
    /// </summary>
    public bool IsVisible => isVisible;

    #region Static Factory Methods

    /// <summary>
    /// Create a world health bar at runtime.
    /// Uses a frame sprite (Bars_4x_6) with colored fill that decreases.
    /// </summary>
    public static WorldHealthBar Create(Transform parent, Transform target, Vector3 offset)
    {
        // Create container
        GameObject go = new GameObject("WorldHealthBar");
        if (parent != null)
            go.transform.SetParent(parent);

        WorldHealthBar healthBar = go.AddComponent<WorldHealthBar>();

        // Create canvas
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(go.transform);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // Render above everything

        // Set canvas RectTransform size (in world units) - smaller for world bars
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.5f, 0.25f); // Smaller world space size
        canvasRect.localScale = Vector3.one;

        CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Load frame sprite from Resources or use programmatic approach
        Sprite frameSprite = null;

        // Try to load Bars_4x_6 sprite at runtime
        // Since we can't use AssetDatabase at runtime, we'll try Resources
        frameSprite = Resources.Load<Sprite>("UI/Bars_4x_6");

        // Create black background for empty portion
        GameObject blackBgGO = new GameObject("BlackBg");
        blackBgGO.transform.SetParent(canvasGO.transform);
        blackBgGO.transform.localPosition = Vector3.zero;
        blackBgGO.transform.localRotation = Quaternion.identity;
        blackBgGO.transform.localScale = Vector3.one;

        RectTransform blackBgRect = blackBgGO.AddComponent<RectTransform>();
        blackBgRect.anchorMin = Vector2.zero;
        blackBgRect.anchorMax = Vector2.one;
        blackBgRect.offsetMin = new Vector2(0.08f, 0.03f); // Padding inside frame
        blackBgRect.offsetMax = new Vector2(-0.08f, -0.03f);

        Image blackBgImage = blackBgGO.AddComponent<Image>();
        blackBgImage.color = new Color(0.05f, 0.05f, 0.05f, 1f); // Almost black

        // Create fill - uses anchor scaling for smooth decrease
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform);
        fillGO.transform.localPosition = Vector3.zero;
        fillGO.transform.localRotation = Quaternion.identity;
        fillGO.transform.localScale = Vector3.one;

        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f); // Will be adjusted by UpdateHealth
        fillRect.offsetMin = new Vector2(0.08f, 0.03f); // Same padding as black bg
        fillRect.offsetMax = new Vector2(-0.08f, -0.03f);
        fillRect.pivot = new Vector2(0f, 0.5f);

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.9f, 0.2f); // Green by default

        // Create frame on top (outline effect)
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(canvasGO.transform);
        frameGO.transform.localPosition = Vector3.zero;
        frameGO.transform.localRotation = Quaternion.identity;
        frameGO.transform.localScale = Vector3.one;

        RectTransform frameRect = frameGO.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImage = frameGO.AddComponent<Image>();
        if (frameSprite != null)
        {
            frameImage.sprite = frameSprite;
            frameImage.type = Image.Type.Sliced;
        }
        else
        {
            // Fallback: use Outline component for border effect
            frameImage.color = new Color(0, 0, 0, 0); // Transparent center
            UnityEngine.UI.Outline outline = frameGO.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.15f, 0.05f, 1f); // Dark brown
            outline.effectDistance = new Vector2(0.02f, 0.02f);
        }

        // Setup references - use fillImage for health updates
        healthBar.canvas = canvas;
        healthBar.canvasGroup = canvasGroup;
        healthBar.backgroundImage = blackBgImage;
        healthBar.fillImage = fillImage;
        healthBar.offset = offset;

        // Initialize
        healthBar.Initialize(target);
        healthBar.HideInstant();

        return healthBar;
    }

    /// <summary>
    /// Create a world health bar with Cryo's Mini GUI sprites.
    /// frameSprite: Use Bars_4x_6 (small red frame from spritesheet)
    /// fillSprite: Use any color strip (Bars_4x_13 for green, etc.)
    /// </summary>
    public static WorldHealthBar CreateWithSprites(Transform parent, Transform target, Vector3 offset, Sprite frameSprite, Sprite fillSprite = null)
    {
        // Create container
        GameObject go = new GameObject("WorldHealthBar");
        if (parent != null)
            go.transform.SetParent(parent);

        WorldHealthBar healthBar = go.AddComponent<WorldHealthBar>();

        // Create canvas
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(go.transform);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        // Set canvas RectTransform size - smaller for sprite-based bar
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.5f, 0.2f); // Smaller world space size
        canvasRect.localScale = Vector3.one;

        CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Create background/frame with sprite
        GameObject bgGO = new GameObject("Frame");
        bgGO.transform.SetParent(canvasGO.transform);
        bgGO.transform.localPosition = Vector3.zero;
        bgGO.transform.localRotation = Quaternion.identity;
        bgGO.transform.localScale = Vector3.one;

        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.pivot = new Vector2(0.5f, 0.5f);

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.sprite = frameSprite;
        bgImage.type = Image.Type.Sliced;

        // Create fill - inside the frame
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform);
        fillGO.transform.localPosition = Vector3.zero;
        fillGO.transform.localRotation = Quaternion.identity;
        fillGO.transform.localScale = Vector3.one;

        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(0.05f, 0.02f); // Padding inside frame
        fillRect.offsetMax = new Vector2(-0.05f, -0.02f);
        fillRect.pivot = new Vector2(0f, 0.5f);

        Image fillImage = fillGO.AddComponent<Image>();
        if (fillSprite != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Sliced;
        }
        fillImage.color = new Color(0.2f, 0.9f, 0.2f); // Default green, will be tinted
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;

        // Setup references
        healthBar.canvas = canvas;
        healthBar.canvasGroup = canvasGroup;
        healthBar.backgroundImage = bgImage;
        healthBar.fillImage = fillImage;
        healthBar.offset = offset;

        // Initialize
        healthBar.Initialize(target);
        healthBar.HideInstant();

        return healthBar;
    }

    #endregion
}
