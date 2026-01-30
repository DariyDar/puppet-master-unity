using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Segmented cargo bar that shows different colors for each resource type.
/// Wood = Brown, Meat = Light Red, Gold = Yellow
/// Each resource is shown as a proportional segment of the bar.
/// </summary>
public class SegmentedCargoBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform fillContainer;
    [SerializeField] private Image woodFill;
    [SerializeField] private Image meatFill;
    [SerializeField] private Image goldFill;
    [SerializeField] private TextMeshProUGUI valueText;

    [Header("Colors")]
    [SerializeField] private Color woodColor = new Color(0.55f, 0.35f, 0.2f);     // Brown
    [SerializeField] private Color meatColor = new Color(0.85f, 0.4f, 0.4f);      // Light red
    [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.2f);        // Yellow

    [Header("Settings")]
    [SerializeField] private string cargoFullText = "CARGO IS FULL";

    // State
    private int cargoMeat;
    private int cargoWood;
    private int cargoGold;
    private int cargoCapacity = 50;

    private void Start()
    {
        // Subscribe to cargo events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.CargoDetailChanged += OnCargoDetailChanged;
        }

        // Set colors
        if (woodFill != null) woodFill.color = woodColor;
        if (meatFill != null) meatFill.color = meatColor;
        if (goldFill != null) goldFill.color = goldColor;

        // Initial update
        UpdateFromGameManager();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.CargoDetailChanged -= OnCargoDetailChanged;
        }
    }

    private void UpdateFromGameManager()
    {
        if (GameManager.Instance == null) return;

        cargoMeat = GameManager.Instance.CargoMeat;
        cargoWood = GameManager.Instance.CargoWood;
        cargoGold = GameManager.Instance.CargoGold;
        cargoCapacity = GameManager.Instance.CargoCapacity;

        UpdateVisuals();
    }

    private void OnCargoDetailChanged(int meat, int wood, int gold, int capacity)
    {
        cargoMeat = meat;
        cargoWood = wood;
        cargoGold = gold;
        cargoCapacity = capacity;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (cargoCapacity <= 0) return;

        int total = cargoMeat + cargoWood + cargoGold;
        bool isFull = total >= cargoCapacity;

        // Calculate ratios relative to capacity
        float woodRatio = (float)cargoWood / cargoCapacity;
        float meatRatio = (float)cargoMeat / cargoCapacity;
        float goldRatio = (float)cargoGold / cargoCapacity;

        // Wood segment starts at 0
        // Meat segment starts after wood
        // Gold segment starts after meat
        float woodEnd = woodRatio;
        float meatEnd = woodRatio + meatRatio;
        float goldEnd = woodRatio + meatRatio + goldRatio;

        // Update wood fill (starts at left edge)
        if (woodFill != null)
        {
            RectTransform rect = woodFill.rectTransform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(woodEnd, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            woodFill.gameObject.SetActive(cargoWood > 0);
        }

        // Update meat fill (starts after wood)
        if (meatFill != null)
        {
            RectTransform rect = meatFill.rectTransform;
            rect.anchorMin = new Vector2(woodEnd, 0);
            rect.anchorMax = new Vector2(meatEnd, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            meatFill.gameObject.SetActive(cargoMeat > 0);
        }

        // Update gold fill (starts after meat)
        if (goldFill != null)
        {
            RectTransform rect = goldFill.rectTransform;
            rect.anchorMin = new Vector2(meatEnd, 0);
            rect.anchorMax = new Vector2(goldEnd, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            goldFill.gameObject.SetActive(cargoGold > 0);
        }

        // Update text
        if (valueText != null)
        {
            if (isFull)
            {
                valueText.text = cargoFullText;
            }
            else
            {
                valueText.text = $"{total}/{cargoCapacity}";
            }
        }
    }

    /// <summary>
    /// Set references (for editor scripts).
    /// </summary>
    public void SetReferences(RectTransform container, Image wood, Image meat, Image gold, TextMeshProUGUI text)
    {
        fillContainer = container;
        woodFill = wood;
        meatFill = meat;
        goldFill = gold;
        valueText = text;

        if (woodFill != null) woodFill.color = woodColor;
        if (meatFill != null) meatFill.color = meatColor;
        if (goldFill != null) goldFill.color = goldColor;
    }

    // Properties
    public int TotalCargo => cargoMeat + cargoWood + cargoGold;
    public int Capacity => cargoCapacity;
    public float FillRatio => cargoCapacity > 0 ? (float)TotalCargo / cargoCapacity : 0f;
}
