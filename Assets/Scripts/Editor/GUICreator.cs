using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Step-by-step GUI Creator for building UI elements one at a time.
/// Tools > Puppet Master > GUI Creator
/// </summary>
public class GUICreator : EditorWindow
{
    // Sprite sheet paths
    private const string BARS_PATH = "Assets/Cryo's Mini GUI/Bars/Bars_4x.png";
    private const string BUTTONS_PATH = "Assets/Cryo's Mini GUI/Buttons/Buttons_4x.png";
    private const string GUI_OUTER_PATH = "Assets/Cryo's Mini GUI/GUI Outer/GUI_Outer_4x.png";
    private const string GUI_INNER_PATH = "Assets/Cryo's Mini GUI/GUI Inner/GUI_Inner_4x.png";

    // Avatar path (spider)
    private const string SPIDER_AVATAR_PATH = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Enemy Avatars/Enemy Avatars_11.png";

    // Font paths (Cryo's Mini GUI)
    private const string FONT_6X6_PATH = "Assets/Cryo's Mini GUI/Font/6x6/cryos-font.ttf";
    private const string FONT_5X5_PATH = "Assets/Cryo's Mini GUI/Font/5x5/cryos-font.ttf";

    // Resource icon paths
    private const string ICON_SKULL_PATH = "Assets/Sprites/Resources/Dead.png";
    private const string ICON_MEAT_PATH = "Assets/Sprites/Resources/Meat.png";
    private const string ICON_WOOD_PATH = "Assets/Sprites/Resources/Wood.png";
    private const string ICON_GOLD_PATH = "Assets/Sprites/Resources/Gold.png";

    private Canvas mainCanvas;
    private TMP_FontAsset cryoFont;

    [MenuItem("Tools/Puppet Master/GUI Creator")]
    public static void ShowWindow()
    {
        GetWindow<GUICreator>("GUI Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Puppet Master GUI Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Canvas section
        EditorGUILayout.LabelField("Step 1: Canvas", EditorStyles.boldLabel);

        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            EditorGUILayout.HelpBox("No Canvas found in scene.", MessageType.Warning);
            if (GUILayout.Button("Create Canvas", GUILayout.Height(30)))
            {
                CreateCanvas();
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"Canvas found: {mainCanvas.name}", MessageType.Info);
            if (GUILayout.Button("Delete Canvas", GUILayout.Height(25)))
            {
                DestroyImmediate(mainCanvas.gameObject);
                mainCanvas = null;
            }
        }

        GUILayout.Space(20);

        // UI Elements section
        EditorGUILayout.LabelField("Step 2: Add UI Elements", EditorStyles.boldLabel);

        GUI.enabled = mainCanvas != null;

        if (GUILayout.Button("Add Player HUD (Portrait + Bars + Level)", GUILayout.Height(35)))
        {
            CreatePlayerHUD();
        }

        if (GUILayout.Button("Add Upgrades Popup", GUILayout.Height(35)))
        {
            CreateUpgradesPopup();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Add Resource Panel (Top)", GUILayout.Height(35)))
        {
            CreateResourcePanel();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Add Upgrade Button", GUILayout.Height(35)))
        {
            CreateUpgradeButton();
        }

        GUI.enabled = true;

        GUILayout.Space(20);

        // Cleanup section
        EditorGUILayout.LabelField("Cleanup", EditorStyles.boldLabel);
        if (GUILayout.Button("Delete All UI", GUILayout.Height(30)))
        {
            DeleteAllUI();
        }
    }

    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("MainCanvas");
        mainCanvas = canvasGO.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create EventSystem if needed (using new Input System)
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>(); // New Input System compatible
        }

        Selection.activeGameObject = canvasGO;
        Debug.Log("[GUICreator] Canvas created");
    }

    private void CreatePlayerHUD()
    {
        TMP_FontAsset cryoFont = LoadCryoFont();

        // Main container for entire HUD
        GameObject hudContainer = new GameObject("PlayerHUD");
        hudContainer.transform.SetParent(mainCanvas.transform, false);

        RectTransform hudRect = hudContainer.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 0);
        hudRect.anchorMax = new Vector2(0, 0);
        hudRect.pivot = new Vector2(0, 0);
        hudRect.anchoredPosition = new Vector2(15, 15);
        hudRect.sizeDelta = new Vector2(380, 140); // Taller for 3 bars

