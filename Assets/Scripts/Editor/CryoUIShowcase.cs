using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Creates a showcase scene with all Cryo's Mini GUI elements for preview and selection.
/// Use Tools > Puppet Master > Create UI Showcase to generate.
/// </summary>
public class CryoUIShowcase : EditorWindow
{
    // Sprite sheet paths
    private const string BARS_PATH = "Assets/Cryo's Mini GUI/Bars/Bars_4x.png";
    private const string BUTTONS_PATH = "Assets/Cryo's Mini GUI/Buttons/Buttons_4x.png";
    private const string GUI_OUTER_PATH = "Assets/Cryo's Mini GUI/GUI Outer/GUI_Outer_4x.png";
    private const string GUI_INNER_PATH = "Assets/Cryo's Mini GUI/GUI Inner/GUI_Inner_4x.png";
    private const string STAT_ICONS_PATH = "Assets/Cryo's Mini GUI/Stat Icons/Stat_Icons_4x.png";

    [MenuItem("Tools/Puppet Master/Create UI Showcase")]
    public static void CreateShowcase()
    {
        // Create main canvas
        GameObject canvasGO = new GameObject("=== CRYO UI SHOWCASE ===");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create scroll view for all content
        GameObject scrollViewGO = new GameObject("ScrollView");
        scrollViewGO.transform.SetParent(canvasGO.transform, false);

        RectTransform scrollRect = scrollViewGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        ScrollRect scroll = scrollViewGO.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        viewportGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

        // Content container
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 30;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;

        // === SECTIONS ===

        // 1. BARS
        CreateSection(contentGO.transform, "═══ BARS (Bars_4x.png) ═══", BARS_PATH, new string[] {
            "Bars_4x_0|Red Bar Frame (HP)|256x64",
            "Bars_4x_1|Red Bar Fill (HP)|240x48",
            "Bars_4x_2|Blue Bar Frame (XP/Mana)|256x64",
            "Bars_4x_3|Blue Bar Fill (XP/Mana)|240x48",
            "Bars_4x_4|Orange Bar Frame (Cargo)|256x48",
            "Bars_4x_5|Orange Bar Fill (Cargo)|240x32"
        });

        // 2. BUTTONS
        CreateSection(contentGO.transform, "═══ BUTTONS (Buttons_4x.png) ═══", BUTTONS_PATH, new string[] {
            "Buttons_4x_0|Beige Button Large|64x64",
            "Buttons_4x_1|Beige Button Medium|64x64",
            "Buttons_4x_2|Beige Button Small|64x64",
            "Buttons_4x_3|Beige Button Pressed|64x64",
            "Buttons_4x_4|DarkBlue Button Large|64x64",
            "Buttons_4x_5|DarkBlue Button Medium|64x64",
            "Buttons_4x_6|Gray Button Large|64x64",
            "Buttons_4x_7|Gray Button Medium|64x64",
            "Buttons_4x_8|Gray Button Small|64x64",
            "Buttons_4x_9|Gray Button Disabled|64x64",
            "Buttons_4x_10|Alternative 1|64x64",
            "Buttons_4x_11|Alternative 2|64x64",
            "Buttons_4x_12|Icon: X (Close)|32x32",
            "Buttons_4x_13|Icon: Check|32x32",
            "Buttons_4x_14|Icon: Plus|32x32",
            "Buttons_4x_15|Icon: Minus|32x32"
        });

        // 3. GUI OUTER (Frames)
        CreateSection(contentGO.transform, "═══ GUI OUTER - Frames (GUI_Outer_4x.png) ═══", GUI_OUTER_PATH, new string[] {
            "GUI_Outer_4x_0|Frame Style 1|128x96",
            "GUI_Outer_4x_1|Frame Style 2|128x96",
            "GUI_Outer_4x_2|Frame Style 3|128x96",
            "GUI_Outer_4x_3|Frame Style 4|128x96",
            "GUI_Outer_4x_4|Frame Style 5|128x96",
            "GUI_Outer_4x_5|Frame Square|96x96",
            "GUI_Outer_4x_6|Frame Row 2 Style 1|128x96",
            "GUI_Outer_4x_7|Frame Row 2 Style 2|128x96",
            "GUI_Outer_4x_8|Frame Row 2 Style 3|128x96",
            "GUI_Outer_4x_9|Frame Row 2 Style 4|128x96",
            "GUI_Outer_4x_10|Small Frame 1|64x64",
            "GUI_Outer_4x_11|Small Frame 2|64x64"
        });

        // 4. GUI INNER (Panels/Slots)
        CreateSection(contentGO.transform, "═══ GUI INNER - Panels & Slots (GUI_Inner_4x.png) ═══", GUI_INNER_PATH, new string[] {
            "GUI_Inner_4x_0|Panel Style 1|96x96",
            "GUI_Inner_4x_1|Panel Style 2|96x96",
            "GUI_Inner_4x_2|Panel Style 3|96x96",
            "GUI_Inner_4x_3|Panel Style 4|96x96",
            "GUI_Inner_4x_4|Panel Style 5|96x96",
            "GUI_Inner_4x_5|Panel Style 6|96x96",
            "GUI_Inner_4x_6|Panel Style 7|96x96",
            "GUI_Inner_4x_7|Small Panel|64x64",
            "GUI_Inner_4x_8|Slot Style 1|64x64",
            "GUI_Inner_4x_9|Slot Style 2|64x64",
            "GUI_Inner_4x_10|Slot Style 3|64x64",
            "GUI_Inner_4x_11|Slot Style 4|64x64"
        });

        // 5. STAT ICONS
        CreateSection(contentGO.transform, "═══ STAT ICONS (Stat_Icons_4x.png) ═══", STAT_ICONS_PATH, new string[] {
            "Stat_Icons_4x_0|Heart Red|32x32",
            "Stat_Icons_4x_1|Heart Blue|32x32",
            "Stat_Icons_4x_2|Shield Gold|32x32",
            "Stat_Icons_4x_3|Leaf Green|32x32",
            "Stat_Icons_4x_4|Arrow Blue|32x32",
            "Stat_Icons_4x_5|Gear Gray|32x32",
            "Stat_Icons_4x_6|Bomb|32x32",
            "Stat_Icons_4x_7|Star|32x32",
            "Stat_Icons_4x_8|Heart Small|32x32",
            "Stat_Icons_4x_9|Drop Blue|32x32",
            "Stat_Icons_4x_10|Sword|32x32",
            "Stat_Icons_4x_11|Leaf Alt|32x32",
            "Stat_Icons_4x_12|Snowflake|32x32",
            "Stat_Icons_4x_13|Gear Small|32x32",
            "Stat_Icons_4x_14|Grenade|32x32",
            "Stat_Icons_4x_15|Star Small|32x32",
            "Stat_Icons_4x_16|Heart Empty|32x32",
            "Stat_Icons_4x_17|Drop Empty|32x32",
            "Stat_Icons_4x_18|Shield Empty|32x32",
            "Stat_Icons_4x_19|Leaf Empty|32x32",
            "Stat_Icons_4x_20|Snowflake Empty|32x32",
            "Stat_Icons_4x_21|Gear Empty|32x32",
            "Stat_Icons_4x_22|Lightning|32x32",
            "Stat_Icons_4x_23|Star Empty|32x32"
        });

        Debug.Log("[CryoUIShowcase] Showcase created! Check the '=== CRYO UI SHOWCASE ===' object in hierarchy.");
        Selection.activeGameObject = canvasGO;
    }

