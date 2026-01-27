using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UpgradeUI - окно прокачки с 3 вкладками:
/// 1. Spider - HP, Damage, Speed, Attack Range, Cargo Capacity
/// 2. Army - Army Limit, Unit HP Bonus, Unit Damage Bonus
/// 3. Base - Storage Capacity, Farm Efficiency
///
/// Используйте Tiny Swords UI ассеты для оформления.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Button spiderTabButton;
    [SerializeField] private Button armyTabButton;
    [SerializeField] private Button baseTabButton;

    [Header("Tab Content Panels")]
    [SerializeField] private GameObject spiderPanel;
    [SerializeField] private GameObject armyPanel;
    [SerializeField] private GameObject basePanel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Spider Upgrades")]
    [SerializeField] private UpgradeSlot spiderHealthSlot;
    [SerializeField] private UpgradeSlot spiderDamageSlot;
    [SerializeField] private UpgradeSlot spiderSpeedSlot;
    [SerializeField] private UpgradeSlot spiderRangeSlot;
    [SerializeField] private UpgradeSlot spiderCargoSlot;

    [Header("Army Upgrades")]
    [SerializeField] private UpgradeSlot armyLimitSlot;
    [SerializeField] private UpgradeSlot armyHPBonusSlot;
    [SerializeField] private UpgradeSlot armyDamageBonusSlot;

    [Header("Base Upgrades")]
    [SerializeField] private UpgradeSlot baseStorageSlot;
    [SerializeField] private UpgradeSlot baseFarmEfficiencySlot;

    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI skullsText;
    [SerializeField] private TextMeshProUGUI meatText;
    [SerializeField] private TextMeshProUGUI woodText;
    [SerializeField] private TextMeshProUGUI goldText;

    private enum Tab { Spider, Army, Base }
    private Tab currentTab = Tab.Spider;

    private void Awake()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        ShowTab(Tab.Spider);
        RefreshAllUpgrades();
        UpdateResourceDisplay();
    }

    private void SetupButtons()
    {
        if (spiderTabButton != null)
            spiderTabButton.onClick.AddListener(() => ShowTab(Tab.Spider));

        if (armyTabButton != null)
            armyTabButton.onClick.AddListener(() => ShowTab(Tab.Army));

        if (baseTabButton != null)
            baseTabButton.onClick.AddListener(() => ShowTab(Tab.Base));

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void ShowTab(Tab tab)
    {
        currentTab = tab;

        // Hide all panels
        if (spiderPanel != null) spiderPanel.SetActive(false);
        if (armyPanel != null) armyPanel.SetActive(false);
        if (basePanel != null) basePanel.SetActive(false);

        // Show selected panel
        switch (tab)
        {
            case Tab.Spider:
                if (spiderPanel != null) spiderPanel.SetActive(true);
                break;
            case Tab.Army:
                if (armyPanel != null) armyPanel.SetActive(true);
                break;
            case Tab.Base:
                if (basePanel != null) basePanel.SetActive(true);
                break;
        }

        // Update tab button visuals
        UpdateTabButtonVisuals();
    }

    private void UpdateTabButtonVisuals()
    {
        // Highlight active tab (you can customize colors here)
        Color activeColor = new Color(1f, 1f, 1f, 1f);
        Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        if (spiderTabButton != null)
        {
            var img = spiderTabButton.GetComponent<Image>();
            if (img != null) img.color = currentTab == Tab.Spider ? activeColor : inactiveColor;
        }

        if (armyTabButton != null)
        {
            var img = armyTabButton.GetComponent<Image>();
            if (img != null) img.color = currentTab == Tab.Army ? activeColor : inactiveColor;
        }

        if (baseTabButton != null)
        {
            var img = baseTabButton.GetComponent<Image>();
            if (img != null) img.color = currentTab == Tab.Base ? activeColor : inactiveColor;
        }
    }

    private void RefreshAllUpgrades()
    {
        // Spider upgrades
        RefreshUpgradeSlot(spiderHealthSlot, "spider_health");
        RefreshUpgradeSlot(spiderDamageSlot, "spider_damage");
        RefreshUpgradeSlot(spiderSpeedSlot, "spider_speed");
        RefreshUpgradeSlot(spiderRangeSlot, "spider_range");
        RefreshUpgradeSlot(spiderCargoSlot, "spider_cargo");

        // Army upgrades
        RefreshUpgradeSlot(armyLimitSlot, "army_limit");
        RefreshUpgradeSlot(armyHPBonusSlot, "army_hp_bonus");
        RefreshUpgradeSlot(armyDamageBonusSlot, "army_damage_bonus");

        // Base upgrades
        RefreshUpgradeSlot(baseStorageSlot, "base_storage");
        RefreshUpgradeSlot(baseFarmEfficiencySlot, "base_farm_efficiency");
    }

    private void RefreshUpgradeSlot(UpgradeSlot slot, string upgradeId)
    {
        if (slot == null) return;

        UpgradeData data = UpgradeManager.Instance?.GetUpgradeData(upgradeId);
        if (data != null)
        {
            slot.Setup(data, OnUpgradePurchased);
        }
    }

    private void OnUpgradePurchased(string upgradeId)
    {
        if (UpgradeManager.Instance == null) return;

        bool success = UpgradeManager.Instance.TryPurchaseUpgrade(upgradeId);
        if (success)
        {
            Debug.Log($"[UpgradeUI] Upgrade purchased: {upgradeId}");
            RefreshAllUpgrades();
            UpdateResourceDisplay();
        }
        else
        {
            Debug.Log($"[UpgradeUI] Cannot purchase upgrade: {upgradeId} (insufficient resources or max level)");
        }
    }

    private void UpdateResourceDisplay()
    {
        if (GameManager.Instance == null) return;

        if (skullsText != null)
            skullsText.text = GameManager.Instance.Skulls.ToString();

        if (meatText != null)
            meatText.text = GameManager.Instance.Meat.ToString();

        if (woodText != null)
            woodText.text = GameManager.Instance.Wood.ToString();

        if (goldText != null)
            goldText.text = GameManager.Instance.Gold.ToString();
    }

    public void Close()
    {
        gameObject.SetActive(false);

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnCloseModal();
        }
    }
}

