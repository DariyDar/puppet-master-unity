using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Editor-only menu for building complete UI system with Cryo's Mini GUI assets.
/// Access via: Tools > Puppet Master > Build Complete UI
/// </summary>
public class UIBuilderEditor : EditorWindow
{
    // Cryo's Mini GUI sprite sheet paths
    private const string CRYO_BARS = "Assets/Cryo's Mini GUI/Bars/Bars_4x.png";
    private const string CRYO_BUTTONS = "Assets/Cryo's Mini GUI/Buttons/Buttons_4x.png";
    private const string CRYO_GUI_OUTER = "Assets/Cryo's Mini GUI/GUI Outer/GUI_Outer_4x.png";
    private const string CRYO_GUI_INNER = "Assets/Cryo's Mini GUI/GUI Inner/GUI_Inner_4x.png";
    private const string CRYO_STAT_ICONS = "Assets/Cryo's Mini GUI/Stat Icons/Stat_Icons_4x.png";

    // Resource icons (game sprites)
    private const string ICON_SKULL = "Assets/Sprites/Resources/Dead.png";
    private const string ICON_MEAT = "Assets/Sprites/Resources/Meat.png";
    private const string ICON_WOOD = "Assets/Sprites/Resources/Wood.png";
    private const string ICON_GOLD = "Assets/Sprites/Resources/Gold.png";

    // Colors - Cryo palette
    private static readonly Color HP_COLOR = new Color(0.85f, 0.25f, 0.25f); // Red
    private static readonly Color XP_COLOR = new Color(0.6f, 0.35f, 0.75f);  // Purple (tint on blue bar)
    private static readonly Color CARGO_COLOR = new Color(0.9f, 0.6f, 0.2f); // Orange
    private static readonly Color TEXT_DARK = new Color(0.15f, 0.12f, 0.1f);
    private static readonly Color TEXT_LIGHT = Color.white;
    private static readonly Color PANEL_DARK = new Color(0.12f, 0.1f, 0.15f, 0.95f);
    private static readonly Color SLOT_BG = new Color(0.18f, 0.16f, 0.22f, 0.9f);

    [MenuItem("Tools/Puppet Master/Build Complete UI")]
    public static void BuildCompleteUI()
    {
        Debug.Log("[UIBuilder] Starting complete UI build with Cryo's Mini GUI assets...");

        // Create or find UIAutoBuilder in scene
        UIAutoBuilder builder = Object.FindFirstObjectByType<UIAutoBuilder>();
        if (builder == null)
        {
            GameObject builderGO = new GameObject("UIAutoBuilder");
            builder = builderGO.AddComponent<UIAutoBuilder>();
            builder.buildOnStart = false;
            Debug.Log("[UIBuilder] Created UIAutoBuilder GameObject");
        }

        // Find or create Canvas
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("MainCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[UIBuilder] Created MainCanvas");
        }

        // Create all UI elements
        CreateTopHUD(mainCanvas.transform);
        CreateBottomLeftHUD(mainCanvas.transform);
        CreateUpgradeUIPopup(mainCanvas.transform);
        CreateAltarUIPopup(mainCanvas.transform);
        CreateQuestUIPanel(mainCanvas.transform);
        CreateUpgradeButtonTopRight(mainCanvas.transform);
        CreateEventSystem();