    private static void CreateSection(Transform parent, string title, string sheetPath, string[] spriteInfos)
    {
        // Section container
        GameObject sectionGO = new GameObject(title.Replace("═", "").Trim());
        sectionGO.transform.SetParent(parent, false);

        RectTransform sectionRect = sectionGO.AddComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = sectionGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = sectionGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(sectionGO.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 40);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.9f, 0.5f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Grid for sprites
        GameObject gridGO = new GameObject("SpriteGrid");
        gridGO.transform.SetParent(sectionGO.transform, false);

        RectTransform gridRect = gridGO.AddComponent<RectTransform>();

        GridLayoutGroup glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(150, 120);
        glg.spacing = new Vector2(10, 10);
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.Flexible;

        ContentSizeFitter gridCsf = gridGO.AddComponent<ContentSizeFitter>();
        gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Load sprites
        Object[] allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath);

        foreach (string info in spriteInfos)
        {
            string[] parts = info.Split('|');
            string spriteName = parts[0];
            string description = parts.Length > 1 ? parts[1] : spriteName;
            string size = parts.Length > 2 ? parts[2] : "";

            // Find sprite
            Sprite sprite = null;
            foreach (Object obj in allSprites)
            {
                if (obj is Sprite s && s.name == spriteName)
                {
                    sprite = s;
                    break;
                }
            }

            CreateSpriteCard(gridGO.transform, spriteName, description, size, sprite);
        }
    }

    private static void CreateSpriteCard(Transform parent, string spriteName, string description, string size, Sprite sprite)
    {
        // Card container
        GameObject cardGO = new GameObject(spriteName);
        cardGO.transform.SetParent(parent, false);

        RectTransform cardRect = cardGO.AddComponent<RectTransform>();

        // Background
        Image bgImage = cardGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        VerticalLayoutGroup vlg = cardGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Sprite display
        GameObject spriteGO = new GameObject("Sprite");
        spriteGO.transform.SetParent(cardGO.transform, false);

        RectTransform spriteRect = spriteGO.AddComponent<RectTransform>();
        spriteRect.sizeDelta = new Vector2(0, 64);

        Image spriteImage = spriteGO.AddComponent<Image>();
        if (sprite != null)
        {
            spriteImage.sprite = sprite;
            spriteImage.preserveAspect = true;
            spriteImage.type = Image.Type.Sliced; // Show 9-slice if configured
        }
        else
        {
            spriteImage.color = Color.red;
        }

        // Sprite name label
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(cardGO.transform, false);

        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(0, 16);

        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = spriteName;
        nameText.fontSize = 10;
        nameText.color = Color.cyan;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        // Description label
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(cardGO.transform, false);

        RectTransform descRect = descGO.AddComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(0, 14);

        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = description;
        descText.fontSize = 9;
        descText.color = Color.white;
        descText.alignment = TextAlignmentOptions.Center;
        descText.enableWordWrapping = false;
        descText.overflowMode = TextOverflowModes.Ellipsis;

        // Size label
        if (!string.IsNullOrEmpty(size))
        {
            GameObject sizeGO = new GameObject("Size");
            sizeGO.transform.SetParent(cardGO.transform, false);

            RectTransform sizeRect = sizeGO.AddComponent<RectTransform>();
            sizeRect.sizeDelta = new Vector2(0, 12);

            TextMeshProUGUI sizeText = sizeGO.AddComponent<TextMeshProUGUI>();
            sizeText.text = size;
            sizeText.fontSize = 8;
            sizeText.color = new Color(0.6f, 0.6f, 0.6f);
            sizeText.alignment = TextAlignmentOptions.Center;
        }
    }
}
