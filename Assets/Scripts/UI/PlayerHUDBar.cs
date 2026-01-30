using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime component for HP and XP bars in the player HUD.
/// Subscribes to EventManager events and updates the bar display.
///
/// SPRITE MAPPING (Cryo's Mini GUI - Bars_4x.png):
/// ================================================
/// GUI Bars (large, left side of spritesheet):
///   - Bars_4x_0: Red/orange frame (y=144, 160x48)
///   - Bars_4x_1: Red/orange fill (y=160, 96x32)
///   - Bars_4x_3: Blue frame (y=96, 136x48)
///   - Bars_4x_4: Blue fill (y=112, 96x32)
///   - Bars_4x_8: Yellow frame (y=48, 160x48)
///   - Bars_4x_10: Yellow fill (y=64, 96x32)
///
/// Color Strips (bottom of spritesheet, y=0-16):
///   - Use for fill color tinting or small indicators
/// </summary>
public class PlayerHUDBar : MonoBehaviour
{
    public enum BarType
    {
        HP,
        XP,
        Cargo
    }

    [Header("Bar Type")]
    [SerializeField] private BarType barType = BarType.HP;

    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI valueText;

    [Header("Sprite-based HP (optional)")]
    [Tooltip("If true, swaps sprites instead of using color tint")]
    [SerializeField] private bool useColorSprites = false;
    [SerializeField] private Sprite greenFillSprite;   // Bars_4x_21 - HP > 75%
    [SerializeField] private Sprite yellowFillSprite;  // Bars_4x_20 - HP 25-75%
    [SerializeField] private Sprite redFillSprite;     // Bars_4x_23 - HP < 25%

    [Header("HP Color Thresholds")]
    [SerializeField] private float highHealthThreshold = 0.75f;
    [SerializeField] private float lowHealthThreshold = 0.25f;

