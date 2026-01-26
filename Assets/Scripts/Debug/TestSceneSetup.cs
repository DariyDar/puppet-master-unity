using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическая настройка тестовой сцены.
/// Добавь этот скрипт на пустой GameObject и нажми Play.
/// Все необходимые объекты будут созданы автоматически.
/// </summary>
public class TestSceneSetup : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool spawnPlayer = true;
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private bool spawnSheep = true;
    [SerializeField] private bool spawnBuildings = true;
    [SerializeField] private bool spawnResources = true;
    [SerializeField] private bool createUI = true;

    [Header("Enemy Count")]
    [SerializeField] private int humanEnemyCount = 3;
    [SerializeField] private int sheepCount = 2;

    [Header("Debug Controls")]
    [SerializeField] private bool showDebugUI = true;

    // References to created objects
    private GameObject player;
    private GameObject mainCamera;

    private void Awake()
    {
        Debug.Log("=== TestSceneSetup: Initializing Test Scene ===");

        // 1. Core Managers (must be first)
        CreateCoreManagers();

        // 2. Camera
        SetupCamera();

        // 3. Player
        if (spawnPlayer)
        {
            CreatePlayer();
        }

        // 4. Buildings
        if (spawnBuildings)
        {
            CreateBuildings();
        }

        // 5. Enemies
        if (spawnEnemies)
        {
            CreateEnemies();
        }

        // 6. Sheep
        if (spawnSheep)
        {
            CreateSheep();
        }

        // 7. Resources
        if (spawnResources)
        {
            CreateTestResources();
        }

        // 8. UI
        if (createUI)
        {
            CreateUI();
        }

        // 9. Debug UI
        if (showDebugUI)
        {
            CreateDebugUI();
        }

        Debug.Log("=== TestSceneSetup: Scene Ready! ===");
    }

    #region Core Managers

    private void CreateCoreManagers()
    {
        // GameManager
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            Debug.Log("[Setup] Created GameManager");
        }

        // EventManager
        if (EventManager.Instance == null)
        {
            GameObject emObj = new GameObject("EventManager");
            emObj.AddComponent<EventManager>();
            Debug.Log("[Setup] Created EventManager");
        }

        // ResourceSpawner
        if (ResourceSpawner.Instance == null)
        {
            GameObject rsObj = new GameObject("ResourceSpawner");
            rsObj.AddComponent<ResourceSpawner>();
            Debug.Log("[Setup] Created ResourceSpawner");
        }

        // EffectManager
        if (EffectManager.Instance == null)
        {
            GameObject efObj = new GameObject("EffectManager");
            efObj.AddComponent<EffectManager>();
            Debug.Log("[Setup] Created EffectManager");
        }

        // UIManager
        if (UIManager.Instance == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiObj.AddComponent<UIManager>();
            Debug.Log("[Setup] Created UIManager");
        }
    }

    #endregion

    #region Camera

    private void SetupCamera()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera == null)
        {
            mainCamera = new GameObject("Main Camera");
            mainCamera.tag = "MainCamera";
            Camera cam = mainCamera.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.2f, 0.3f, 0.2f);
            mainCamera.AddComponent<AudioListener>();
        }

        // Add CameraFollow if player exists
        if (mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.AddComponent<CameraFollow>();
        }

        Debug.Log("[Setup] Camera configured");
    }

    #endregion

    #region Player

    private void CreatePlayer()
    {
        player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = Vector3.zero;

        // Sprite
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(Color.cyan, 32, 48);
        sr.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        // Components
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<SkullCollector>();
        player.AddComponent<ResourceMagnet>();

        // Link camera
        CameraFollow camFollow = mainCamera.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            // CameraFollow will find player by tag
        }

        Debug.Log("[Setup] Created Player at (0, 0)");
    }

    #endregion

    #region Buildings

    private void CreateBuildings()
    {
        // Storage (chest)
        CreateStorage(new Vector3(-3f, 3f, 0f));

        // Workbench
        CreateWorkbench(new Vector3(3f, 3f, 0f));

        // Gold Mine
        CreateGoldMine(new Vector3(6f, -2f, 0f));
    }

    private void CreateStorage(Vector3 position)
    {
        GameObject storage = new GameObject("Storage");
        storage.transform.position = position;

        SpriteRenderer sr = storage.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(new Color(0.6f, 0.4f, 0.2f), 48, 32);
        sr.sortingOrder = 5;

        BoxCollider2D col = storage.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1f);

        storage.AddComponent<Storage>();

        Debug.Log($"[Setup] Created Storage at {position}");
    }

    private void CreateWorkbench(Vector3 position)
    {
        GameObject workbench = new GameObject("Workbench");
        workbench.transform.position = position;

        SpriteRenderer sr = workbench.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(new Color(0.5f, 0.3f, 0.1f), 48, 32);
        sr.sortingOrder = 5;

        BoxCollider2D col = workbench.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1f);

        workbench.AddComponent<Workbench>();

        Debug.Log($"[Setup] Created Workbench at {position}");
    }

    private void CreateGoldMine(Vector3 position)
    {
        GameObject mine = new GameObject("GoldMine");
        mine.transform.position = position;

        SpriteRenderer sr = mine.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(new Color(1f, 0.85f, 0f), 64, 48);
        sr.sortingOrder = 5;

        BoxCollider2D col = mine.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 1.5f);

        mine.AddComponent<GoldMine>();

        Debug.Log($"[Setup] Created GoldMine at {position}");
    }

    #endregion

    #region Enemies

    private void CreateEnemies()
    {
        for (int i = 0; i < humanEnemyCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-5f, -2f),
                0f
            );
            CreateHumanEnemy(pos, i);
        }
    }

    private void CreateHumanEnemy(Vector3 position, int index)
    {
        GameObject enemy = new GameObject($"HumanEnemy_{index}");
        enemy.tag = "Enemy";
        enemy.transform.position = position;

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(Color.red, 32, 48);
        sr.sortingOrder = 10;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        HumanEnemy he = enemy.AddComponent<HumanEnemy>();
        // Config will be created automatically or assign one

        Debug.Log($"[Setup] Created HumanEnemy at {position}");
    }

    #endregion

    #region Sheep

    private void CreateSheep()
    {
        for (int i = 0; i < sheepCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(3f, 7f),
                Random.Range(-1f, 2f),
                0f
            );
            CreateSingleSheep(pos, i);
        }
    }

    private void CreateSingleSheep(Vector3 position, int index)
    {
        GameObject sheep = new GameObject($"Sheep_{index}");
        sheep.transform.position = position;

        SpriteRenderer sr = sheep.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColoredSprite(Color.white, 32, 32);
        sr.sortingOrder = 10;

        CircleCollider2D col = sheep.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        sheep.AddComponent<Sheep>();

        Debug.Log($"[Setup] Created Sheep at {position}");
    }

    #endregion

    #region Resources

    private void CreateTestResources()
    {
        // Spawn some test resources
        if (ResourceSpawner.Instance != null)
        {
            ResourceSpawner.Instance.SpawnMeat(new Vector3(-2f, -2f, 0f), 5);
            ResourceSpawner.Instance.SpawnWood(new Vector3(-1f, -2f, 0f), 3);
            ResourceSpawner.Instance.SpawnGold(new Vector3(0f, -2f, 0f), 2);
            ResourceSpawner.Instance.SpawnMeat(new Vector3(1f, -2f, 0f), 10);

            Debug.Log("[Setup] Spawned test resources");
        }
    }

    #endregion

    #region UI

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create TopHUD
        GameObject topHUD = new GameObject("TopHUD");
        topHUD.transform.SetParent(canvasObj.transform, false);
        RectTransform topRect = topHUD.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 100);
        topRect.anchoredPosition = Vector2.zero;
        topHUD.AddComponent<TopHUD>();

        // Create BottomHUD
        GameObject bottomHUD = new GameObject("BottomHUD");
        bottomHUD.transform.SetParent(canvasObj.transform, false);
        RectTransform bottomRect = bottomHUD.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 100);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomHUD.AddComponent<BottomHUD>();

        Debug.Log("[Setup] Created UI Canvas");
    }

    #endregion

    #region Debug UI

    private void CreateDebugUI()
    {
        GameObject debugCanvas = new GameObject("DebugCanvas");
        Canvas canvas = debugCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = debugCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        debugCanvas.AddComponent<GraphicRaycaster>();

        // Debug Panel
        GameObject panel = new GameObject("DebugPanel");
        panel.transform.SetParent(debugCanvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.5f);
        panelRect.anchorMax = new Vector2(0, 0.5f);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.sizeDelta = new Vector2(300, 400);
        panelRect.anchoredPosition = new Vector2(10, 0);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);

        // Add DebugPanel component
        panel.AddComponent<DebugPanel>();

        Debug.Log("[Setup] Created Debug UI");
    }

    #endregion

    #region Helpers

    private Sprite CreateColoredSprite(Color color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            16f
        );
    }

    #endregion
}