/// <summary>
/// Single upgrade slot with name, description, level, cost, and buy button.
/// Assign to each upgrade row in the UI.
/// </summary>
[System.Serializable]
public class UpgradeSlot : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image iconImage;

    private UpgradeData currentData;
    private System.Action<string> onPurchaseCallback;

    public void Setup(UpgradeData data, System.Action<string> onPurchase)
    {
        currentData = data;
        onPurchaseCallback = onPurchase;

        if (nameText != null)
            nameText.text = data.displayName;

        if (descriptionText != null)
            descriptionText.text = data.GetCurrentDescription();

        if (levelText != null)
            levelText.text = $"Lv. {data.currentLevel}/{data.maxLevel}";

        if (costText != null)
        {
            if (data.IsMaxLevel)
            {
                costText.text = "MAX";
            }
            else
            {
                costText.text = data.GetCurrentCostString();
            }
        }

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        // Setup buy button
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onPurchaseCallback?.Invoke(data.id));

            // Disable if max level or can't afford
            buyButton.interactable = !data.IsMaxLevel && data.CanAfford();
        }
    }
}

/// <summary>
/// Data class for a single upgrade.
/// </summary>
[System.Serializable]
public class UpgradeData
{
    public string id;
    public string displayName;
    public string descriptionFormat; // e.g., "HP +{0}" where {0} is the bonus value
    public Sprite icon;
    public int currentLevel = 0;
    public int maxLevel = 10;

    // Cost per level (increases with level)
    public int baseCostSkulls = 0;
    public int baseCostMeat = 0;
    public int baseCostWood = 0;
    public int baseCostGold = 0;
    public float costMultiplierPerLevel = 1.5f;

    // Value per level
    public float baseValue = 10f;
    public float valuePerLevel = 10f;

    public bool IsMaxLevel => currentLevel >= maxLevel;

    public float GetCurrentValue()
    {
        return baseValue + (currentLevel * valuePerLevel);
    }

    public string GetCurrentDescription()
    {
        float nextValue = baseValue + ((currentLevel + 1) * valuePerLevel);
        return string.Format(descriptionFormat, nextValue);
    }

    public int GetCostSkulls() => Mathf.RoundToInt(baseCostSkulls * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    public int GetCostMeat() => Mathf.RoundToInt(baseCostMeat * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    public int GetCostWood() => Mathf.RoundToInt(baseCostWood * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    public int GetCostGold() => Mathf.RoundToInt(baseCostGold * Mathf.Pow(costMultiplierPerLevel, currentLevel));

    public string GetCurrentCostString()
    {
        List<string> costs = new List<string>();
        int skulls = GetCostSkulls();
        int meat = GetCostMeat();
        int wood = GetCostWood();
        int gold = GetCostGold();

        if (skulls > 0) costs.Add($"{skulls} Skulls");
        if (meat > 0) costs.Add($"{meat} Meat");
        if (wood > 0) costs.Add($"{wood} Wood");
        if (gold > 0) costs.Add($"{gold} Gold");

        return string.Join(", ", costs);
    }

    public bool CanAfford()
    {
        if (GameManager.Instance == null) return false;

        return GameManager.Instance.Skulls >= GetCostSkulls() &&
               GameManager.Instance.Meat >= GetCostMeat() &&
               GameManager.Instance.Wood >= GetCostWood() &&
               GameManager.Instance.Gold >= GetCostGold();
    }
}