    [Header("HP Colors (when not using sprites)")]
    [SerializeField] private Color highHealthColor = new Color(0.2f, 0.9f, 0.2f);   // Green
    [SerializeField] private Color midHealthColor = new Color(1f, 0.85f, 0.2f);      // Yellow
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f);     // Red

    [Header("XP Color (when not using sprites)")]
    [SerializeField] private Color xpColor = new Color(0.6f, 0.3f, 0.9f);           // Purple

    [Header("Cargo Bar Sprites")]
    [SerializeField] private Sprite cargoNormalSprite;  // Bars_4x_18 - pink
    [SerializeField] private Sprite cargoFullSprite;    // Bars_4x_24 - dark pink
    [SerializeField] private string cargoFullText = "CARGO IS FULL";

    [Header("Cargo Resource Colors")]
    [SerializeField] private Color meatColor = new Color(0.85f, 0.4f, 0.4f);      // Light red/pink for meat
    [SerializeField] private Color woodColor = new Color(0.55f, 0.35f, 0.2f);     // Brown for wood
    [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.2f);        // Yellow/gold

    private int currentValue;
    private int maxValue;

    // Cargo details for coloring
    private int cargoMeat;
    private int cargoWood;
    private int cargoGold;

    private void Start()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            if (barType == BarType.HP)
            {
                EventManager.Instance.PlayerHealthChanged += OnPlayerHealthChanged;
                Debug.Log($"[PlayerHUDBar] HP bar subscribed to events");
            }
            else if (barType == BarType.XP)
            {
                EventManager.Instance.XpGained += OnXpGained;
                EventManager.Instance.LevelUp += OnLevelUp;
                Debug.Log($"[PlayerHUDBar] XP bar subscribed to events");
            }
            else if (barType == BarType.Cargo)
            {
                EventManager.Instance.CargoChanged += OnCargoChanged;
                EventManager.Instance.CargoDetailChanged += OnCargoDetailChanged;
                Debug.Log($"[PlayerHUDBar] Cargo bar subscribed to events");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerHUDBar] EventManager.Instance is null! Bar type: {barType}");
        }

        // Initial update from GameManager
        UpdateFromGameManager();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            if (barType == BarType.HP)
            {
                EventManager.Instance.PlayerHealthChanged -= OnPlayerHealthChanged;
            }
            else if (barType == BarType.XP)
            {
                EventManager.Instance.XpGained -= OnXpGained;
                EventManager.Instance.LevelUp -= OnLevelUp;
            }
            else if (barType == BarType.Cargo)
            {
                EventManager.Instance.CargoChanged -= OnCargoChanged;
                EventManager.Instance.CargoDetailChanged -= OnCargoDetailChanged;
            }
        }
    }

    private void UpdateFromGameManager()
    {
        if (GameManager.Instance == null) return;

        if (barType == BarType.HP)
        {
            UpdateBar(GameManager.Instance.CurrentHealth, GameManager.Instance.MaxHealth);
        }
        else if (barType == BarType.XP)
        {
            UpdateBar(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);
        }
        else if (barType == BarType.Cargo)
        {
            UpdateBar(GameManager.Instance.CurrentCargo, GameManager.Instance.CargoCapacity);
            // Also load cargo details for coloring
            cargoMeat = GameManager.Instance.CargoMeat;
            cargoWood = GameManager.Instance.CargoWood;
            cargoGold = GameManager.Instance.CargoGold;
            UpdateCargoColor();
        }
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        if (barType == BarType.HP)
        {
            UpdateBar(current, max);
        }
    }

    private void OnXpGained(int amount)
    {
        if (barType == BarType.XP && GameManager.Instance != null)
        {
            Debug.Log($"[PlayerHUDBar] XP gained: {amount}, current: {GameManager.Instance.CurrentXp}/{GameManager.Instance.XpToNextLevel}");
            UpdateBar(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);
        }
    }

    private void OnLevelUp(int newLevel)
    {
        if (barType == BarType.XP && GameManager.Instance != null)
        {
            UpdateBar(GameManager.Instance.CurrentXp, GameManager.Instance.XpToNextLevel);
        }
    }

    private void OnCargoChanged(int current, int max)
    {
        if (barType == BarType.Cargo)
        {
            UpdateBar(current, max);
        }
    }

    private void OnCargoDetailChanged(int meat, int wood, int gold, int capacity)
    {
        if (barType == BarType.Cargo)
        {
            cargoMeat = meat;
            cargoWood = wood;
            cargoGold = gold;

            // Update color based on cargo composition
            UpdateCargoColor();
        }
    }

    /// <summary>
    /// Update cargo bar color based on resource mix.
    /// Color is weighted average of resource colors.
    /// </summary>
    private void UpdateCargoColor()
    {
        if (fillImage == null) return;

        int total = cargoMeat + cargoWood + cargoGold;
        if (total <= 0)
        {
            fillImage.color = Color.white;
            return;
        }

        // Calculate weighted color
        float meatRatio = (float)cargoMeat / total;
        float woodRatio = (float)cargoWood / total;
        float goldRatio = (float)cargoGold / total;

        Color blendedColor = meatColor * meatRatio + woodColor * woodRatio + goldColor * goldRatio;
        fillImage.color = blendedColor;
    }

    private void UpdateBar(int current, int max)
    {
        currentValue = current;
        maxValue = max;

        if (max <= 0) return;

        float ratio = Mathf.Clamp01((float)current / max);
        bool isFull = current >= max;

        // Update fill
        if (fillImage != null)
        {
            // For Sliced/Tiled images, we scale the width via anchors
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMax = new Vector2(ratio, 1f);

            // Update appearance based on bar type
            if (barType == BarType.HP && useColorSprites)
            {
                // Swap sprite based on HP level
                fillImage.sprite = GetHPSprite(ratio);
                fillImage.color = Color.white; // No tint when using colored sprites
            }
            else if (barType == BarType.Cargo)
            {
                // Swap sprite when cargo is full
                if (isFull && cargoFullSprite != null)
                {
                    fillImage.sprite = cargoFullSprite;
                }
                else if (cargoNormalSprite != null)
                {
                    fillImage.sprite = cargoNormalSprite;
                }
                // Color is handled by UpdateCargoColor() based on resource mix
                // If no cargo detail yet, use white
                if (cargoMeat + cargoWood + cargoGold <= 0)
                {
                    fillImage.color = Color.white;
                }
            }
            else
            {
                fillImage.color = GetBarColor(ratio);
            }
        }

        // Update text
        if (valueText != null)
        {
            if (barType == BarType.Cargo && isFull)
            {
                valueText.text = cargoFullText;
            }
            else
            {
                valueText.text = $"{current}/{max}";
            }
        }
    }

    private Color GetBarColor(float ratio)
    {
        if (barType == BarType.XP)
        {
            return xpColor; // Always purple for XP
        }

        // HP dynamic color
        if (ratio > highHealthThreshold)
        {
            return highHealthColor; // Green
        }
        else if (ratio > lowHealthThreshold)
        {
            // Lerp between yellow and green
            float t = (ratio - lowHealthThreshold) / (highHealthThreshold - lowHealthThreshold);
            return Color.Lerp(midHealthColor, highHealthColor, t);
        }
        else
        {
            // Lerp between red and yellow
            float t = ratio / lowHealthThreshold;
            return Color.Lerp(lowHealthColor, midHealthColor, t);
        }
    }

    private Sprite GetHPSprite(float ratio)
    {
        if (ratio > highHealthThreshold)
        {
            return greenFillSprite;
        }
        else if (ratio > lowHealthThreshold)
        {
            return yellowFillSprite;
        }
        else
        {
            return redFillSprite;
        }
    }

    /// <summary>
    /// Manual update method for editor testing.
    /// </summary>
    public void SetValues(int current, int max)
    {
        UpdateBar(current, max);
    }

    // Properties
    public BarType Type => barType;
    public int CurrentValue => currentValue;
    public int MaxValue => maxValue;
    public float Ratio => maxValue > 0 ? (float)currentValue / maxValue : 0f;
}
