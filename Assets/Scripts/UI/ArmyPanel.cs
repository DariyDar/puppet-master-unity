using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Panel for displaying army units with individual slot icons and HP bars.
/// Tiny Swords style UI.
/// </summary>
public class ArmyPanel : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int maxSlots = 10;
    [SerializeField] private List<ArmySlot> slots = new List<ArmySlot>();

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI armyCountText;
    [SerializeField] private bool showHealthBars = true;

    [Header("Layout")]
    [Tooltip("Spacing between slots (for future layout implementation)")]
    [SerializeField] private float slotSpacing = 10f;
    [Tooltip("Horizontal or vertical layout (for future layout implementation)")]
    [SerializeField] private bool horizontalLayout = true;

    // Tracked units
    private List<GameObject> trackedUnits = new List<GameObject>();

    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ArmyUpdated += OnArmyUpdated;
            EventManager.Instance.UnitSpawned += OnUnitSpawned;
            EventManager.Instance.UnitDied += OnUnitDied;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ArmyUpdated -= OnArmyUpdated;
            EventManager.Instance.UnitSpawned -= OnUnitSpawned;
            EventManager.Instance.UnitDied -= OnUnitDied;
        }
    }

    private void Start()
    {
        InitializeSlots();
        UpdateCountDisplay();
    }

    private void Update()
    {
        // Update HP bars for all units
        if (showHealthBars)
        {
            UpdateHealthBars();
        }
    }

    #region Slot Management

    /// <summary>
    /// Initialize all slots
    /// </summary>
    private void InitializeSlots()
    {
        slots.Clear();

        if (slotsContainer == null) return;

        // Create slots if needed
        if (slotPrefab != null)
        {
            // Clear existing
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new slots
            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                slotObj.name = $"ArmySlot_{i}";

                ArmySlot slot = slotObj.GetComponent<ArmySlot>();
                if (slot == null)
                {
                    slot = slotObj.AddComponent<ArmySlot>();
                }

                slot.SetEmpty();
                slots.Add(slot);
            }
        }
        else
        {
            // Find existing slots
            foreach (Transform child in slotsContainer)
            {
                ArmySlot slot = child.GetComponent<ArmySlot>();
                if (slot != null)
                {
                    slots.Add(slot);
                }
            }
        }
    }

    /// <summary>
    /// Get first empty slot
    /// </summary>
    private ArmySlot GetEmptySlot()
    {
        foreach (ArmySlot slot in slots)
        {
            if (slot.IsEmpty)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Find slot containing specific unit
    /// </summary>
    private ArmySlot FindSlotWithUnit(GameObject unit)
    {
        foreach (ArmySlot slot in slots)
        {
            if (slot.Unit == unit)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Add unit to first available slot
    /// </summary>
    public bool AddUnit(GameObject unit)
    {
        if (unit == null) return false;

        // Check if already tracked
        if (trackedUnits.Contains(unit)) return false;

        ArmySlot slot = GetEmptySlot();
        if (slot == null) return false;

        slot.SetUnit(unit);
        trackedUnits.Add(unit);

        // Spawn animation
        StartCoroutine(SlotSpawnAnimation(slot));

        UpdateCountDisplay();
        return true;
    }

    /// <summary>
    /// Remove unit from slots
    /// </summary>
    public bool RemoveUnit(GameObject unit)
    {
        ArmySlot slot = FindSlotWithUnit(unit);
        if (slot == null) return false;

        trackedUnits.Remove(unit);

        // Death animation then clear
        StartCoroutine(SlotDeathAnimation(slot));

        UpdateCountDisplay();
        return true;
    }

    /// <summary>
    /// Clear all slots
    /// </summary>
    public void ClearAllSlots()
    {
        trackedUnits.Clear();

        foreach (ArmySlot slot in slots)
        {
            slot.SetEmpty();
        }

        UpdateCountDisplay();
    }

    /// <summary>
    /// Set all slots from unit list
    /// </summary>
    public void SetUnits(List<GameObject> units)
    {
        ClearAllSlots();

        foreach (GameObject unit in units)
        {
            AddUnit(unit);
        }
    }

    #endregion

    #region Event Handlers

    private void OnArmyUpdated(int count, int limit)
    {
        UpdateCountDisplay();
    }

    private void OnUnitSpawned(GameObject unit)
    {
        AddUnit(unit);
    }

    private void OnUnitDied(GameObject unit)
    {
        RemoveUnit(unit);
    }

    #endregion

    #region Display Updates

    private void UpdateCountDisplay()
    {
        if (armyCountText == null) return;

        int activeCount = 0;
        foreach (ArmySlot slot in slots)
        {
            if (!slot.IsEmpty)
            {
                activeCount++;
            }
        }

        armyCountText.text = $"{activeCount}/{maxSlots}";

        // Color warning when full
        if (activeCount >= maxSlots)
        {
            armyCountText.color = new Color(1f, 0.5f, 0.2f);
        }
        else
        {
            armyCountText.color = Color.white;
        }
    }

    private void UpdateHealthBars()
    {
        foreach (ArmySlot slot in slots)
        {
            slot.UpdateHealthBar();
        }
    }

    #endregion

    #region Animations

    private IEnumerator SlotSpawnAnimation(ArmySlot slot)
    {
        if (slot == null) yield break;

        Vector3 originalScale = slot.transform.localScale;
        slot.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Overshoot bounce
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.2f;
            slot.transform.localScale = originalScale * (t + bounce);

            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.transform.localScale = originalScale;
    }

    private IEnumerator SlotDeathAnimation(ArmySlot slot)
    {
        if (slot == null) yield break;

        Vector3 originalScale = slot.transform.localScale;

        // Shake
        float shakeTime = 0.15f;
        float shakeElapsed = 0f;
        while (shakeElapsed < shakeTime)
        {
            float shake = Mathf.Sin(shakeElapsed * 60f) * 3f;
            slot.transform.localPosition += Vector3.right * shake * Time.deltaTime * 10f;
            shakeElapsed += Time.deltaTime;
            yield return null;
        }

        // Shrink
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            slot.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.SetEmpty();
        slot.transform.localScale = originalScale;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Get all slots
    /// </summary>
    public List<ArmySlot> Slots => slots;

    /// <summary>
    /// Get tracked units
    /// </summary>
    public List<GameObject> TrackedUnits => trackedUnits;

    /// <summary>
    /// Get current unit count
    /// </summary>
    public int UnitCount => trackedUnits.Count;

    /// <summary>
    /// Get max slots
    /// </summary>
    public int MaxSlots => maxSlots;

    /// <summary>
    /// Check if army is full
    /// </summary>
    public bool IsFull => trackedUnits.Count >= maxSlots;

    #endregion
}

/// <summary>
/// Single slot in the army panel displaying one unit.
/// </summary>
public class ArmySlot : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private Image borderImage;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color healthFullColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color healthLowColor = new Color(0.8f, 0.2f, 0.2f);

    // State
    private GameObject unit;
    private bool isEmpty = true;

    private void Awake()
    {
        // Try to find components if not assigned
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
    }

    /// <summary>
    /// Set this slot as empty
    /// </summary>
    public void SetEmpty()
    {
        unit = null;
        isEmpty = true;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = emptyColor;
        }

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 0f;
            healthBarFill.gameObject.SetActive(false);
        }

        if (healthBarBackground != null)
        {
            healthBarBackground.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Set this slot with a unit
    /// </summary>
    public void SetUnit(GameObject unitObj)
    {
        unit = unitObj;
        isEmpty = false;

        if (iconImage != null)
        {
            iconImage.color = filledColor;

            // Try to get sprite from unit
            SpriteRenderer sr = unitObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                iconImage.sprite = sr.sprite;
            }
        }

        if (healthBarFill != null)
        {
            healthBarFill.gameObject.SetActive(true);
            healthBarFill.fillAmount = 1f;
            healthBarFill.color = healthFullColor;
        }

        if (healthBarBackground != null)
        {
            healthBarBackground.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Set slot icon directly
    /// </summary>
    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = filledColor;
        }
        isEmpty = sprite == null;
    }

    /// <summary>
    /// Update health bar based on unit's current HP
    /// </summary>
    public void UpdateHealthBar()
    {
        if (isEmpty || unit == null || healthBarFill == null) return;

        // Try to get health component from unit
        // First check for UnitHealth component (if exists)
        var unitHealth = unit.GetComponent<UnitHealth>();
        if (unitHealth != null)
        {
            float fill = unitHealth.MaxHealth > 0 ?
                (float)unitHealth.CurrentHealth / unitHealth.MaxHealth : 0f;
            healthBarFill.fillAmount = fill;
            healthBarFill.color = Color.Lerp(healthLowColor, healthFullColor, fill);
        }
    }

    /// <summary>
    /// Set health bar manually
    /// </summary>
    public void SetHealthBar(float fillAmount)
    {
        if (healthBarFill == null) return;

        healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);
        healthBarFill.color = Color.Lerp(healthLowColor, healthFullColor, fillAmount);
    }

    /// <summary>
    /// Get the unit in this slot
    /// </summary>
    public GameObject Unit => unit;

    /// <summary>
    /// Check if slot is empty
    /// </summary>
    public bool IsEmpty => isEmpty;
}

/// <summary>
/// Simple interface for units with health (if UnitHealth doesn't exist)
/// </summary>
public interface IUnitHealth
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
}

/// <summary>
/// Basic unit health component for tracking HP
/// </summary>
public class UnitHealth : MonoBehaviour, IUnitHealth
{
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void SetHealth(int current, int max)
    {
        currentHealth = current;
        maxHealth = max;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void Die()
    {
        EventManager.Instance?.OnUnitDied(gameObject);
        // Destruction handled by unit manager or combat system
    }
}
