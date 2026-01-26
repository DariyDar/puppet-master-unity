using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bottom HUD panel displaying army units and cargo status.
/// Tiny Swords style UI with animated updates.
/// </summary>
public class BottomHUD : MonoBehaviour
{
    [Header("Army")]
    [SerializeField] private Transform armySlotsContainer;
    [SerializeField] private TextMeshProUGUI armyCountText;
    [SerializeField] private GameObject armySlotPrefab;
    [SerializeField] private int maxDisplaySlots = 10;

    [Header("Cargo")]
    [SerializeField] private Image cargoBarFill;
    [SerializeField] private Image cargoBarBackground;
    [SerializeField] private TextMeshProUGUI cargoText;
    [SerializeField] private Color cargoNormalColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] private Color cargoFullColor = new Color(0.9f, 0.3f, 0.2f);

    [Header("Cargo Details - Per GDD v5")]
    [SerializeField] private TextMeshProUGUI cargoMeatText;
    [SerializeField] private TextMeshProUGUI cargoWoodText;
    [SerializeField] private TextMeshProUGUI cargoGoldText;

    [Header("Animation")]
    [SerializeField] private float barAnimationSpeed = 5f;
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.2f;

    // Army slots management
    private List<ArmySlot> armySlots = new List<ArmySlot>();

    // Cargo animation
    private float targetCargoFill;
    private float currentCargoFill;
    private bool isCargoFull;

    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ArmyUpdated += OnArmyUpdated;
            EventManager.Instance.UnitSpawned += OnUnitSpawned;
            EventManager.Instance.UnitDied += OnUnitDied;
            EventManager.Instance.ResourceCollected += OnResourceCollected;
            EventManager.Instance.CargoFull += OnCargoFull;
            EventManager.Instance.CargoDeposited += OnCargoDeposited;
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
            EventManager.Instance.ResourceCollected -= OnResourceCollected;
            EventManager.Instance.CargoFull -= OnCargoFull;
            EventManager.Instance.CargoDeposited -= OnCargoDeposited;
        }
    }

    private void Start()
    {
        // Initialize army slots
        InitializeArmySlots();

        // Initial update from GameManager
        UpdateAllFromGameManager();
    }

    private void Update()
    {
        // Smooth cargo bar animation
        AnimateCargoBar();
    }

    private void AnimateCargoBar()
    {
        if (cargoBarFill != null && Mathf.Abs(currentCargoFill - targetCargoFill) > 0.001f)
        {
            currentCargoFill = Mathf.Lerp(currentCargoFill, targetCargoFill, Time.deltaTime * barAnimationSpeed);
            cargoBarFill.fillAmount = currentCargoFill;

            // Update color when full
            cargoBarFill.color = currentCargoFill >= 0.99f ? cargoFullColor : cargoNormalColor;
        }
    }

    #region Army Slots

    private void InitializeArmySlots()
    {
        armySlots.Clear();

        if (armySlotsContainer == null) return;

        // Create slots if prefab is available
        if (armySlotPrefab != null)
        {
            for (int i = 0; i < maxDisplaySlots; i++)
            {
                GameObject slotObj = Instantiate(armySlotPrefab, armySlotsContainer);
                ArmySlot slot = slotObj.GetComponent<ArmySlot>();
                if (slot != null)
                {
                    slot.SetEmpty();
                    armySlots.Add(slot);
                }
            }
        }
        else
        {
            // Try to find existing slots in container
            foreach (Transform child in armySlotsContainer)
            {
                ArmySlot slot = child.GetComponent<ArmySlot>();
                if (slot != null)
                {
                    armySlots.Add(slot);
                }
            }
        }
    }

    private void OnArmyUpdated(int count, int limit)
    {
        if (armyCountText != null)
        {
            armyCountText.text = $"{count}/{limit}";

            // Color change when near limit
            if (count >= limit)
            {
                armyCountText.color = new Color(1f, 0.5f, 0.2f);
            }
            else
            {
                armyCountText.color = Color.white;
            }
        }
    }

    private void OnUnitSpawned(GameObject unit)
    {
        // Find empty slot and fill it
        foreach (ArmySlot slot in armySlots)
        {
            if (slot.IsEmpty)
            {
                slot.SetUnit(unit);
                StartCoroutine(SlotSpawnEffect(slot));
                break;
            }
        }

        // Update count
        if (GameManager.Instance != null)
        {
            OnArmyUpdated(GameManager.Instance.ArmyCount, GameManager.Instance.ArmyLimit);
        }
    }

    private void OnUnitDied(GameObject unit)
    {
        // Find slot with this unit and clear it
        foreach (ArmySlot slot in armySlots)
        {
            if (slot.Unit == unit)
            {
                StartCoroutine(SlotDeathEffect(slot));
                break;
            }
        }

        // Update count
        if (GameManager.Instance != null)
        {
            OnArmyUpdated(GameManager.Instance.ArmyCount, GameManager.Instance.ArmyLimit);
        }
    }

    private IEnumerator SlotSpawnEffect(ArmySlot slot)
    {
        if (slot == null) yield break;

        Vector3 originalScale = slot.transform.localScale;

        // Pop-in effect
        slot.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f);
            slot.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.transform.localScale = originalScale;
    }

    private IEnumerator SlotDeathEffect(ArmySlot slot)
    {
        if (slot == null) yield break;

        Vector3 originalScale = slot.transform.localScale;

        // Shrink effect
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

    #region Cargo

    private void OnResourceCollected(string type, int amount)
    {
        UpdateCargoFromGameManager();

        // Pulse effect
        StartCoroutine(CargoPulseEffect());
    }

    private void OnCargoFull()
    {
        isCargoFull = true;
        StartCoroutine(CargoFullWarning());
    }

    private void OnCargoDeposited(int totalAmount)
    {
        isCargoFull = false;
        UpdateCargoFromGameManager();

        // Flash effect
        StartCoroutine(CargoDepositEffect());
    }

    private void UpdateCargoFromGameManager()
    {
        if (GameManager.Instance == null) return;

        int current = GameManager.Instance.CurrentCargo;
        int max = GameManager.Instance.CargoCapacity;

        targetCargoFill = max > 0 ? (float)current / max : 0f;

        if (cargoText != null)
        {
            cargoText.text = $"{current}/{max}";
        }

        // Update detail texts - Per GDD v5 (3 cargo types)
        if (cargoMeatText != null)
            cargoMeatText.text = GameManager.Instance.CargoMeat.ToString();

        if (cargoWoodText != null)
            cargoWoodText.text = GameManager.Instance.CargoWood.ToString();

        if (cargoGoldText != null)
            cargoGoldText.text = GameManager.Instance.CargoGold.ToString();
    }

    private IEnumerator CargoPulseEffect()
    {
        if (cargoBarFill == null) yield break;

        Vector3 originalScale = cargoBarFill.transform.localScale;
        cargoBarFill.transform.localScale = originalScale * pulseScale;

        yield return new WaitForSeconds(pulseDuration);

        cargoBarFill.transform.localScale = originalScale;
    }

    private IEnumerator CargoFullWarning()
    {
        if (cargoBarFill == null) yield break;

        // Flash red 3 times
        for (int i = 0; i < 3; i++)
        {
            cargoBarFill.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            cargoBarFill.color = cargoFullColor;
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator CargoDepositEffect()
    {
        if (cargoBarFill == null) yield break;

        // Flash green
        Color original = cargoBarFill.color;
        cargoBarFill.color = Color.green;

        yield return new WaitForSeconds(0.3f);

        cargoBarFill.color = cargoNormalColor;

        // Smooth decrease animation
        currentCargoFill = targetCargoFill + 0.5f;
    }

    #endregion

    #region Data Updates

    public void UpdateAllFromGameManager()
    {
        if (GameManager.Instance == null) return;

        // Update army count
        OnArmyUpdated(GameManager.Instance.ArmyCount, GameManager.Instance.ArmyLimit);

        // Update cargo
        UpdateCargoFromGameManager();
    }

    /// <summary>
    /// Manually update army slots with unit list
    /// </summary>
    public void UpdateArmySlots(List<GameObject> units)
    {
        // Clear all slots
        foreach (ArmySlot slot in armySlots)
        {
            slot.SetEmpty();
        }

        // Fill slots with units
        int slotIndex = 0;
        foreach (GameObject unit in units)
        {
            if (slotIndex >= armySlots.Count) break;

            armySlots[slotIndex].SetUnit(unit);
            slotIndex++;
        }
    }

    /// <summary>
    /// Force refresh all displays
    /// </summary>
    public void RefreshAll()
    {
        UpdateAllFromGameManager();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Get current cargo fill amount (0-1)
    /// </summary>
    public float CargoFillAmount => currentCargoFill;

    /// <summary>
    /// Check if cargo is full
    /// </summary>
    public bool IsCargoFull => isCargoFull;

    /// <summary>
    /// Get army slots list
    /// </summary>
    public List<ArmySlot> ArmySlots => armySlots;

    #endregion
}
