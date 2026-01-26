using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Создаёт полную тестовую сцену со всеми игровыми объектами.
///
/// КАК ИСПОЛЬЗОВАТЬ:
/// 1. Создай новую сцену (File > New Scene > Basic 2D)
/// 2. Создай пустой GameObject и назови его "SceneBuilder"
/// 3. Добавь этот компонент на него
/// 4. (ОПЦИОНАЛЬНО) Назначь спрайты в Inspector для реальной графики
/// 5. В Inspector нажми правой кнопкой на компонент > "Build Full Scene"
///    ИЛИ просто нажми Play - сцена создастся автоматически
/// 6. После создания можешь перемещать объекты как хочешь
/// 7. Сохрани сцену (Ctrl+S)
/// </summary>
public class FullSceneBuilder : MonoBehaviour
{
    [Header("=== BUILD SETTINGS ===")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool createGround = true;
    [SerializeField] private Vector2 groundSize = new Vector2(50f, 30f);

    [Header("=== SPAWN COUNTS ===")]
    [SerializeField] private int treeCount = 15;
    [SerializeField] private int bushCount = 20;
    [SerializeField] private int rockCount = 12;
    [SerializeField] private int sheepCount = 5;

    [Header("=== SPRITE ASSIGNMENTS (Tiny Swords) ===")]
    [Tooltip("Drag sprites here from Tiny Swords asset")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite groundTileSprite;
    [SerializeField] private Sprite treeSprite;
    [SerializeField] private Sprite bushSprite;
    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Sprite houseSprite;
    [SerializeField] private Sprite castleSprite;
    [SerializeField] private Sprite storageSprite;
    [SerializeField] private Sprite farmSprite;
    [SerializeField] private Sprite sheepSprite;

    [Header("=== ENEMY SPRITES (Blue Units) ===")]
    [SerializeField] private Sprite peasantSprite;
    [SerializeField] private Sprite warriorSprite;
    [SerializeField] private Sprite archerSprite;
    [SerializeField] private Sprite lancerSprite;
    [SerializeField] private Sprite monkSprite;

    // Created object containers
    private Transform environmentContainer;
    private Transform enemiesContainer;
    private Transform buildingsContainer;
    private Transform resourcesContainer;

    private void Start()
    {
        if (buildOnStart)
        {
            // Try to load sprites from Resources if not assigned
            LoadSpritesFromResources();
            BuildFullScene();
        }
    }

    /// <summary>
    /// Load sprites from Resources folder if they exist and aren't already assigned.
    /// Run "Tools > Puppet Master > Setup Resources Folder" to copy sprites first.
    /// </summary>
    private void LoadSpritesFromResources()
    {
        // Player
        if (playerSprite == null)
            playerSprite = Resources.Load<Sprite>("Sprites/Units/Player");

        // Enemies
        if (peasantSprite == null)
            peasantSprite = Resources.Load<Sprite>("Sprites/Enemies/Peasant");
        if (warriorSprite == null)
            warriorSprite = Resources.Load<Sprite>("Sprites/Enemies/Warrior");
        if (archerSprite == null)
            archerSprite = Resources.Load<Sprite>("Sprites/Enemies/Archer");
        if (lancerSprite == null)
            lancerSprite = Resources.Load<Sprite>("Sprites/Enemies/Lancer");
        if (monkSprite == null)
            monkSprite = Resources.Load<Sprite>("Sprites/Enemies/Monk");

        // Sheep (resource, not enemy)
        if (sheepSprite == null)
            sheepSprite = Resources.Load<Sprite>("Sprites/Enemies/Sheep");

        // Buildings
        if (houseSprite == null)
            houseSprite = Resources.Load<Sprite>("Sprites/Buildings/House");
        if (castleSprite == null)
            castleSprite = Resources.Load<Sprite>("Sprites/Buildings/Castle");
        if (storageSprite == null)
            storageSprite = Resources.Load<Sprite>("Sprites/Buildings/Storage");

        int loaded = 0;
        if (playerSprite != null) loaded++;
        if (peasantSprite != null) loaded++;
        if (houseSprite != null) loaded++;

        if (loaded > 0)
        {
            Debug.Log($"[FullSceneBuilder] Loaded {loaded} sprites from Resources folder");
        }
        else
        {
            Debug.Log("[FullSceneBuilder] No sprites in Resources. Run 'Tools > Puppet Master > Setup Resources Folder' in Editor.");
        }
    }

    [ContextMenu("Build Full Scene")]
    public void BuildFullScene()
    {
        Debug.Log("========================================");
        Debug.Log("=== BUILDING FULL TEST SCENE ===");
        Debug.Log("========================================");

        // Create containers
        CreateContainers();

        // 1. Core systems (as root objects for DontDestroyOnLoad)
        CreateCoreSystems();

        // 2. Camera
        CreateCamera();

        // 3. Ground/Tiles
        if (createGround)
        {
            CreateGround();
        }

        // 4. Player
        CreatePlayer();

        // 5. Buildings
        CreateAllBuildings();

        // 6. Decorations
        CreateAllDecorations();

        // 7. Enemies
        CreateAllEnemies();

        // 8. Resources on ground
        CreateStartingResources();

        // 9. UI
        CreateUI();

        // 10. Debug panel
        CreateDebugPanel();

        Debug.Log("========================================");
        Debug.Log("=== SCENE BUILD COMPLETE! ===");
        if (playerSprite == null)
        {
            Debug.Log("NOTE: No sprites assigned - using placeholder colors.");
            Debug.Log("Assign Tiny Swords sprites in Inspector for proper visuals.");
        }
        Debug.Log("Press Play to test. WASD to move.");
        Debug.Log("========================================");
    }

    private void CreateContainers()
    {
        environmentContainer = new GameObject("--- ENVIRONMENT ---").transform;
        enemiesContainer = new GameObject("--- ENEMIES ---").transform;
        buildingsContainer = new GameObject("--- BUILDINGS ---").transform;
        resourcesContainer = new GameObject("--- RESOURCES ---").transform;
    }

    #region Core Systems

    private void CreateCoreSystems()
    {
        // Create systems as ROOT objects (not children) for DontDestroyOnLoad to work

        // GameManager
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            // Don't set parent - must be root for DontDestroyOnLoad
            gm.AddComponent<GameManager>();
        }

        // EventManager
        if (EventManager.Instance == null)
        {
            GameObject em = new GameObject("EventManager");
            em.AddComponent<EventManager>();
        }

        // ResourceSpawner
        if (ResourceSpawner.Instance == null)
        {
            GameObject rs = new GameObject("ResourceSpawner");
            rs.AddComponent<ResourceSpawner>();
        }

        // EffectManager
        if (EffectManager.Instance == null)
        {
            GameObject ef = new GameObject("EffectManager");
            ef.AddComponent<EffectManager>();
        }

        // UIManager will be created with Canvas

        Debug.Log("[Build] Core systems created (as root objects)");
    }

    #endregion

    #region Camera

    private void CreateCamera()
    {
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        if (cam == null)
        {
            cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera c = cam.AddComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 8f;
            c.backgroundColor = new Color(0.15f, 0.25f, 0.15f);
            cam.AddComponent<AudioListener>();
        }

        if (cam.GetComponent<CameraFollow>() == null)
        {
            cam.AddComponent<CameraFollow>();
        }

        cam.transform.position = new Vector3(0, 0, -10);
        Debug.Log("[Build] Camera created");
    }

    #endregion

    #region Ground

    private void CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(environmentContainer);
        ground.transform.position = Vector3.zero;

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = groundTileSprite != null ? groundTileSprite : CreateSprite(new Color(0.3f, 0.5f, 0.25f), 64, 64);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = groundSize;
        sr.sortingOrder = -100;

        // Add some grass patches
        for (int i = 0; i < 30; i++)
        {
            CreateGrassPatch(new Vector3(
                Random.Range(-groundSize.x / 2, groundSize.x / 2),
                Random.Range(-groundSize.y / 2, groundSize.y / 2),
                0
            ));
        }

        Debug.Log("[Build] Ground created");
    }

