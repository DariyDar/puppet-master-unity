#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.InputSystem;

/// <summary>
/// Creates prefabs for all game objects that you can then place manually in the scene.
/// Run from menu: Tools > Puppet Master > Create All Prefabs
/// </summary>
public class PrefabCreator : EditorWindow
{
    private const string PREFAB_PATH = "Assets/Prefabs";
    private const string FREE_PACK = "Assets/Sprites/Tiny Swords (Free Pack)/Tiny Swords (Free Pack)";
    private const string ENEMY_PACK = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private const string UPDATE_010 = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)";

    [MenuItem("Tools/Puppet Master/Create All Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabCreator>("Create Prefabs");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Puppet Master - Prefab Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will create prefabs for all game objects (per GDD v5):\n" +
            "- Player (Spider - with auto-attack)\n" +
            "- Army Units (Skull, Gnoll, Gnome, TNT, Shaman)\n" +
            "- Enemies (Blue Units: Pawn, Lancer, Archer, Warrior, Monk)\n" +
            "- Buildings (House, Tower, Castle, GoldMine, etc)\n" +
            "- Environment (Trees, Rocks, Bushes, Sheep)\n" +
            "- Resources (Meat, Wood, Gold)\n\n" +
            "Prefabs will be saved to: Assets/Prefabs/",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create All Prefabs", GUILayout.Height(40)))
        {
            CreateAllPrefabs();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Minimal Test Scene", GUILayout.Height(30)))
        {
            CreateMinimalScene();
        }
    }

    private void CreateAllPrefabs()
    {
        // Create directories
        CreateDirectory($"{PREFAB_PATH}/Player");
        CreateDirectory($"{PREFAB_PATH}/Units");
        CreateDirectory($"{PREFAB_PATH}/Enemies");
        CreateDirectory($"{PREFAB_PATH}/Buildings");
        CreateDirectory($"{PREFAB_PATH}/Environment");
        CreateDirectory($"{PREFAB_PATH}/Resources");
        CreateDirectory($"{PREFAB_PATH}/Core");
        CreateDirectory($"{PREFAB_PATH}/Projectiles");

        int created = 0;

        // === PLAYER (Spider) - Per GDD ===
        created += CreatePlayerPrefab();

        // === ARMY UNITS (Player's Army) - Per GDD ===
        // Skull (Dead/Creepy Clown)
        created += CreateUnitPrefab("Skull", $"{UPDATE_010}/Factions/Knights/Troops/Dead/Dead.png");
        // Gnoll (Bonnie)
        created += CreateUnitPrefab("Gnoll", $"{ENEMY_PACK}/Gnoll/Gnoll_Idle.png");
        // Gnome (Foxy)
        created += CreateUnitPrefab("Gnome", $"{ENEMY_PACK}/Gnome/Gnome_Idle.png");
        // TNT (Chica) - Blue TNT
        created += CreateUnitPrefab("TNT", $"{UPDATE_010}/Factions/Goblins/Troops/TNT/Blue/TNT_Blue_Idle.png");
        // Shaman (Marionette)
        created += CreateUnitPrefab("Shaman", $"{ENEMY_PACK}/Shaman/Shaman_Idle.png");

        // === ENEMIES (Blue Units faction - per GDD) ===
        // All enemies are from Blue Units folder
        created += CreateEnemyPrefab("PawnUnarmed", $"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle.png");
        created += CreateEnemyPrefab("PawnAxe", $"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Axe.png");
        created += CreateEnemyPrefab("Lancer", $"{FREE_PACK}/Units/Blue Units/Lancer/Lancer_Idle.png");
        created += CreateEnemyPrefab("Archer", $"{FREE_PACK}/Units/Blue Units/Archer/Archer_Idle.png");
        created += CreateEnemyPrefab("Warrior", $"{FREE_PACK}/Units/Blue Units/Warrior/Warrior_Idle.png");
        created += CreateEnemyPrefab("Monk", $"{FREE_PACK}/Units/Blue Units/Monk/Idle.png");
        // Miner (PawnMiner - guard for gold mines)
        created += CreateMinerPrefab();

        // === BUILDINGS - Per GDD ===
        // Enemy buildings
        created += CreateBuildingPrefab("EnemyHouse", $"{FREE_PACK}/Buildings/Red Buildings/House1.png", "EnemyHouse");
        created += CreateBuildingPrefab("Tower", $"{FREE_PACK}/Buildings/Red Buildings/Tower.png", "Watchtower");
        created += CreateBuildingPrefab("Castle", $"{FREE_PACK}/Buildings/Red Buildings/Castle.png", "Castle");
        // Player buildings
        created += CreateBuildingPrefab("Storage", $"{FREE_PACK}/Buildings/Blue Buildings/House1.png", "Storage");
        created += CreateBuildingPrefab("Workbench", $"{FREE_PACK}/Buildings/Blue Buildings/House2.png", "Workbench");
        created += CreateBuildingPrefab("Farm", $"{FREE_PACK}/Buildings/Blue Buildings/House3.png", "Farm");
        // Resources
        created += CreateGoldMinePrefab();

        // === ENVIRONMENT ===
        created += CreateDecorationPrefab("Tree", "DestructibleTree");
        created += CreateDecorationPrefab("Bush", "Bush");
        created += CreateDecorationPrefab("Rock", "Rock");
        // Sheep (resource source, not enemy)
        created += CreateSheepPrefab();

        // === RESOURCES ===
        created += CreateResourcePrefab("MeatPickup", $"{FREE_PACK}/Terrain/Resources/Meat/Meat.png", "Meat");
        created += CreateResourcePrefab("WoodPickup", $"{FREE_PACK}/Terrain/Resources/Wood/Wood Resource/Wood Resource.png", "Wood");
        created += CreateResourcePrefab("GoldPickup", $"{FREE_PACK}/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png", "Gold");

        // === PROJECTILES ===
        created += CreateArrowPrefab();

        // === CORE SYSTEMS ===
        created += CreateSystemPrefab("GameManager", "GameManager");
        created += CreateSystemPrefab("EventManager", "EventManager");
        created += CreateSystemPrefab("ResourceSpawner", "ResourceSpawner");
        created += CreateSystemPrefab("UpgradeSystem", "UpgradeSystem");
        created += CreateSystemPrefab("QuestSystem", "QuestSystem");

        AssetDatabase.Refresh();

        Debug.Log($"[PrefabCreator] Created {created} prefabs in {PREFAB_PATH}");
        EditorUtility.DisplayDialog("Prefabs Created",
            $"Created {created} prefabs!\n\n" +
            "Now you can:\n" +
            "1. Create a new scene\n" +
            "2. Drag prefabs from Assets/Prefabs/ into the scene\n" +
            "3. Arrange them as you want\n" +
            "4. Save the scene",
            "OK");
    }

    /// <summary>
    /// Create Player prefab - Spider per GDD.
    /// </summary>
    private int CreatePlayerPrefab()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";

        // Sprite - Spider from Enemy Pack
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite($"{ENEMY_PACK}/Spider/Spider_Idle.png");
        sr.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(1.0f, 0.8f);

        // Input System - find and assign the Input Actions Asset
        PlayerInput playerInput = player.AddComponent<PlayerInput>();
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        }

        // Components - Per GDD: auto-attack, skull collector, resource magnet
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<SkullCollector>();   // Automatic skull collection with progress bar
        player.AddComponent<ResourceMagnet>();   // Attracts Meat/Wood/Gold (not Skulls)

        // Save prefab
        string path = $"{PREFAB_PATH}/Player/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, path);
        DestroyImmediate(player);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private int CreateUnitPrefab(string name, string spritePath)
    {
        GameObject unit = new GameObject(name);
        unit.tag = "Unit";

        SpriteRenderer sr = unit.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        sr.sortingOrder = 10;

        Rigidbody2D rb = unit.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = unit.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        unit.AddComponent<UnitBase>();
        unit.AddComponent<UnitAI>();

        string path = $"{PREFAB_PATH}/Units/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(unit, path);
        DestroyImmediate(unit);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private int CreateEnemyPrefab(string name, string spritePath)
    {
        GameObject enemy = new GameObject(name);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        sr.sortingOrder = 10;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        enemy.AddComponent<HumanEnemy>();

        string path = $"{PREFAB_PATH}/Enemies/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(enemy, path);
        DestroyImmediate(enemy);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    /// <summary>
    /// Create Miner prefab - PawnMiner guards gold mines.
    /// </summary>
    private int CreateMinerPrefab()
    {
        GameObject miner = new GameObject("Miner");
        miner.tag = "Enemy";

        SpriteRenderer sr = miner.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Pickaxe.png");
        sr.sortingOrder = 10;

        Rigidbody2D rb = miner.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = miner.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        miner.AddComponent<Miner>();

        string path = $"{PREFAB_PATH}/Enemies/Miner.prefab";
        PrefabUtility.SaveAsPrefabAsset(miner, path);
        DestroyImmediate(miner);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    /// <summary>
    /// Create Sheep prefab - resource source (Meat), not enemy.
    /// </summary>
    private int CreateSheepPrefab()
    {
        GameObject sheep = new GameObject("Sheep");
        // Sheep is NOT an enemy, just a resource

        SpriteRenderer sr = sheep.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite($"{FREE_PACK}/Terrain/Resources/Meat/Sheep/Sheep_Idle.png");
        sr.sortingOrder = 5;

        Rigidbody2D rb = sheep.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = sheep.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        sheep.AddComponent<Sheep>();

        string path = $"{PREFAB_PATH}/Environment/Sheep.prefab";
        PrefabUtility.SaveAsPrefabAsset(sheep, path);
        DestroyImmediate(sheep);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    /// <summary>
    /// Create GoldMine prefab - per GDD: cannot be destroyed, 70% extraction.
    /// </summary>
    private int CreateGoldMinePrefab()
    {
        GameObject mine = new GameObject("GoldMine");

        SpriteRenderer sr = mine.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite($"{UPDATE_010}/Resources/Gold Mine/GoldMine_Active.png");
        sr.sortingOrder = 5;

        BoxCollider2D col = mine.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 2f);
        col.isTrigger = true;

        mine.AddComponent<GoldMine>();

        string path = $"{PREFAB_PATH}/Buildings/GoldMine.prefab";
        PrefabUtility.SaveAsPrefabAsset(mine, path);
        DestroyImmediate(mine);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    /// <summary>
    /// Create resource pickup prefab.
    /// </summary>
    private int CreateResourcePrefab(string name, string spritePath, string resourceType)
    {
        GameObject pickup = new GameObject(name);

        SpriteRenderer sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        sr.sortingOrder = 3;

        CircleCollider2D col = pickup.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        ResourcePickup rp = pickup.AddComponent<ResourcePickup>();
        // Note: Type will be set at runtime

        string path = $"{PREFAB_PATH}/Resources/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(pickup, path);
        DestroyImmediate(pickup);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    /// <summary>
    /// Create Arrow projectile prefab - parabolic trajectory per GDD.
    /// </summary>
    private int CreateArrowPrefab()
    {
        GameObject arrow = new GameObject("Arrow");

        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite($"{FREE_PACK}/Units/Blue Units/Archer/Arrow.png");
        sr.sortingOrder = 15;

        Rigidbody2D rb = arrow.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        CapsuleCollider2D col = arrow.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.1f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;

        arrow.AddComponent<ArrowProjectile>();

        string path = $"{PREFAB_PATH}/Projectiles/Arrow.prefab";
        PrefabUtility.SaveAsPrefabAsset(arrow, path);
        DestroyImmediate(arrow);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private int CreateBuildingPrefab(string name, string spritePath, string componentName)
    {
        GameObject building = new GameObject(name);

        SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        sr.sortingOrder = 5;

        BoxCollider2D col = building.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 2f);

        // Add specific component
        switch (componentName)
        {
            case "EnemyHouse": building.AddComponent<EnemyHouse>(); break;
            case "Castle": building.AddComponent<Castle>(); break;
            case "Storage":
                col.isTrigger = true;
                building.AddComponent<Storage>();
                break;
            case "Workbench":
                col.isTrigger = true;
                building.AddComponent<Workbench>();
                break;
            case "Farm":
                col.isTrigger = true;
                building.AddComponent<Farm>();
                break;
            case "GoldMine":
                col.isTrigger = true;
                building.AddComponent<GoldMine>();
                break;
            case "Watchtower": building.AddComponent<Watchtower>(); break;
        }

        string path = $"{PREFAB_PATH}/Buildings/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(building, path);
        DestroyImmediate(building);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private int CreateDecorationPrefab(string name, string componentName)
    {
        GameObject deco = new GameObject(name);

        SpriteRenderer sr = deco.AddComponent<SpriteRenderer>();
        sr.color = GetDecorationColor(name);
        sr.sortingOrder = 2;

        BoxCollider2D col = deco.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        switch (componentName)
        {
            case "DestructibleTree": deco.AddComponent<DestructibleTree>(); break;
            case "Bush":
                col.isTrigger = true;
                deco.AddComponent<Bush>();
                break;
            case "Rock": deco.AddComponent<Rock>(); break;
        }

        string path = $"{PREFAB_PATH}/Environment/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(deco, path);
        DestroyImmediate(deco);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private int CreateSystemPrefab(string name, string componentName)
    {
        GameObject system = new GameObject(name);

        switch (componentName)
        {
            case "GameManager": system.AddComponent<GameManager>(); break;
            case "EventManager": system.AddComponent<EventManager>(); break;
            case "ResourceSpawner": system.AddComponent<ResourceSpawner>(); break;
            case "UpgradeSystem": system.AddComponent<UpgradeSystem>(); break;
            case "QuestSystem": system.AddComponent<QuestSystem>(); break;
        }

        string path = $"{PREFAB_PATH}/Core/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(system, path);
        DestroyImmediate(system);

        Debug.Log($"[Prefab] Created: {path}");
        return 1;
    }

    private void CreateMinimalScene()
    {
        // Create a new scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single
        );

        // Setup camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.2f, 0.35f, 0.2f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.gameObject.AddComponent<CameraFollow>();
        }

        // Add core systems
        CreateSystemInScene("GameManager");
        CreateSystemInScene("EventManager");
        CreateSystemInScene("ResourceSpawner");
        CreateSystemInScene("UpgradeSystem");
        CreateSystemInScene("QuestSystem");

        // Add player from prefab or create new
        string playerPrefabPath = $"{PREFAB_PATH}/Player/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.transform.position = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("Player prefab not found. Run 'Create All Prefabs' first.");
        }

        // Create simple ground
        GameObject ground = new GameObject("Ground");
        SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.color = new Color(0.3f, 0.5f, 0.3f);
        groundSr.drawMode = SpriteDrawMode.Tiled;
        groundSr.size = new Vector2(50, 30);
        groundSr.sortingOrder = -100;

        Debug.Log("[PrefabCreator] Created minimal test scene. Now drag prefabs from Assets/Prefabs/ to add objects!");
        EditorUtility.DisplayDialog("Scene Created",
            "Minimal scene created!\n\n" +
            "Now drag prefabs from Assets/Prefabs/ into the Hierarchy to add:\n" +
            "- Enemies\n" +
            "- Buildings\n" +
            "- Environment objects\n\n" +
            "Don't forget to save the scene (Ctrl+S)!",
            "OK");
    }

    private void CreateSystemInScene(string name)
    {
        string prefabPath = $"{PREFAB_PATH}/Core/{name}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab != null)
        {
            PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            // Create directly if prefab doesn't exist
            GameObject obj = new GameObject(name);
            switch (name)
            {
                case "GameManager": obj.AddComponent<GameManager>(); break;
                case "EventManager": obj.AddComponent<EventManager>(); break;
                case "ResourceSpawner": obj.AddComponent<ResourceSpawner>(); break;
                case "UpgradeSystem": obj.AddComponent<UpgradeSystem>(); break;
                case "QuestSystem": obj.AddComponent<QuestSystem>(); break;
            }
        }
    }

    private Sprite LoadSprite(string path)
    {
        // Try to load as sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // If failed, try loading all sub-assets (for sprite sheets)
        if (sprite == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                {
                    sprite = s;
                    break;
                }
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[Sprite] Not found: {path}");
        }

        return sprite;
    }

    private Color GetDecorationColor(string name)
    {
        switch (name.ToLower())
        {
            case "tree": return new Color(0.2f, 0.5f, 0.2f);
            case "bush": return new Color(0.3f, 0.6f, 0.3f);
            case "rock": return new Color(0.5f, 0.5f, 0.5f);
            default: return Color.gray;
        }
    }

    private void CreateDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                CreateDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
