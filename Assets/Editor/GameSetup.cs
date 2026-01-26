using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Unified game setup tool. One script to rule them all.
/// Access via menu: Tools > Game Setup > ...
/// </summary>
public class GameSetup : EditorWindow
{
    private static readonly string EnemySpritesPath = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private static readonly string UnitAnimationsPath = "Assets/Animations/Units";
    private static readonly string EnemyAnimationsPath = "Assets/Animations/Enemies";
    private static readonly string PropsSpritePath = "Assets/Pixel Art Top Down - Basic v1.2.3/Texture/TX Props.png";
    private static readonly string ResourceSpritesPath = "Assets/Sprites/Resources";

    #region Main Setup Commands

    [MenuItem("Tools/Game Setup/1. Setup Everything (Run This First!)", priority = 0)]
    public static void SetupEverything()
    {
        EditorUtility.DisplayProgressBar("Game Setup", "Setting up sprites...", 0.1f);
        SetupAllSprites();

        EditorUtility.DisplayProgressBar("Game Setup", "Setting up layers...", 0.2f);
        SetupLayers();

        EditorUtility.DisplayProgressBar("Game Setup", "Setting up GameManager...", 0.3f);
        SetupGameManager();

        EditorUtility.DisplayProgressBar("Game Setup", "Setting up Player...", 0.4f);
        SetupPlayer();

        EditorUtility.DisplayProgressBar("Game Setup", "Creating HUD...", 0.6f);
        CreateGameHUD();

        EditorUtility.DisplayProgressBar("Game Setup", "Assigning animators...", 0.8f);
        FixSceneAnimators();

        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Game Setup Complete",
            "Setup complete! Configured:\n\n" +
            "- All sprites for pixel art\n" +
            "- Layers (Enemy, Player, Pickup, Projectile)\n" +
            "- GameManager + EventManager\n" +
            "- Player (tag, components)\n" +
            "- Game HUD\n" +
            "- Scene animators\n\n" +
            "Next: Create enemies with Tools > Game Setup > Create Enemy",
            "OK");
    }

    [MenuItem("Tools/Game Setup/2. Create Enemy/Gnome", priority = 10)]
    public static void CreateGnome() => CreateEnemy("Gnome");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Bear", priority = 11)]
    public static void CreateBear() => CreateEnemy("Bear");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Gnoll", priority = 12)]
    public static void CreateGnoll() => CreateEnemy("Gnoll");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Shaman", priority = 13)]
    public static void CreateShaman() => CreateEnemy("Shaman");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Lizard", priority = 14)]
    public static void CreateLizard() => CreateEnemy("Lizard");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Skull", priority = 15)]
    public static void CreateSkull() => CreateEnemy("Skull");

    [MenuItem("Tools/Game Setup/2. Create Enemy/Minotaur", priority = 16)]
    public static void CreateMinotaur() => CreateEnemy("Minotaur");

    [MenuItem("Tools/Game Setup/3. Create Building/Storage (Chest)", priority = 20)]
    public static void CreateStorageChest()
    {
        // Slice chest sprites from TX Props first
        SliceChestSprites();

        // Create Storage with chest sprites
        CreateStorageBuilding();
    }

    [MenuItem("Tools/Game Setup/3. Create Building/Workbench", priority = 21)]
    public static void CreateWorkbench() => CreateBuilding<Workbench>("Workbench");

    [MenuItem("Tools/Game Setup/3. Create Building/Farm", priority = 22)]
    public static void CreateFarm() => CreateBuilding<Farm>("Farm");

    [MenuItem("Tools/Game Setup/4. Fix Scene Animators", priority = 30)]
    public static void FixSceneAnimators()
    {
        int fixedCount = 0;

        // Fix enemies
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (TryAssignAnimator(enemy.gameObject, GetEnemyType(enemy.gameObject.name), false))
                fixedCount++;
        }

        // Fix units
        UnitBase[] units = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        foreach (UnitBase unit in units)
        {
            if (TryAssignAnimator(unit.gameObject, GetUnitType(unit.gameObject.name), true))
                fixedCount++;
        }

        if (fixedCount > 0)
        {
            Debug.Log($"[GameSetup] Fixed {fixedCount} animators in scene");
        }
    }

    [MenuItem("Tools/Game Setup/5. Assign Animator to Selected", priority = 40)]
    public static void AssignAnimatorToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject.", "OK");
            return;
        }

        string name = selected.name.Replace("(Clone)", "").Replace("Enemy_", "").Trim();

        // Try enemy first
        if (TryAssignAnimator(selected, name, false))
        {
            EditorUtility.DisplayDialog("Animator Assigned", $"Assigned enemy animator to {selected.name}", "OK");
            return;
        }

        // Try unit
        if (TryAssignAnimator(selected, name, true))
        {
            EditorUtility.DisplayDialog("Animator Assigned", $"Assigned unit animator to {selected.name}", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Not Found", $"Could not find animator for '{name}'", "OK");
    }

    #endregion

    #region Setup Helpers

    private static void SetupAllSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                bool changed = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (importer.spritePixelsPerUnit != 16)
                {
                    importer.spritePixelsPerUnit = 16;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        Debug.Log($"[GameSetup] Configured {count} sprites for pixel art");
    }

    private static void SetupLayers()
    {
        CreateLayerIfNotExists("Enemy");
        CreateLayerIfNotExists("Player");
        CreateLayerIfNotExists("Pickup");
        CreateLayerIfNotExists("Projectile");
        Debug.Log("[GameSetup] Layers configured");
    }

    private static void SetupGameManager()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            Debug.Log("[GameSetup] GameManager already exists");
            return;
        }

        GameObject gmObject = new GameObject("GameManager");
        gmObject.AddComponent<GameManager>();
        gmObject.AddComponent<EventManager>();

        // Add ResourceSpawner
        gmObject.AddComponent<ResourceSpawner>();

        Debug.Log("[GameSetup] Created GameManager");
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[GameSetup] No 'Player' object found!");
            return;
        }

        player.tag = "Player";

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
            player.layer = playerLayer;

        if (player.GetComponent<PlayerHealth>() == null)
            player.AddComponent<PlayerHealth>();

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
            combat = player.AddComponent<PlayerCombat>();

        // Configure combat layer
        SerializedObject combatSO = new SerializedObject(combat);
        SerializedProperty enemyLayerProp = combatSO.FindProperty("enemyLayer");
        if (enemyLayerProp != null)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                enemyLayerProp.intValue = 1 << enemyLayer;
                combatSO.ApplyModifiedProperties();
            }
        }

        Debug.Log("[GameSetup] Player configured");
    }

    private static void CreateEnemy(string enemyType)
    {
        CreateLayerIfNotExists("Enemy");

        GameObject enemy = new GameObject($"Enemy_{enemyType}");

        // Position near camera
        Camera cam = Camera.main;
        enemy.transform.position = cam != null
            ? cam.transform.position + new Vector3(Random.Range(2f, 5f), Random.Range(-2f, 2f), 10)
            : new Vector3(3, 0, 0);

        // SpriteRenderer
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Load sprite
        string spritePath = $"{EnemySpritesPath}/{enemyType}/{enemyType}_Idle.png";
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (Object obj in sprites)
        {
            if (obj is Sprite s)
            {
                sr.sprite = s;
                break;
            }
        }

        // Physics
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // AI
        enemy.AddComponent<EnemyAI>();

        // Layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
            enemy.layer = enemyLayer;

        // Animator
        TryAssignAnimator(enemy, enemyType, false);

        Selection.activeGameObject = enemy;
        Debug.Log($"[GameSetup] Created {enemyType} enemy");
    }

    private static void CreateBuilding<T>(string buildingName) where T : BuildingBase
    {
        GameObject building = new GameObject(buildingName);

        // Position at scene view center or origin
        building.transform.position = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot
            : Vector3.zero;

        // Sprite
        SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite(buildingName);
        sr.sortingOrder = 0;

        // Collider
        BoxCollider2D col = building.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);

        // Building component
        T component = building.AddComponent<T>();

        SerializedObject so = new SerializedObject(component);
        so.FindProperty("buildingName").stringValue = buildingName;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("interactionRange").floatValue = 2.5f;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = building;
        Debug.Log($"[GameSetup] Created {buildingName}");
    }

    private static void CreateGameHUD()
    {
        // Check if UIManager exists
        if (Object.FindFirstObjectByType<UIManager>() != null)
        {
            Debug.Log("[GameSetup] UIManager already exists");
            return;
        }

        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Add UIManager to canvas
        canvas.gameObject.AddComponent<UIManager>();

        // Create TopHUD
        GameObject topHudObject = new GameObject("TopHUD");
        topHudObject.transform.SetParent(canvas.transform, false);
        RectTransform topRect = topHudObject.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0, 100);
        topHudObject.AddComponent<TopHUD>();

        // Create BottomHUD
        GameObject bottomHudObject = new GameObject("BottomHUD");
        bottomHudObject.transform.SetParent(canvas.transform, false);
        RectTransform bottomRect = bottomHudObject.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0, 80);
        bottomHudObject.AddComponent<BottomHUD>();

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        Debug.Log("[GameSetup] Created UI (UIManager, TopHUD, BottomHUD)");
    }

    #endregion

    #region Animator Helpers

    private static bool TryAssignAnimator(GameObject obj, string typeName, bool isUnit)
    {
        string controllerPath = isUnit
            ? $"{UnitAnimationsPath}/{typeName}/{typeName}_Animator.controller"
            : $"{EnemyAnimationsPath}/{typeName}/{typeName}Animator.controller";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return false;

        Animator animator = obj.GetComponent<Animator>();
        if (animator == null)
            animator = obj.AddComponent<Animator>();

        if (animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;
            return true;
        }

        return false;
    }

    private static string GetEnemyType(string name)
    {
        string[] types = { "Gnome", "Bear", "Gnoll", "Shaman", "Lizard", "Skull", "Minotaur" };
        foreach (string type in types)
        {
            if (name.ToLower().Contains(type.ToLower()))
                return type;
        }
        return name.Replace("Enemy_", "").Replace("(Clone)", "").Trim();
    }

    private static string GetUnitType(string name)
    {
        string[] types = { "Pawn", "Warrior", "Archer", "Lancer", "Monk" };
        foreach (string type in types)
        {
            if (name.ToLower().Contains(type.ToLower()))
                return type;
        }
        return name.Replace("(Clone)", "").Trim();
    }

    #endregion

    #region UI Helpers

    private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return panel;
    }

    private static Slider CreateUISlider(Transform parent, string name, Color fillColor)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(280, 20);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 5);
        fillAreaRect.offsetMax = new Vector2(-5, -5);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        return slider;
    }

    private static TextMeshProUGUI CreateUIText(Transform parent, string name, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(150, 30);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    private static Sprite CreatePlaceholderSprite(string name)
    {
        Texture2D texture = new Texture2D(64, 64);
        Color color = name.ToLower() switch
        {
            "storage" => new Color(0.6f, 0.4f, 0.2f),
            "workbench" => new Color(0.4f, 0.3f, 0.2f),
            "farm" => new Color(0.8f, 0.7f, 0.2f),
            _ => Color.gray
        };

        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                texture.SetPixel(x, y, (x < 2 || x > 61 || y < 2 || y > 61) ? Color.black : color);

        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 16);
    }

    private static void CreateLayerIfNotExists(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1) return;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[GameSetup] Created layer '{layerName}'");
                return;
            }
        }
    }

    #endregion

    #region Storage Setup

    private static void SliceChestSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(PropsSpritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[GameSetup] Could not find TX Props at: {PropsSpritePath}");
            return;
        }

        // Configure for pixel art
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;
        importer.mipmapEnabled = false;

        importer.SaveAndReimport();

        // Use ISpriteEditorDataProvider API
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // TX Props is 512x512
        // Based on user screenshot: chest is column 2 (x~64), rows 1-2 from top
        // Unity Rect y=0 is BOTTOM, so y = 512 - (topY + height)
        //
        // Visual inspection of 512x512 image:
        // - Gray square: x=0-32, y_top=0-32
        // - Closed chest: x=64-96, y_top=0-32 (Unity: y=480, h=32)
        // - Open chest: x=64-96, y_top=32-64 (Unity: y=448, h=32)
        var spriteRects = new List<SpriteRect>();

        // Closed chest at top
        AddChestSpriteRect(spriteRects, "Chest_Closed", 64, 480, 32, 32);

        // Open chest below it
        AddChestSpriteRect(spriteRects, "Chest_Open", 64, 448, 32, 32);

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        var assetImporter = dataProvider.targetObject as AssetImporter;
        assetImporter.SaveAndReimport();

        AssetDatabase.ImportAsset(PropsSpritePath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[GameSetup] Sliced chest sprites from TX Props");
    }

    private static void AddChestSpriteRect(List<SpriteRect> list, string name, int x, int y, int width, int height)
    {
        var rect = new SpriteRect();
        rect.name = name;
        rect.rect = new Rect(x, y, width, height);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.alignment = SpriteAlignment.BottomCenter;
        rect.spriteID = GUID.Generate();
        list.Add(rect);
    }

    private static void CreateStorageBuilding()
    {
        // Ensure resources folder exists
        EnsureFolderExists(ResourceSpritesPath);

        // Load chest sprites
        Object[] propsSprites = AssetDatabase.LoadAllAssetsAtPath(PropsSpritePath);

        Sprite chestClosed = null;
        Sprite chestOpen = null;

        foreach (Object obj in propsSprites)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name == "Chest_Closed")
                    chestClosed = sprite;
                else if (sprite.name == "Chest_Open")
                    chestOpen = sprite;
            }
        }

        // Load resource sprites
        Sprite goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ResourceSpritesPath}/Gold.png");
        Sprite meatSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ResourceSpritesPath}/Meat.png");
        Sprite woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ResourceSpritesPath}/Wood.png");

        // Create Storage GameObject
        GameObject storageObj = new GameObject("Storage");

        // Position at scene view center
        storageObj.transform.position = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot
            : Vector3.zero;

        // Add SpriteRenderer
        SpriteRenderer sr = storageObj.AddComponent<SpriteRenderer>();
        sr.sprite = chestClosed;
        sr.sortingOrder = 5;

        // Add BoxCollider2D for interaction
        BoxCollider2D collider = storageObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 2f);

        // Add Storage component
        Storage storage = storageObj.AddComponent<Storage>();

        // Set serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(storage);

        so.FindProperty("chestClosed").objectReferenceValue = chestClosed;
        so.FindProperty("chestOpen").objectReferenceValue = chestOpen;
        so.FindProperty("goldSprite").objectReferenceValue = goldSprite;
        so.FindProperty("meatSprite").objectReferenceValue = meatSprite;
        so.FindProperty("woodSprite").objectReferenceValue = woodSprite;
        so.FindProperty("interactionRange").floatValue = 2f;

        so.ApplyModifiedProperties();

        Selection.activeGameObject = storageObj;

        string status = $"Storage (chest) created!\n\n" +
            $"Chest Closed: {(chestClosed != null ? "OK" : "NOT FOUND")}\n" +
            $"Chest Open: {(chestOpen != null ? "OK" : "NOT FOUND")}\n" +
            $"Gold sprite: {(goldSprite != null ? "OK" : "Copy Gold.png to Assets/Sprites/Resources/")}\n" +
            $"Meat sprite: {(meatSprite != null ? "OK" : "Copy Meat.png to Assets/Sprites/Resources/")}\n" +
            $"Wood sprite: {(woodSprite != null ? "OK" : "Copy Wood.png to Assets/Sprites/Resources/")}";

        Debug.Log($"[GameSetup] {status}");
        EditorUtility.DisplayDialog("Storage Created", status, "OK");
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolderExists(parentPath);
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    #endregion
}