        // === PORTRAIT FRAME (left side) ===
        // Height matches 3 bars (3 * 40 = 120)
        GameObject portraitFrame = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(hudContainer.transform, false);

        RectTransform portraitRect = portraitFrame.AddComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0, 0);
        portraitRect.anchorMax = new Vector2(0, 0);
        portraitRect.pivot = new Vector2(0, 0);
        portraitRect.anchoredPosition = new Vector2(0, 10);
        portraitRect.sizeDelta = new Vector2(120, 120); // Square, height of 3 bars

        Image portraitFrameImg = portraitFrame.AddComponent<Image>();
        portraitFrameImg.sprite = LoadSprite(GUI_INNER_PATH, "GUI_Inner_4x_5");
        portraitFrameImg.type = Image.Type.Sliced;

        // Portrait image (spider avatar)
        GameObject portrait = new GameObject("Portrait");
        portrait.transform.SetParent(portraitFrame.transform, false);

        RectTransform portRect = portrait.AddComponent<RectTransform>();
        portRect.anchorMin = new Vector2(0, 0);
        portRect.anchorMax = new Vector2(1, 1);
        portRect.offsetMin = new Vector2(8, 8);
        portRect.offsetMax = new Vector2(-8, -8);

        Image portImage = portrait.AddComponent<Image>();
        Sprite spiderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPIDER_AVATAR_PATH);
        if (spiderSprite != null)
            portImage.sprite = spiderSprite;
        portImage.preserveAspect = true;

        // === BARS CONTAINER (right of portrait) ===
        // Align bottom of XP bar with bottom of portrait frame (y=10)
        GameObject barsContainer = new GameObject("Bars");
        barsContainer.transform.SetParent(hudContainer.transform, false);

        RectTransform barsRect = barsContainer.AddComponent<RectTransform>();
        barsRect.anchorMin = new Vector2(0, 0);
        barsRect.anchorMax = new Vector2(0, 0);
        barsRect.pivot = new Vector2(0, 0);
        barsRect.anchoredPosition = new Vector2(125, 10); // Shifted right for larger portrait
        barsRect.sizeDelta = new Vector2(270, 120); // Taller to fit 3 bars

        // === HP BAR (top) ===
        CreateBar(barsContainer.transform, "HPBar", 82, "Bars_4x_0", "Bars_4x_21",
                  PlayerHUDBar.BarType.HP, "100/100", cryoFont, 1.15f);

        // === CARGO BAR (middle) ===
        CreateCargoBar(barsContainer.transform, "CargoBar", 41, cryoFont, 1.15f);

        // === XP BAR (bottom) ===
        CreateBar(barsContainer.transform, "XPBar", 0, "Bars_4x_3", "Bars_4x_19",
                  PlayerHUDBar.BarType.XP, "0/100", cryoFont, 1.15f);

        // === UPGRADES BUTTON (above portrait) ===
        GameObject upgradesBtn = new GameObject("UpgradesButton");
        upgradesBtn.transform.SetParent(hudContainer.transform, false);

        RectTransform upgBtnRect = upgradesBtn.AddComponent<RectTransform>();
        upgBtnRect.anchorMin = new Vector2(0, 0);
        upgBtnRect.anchorMax = new Vector2(0, 0);
        upgBtnRect.pivot = new Vector2(0, 0); // Left-aligned
        upgBtnRect.anchoredPosition = new Vector2(0, 135); // Moved up for taller HUD
        upgBtnRect.sizeDelta = new Vector2(240, 80); // 2x larger

        Image upgBtnImg = upgradesBtn.AddComponent<Image>();
        upgBtnImg.sprite = LoadSprite(BUTTONS_PATH, "Buttons_4x_6");
        upgBtnImg.type = Image.Type.Sliced;

        Button upgButton = upgradesBtn.AddComponent<Button>();
        upgButton.targetGraphic = upgBtnImg;

        // Add UpgradesUIController to handle popup
        UpgradesUIController uiController = upgradesBtn.AddComponent<UpgradesUIController>();

        // Button text
        GameObject upgTextGO = new GameObject("Text");
        upgTextGO.transform.SetParent(upgradesBtn.transform, false);

        RectTransform upgTextRect = upgTextGO.AddComponent<RectTransform>();
        upgTextRect.anchorMin = Vector2.zero;
        upgTextRect.anchorMax = Vector2.one;
        upgTextRect.offsetMin = Vector2.zero;
        upgTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI upgText = upgTextGO.AddComponent<TextMeshProUGUI>();
        upgText.text = "Upgrades";
        upgText.fontSize = 22; // 2x larger to match button
        upgText.fontStyle = FontStyles.Bold;
        // Light color when upgrades available, dark when not
        // Buttons_4x_12 is light square, Buttons_4x_13 is dark square
        upgText.color = new Color(1f, 0.95f, 0.8f); // Light cream (has upgrades)
        // upgText.color = new Color(0.4f, 0.35f, 0.3f); // Dark brown (no upgrades)
        upgText.alignment = TextAlignmentOptions.Center;
        if (cryoFont != null) upgText.font = cryoFont;

        // === LEVEL INDICATOR (bottom-right corner of portrait) ===
        GameObject levelContainer = new GameObject("LevelIndicator");
        levelContainer.transform.SetParent(portraitFrame.transform, false);

        RectTransform levelRect = levelContainer.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1, 0);
        levelRect.anchorMax = new Vector2(1, 0);
        levelRect.pivot = new Vector2(1, 0);
        levelRect.anchoredPosition = new Vector2(5, -5);
        levelRect.sizeDelta = new Vector2(32, 32);

        // Level frame (GUI_Inner_4x_12)
        Image levelFrameImg = levelContainer.AddComponent<Image>();
        levelFrameImg.sprite = LoadSprite(GUI_INNER_PATH, "GUI_Inner_4x_12");
        levelFrameImg.type = Image.Type.Sliced;

        // Level inner (GUI_Inner_4x_14)
        GameObject levelInner = new GameObject("Inner");
        levelInner.transform.SetParent(levelContainer.transform, false);

        RectTransform levelInnerRect = levelInner.AddComponent<RectTransform>();
        levelInnerRect.anchorMin = new Vector2(0, 0);
        levelInnerRect.anchorMax = new Vector2(1, 1);
        levelInnerRect.offsetMin = new Vector2(4, 4);
        levelInnerRect.offsetMax = new Vector2(-4, -4);

        Image levelInnerImg = levelInner.AddComponent<Image>();
        levelInnerImg.sprite = LoadSprite(GUI_INNER_PATH, "GUI_Inner_4x_14");
        levelInnerImg.type = Image.Type.Sliced;

        // Level text
        GameObject levelText = new GameObject("LevelText");
        levelText.transform.SetParent(levelContainer.transform, false);

        RectTransform levelTextRect = levelText.AddComponent<RectTransform>();
        levelTextRect.anchorMin = Vector2.zero;
        levelTextRect.anchorMax = Vector2.one;
        levelTextRect.offsetMin = Vector2.zero;
        levelTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI lvlText = levelText.AddComponent<TextMeshProUGUI>();
        lvlText.text = "1";
        lvlText.fontSize = 14;
        lvlText.fontStyle = FontStyles.Bold;
        lvlText.color = Color.white;
        lvlText.alignment = TextAlignmentOptions.Center;
        if (cryoFont != null) lvlText.font = cryoFont;

        Selection.activeGameObject = hudContainer;
        Debug.Log("[GUICreator] Player HUD created with Portrait, HP/XP Bars, and Level indicator.");
    }

    private void CreateUpgradesPopup()
    {
        TMP_FontAsset cryoFont = LoadCryoFont();

        // Main popup container - centered on screen, starts hidden
        GameObject popup = new GameObject("UpgradesPopup");
        popup.transform.SetParent(mainCanvas.transform, false);

        RectTransform popupRect = popup.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(400, 420); // 20% taller

        // Outer frame - GUI_Outer_4x_1
        Image frameImg = popup.AddComponent<Image>();
        frameImg.sprite = LoadSprite(GUI_OUTER_PATH, "GUI_Outer_4x_1");
        frameImg.type = Image.Type.Sliced;

        // Inner fill - GUI_Inner_4x_6
        GameObject innerFill = new GameObject("InnerFill");
        innerFill.transform.SetParent(popup.transform, false);

        RectTransform innerRect = innerFill.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(16, 16);
        innerRect.offsetMax = new Vector2(-16, -16);

        Image innerImg = innerFill.AddComponent<Image>();
        innerImg.sprite = LoadSprite(GUI_INNER_PATH, "GUI_Inner_4x_6");
        innerImg.type = Image.Type.Sliced;

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(popup.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(200, 30);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "UPGRADES";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.95f, 0.85f);
        titleText.alignment = TextAlignmentOptions.Center;
        if (cryoFont != null) titleText.font = cryoFont;

        // Content area with placeholder upgrade rows
        GameObject content = new GameObject("Content");
        content.transform.SetParent(popup.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(25, 50);
        contentRect.offsetMax = new Vector2(-25, -70); // More space after title

        // Add placeholder upgrade rows
        string[] upgradeNames = { "Attack Speed", "Move Speed", "Max Health", "Damage", "Cargo Capacity" };
        for (int i = 0; i < upgradeNames.Length; i++)
        {
            CreateUpgradeRow(content.transform, upgradeNames[i], i, upgradeNames.Length, cryoFont);
        }

        // Close button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(popup.transform, false);

        RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.anchoredPosition = new Vector2(-8, -8);
        closeBtnRect.sizeDelta = new Vector2(28, 28);

        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.sprite = LoadSprite(BUTTONS_PATH, "Buttons_4x_6");
        closeBtnImg.type = Image.Type.Sliced;

        Button closeButton = closeBtn.AddComponent<Button>();
        closeButton.targetGraphic = closeBtnImg;

        // X text
        GameObject xTextGO = new GameObject("X");
        xTextGO.transform.SetParent(closeBtn.transform, false);

        RectTransform xRect = xTextGO.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;

        TextMeshProUGUI xText = xTextGO.AddComponent<TextMeshProUGUI>();
        xText.text = "X";
        xText.fontSize = 14;
        xText.fontStyle = FontStyles.Bold;
        xText.color = new Color(0.9f, 0.3f, 0.2f);
        xText.alignment = TextAlignmentOptions.Center;
        if (cryoFont != null) xText.font = cryoFont;

        Selection.activeGameObject = popup;
        Debug.Log("[GUICreator] Upgrades Popup created.");
    }

    private void CreateUpgradeRow(Transform parent, string upgradeName, int index, int totalRows, TMP_FontAsset font)
    {
        float rowHeight = 45;
        float rowSpacing = 15; // Space between rows (+25%)
        // Rows anchored to bottom, index 0 = bottom row
        int reverseIndex = totalRows - 1 - index;
        float yPos = reverseIndex * (rowHeight + rowSpacing);

        GameObject row = new GameObject($"Upgrade_{upgradeName.Replace(" ", "")}");
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 0); // Bottom anchor
        rowRect.anchorMax = new Vector2(1, 0);
        rowRect.pivot = new Vector2(0, 0);
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(0, rowHeight);

        // Row background
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.15f, 0.12f, 0.1f, 0.5f);

        // Upgrade name
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(row.transform, false);

        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(0.4f, 1);
        nameRect.offsetMin = new Vector2(10, 5);
        nameRect.offsetMax = new Vector2(0, -5);

        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = upgradeName;
        nameText.fontSize = 12;
        nameText.color = new Color(0.9f, 0.85f, 0.75f);
        nameText.alignment = TextAlignmentOptions.Left;
        if (font != null) nameText.font = font;

        // Current level
        GameObject levelGO = new GameObject("Level");
        levelGO.transform.SetParent(row.transform, false);

        RectTransform levelRect = levelGO.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.4f, 0);
        levelRect.anchorMax = new Vector2(0.6f, 1);
        levelRect.offsetMin = new Vector2(0, 5);
        levelRect.offsetMax = new Vector2(0, -5);

        TextMeshProUGUI levelText = levelGO.AddComponent<TextMeshProUGUI>();
        levelText.text = "Lv. 0";
        levelText.fontSize = 11;
        levelText.color = new Color(0.7f, 0.65f, 0.55f);
        levelText.alignment = TextAlignmentOptions.Center;
        if (font != null) levelText.font = font;

        // Cost
        GameObject costGO = new GameObject("Cost");
        costGO.transform.SetParent(row.transform, false);

        RectTransform costRect = costGO.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.6f, 0);
        costRect.anchorMax = new Vector2(0.8f, 1);
        costRect.offsetMin = new Vector2(0, 5);
        costRect.offsetMax = new Vector2(0, -5);

        TextMeshProUGUI costText = costGO.AddComponent<TextMeshProUGUI>();
        costText.text = "100G";
        costText.fontSize = 11;
        costText.color = new Color(1f, 0.85f, 0.3f); // Gold color
        costText.alignment = TextAlignmentOptions.Center;
        if (font != null) costText.font = font;

        // Buy button
        GameObject buyBtn = new GameObject("BuyButton");
        buyBtn.transform.SetParent(row.transform, false);

        RectTransform buyRect = buyBtn.AddComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(0.8f, 0.1f);
        buyRect.anchorMax = new Vector2(0.98f, 0.9f);
        buyRect.offsetMin = Vector2.zero;
        buyRect.offsetMax = Vector2.zero;

        Image buyImg = buyBtn.AddComponent<Image>();
        buyImg.sprite = LoadSprite(BUTTONS_PATH, "Buttons_4x_6");
        buyImg.type = Image.Type.Sliced;

        Button buyButton = buyBtn.AddComponent<Button>();
        buyButton.targetGraphic = buyImg;

        // Buy text
        GameObject buyTextGO = new GameObject("Text");
        buyTextGO.transform.SetParent(buyBtn.transform, false);

        RectTransform buyTextRect = buyTextGO.AddComponent<RectTransform>();
        buyTextRect.anchorMin = Vector2.zero;
        buyTextRect.anchorMax = Vector2.one;
        buyTextRect.offsetMin = Vector2.zero;
        buyTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buyText = buyTextGO.AddComponent<TextMeshProUGUI>();
        buyText.text = "BUY";
        buyText.fontSize = 10;
        buyText.fontStyle = FontStyles.Bold;
        buyText.color = new Color(0.2f, 0.8f, 0.3f);
        buyText.alignment = TextAlignmentOptions.Center;
        if (font != null) buyText.font = font;
    }

    private void CreateBar(Transform parent, string name, float yPos, string frameSprite, string fillSprite,
                           PlayerHUDBar.BarType barType, string defaultValue, TMP_FontAsset font, float scale = 1f)
    {
        float barHeight = 35 * scale; // Base height scaled proportionally

        GameObject barContainer = new GameObject(name);
        barContainer.transform.SetParent(parent, false);

        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(0, yPos);
        containerRect.sizeDelta = new Vector2(0, barHeight);

        // Scaled padding values
        float padX = 10 * scale;
        float padY = 8 * scale;

        // Black background for empty space
        GameObject blackBg = new GameObject("BlackBg");
        blackBg.transform.SetParent(barContainer.transform, false);

        RectTransform blackRect = blackBg.AddComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = new Vector2(padX, padY);
        blackRect.offsetMax = new Vector2(-padX, -padY);

        Image blackImg = blackBg.AddComponent<Image>();
        blackImg.color = new Color(0.05f, 0.05f, 0.05f, 1f); // Almost black

        // Frame
        GameObject frame = new GameObject("Frame");
        frame.transform.SetParent(barContainer.transform, false);

        RectTransform frameRect = frame.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImg = frame.AddComponent<Image>();
        frameImg.sprite = LoadSprite(BARS_PATH, frameSprite);
        frameImg.type = Image.Type.Sliced;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barContainer.transform, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = new Vector2(padX, padY);
        fillRect.offsetMax = new Vector2(-padX, -padY);

        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = LoadSprite(BARS_PATH, fillSprite);
        fillImg.type = Image.Type.Tiled;
        fillImg.color = Color.white;

        // Value text - centered, smaller
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(barContainer.transform, false);

        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.text = defaultValue;
        valueText.fontSize = 10 * scale; // Scaled font size
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.enableAutoSizing = false;
        if (font != null) valueText.font = font;

        // Add runtime component
        PlayerHUDBar hudBar = barContainer.AddComponent<PlayerHUDBar>();
        var so = new SerializedObject(hudBar);
        so.FindProperty("barType").enumValueIndex = (int)barType;
        so.FindProperty("fillImage").objectReferenceValue = fillImg;
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void CreateCargoBar(Transform parent, string name, float yPos, TMP_FontAsset font, float scale = 1f)
    {
        Debug.Log($"[GUICreator] Creating Segmented Cargo Bar at Y={yPos}");
        float barHeight = 35 * scale;

        GameObject barContainer = new GameObject(name);
        barContainer.transform.SetParent(parent, false);

        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(0, yPos);
        containerRect.sizeDelta = new Vector2(0, barHeight);

        float padX = 10 * scale;
        float padY = 8 * scale;

        // Black background for empty space
        GameObject blackBg = new GameObject("BlackBg");
        blackBg.transform.SetParent(barContainer.transform, false);

        RectTransform blackRect = blackBg.AddComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = new Vector2(padX, padY);
        blackRect.offsetMax = new Vector2(-padX, -padY);

        Image blackImg = blackBg.AddComponent<Image>();
        blackImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);

        // Fill container (holds the 3 segment fills)
        GameObject fillContainer = new GameObject("FillContainer");
        fillContainer.transform.SetParent(barContainer.transform, false);

        RectTransform fillContainerRect = fillContainer.AddComponent<RectTransform>();
        fillContainerRect.anchorMin = Vector2.zero;
        fillContainerRect.anchorMax = Vector2.one;
        fillContainerRect.offsetMin = new Vector2(padX, padY);
        fillContainerRect.offsetMax = new Vector2(-padX, -padY);

        // Wood fill segment (brown) - ORDER: Wood, Meat, Gold (left to right)
        GameObject woodFill = new GameObject("WoodFill");
        woodFill.transform.SetParent(fillContainer.transform, false);

        RectTransform woodRect = woodFill.AddComponent<RectTransform>();
        woodRect.anchorMin = new Vector2(0, 0);
        woodRect.anchorMax = new Vector2(0, 1); // Will be adjusted by script
        woodRect.offsetMin = Vector2.zero;
        woodRect.offsetMax = Vector2.zero;

        Image woodImg = woodFill.AddComponent<Image>();
        woodImg.color = new Color(0.55f, 0.35f, 0.2f); // Brown

        // Meat fill segment (light red)
        GameObject meatFill = new GameObject("MeatFill");
        meatFill.transform.SetParent(fillContainer.transform, false);

        RectTransform meatRect = meatFill.AddComponent<RectTransform>();
        meatRect.anchorMin = new Vector2(0, 0);
        meatRect.anchorMax = new Vector2(0, 1);
        meatRect.offsetMin = Vector2.zero;
        meatRect.offsetMax = Vector2.zero;

        Image meatImg = meatFill.AddComponent<Image>();
        meatImg.color = new Color(0.85f, 0.4f, 0.4f); // Light red

        // Gold fill segment (yellow)
        GameObject goldFill = new GameObject("GoldFill");
        goldFill.transform.SetParent(fillContainer.transform, false);

        RectTransform goldRect = goldFill.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 0);
        goldRect.anchorMax = new Vector2(0, 1);
        goldRect.offsetMin = Vector2.zero;
        goldRect.offsetMax = Vector2.zero;

        Image goldImg = goldFill.AddComponent<Image>();
        goldImg.color = new Color(1f, 0.85f, 0.2f); // Yellow/gold

        // Frame - Bars_4x_8 (yellow/orange frame) - AFTER fills so it's on top
        GameObject frame = new GameObject("Frame");
        frame.transform.SetParent(barContainer.transform, false);

        RectTransform frameRect = frame.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImg = frame.AddComponent<Image>();
        frameImg.sprite = LoadSprite(BARS_PATH, "Bars_4x_8");
        frameImg.type = Image.Type.Sliced;

        // Value text - black text for cargo
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(barContainer.transform, false);

        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.text = "0/50";
        valueText.fontSize = 10 * scale;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = Color.black; // Black text for cargo
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.enableAutoSizing = false;
        if (font != null) valueText.font = font;

        // Add SegmentedCargoBar runtime component with serialized references
        SegmentedCargoBar cargoBar = barContainer.AddComponent<SegmentedCargoBar>();
        var so = new SerializedObject(cargoBar);
        so.FindProperty("fillContainer").objectReferenceValue = fillContainerRect;
        so.FindProperty("woodFill").objectReferenceValue = woodImg;
        so.FindProperty("meatFill").objectReferenceValue = meatImg;
        so.FindProperty("goldFill").objectReferenceValue = goldImg;
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[GUICreator] Cargo Bar created successfully: {barContainer.name}");
    }

    private void CreateResourcePanel()
    {
        TMP_FontAsset cryoFont = LoadCryoFont();

        // Container at top-LEFT
        GameObject panel = new GameObject("ResourcePanel");
        panel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1); // Top-left anchor
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(15, -15);
        panelRect.sizeDelta = new Vector2(320, 50);

        // Background
        Image bgImage = panel.AddComponent<Image>();
        bgImage.sprite = LoadSprite(GUI_OUTER_PATH, "GUI_Outer_4x_0");
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(1, 1, 1, 0.95f);

        // Horizontal layout
        HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Load actual resource sprites
        Sprite skullSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_SKULL_PATH);
        Sprite meatSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_MEAT_PATH);
        Sprite woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_WOOD_PATH);
        Sprite goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_GOLD_PATH);

        // Add resource displays with real icons
        // Names must contain "skull", "meat", "wood", "gold" for RuntimeTopHUD to find them
        CreateResourceDisplay(panel.transform, "SkullsDisplay", "0", skullSprite, cryoFont);
        CreateResourceDisplay(panel.transform, "MeatDisplay", "0", meatSprite, cryoFont);
        CreateResourceDisplay(panel.transform, "WoodDisplay", "0", woodSprite, cryoFont);
        CreateResourceDisplay(panel.transform, "GoldDisplay", "0", goldSprite, cryoFont);

        // Add RuntimeTopHUD component to handle updates
        panel.AddComponent<RuntimeTopHUD>();

        Selection.activeGameObject = panel;
        Debug.Log("[GUICreator] Resource Panel created with real icons at top-left.");
    }

    private void CreateResourceDisplay(Transform parent, string name, string value, Sprite iconSprite, TMP_FontAsset font)
    {
        GameObject display = new GameObject(name);
        display.transform.SetParent(parent, false);

        RectTransform rect = display.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(65, 38);

        // Icon with actual sprite
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(display.transform, false);

        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(28, 28);

        Image iconImage = iconGO.AddComponent<Image>();
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
        }
        else
        {
            // Fallback to colored square if sprite not found
            iconImage.color = Color.magenta;
            Debug.LogWarning($"[GUICreator] Sprite not found for {name}");
        }

        // Value text - name contains resource type for RuntimeTopHUD to find
        // e.g., "SkullsDisplay" -> text object named "SkullsText"
        string resourceType = name.Replace("Display", "");
        GameObject valueGO = new GameObject($"{resourceType}Text");
        valueGO.transform.SetParent(display.transform, false);

        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0, 0.5f);
        valueRect.anchorMax = new Vector2(0, 0.5f);
        valueRect.pivot = new Vector2(0, 0.5f);
        valueRect.anchoredPosition = new Vector2(32, 0);
        valueRect.sizeDelta = new Vector2(35, 30);

        TextMeshProUGUI text = valueGO.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = 16;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.95f, 0.85f); // Light cream
        text.alignment = TextAlignmentOptions.Left;
        if (font != null) text.font = font;
    }

    private void CreateUpgradeButton()
    {
        GameObject button = new GameObject("UpgradeButton");
        button.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = button.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(80, 80);

        Image image = button.AddComponent<Image>();
        image.sprite = LoadSprite(BUTTONS_PATH, "Buttons_4x_0");
        image.type = Image.Type.Sliced;

        Button btn = button.AddComponent<Button>();
        btn.targetGraphic = image;

        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(button.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "UP";
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.2f, 0.15f, 0.1f);
        text.alignment = TextAlignmentOptions.Center;

        Selection.activeGameObject = button;
        Debug.Log("[GUICreator] Upgrade Button created.");
    }

    private void DeleteAllUI()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            DestroyImmediate(c.gameObject);
        }

        var eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        foreach (var es in eventSystems)
        {
            DestroyImmediate(es.gameObject);
        }

        mainCanvas = null;
        Debug.Log("[GUICreator] All UI deleted.");
    }

    private Sprite LoadSprite(string sheetPath, string spriteName)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[GUICreator] No sprites found at: {sheetPath}");
            return null;
        }

        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"[GUICreator] Sprite '{spriteName}' not found in: {sheetPath}");
        return null;
    }

    private TMP_FontAsset LoadCryoFont()
    {
        // Try to find existing TMP font asset for Cryo font
        string[] guids = AssetDatabase.FindAssets("cryos-font t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        // If no TMP font asset exists, try to load the TTF and create one
        Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(FONT_6X6_PATH);
        if (ttfFont != null)
        {
            Debug.Log("[GUICreator] Found Cryo TTF font. To use it with TextMeshPro:");
            Debug.Log("1. Window > TextMeshPro > Font Asset Creator");
            Debug.Log("2. Select the font from: " + FONT_6X6_PATH);
            Debug.Log("3. Click 'Generate Font Atlas' then 'Save'");
        }

        return null;
    }
}