        Debug.Log("[UIBuilder] Complete UI build finished!");
        EditorUtility.DisplayDialog("UI Builder",
            "Complete UI has been built with Cryo's Mini GUI assets!\n\n" +
            "Created:\n" +
            "- TopHUD (resources)\n" +
            "- BottomHUD (HP red, XP purple)\n" +
            "- UpgradeUI (3 tabs)\n" +
            "- AltarUI (5 units)\n" +
            "- QuestUI\n" +
            "- Upgrade Button", "OK");
    }

    [MenuItem("Tools/Puppet Master/Rebuild TopHUD Only")]
    public static void RebuildTopHUDOnly()
    {
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[UIBuilder] No Canvas found!");
            return;
        }
        CreateTopHUD(mainCanvas.transform);
        Debug.Log("[UIBuilder] TopHUD rebuilt!");
    }

    /// <summary>
    /// Create Top HUD with resource displays.
    /// </summary>
    private static void CreateTopHUD(Transform parent)
    {
        Transform existing = parent.Find("TopHUD");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load sprites
        Sprite panelSprite = LoadSpriteFromSheet(CRYO_GUI_OUTER, 1); // Wood frame without top
        Sprite barBg = LoadSpriteFromSheet(CRYO_BARS, 4);  // Orange bar frame
        Sprite barFill = LoadSpriteFromSheet(CRYO_BARS, 5); // Orange bar fill
        Sprite skullIcon = LoadSprite(ICON_SKULL);
        Sprite meatIcon = LoadSprite(ICON_MEAT);
        Sprite woodIcon = LoadSprite(ICON_WOOD);
        Sprite goldIcon = LoadSprite(ICON_GOLD);

        // Main panel
        GameObject topHUD = CreatePanel("TopHUD", parent);
        RectTransform rt = topHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 80);

        Image bg = topHUD.GetComponent<Image>();
        if (panelSprite != null)
        {
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = PANEL_DARK;
        }

        HorizontalLayoutGroup layout = topHUD.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 15, 15);
        layout.spacing = 50;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Resource displays
        CreateResourceDisplay(topHUD.transform, "SkullsDisplay", skullIcon, "0", new Color(0.9f, 0.9f, 0.9f));
        CreateResourceDisplay(topHUD.transform, "MeatDisplay", meatIcon, "0", new Color(1f, 0.5f, 0.5f));
        CreateResourceDisplay(topHUD.transform, "WoodDisplay", woodIcon, "0", new Color(0.7f, 0.5f, 0.3f));
        CreateResourceDisplay(topHUD.transform, "GoldDisplay", goldIcon, "0", new Color(1f, 0.85f, 0f));

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(topHUD.transform, false);
        spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Cargo display
        CreateCargoDisplay(topHUD.transform, barBg, barFill);

        // Add RuntimeTopHUD script
        if (topHUD.GetComponent<RuntimeTopHUD>() == null)
            topHUD.AddComponent<RuntimeTopHUD>();

        Debug.Log("[UIBuilder] Created TopHUD with Cryo's Mini GUI");
    }

    /// <summary>
    /// Create Bottom Left HUD with HP and XP bars.
    /// </summary>
    private static void CreateBottomLeftHUD(Transform parent)
    {
        Transform existing = parent.Find("BottomLeftHUD");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load Cryo sprites
        Sprite panelSprite = LoadSpriteFromSheet(CRYO_GUI_OUTER, 0); // Wood frame full
        Sprite hpBarBg = LoadSpriteFromSheet(CRYO_BARS, 0);   // Red bar frame
        Sprite hpBarFill = LoadSpriteFromSheet(CRYO_BARS, 1); // Red bar fill
        Sprite xpBarBg = LoadSpriteFromSheet(CRYO_BARS, 2);   // Blue bar frame
        Sprite xpBarFill = LoadSpriteFromSheet(CRYO_BARS, 3); // Blue bar fill
        Sprite buttonSprite = LoadSpriteFromSheet(CRYO_BUTTONS, 0); // Beige button large

        // Main panel
        GameObject bottomHUD = CreatePanel("BottomLeftHUD", parent);
        RectTransform rt = bottomHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(320, 150);

        Image bg = bottomHUD.GetComponent<Image>();
        if (panelSprite != null)
        {
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = PANEL_DARK;
        }

        VerticalLayoutGroup layout = bottomHUD.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        // HP Bar (RED)
        CreateStatBar(bottomHUD.transform, "HP", HP_COLOR, hpBarBg, hpBarFill, 100, 100);

        // XP Bar (PURPLE tint on blue)
        CreateStatBar(bottomHUD.transform, "XP", XP_COLOR, xpBarBg, xpBarFill, 0, 100);

        // Level display
        CreateLevelDisplay(bottomHUD.transform);

        // Upgrades button
        CreateStyledButton(bottomHUD.transform, "UpgradesButton", "UPGRADES", buttonSprite, new Color(0.8f, 0.7f, 0.5f));

        Debug.Log("[UIBuilder] Created BottomLeftHUD with Cryo bars");
    }

    /// <summary>
    /// Create the Upgrade UI popup with 3 tabs.
    /// </summary>
    private static void CreateUpgradeUIPopup(Transform parent)
    {
        Transform existing = parent.Find("UpgradeUIPopup");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load Cryo sprites
        Sprite panelSprite = LoadSpriteFromSheet(CRYO_GUI_OUTER, 0); // Wood frame
        Sprite titlePanel = LoadSpriteFromSheet(CRYO_GUI_INNER, 3);  // Brown panel
        Sprite buttonBeige = LoadSpriteFromSheet(CRYO_BUTTONS, 0);   // Beige large
        Sprite buttonDark = LoadSpriteFromSheet(CRYO_BUTTONS, 3);    // Dark blue large
        Sprite closeIcon = LoadSpriteFromSheet(CRYO_BUTTONS, 13);    // X icon
        Sprite slotBg = LoadSpriteFromSheet(CRYO_GUI_INNER, 8);      // Slot background

        // Main popup
        GameObject popup = CreatePanel("UpgradeUIPopup", parent);
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.pivot = new Vector2(0.5f, 0.5f);
        popupRT.anchoredPosition = Vector2.zero;
        popupRT.sizeDelta = new Vector2(750, 550);

        Image popupBg = popup.GetComponent<Image>();
        if (panelSprite != null)
        {
            popupBg.sprite = panelSprite;
            popupBg.type = Image.Type.Sliced;
            popupBg.color = Color.white;
        }
        else
        {
            popupBg.color = PANEL_DARK;
        }

        // Title bar
        GameObject titleBar = CreatePanel("TitleBar", popup.transform);
        RectTransform titleRT = titleBar.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -5);
        titleRT.sizeDelta = new Vector2(300, 45);
        Image titleBg = titleBar.GetComponent<Image>();
        if (titlePanel != null)
        {
            titleBg.sprite = titlePanel;
            titleBg.type = Image.Type.Sliced;
        }
        else
        {
            titleBg.color = new Color(0.6f, 0.4f, 0.25f);
        }

        GameObject titleText = CreateText("TitleText", titleBar.transform, "UPGRADES", 26, FontStyles.Bold);
        SetFullStretch(titleText);
        titleText.GetComponent<TextMeshProUGUI>().color = TEXT_DARK;

        // Close button
        GameObject closeBtn = CreateIconButton(popup.transform, "CloseButton", closeIcon, new Color(0.8f, 0.3f, 0.3f));
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 1);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-10, -10);
        closeBtnRT.sizeDelta = new Vector2(40, 40);

        // Tab buttons
        GameObject tabBar = CreatePanel("TabBar", popup.transform);
        RectTransform tabBarRT = tabBar.GetComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0, 1);
        tabBarRT.anchorMax = new Vector2(1, 1);
        tabBarRT.pivot = new Vector2(0.5f, 1);
        tabBarRT.anchoredPosition = new Vector2(0, -55);
        tabBarRT.sizeDelta = new Vector2(-60, 45);
        tabBar.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 15;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;

        CreateStyledButton(tabBar.transform, "SpiderTab", "SPIDER", buttonBeige, new Color(0.85f, 0.75f, 0.55f));
        CreateStyledButton(tabBar.transform, "ArmyTab", "ARMY", buttonDark, new Color(0.35f, 0.4f, 0.55f));
        CreateStyledButton(tabBar.transform, "BaseTab", "BASE", buttonDark, new Color(0.35f, 0.4f, 0.55f));

        // Content area
        GameObject contentArea = CreatePanel("ContentArea", popup.transform);
        RectTransform contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.offsetMin = new Vector2(25, 70);
        contentRT.offsetMax = new Vector2(-25, -110);
        contentArea.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        // Spider Panel
        GameObject spiderPanel = CreateUpgradePanel(contentArea.transform, "SpiderPanel", new string[] {
            "Health|Max HP +20|Lv 0/10",
            "Damage|Attack +2|Lv 0/10",
            "Speed|Move Speed +0.5|Lv 0/5",
            "Skull Harvest|Collection -0.2s|Lv 0/5",
            "Cargo|Capacity +10|Lv 0/10",
            "Magnet|Radius +1.5|Lv 0/5"
        }, buttonBeige, slotBg);

        // Army Panel (hidden)
        GameObject armyPanel = CreateUpgradePanel(contentArea.transform, "ArmyPanel", new string[] {
            "Army Size|Max Units +2|Lv 0/10",
            "Skull Upgrade|Stats +10%|Lv 0/5",
            "Gnoll Upgrade|Stats +10%|Lv 0/5",
            "Gnome Upgrade|Stats +10%|Lv 0/5",
            "TNT Upgrade|Stats +10%|Lv 0/5",
            "Shaman Upgrade|Stats +10%|Lv 0/5"
        }, buttonBeige, slotBg);
        armyPanel.SetActive(false);

        // Base Panel (hidden)
        GameObject basePanel = CreateUpgradePanel(contentArea.transform, "BasePanel", new string[] {
            "Meat Farm|Auto-generate Meat|BUILD",
            "Lumber Mill|Auto-generate Wood|BUILD",
            "Gold Mine|Auto-generate Gold|BUILD",
            "Mining Speed|Production +10%|Lv 0/10"
        }, buttonBeige, slotBg);
        basePanel.SetActive(false);

        // Resource bar at bottom
        CreateResourceBar(popup.transform);

        // Add UpgradeUI component
        if (popup.GetComponent<UpgradeUI>() == null)
            popup.AddComponent<UpgradeUI>();

        popup.SetActive(false);
        Debug.Log("[UIBuilder] Created UpgradeUI popup with Cryo styling");
    }

    /// <summary>
    /// Create the Altar UI popup.
    /// </summary>
    private static void CreateAltarUIPopup(Transform parent)
    {
        Transform existing = parent.Find("AltarUIPopup");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load Cryo sprites
        Sprite panelSprite = LoadSpriteFromSheet(CRYO_GUI_OUTER, 4); // Brown wood frame
        Sprite titlePanel = LoadSpriteFromSheet(CRYO_GUI_INNER, 3);  // Brown panel
        Sprite buttonBeige = LoadSpriteFromSheet(CRYO_BUTTONS, 0);   // Beige large
        Sprite closeIcon = LoadSpriteFromSheet(CRYO_BUTTONS, 13);    // X icon
        Sprite slotBg = LoadSpriteFromSheet(CRYO_GUI_INNER, 9);      // Bordered slot

        GameObject popup = CreatePanel("AltarUIPopup", parent);
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.pivot = new Vector2(0.5f, 0.5f);
        popupRT.anchoredPosition = Vector2.zero;
        popupRT.sizeDelta = new Vector2(850, 420);

        Image popupBg = popup.GetComponent<Image>();
        if (panelSprite != null)
        {
            popupBg.sprite = panelSprite;
            popupBg.type = Image.Type.Sliced;
            popupBg.color = Color.white;
        }
        else
        {
            popupBg.color = PANEL_DARK;
        }

        // Title
        GameObject titleBar = CreatePanel("TitleBar", popup.transform);
        RectTransform titleRT = titleBar.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -5);
        titleRT.sizeDelta = new Vector2(350, 45);
        Image titleBg = titleBar.GetComponent<Image>();
        if (titlePanel != null)
        {
            titleBg.sprite = titlePanel;
            titleBg.type = Image.Type.Sliced;
        }
        else
        {
            titleBg.color = new Color(0.7f, 0.5f, 0.25f);
        }

        GameObject titleText = CreateText("TitleText", titleBar.transform, "SUMMON UNITS", 26, FontStyles.Bold);
        SetFullStretch(titleText);
        titleText.GetComponent<TextMeshProUGUI>().color = TEXT_DARK;

        // Close button
        GameObject closeBtn = CreateIconButton(popup.transform, "CloseButton", closeIcon, new Color(0.8f, 0.3f, 0.3f));
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 1);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-10, -10);
        closeBtnRT.sizeDelta = new Vector2(40, 40);

        // Unit slots container
        GameObject slotsContainer = CreatePanel("UnitSlots", popup.transform);
        RectTransform slotsRT = slotsContainer.GetComponent<RectTransform>();
        slotsRT.anchorMin = new Vector2(0, 0);
        slotsRT.anchorMax = new Vector2(1, 1);
        slotsRT.offsetMin = new Vector2(25, 25);
        slotsRT.offsetMax = new Vector2(-25, -60);
        slotsContainer.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        HorizontalLayoutGroup slotsLayout = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.spacing = 15;
        slotsLayout.padding = new RectOffset(10, 10, 10, 10);
        slotsLayout.childAlignment = TextAnchor.MiddleCenter;
        slotsLayout.childControlWidth = true;
        slotsLayout.childControlHeight = true;
        slotsLayout.childForceExpandWidth = true;

        // Create 5 unit slots
        CreateUnitSlot(slotsContainer.transform, "Skull", "7 Skulls", 1, true, buttonBeige, slotBg);
        CreateUnitSlot(slotsContainer.transform, "Gnoll", "5S 5M 5W", 3, false, buttonBeige, slotBg);
        CreateUnitSlot(slotsContainer.transform, "Gnome", "5S 3M 3W 2G", 5, false, buttonBeige, slotBg);
        CreateUnitSlot(slotsContainer.transform, "TNT", "5S 3M 5W 3G", 7, false, buttonBeige, slotBg);
        CreateUnitSlot(slotsContainer.transform, "Shaman", "5S 3M 5W 5G", 10, false, buttonBeige, slotBg);

        if (popup.GetComponent<AltarUI>() == null)
            popup.AddComponent<AltarUI>();

        popup.SetActive(false);
        Debug.Log("[UIBuilder] Created AltarUI popup with Cryo styling");
    }

    /// <summary>
    /// Create Quest UI panel.
    /// </summary>
    private static void CreateQuestUIPanel(Transform parent)
    {
        Transform existing = parent.Find("QuestUIPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Load Cryo sprites
        Sprite panelSprite = LoadSpriteFromSheet(CRYO_GUI_OUTER, 10); // Simple frame
        Sprite barBg = LoadSpriteFromSheet(CRYO_BARS, 2);   // Blue bar frame (small)
        Sprite barFill = LoadSpriteFromSheet(CRYO_BARS, 3); // Blue bar fill

        GameObject panel = CreatePanel("QuestUIPanel", parent);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 1);
        panelRT.anchorMax = new Vector2(0, 1);
        panelRT.pivot = new Vector2(0, 1);
        panelRT.anchoredPosition = new Vector2(20, -100);
        panelRT.sizeDelta = new Vector2(300, 100);

        Image panelBg = panel.GetComponent<Image>();
        if (panelSprite != null)
        {
            panelBg.sprite = panelSprite;
            panelBg.type = Image.Type.Sliced;
            panelBg.color = Color.white;
        }
        else
        {
            panelBg.color = PANEL_DARK;
        }

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 12, 12);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Quest title
        GameObject questTitle = CreateText("QuestTitle", panel.transform, "Current Quest", 18, FontStyles.Bold);
        questTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        questTitle.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.9f, 0.5f);
        questTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Quest description
        GameObject questDesc = CreateText("QuestDescription", panel.transform, "Kill 10 enemies", 14, FontStyles.Normal);
        questDesc.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
        questDesc.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Progress bar
        GameObject progressContainer = CreatePanel("ProgressBar", panel.transform);
        progressContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        Image progressBg = progressContainer.GetComponent<Image>();
        if (barBg != null)
        {
            progressBg.sprite = barBg;
            progressBg.type = Image.Type.Sliced;
            progressBg.color = Color.white;
        }
        else
        {
            progressBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }

        GameObject progressFill = CreatePanel("Fill", progressContainer.transform);
        RectTransform fillRT = progressFill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0.3f, 1);
        fillRT.offsetMin = new Vector2(4, 4);
        fillRT.offsetMax = new Vector2(-4, -4);
        Image fillImg = progressFill.GetComponent<Image>();
        if (barFill != null)
        {
            fillImg.sprite = barFill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.3f, 0.9f, 0.3f);
        }
        else
        {
            fillImg.color = new Color(0.3f, 0.8f, 0.3f);
        }

        GameObject progressText = CreateText("ProgressText", progressContainer.transform, "3/10", 14, FontStyles.Bold);
        SetFullStretch(progressText);

        if (panel.GetComponent<QuestUI>() == null)
            panel.AddComponent<QuestUI>();

        Debug.Log("[UIBuilder] Created QuestUI panel with Cryo styling");
    }

    /// <summary>
    /// Create Upgrade button in top right corner.
    /// </summary>
    private static void CreateUpgradeButtonTopRight(Transform parent)
    {
        Transform existing = parent.Find("UpgradeButtonTopRight");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Sprite buttonSprite = LoadSpriteFromSheet(CRYO_BUTTONS, 0); // Beige large

        GameObject btn = CreateStyledButton(parent, "UpgradeButtonTopRight", "UP", buttonSprite, new Color(0.85f, 0.75f, 0.55f));
        RectTransform btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 1);
        btnRT.anchorMax = new Vector2(1, 1);
        btnRT.pivot = new Vector2(1, 1);
        btnRT.anchoredPosition = new Vector2(-20, -100);
        btnRT.sizeDelta = new Vector2(65, 65);

        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = 26;
            text.color = TEXT_DARK;
        }

        Debug.Log("[UIBuilder] Created Upgrade button with Cryo styling");
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[UIBuilder] Created EventSystem");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Load a sprite from a single-sprite file.
    /// </summary>
    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                if (sprites.Length > 0 && sprites[0] is Sprite s)
                    return s;
            }
            Debug.LogWarning($"[UIBuilder] Could not load sprite: {path}");
        }
        return sprite;
    }

    /// <summary>
    /// Load a specific sprite from a sprite sheet by index.
    /// Unity names sliced sprites as "SheetName_0", "SheetName_1", etc.
    /// </summary>
    private static Sprite LoadSpriteFromSheet(string sheetPath, int index)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath);

        if (sprites == null || sprites.Length == 0)
        {
            // Sprite sheet not sliced yet - return null (will use fallback color)
            Debug.LogWarning($"[UIBuilder] Sprite sheet not sliced or empty: {sheetPath}. Please slice it in Unity Sprite Editor.");
            return null;
        }

        if (index >= 0 && index < sprites.Length)
        {
            if (sprites[index] is Sprite sprite)
                return sprite;
        }

        // Try to find by constructed name
        string baseName = Path.GetFileNameWithoutExtension(sheetPath);
        string spriteName = $"{baseName}_{index}";

        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning($"[UIBuilder] Sprite index {index} not found in: {sheetPath} (total: {sprites.Length})");
        return sprites.Length > 0 && sprites[0] is Sprite firstSprite ? firstSprite : null;
    }

    /// <summary>
    /// Load sprite from sheet by name.
    /// </summary>
    private static Sprite LoadSpriteFromSheetByName(string sheetPath, string spriteName)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[UIBuilder] Sprite sheet not sliced: {sheetPath}");
            return null;
        }

        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning($"[UIBuilder] Sprite '{spriteName}' not found in: {sheetPath}");
        return null;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        panel.AddComponent<CanvasRenderer>();
        panel.AddComponent<Image>();
        return panel;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        textGO.AddComponent<RectTransform>();
        textGO.AddComponent<CanvasRenderer>();

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return textGO;
    }

    private static GameObject CreateStyledButton(Transform parent, string name, string text, Sprite sprite, Color fallbackColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<RectTransform>();
        btnGO.AddComponent<CanvasRenderer>();

        Image img = btnGO.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = fallbackColor;
        }

        Button btn = btnGO.AddComponent<Button>();

        GameObject textGO = CreateText("Text", btnGO.transform, text, 20, FontStyles.Bold);
        SetFullStretch(textGO);
        textGO.GetComponent<TextMeshProUGUI>().color = TEXT_DARK;

        return btnGO;
    }

    private static GameObject CreateIconButton(Transform parent, string name, Sprite iconSprite, Color fallbackColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<RectTransform>();
        btnGO.AddComponent<CanvasRenderer>();

        Image img = btnGO.AddComponent<Image>();
        if (iconSprite != null)
        {
            img.sprite = iconSprite;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            img.color = fallbackColor;
        }

        btnGO.AddComponent<Button>();
        return btnGO;
    }

    private static void SetFullStretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateResourceDisplay(Transform parent, string name, Sprite icon, string value, Color textColor)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110, 50);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(container.transform, false);
        RectTransform iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(40, 40);
        Image iconImg = iconGO.AddComponent<Image>();
        if (icon != null)
        {
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
        }
        else
        {
            iconImg.color = textColor;
        }

        // Text
        GameObject textGO = CreateText("Value", container.transform, value, 28, FontStyles.Bold);
        textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 40);
        textGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
    }

    private static void CreateCargoDisplay(Transform parent, Sprite barBase, Sprite barFill)
    {
        GameObject container = new GameObject("CargoDisplay");
        container.transform.SetParent(parent, false);
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Label
        GameObject label = CreateText("Label", container.transform, "Cargo:", 18, FontStyles.Normal);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 30);
        label.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);

        // Bar container
        GameObject barContainer = CreatePanel("BarContainer", container.transform);
        barContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 28);
        Image barBg = barContainer.GetComponent<Image>();
        if (barBase != null)
        {
            barBg.sprite = barBase;
            barBg.type = Image.Type.Sliced;
            barBg.color = Color.white;
        }
        else
        {
            barBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }

        // Fill
        GameObject fill = CreatePanel("Fill", barContainer.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0.5f, 1);
        fillRT.offsetMin = new Vector2(4, 4);
        fillRT.offsetMax = new Vector2(-4, -4);
        Image fillImg = fill.GetComponent<Image>();
        if (barFill != null)
        {
            fillImg.sprite = barFill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = CARGO_COLOR;
        }
        else
        {
            fillImg.color = CARGO_COLOR;
        }
    }

    private static void CreateStatBar(Transform parent, string statName, Color barColor, Sprite barBase, Sprite barFill, int current, int max)
    {
        GameObject container = new GameObject(statName + "Bar");
        container.transform.SetParent(parent, false);
        container.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        // Label
        GameObject label = CreateText("Label", container.transform, statName, 20, FontStyles.Bold);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 0);
        label.GetComponent<TextMeshProUGUI>().color = barColor;
        label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Bar
        GameObject barContainer = CreatePanel("Bar", container.transform);
        barContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 0);
        barContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        Image barBg = barContainer.GetComponent<Image>();
        if (barBase != null)
        {
            barBg.sprite = barBase;
            barBg.type = Image.Type.Sliced;
            barBg.color = Color.white;
        }
        else
        {
            barBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        }

        GameObject fill = CreatePanel("Fill", barContainer.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(4, 4);
        fillRT.offsetMax = new Vector2(-4, -4);
        Image fillImg = fill.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = max > 0 ? (float)current / max : 1f;
        if (barFill != null)
        {
            fillImg.sprite = barFill;
            fillImg.color = barColor;
        }
        else
        {
            fillImg.color = barColor;
        }

        // Value text
        GameObject valueText = CreateText("Value", container.transform, $"{current}/{max}", 16, FontStyles.Normal);
        valueText.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 0);
        valueText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
    }

    private static void CreateLevelDisplay(Transform parent)
    {
        GameObject container = new GameObject("LevelDisplay");
        container.transform.SetParent(parent, false);
        container.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        GameObject label = CreateText("Label", container.transform, "Level:", 16, FontStyles.Normal);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(55, 0);
        label.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);
        label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        GameObject value = CreateText("Value", container.transform, "1", 22, FontStyles.Bold);
        value.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 0);
        value.GetComponent<TextMeshProUGUI>().color = new Color(0.3f, 1f, 0.3f);
        value.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
    }

    private static GameObject CreateUpgradePanel(Transform parent, string name, string[] upgrades, Sprite buttonSprite, Sprite slotSprite)
    {
        GameObject panel = CreatePanel(name, parent);
        SetFullStretch(panel);
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        foreach (string upgrade in upgrades)
        {
            string[] parts = upgrade.Split('|');
            CreateUpgradeSlot(panel.transform, parts[0], parts.Length > 1 ? parts[1] : "", parts.Length > 2 ? parts[2] : "Lv 0", buttonSprite, slotSprite);
        }

        return panel;
    }

    private static void CreateUpgradeSlot(Transform parent, string upgradeName, string desc, string level, Sprite buttonSprite, Sprite slotSprite)
    {
        GameObject slot = CreatePanel(upgradeName.Replace(" ", "") + "Slot", parent);
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 45);

        Image slotImg = slot.GetComponent<Image>();
        if (slotSprite != null)
        {
            slotImg.sprite = slotSprite;
            slotImg.type = Image.Type.Sliced;
            slotImg.color = Color.white;
        }
        else
        {
            slotImg.color = SLOT_BG;
        }

        HorizontalLayoutGroup layout = slot.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 8, 8);
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        GameObject nameText = CreateText("Name", slot.transform, upgradeName, 17, FontStyles.Bold);
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 0);
        nameText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        GameObject descText = CreateText("Desc", slot.transform, desc, 14, FontStyles.Normal);
        descText.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 0);
        descText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        descText.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

        GameObject levelText = CreateText("Level", slot.transform, level, 14, FontStyles.Normal);
        levelText.GetComponent<RectTransform>().sizeDelta = new Vector2(75, 0);

        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(slot.transform, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        GameObject buyBtn = CreateStyledButton(slot.transform, "BuyBtn", "BUY", buttonSprite, new Color(0.7f, 0.6f, 0.4f));
        buyBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(65, 0);
        buyBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
    }

    private static void CreateResourceBar(Transform parent)
    {
        Sprite skullIcon = LoadSprite(ICON_SKULL);
        Sprite meatIcon = LoadSprite(ICON_MEAT);
        Sprite woodIcon = LoadSprite(ICON_WOOD);
        Sprite goldIcon = LoadSprite(ICON_GOLD);

        GameObject resourceBar = CreatePanel("ResourceBar", parent);
        RectTransform rt = resourceBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 15);
        rt.sizeDelta = new Vector2(-50, 45);
        resourceBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

        HorizontalLayoutGroup layout = resourceBar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40;
        layout.padding = new RectOffset(30, 30, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        CreateMiniResource(resourceBar.transform, "Skulls", skullIcon, "0");
        CreateMiniResource(resourceBar.transform, "Meat", meatIcon, "0");
        CreateMiniResource(resourceBar.transform, "Wood", woodIcon, "0");
        CreateMiniResource(resourceBar.transform, "Gold", goldIcon, "0");
    }

    private static void CreateMiniResource(Transform parent, string name, Sprite icon, string value)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);
        container.AddComponent<RectTransform>().sizeDelta = new Vector2(90, 30);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(container.transform, false);
        iconGO.AddComponent<RectTransform>().sizeDelta = new Vector2(26, 26);
        Image iconImg = iconGO.AddComponent<Image>();
        if (icon != null)
        {
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
        }

        GameObject text = CreateText("Value", container.transform, value, 18, FontStyles.Bold);
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 0);
        text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
    }

    private static void CreateUnitSlot(Transform parent, string unitName, string cost, int unlockLevel, bool unlocked, Sprite buttonSprite, Sprite slotSprite)
    {
        GameObject slot = CreatePanel(unitName + "Slot", parent);
        Image slotBg = slot.GetComponent<Image>();
        if (slotSprite != null)
        {
            slotBg.sprite = slotSprite;
            slotBg.type = Image.Type.Sliced;
            slotBg.color = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
        else
        {
            slotBg.color = unlocked ? SLOT_BG : new Color(0.15f, 0.15f, 0.15f, 0.7f);
        }

        VerticalLayoutGroup layout = slot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 15, 15);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Avatar placeholder
        GameObject avatar = CreatePanel("Avatar", slot.transform);
        avatar.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
        avatar.GetComponent<Image>().color = unlocked ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.2f, 0.2f, 0.2f);

        // Name
        GameObject nameText = CreateText("Name", slot.transform, unitName, 18, FontStyles.Bold);
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        nameText.GetComponent<TextMeshProUGUI>().color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        // Cost
        GameObject costText = CreateText("Cost", slot.transform, cost, 13, FontStyles.Normal);
        costText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
        costText.GetComponent<TextMeshProUGUI>().color = unlocked ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.4f, 0.4f, 0.4f);

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(slot.transform, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

        // Button or lock text
        if (unlocked)
        {
            GameObject buyBtn = CreateStyledButton(slot.transform, "BuyButton", "SUMMON", buttonSprite, new Color(0.7f, 0.6f, 0.4f));
            buyBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            buyBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
        }
        else
        {
            GameObject lockText = CreateText("LockText", slot.transform, $"Lvl {unlockLevel}", 16, FontStyles.Bold);
            lockText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            lockText.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.3f, 0.3f);
        }

        CanvasGroup cg = slot.AddComponent<CanvasGroup>();
        cg.alpha = unlocked ? 1f : 0.65f;
    }

    #endregion
}