    private void CreateGrassPatch(Vector3 position)
    {
        GameObject grass = new GameObject("Grass");
        grass.transform.SetParent(environmentContainer);
        grass.transform.position = position;

        SpriteRenderer sr = grass.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSprite(new Color(0.35f, 0.55f, 0.3f), 32, 16);
        sr.sortingOrder = -99;

        grass.transform.localScale = new Vector3(
            Random.Range(0.5f, 1.5f),
            Random.Range(0.3f, 0.8f),
            1f
        );
    }

    #endregion

    #region Player

    private void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = Vector3.zero;

        // Visual
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = playerSprite != null ? playerSprite : CreateSprite(new Color(0.2f, 0.8f, 0.9f), 32, 48);
        sr.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);
        col.offset = new Vector2(0, -0.2f);

        // Add PlayerInput component for new Input System
        PlayerInput playerInput = player.AddComponent<PlayerInput>();

        // Components
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<SkullCollector>();
        player.AddComponent<ResourceMagnet>();

        Debug.Log("[Build] Player created at (0, 0) - Use WASD to move");
    }

    #endregion

    #region Buildings

    private void CreateAllBuildings()
    {
        // === PLAYER BUILDINGS ===
        CreateStorage(new Vector3(-8f, 5f, 0f));
        CreateWorkbench(new Vector3(-5f, 5f, 0f));

        // === ENEMY BUILDINGS ===
        // Village area (right side)
        CreateEnemyHouse(new Vector3(12f, 4f, 0f), "House_1");
        CreateEnemyHouse(new Vector3(15f, 2f, 0f), "House_2");
        CreateEnemyHouse(new Vector3(18f, 5f, 0f), "House_3");

        // Castle (far right - main objective)
        CreateCastle(new Vector3(25f, 0f, 0f));

        // Watchtowers
        CreateWatchtower(new Vector3(10f, 8f, 0f));
        CreateWatchtower(new Vector3(10f, -8f, 0f));

        // Farm area (bottom)
        CreateFarm(new Vector3(5f, -8f, 0f));
        CreateFarm(new Vector3(8f, -10f, 0f));

        // Gold Mine (guarded)
        CreateGoldMine(new Vector3(-15f, -5f, 0f));

        Debug.Log("[Build] All buildings created");
    }

    private void CreateStorage(Vector3 pos)
    {
        GameObject obj = new GameObject("Storage (Chest)");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = storageSprite != null ? storageSprite : CreateSprite(new Color(0.6f, 0.4f, 0.2f), 48, 32);
        sr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1f);

        obj.AddComponent<Storage>();

        CreateLabel(obj.transform, "STORAGE", Color.green);
    }

    private void CreateWorkbench(Vector3 pos)
    {
        GameObject obj = new GameObject("Workbench");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSprite(new Color(0.5f, 0.35f, 0.2f), 48, 40);
        sr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1.2f);

        obj.AddComponent<Workbench>();

        CreateLabel(obj.transform, "WORKBENCH", Color.cyan);
    }

    private void CreateEnemyHouse(Vector3 pos, string name)
    {
        GameObject obj = new GameObject($"EnemyHouse_{name}");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = houseSprite != null ? houseSprite : CreateSprite(new Color(0.7f, 0.5f, 0.3f), 64, 80);
        sr.sortingOrder = 4;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 2.5f);

        obj.AddComponent<EnemyHouse>();

        CreateLabel(obj.transform, "ENEMY HOUSE", Color.red);
    }

    private void CreateCastle(Vector3 pos)
    {
        GameObject obj = new GameObject("Castle");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = castleSprite != null ? castleSprite : CreateSprite(new Color(0.5f, 0.5f, 0.55f), 128, 160);
        sr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4f, 5f);

        obj.AddComponent<Castle>();

        CreateLabel(obj.transform, "CASTLE (BOSS)", Color.magenta);
    }

    private void CreateWatchtower(Vector3 pos)
    {
        GameObject obj = new GameObject("Watchtower");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSprite(new Color(0.55f, 0.45f, 0.35f), 48, 96);
        sr.sortingOrder = 4;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 1f;

        obj.AddComponent<Watchtower>();

        CreateLabel(obj.transform, "WATCHTOWER", Color.yellow);
    }

    private void CreateFarm(Vector3 pos)
    {
        GameObject obj = new GameObject("Farm");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = farmSprite != null ? farmSprite : CreateSprite(new Color(0.8f, 0.7f, 0.4f), 80, 48);
        sr.sortingOrder = 4;

        // Field
        GameObject field = new GameObject("Field");
        field.transform.SetParent(obj.transform);
        field.transform.localPosition = new Vector3(0, -1.5f, 0);
        SpriteRenderer fieldSr = field.AddComponent<SpriteRenderer>();
        fieldSr.sprite = CreateSprite(new Color(0.4f, 0.6f, 0.3f), 96, 32);
        fieldSr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.5f, 1.5f);

        obj.AddComponent<Farm>();

        CreateLabel(obj.transform, "FARM", Color.green);
    }

    private void CreateGoldMine(Vector3 pos)
    {
        GameObject obj = new GameObject("GoldMine");
        obj.transform.SetParent(buildingsContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSprite(new Color(0.4f, 0.35f, 0.3f), 64, 48);
        sr.sortingOrder = 4;

        // Gold pile visual
        GameObject gold = new GameObject("GoldPile");
        gold.transform.SetParent(obj.transform);
        gold.transform.localPosition = new Vector3(0, -0.3f, 0);
        SpriteRenderer goldSr = gold.AddComponent<SpriteRenderer>();
        goldSr.sprite = CreateSprite(new Color(1f, 0.85f, 0f), 32, 16);
        goldSr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 1.5f);

        obj.AddComponent<GoldMine>();

        CreateLabel(obj.transform, "GOLD MINE", new Color(1f, 0.85f, 0f));

        // Create a miner guarding it
        CreateMiner(pos + new Vector3(2f, 0, 0));
    }

    #endregion

    #region Decorations

    private void CreateAllDecorations()
    {
        // Trees
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = GetRandomPosition(-20f, 20f, -12f, 12f);
            CreateTree(pos);
        }

        // Bushes
        for (int i = 0; i < bushCount; i++)
        {
            Vector3 pos = GetRandomPosition(-22f, 22f, -14f, 14f);
            CreateBush(pos);
        }

        // Rocks
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = GetRandomPosition(-20f, 20f, -12f, 12f);
            CreateRock(pos);
        }

        // Clouds (high in the sky)
        for (int i = 0; i < 5; i++)
        {
            CreateCloud(new Vector3(Random.Range(-25f, 25f), Random.Range(10f, 15f), 0));
        }

        Debug.Log("[Build] Decorations created");
    }

    private void CreateTree(Vector3 pos)
    {
        GameObject obj = new GameObject("Tree");
        obj.transform.SetParent(environmentContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        if (treeSprite != null)
        {
            sr.sprite = treeSprite;
        }
        else
        {
            // Create placeholder tree with trunk + foliage
            sr.sprite = CreateSprite(new Color(0.45f, 0.3f, 0.15f), 16, 48);

            GameObject foliage = new GameObject("Foliage");
            foliage.transform.SetParent(obj.transform);
            foliage.transform.localPosition = new Vector3(0, 1.2f, 0);
            SpriteRenderer foliageSr = foliage.AddComponent<SpriteRenderer>();
            foliageSr.sprite = CreateSprite(new Color(0.2f, 0.5f, 0.2f), 48, 48);
            foliageSr.sortingOrder = 3;
        }
        sr.sortingOrder = 2;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 1.5f);
        col.offset = new Vector2(0, -0.5f);

        obj.AddComponent<DestructibleTree>();
    }

    private void CreateBush(Vector3 pos)
    {
        GameObject obj = new GameObject("Bush");
        obj.transform.SetParent(environmentContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = bushSprite != null ? bushSprite : CreateSprite(new Color(0.25f, 0.45f, 0.2f), 32, 24);
        sr.sortingOrder = 1;

        obj.transform.localScale = new Vector3(
            Random.Range(0.8f, 1.3f),
            Random.Range(0.8f, 1.2f),
            1f
        );

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        obj.AddComponent<Bush>();
    }

    private void CreateRock(Vector3 pos)
    {
        GameObject obj = new GameObject("Rock");
        obj.transform.SetParent(environmentContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        if (rockSprite != null)
        {
            sr.sprite = rockSprite;
        }
        else
        {
            Color rockColor = Random.value > 0.5f
                ? new Color(0.5f, 0.5f, 0.5f)
                : new Color(0.6f, 0.55f, 0.5f);
            sr.sprite = CreateSprite(rockColor, 32, 24);
        }
        sr.sortingOrder = 1;

        obj.transform.localScale = new Vector3(
            Random.Range(0.6f, 1.5f),
            Random.Range(0.6f, 1.2f),
            1f
        );

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.5f);

        obj.AddComponent<Rock>();
    }

    private void CreateCloud(Vector3 pos)
    {
        GameObject obj = new GameObject("Cloud");
        obj.transform.SetParent(environmentContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSprite(new Color(1f, 1f, 1f, 0.7f), 64, 32);
        sr.sortingOrder = 100;

        obj.transform.localScale = new Vector3(
            Random.Range(1f, 2f),
            Random.Range(0.8f, 1.2f),
            1f
        );

        obj.AddComponent<Cloud>();
    }

    #endregion

    #region Enemies

    private void CreateAllEnemies()
    {
        // === HUMAN ENEMIES (Blue Units) ===

        // Unarmed Pawns (cowards near farms)
        CreateHumanEnemy(new Vector3(6f, -6f, 0), EnemyType.PawnUnarmed, "Pawn_1");
        CreateHumanEnemy(new Vector3(9f, -8f, 0), EnemyType.PawnUnarmed, "Pawn_2");

        // Pawns with axes (near houses)
        CreateHumanEnemy(new Vector3(13f, 3f, 0), EnemyType.PawnAxe, "PawnAxe_1");
        CreateHumanEnemy(new Vector3(16f, 1f, 0), EnemyType.PawnAxe, "PawnAxe_2");

        // Lancers (guarding paths)
        CreateHumanEnemy(new Vector3(10f, 0f, 0), EnemyType.Lancer, "Lancer_1");
        CreateHumanEnemy(new Vector3(15f, -3f, 0), EnemyType.Lancer, "Lancer_2");

        // Archers (on towers/elevated)
        CreateHumanEnemy(new Vector3(10f, 7f, 0), EnemyType.Archer, "Archer_1");
        CreateHumanEnemy(new Vector3(10f, -7f, 0), EnemyType.Archer, "Archer_2");

        // Warriors (heavy melee)
        CreateHumanEnemy(new Vector3(19f, 1f, 0), EnemyType.Warrior, "Warrior_1");
        CreateHumanEnemy(new Vector3(21f, -1f, 0), EnemyType.Warrior, "Warrior_2");

        // Monks (healers)
        CreateHumanEnemy(new Vector3(21f, 7f, 0), EnemyType.Monk, "Monk_1");

        // === SHEEP (Resource source) ===
        for (int i = 0; i < sheepCount; i++)
        {
            CreateSheep(new Vector3(
                Random.Range(3f, 10f),
                Random.Range(-12f, -6f),
                0
            ), $"Sheep_{i}");
        }

        Debug.Log("[Build] All enemies created");
    }

    private void CreateHumanEnemy(Vector3 pos, EnemyType type, string name)
    {
        GameObject obj = new GameObject($"HumanEnemy_{name}");
        obj.tag = "Enemy";
        obj.transform.SetParent(enemiesContainer);
        obj.transform.position = pos;

        Color enemyColor = GetEnemyColor(type);
        Vector2 size = GetEnemySize(type);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetEnemySprite(type) ?? CreateSprite(enemyColor, (int)size.x, (int)size.y);
        sr.sortingOrder = 10;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = size.x / 64f;

        HumanEnemy he = obj.AddComponent<HumanEnemy>();
        // Note: Config will need to be assigned or created

        CreateLabel(obj.transform, type.ToString(), enemyColor);
    }

    private Sprite GetEnemySprite(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.PawnUnarmed:
            case EnemyType.PawnAxe:
            case EnemyType.PeasantUnarmed:
            case EnemyType.PeasantAxe:
                return peasantSprite;
            case EnemyType.Warrior:
                return warriorSprite;
            case EnemyType.Archer:
                return archerSprite;
            case EnemyType.Lancer:
                return lancerSprite;
            case EnemyType.Monk:
                return monkSprite;
            case EnemyType.Miner:
            case EnemyType.PawnMiner:
                return peasantSprite;
            default:
                return null;
        }
    }

    private void CreateMiner(Vector3 pos)
    {
        GameObject obj = new GameObject("Miner");
        obj.tag = "Enemy";
        obj.transform.SetParent(enemiesContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = peasantSprite != null ? peasantSprite : CreateSprite(new Color(0.6f, 0.5f, 0.3f), 32, 48);
        sr.sortingOrder = 10;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        obj.AddComponent<Miner>();

        CreateLabel(obj.transform, "MINER", new Color(0.6f, 0.5f, 0.3f));
    }

    private void CreateSheep(Vector3 pos, string name)
    {
        GameObject obj = new GameObject($"Sheep_{name}");
        obj.transform.SetParent(enemiesContainer);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sheepSprite != null ? sheepSprite : CreateSprite(Color.white, 32, 28);
        sr.sortingOrder = 10;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        obj.AddComponent<Sheep>();

        CreateLabel(obj.transform, "SHEEP", Color.white);
    }

    #endregion

    #region Resources

    private void CreateStartingResources()
    {
        if (ResourceSpawner.Instance == null) return;

        // Scatter some resources around - Per GDD v5 (4 resources: Skulls, Meat, Wood, Gold)
        ResourceSpawner.Instance.SpawnWood(new Vector3(-3f, -2f, 0), 5);
        ResourceSpawner.Instance.SpawnWood(new Vector3(-2f, -3f, 0), 3);
        ResourceSpawner.Instance.SpawnGold(new Vector3(-4f, -1f, 0), 2);
        ResourceSpawner.Instance.SpawnMeat(new Vector3(2f, -4f, 0), 8);

        Debug.Log("[Build] Starting resources spawned");
    }

    #endregion

    #region UI

    private void CreateUI()
    {
        GameObject canvas = new GameObject("GameCanvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Add UIManager to canvas (as child so it's part of hierarchy)
        if (UIManager.Instance == null)
        {
            canvas.AddComponent<UIManager>();
        }

        // TopHUD
        GameObject topHUD = new GameObject("TopHUD");
        topHUD.transform.SetParent(canvas.transform, false);
        RectTransform topRect = topHUD.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 100);
        topRect.anchoredPosition = Vector2.zero;
        topHUD.AddComponent<TopHUD>();

        // BottomHUD
        GameObject bottomHUD = new GameObject("BottomHUD");
        bottomHUD.transform.SetParent(canvas.transform, false);
        RectTransform bottomRect = bottomHUD.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 100);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomHUD.AddComponent<BottomHUD>();

        Debug.Log("[Build] UI created");
    }

    private void CreateDebugPanel()
    {
        GameObject debugCanvas = new GameObject("DebugCanvas");
        Canvas c = debugCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 200;

        UnityEngine.UI.CanvasScaler scaler = debugCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        debugCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Debug Panel with OnGUI
        GameObject panel = new GameObject("DebugPanel");
        panel.transform.SetParent(debugCanvas.transform, false);
        panel.AddComponent<DebugPanel>();

        Debug.Log("[Build] Debug panel created");
    }

    #endregion

    #region Helpers

    private Sprite CreateSprite(Color color, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Add slight gradient for depth
                float brightness = 1f - (y / (float)height) * 0.2f;
                pixels[y * width + x] = color * brightness;
                pixels[y * width + x].a = color.a;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    private void CreateLabel(Transform parent, string text, Color color)
    {
        GameObject label = new GameObject("Label");
        label.transform.SetParent(parent);
        label.transform.localPosition = new Vector3(0, 2f, 0);

        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 24;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        // Add outline
        MeshRenderer mr = label.GetComponent<MeshRenderer>();
        mr.sortingOrder = 200;
    }

    private Vector3 GetRandomPosition(float minX, float maxX, float minY, float maxY)
    {
        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            0
        );
    }

    private Color GetEnemyColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.PawnUnarmed:
            case EnemyType.PeasantUnarmed: return new Color(0.8f, 0.7f, 0.5f);
            case EnemyType.PawnAxe:
            case EnemyType.PeasantAxe: return new Color(0.7f, 0.5f, 0.3f);
            case EnemyType.Lancer: return new Color(0.3f, 0.4f, 0.7f);
            case EnemyType.Archer: return new Color(0.2f, 0.6f, 0.3f);
            case EnemyType.Warrior: return new Color(0.7f, 0.3f, 0.3f);
            case EnemyType.Monk: return new Color(0.9f, 0.9f, 0.7f);
            case EnemyType.Miner:
            case EnemyType.PawnMiner: return new Color(0.6f, 0.5f, 0.3f);
            case EnemyType.Minotaur: return new Color(0.5f, 0.2f, 0.2f);
            default: return Color.red;
        }
    }

    private Vector2 GetEnemySize(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Minotaur: return new Vector2(64, 80);
            case EnemyType.Warrior: return new Vector2(40, 56);
            case EnemyType.Lancer: return new Vector2(36, 60);
            default: return new Vector2(32, 48);
        }
    }

    #endregion
}