/// <summary>
/// Debug panel with test buttons and info display.
/// </summary>
public class DebugPanel : MonoBehaviour
{

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 280, 500));

        GUILayout.Label("=== DEBUG PANEL ===", GUI.skin.box);

        // Resources
        if (GameManager.Instance != null)
        {
            GUILayout.Label($"Skulls: {GameManager.Instance.Skulls}");
            GUILayout.Label($"Meat: {GameManager.Instance.Meat}");
            GUILayout.Label($"Wood: {GameManager.Instance.Wood}");
            GUILayout.Label($"Gold: {GameManager.Instance.Gold}");
            GUILayout.Space(10);
            GUILayout.Label($"Cargo: {GameManager.Instance.CurrentCargo}/{GameManager.Instance.CargoCapacity}");
            GUILayout.Label($"Army: {GameManager.Instance.ArmyCount}/{GameManager.Instance.ArmyLimit}");
            GUILayout.Label($"HP: {GameManager.Instance.CurrentHealth}/{GameManager.Instance.MaxHealth}");
            GUILayout.Label($"Level: {GameManager.Instance.Level} (XP: {GameManager.Instance.CurrentXp}/{GameManager.Instance.XpToNextLevel})");
        }

        GUILayout.Space(20);
        GUILayout.Label("=== CHEATS ===", GUI.skin.box);

        if (GUILayout.Button("Add 10 Skulls"))
        {
            GameManager.Instance?.AddSkull(10);
        }

        if (GUILayout.Button("Add 100 All Resources"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddSkull(100);
                // For cargo resources, we need to add and deposit
                var gm = GameManager.Instance;
                // Direct field access not possible, so spawn and collect
            }
        }

        if (GUILayout.Button("Spawn Enemy"))
        {
            SpawnTestEnemy();
        }

        if (GUILayout.Button("Spawn Sheep"))
        {
            SpawnTestSheep();
        }

        if (GUILayout.Button("Heal Player"))
        {
            GameManager.Instance?.Heal(100);
        }

        if (GUILayout.Button("Level Up"))
        {
            GameManager.Instance?.AddXp(GameManager.Instance?.XpToNextLevel ?? 100);
        }

        if (GUILayout.Button("Reset Progress"))
        {
            GameManager.Instance?.ResetProgress();
        }

        GUILayout.Space(20);
        GUILayout.Label("=== CONTROLS ===", GUI.skin.box);
        GUILayout.Label("WASD - Move");
        GUILayout.Label("Space/Click - Attack");
        GUILayout.Label("E - Soul Drain (near corpse)");

        GUILayout.EndArea();
    }

    private void SpawnTestEnemy()
    {
        GameObject enemy = new GameObject("TestEnemy");
        enemy.tag = "Enemy";

        Vector3 playerPos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerPos = player.transform.position;
        }

        enemy.transform.position = playerPos + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 48);
        Color[] pixels = new Color[32 * 48];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.red;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0.5f), 16f);
        sr.sortingOrder = 10;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        enemy.AddComponent<HumanEnemy>();
    }

    private void SpawnTestSheep()
    {
        GameObject sheep = new GameObject("TestSheep");

        Vector3 playerPos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerPos = player.transform.position;
        }

        sheep.transform.position = playerPos + new Vector3(Random.Range(2f, 5f), Random.Range(-2f, 2f), 0);

        SpriteRenderer sr = sheep.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
        sr.sortingOrder = 10;

        CircleCollider2D col = sheep.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        sheep.AddComponent<Sheep>();
    }
}
