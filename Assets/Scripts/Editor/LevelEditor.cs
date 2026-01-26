using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Tilemaps;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Level Editor - расширенный редактор уровней для Puppet Master.
/// Организован по вкладкам для удобной навигации.
///
/// Использование:
/// 1. Window > Puppet Master > Level Editor
/// 2. Выберите вкладку (Enemies, Buildings, Resources, Decorations)
/// 3. Кликните на кнопку объекта для создания в центре экрана
/// 4. Перетащите объект в нужную позицию
///
/// Для AI-агентов:
/// - Все сущности создаются через статические методы Create*()
/// - Позиция передаётся как параметр Vector3
/// - Методы возвращают созданный GameObject
/// </summary>
public class LevelEditor : EditorWindow
{
    // Sprite paths
    private const string SPRITES_ROOT = "Assets/Sprites";
    private const string BLUE_UNITS = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Units/Blue Units";
    private const string ENEMY_PACK = "Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private const string BLUE_BUILDINGS = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Buildings/Blue Buildings";
    private const string RED_BUILDINGS = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Buildings/Red Buildings";
    private const string RESOURCES_PATH = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Terrain/Resources";
    private const string TERRAIN_PATH = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Terrain";
    private const string DECORATIONS_PATH = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Terrain/Decorations";

    // Scale constants
    private const float UNIT_SCALE = 4f;
    private const float PLAYER_SCALE = 1f;
    private const float BUILDING_SCALE = 1f;  // Changed from 2 to 1
    private const float RESOURCE_SCALE = 0.33f;
    private const float DECORATION_SCALE = 1f;
    private const float TREE_SCALE = 1f;
    private const float PIXEL_ART_PROP_SCALE = 5f;  // Scale for Pixel Art Top Down props

    // Additional paths for decorations
    private const string TREES_PATH = "Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees";

    // Collider settings (spider sprite ~1.9 units at PPU=100)
    private const float PLAYER_COLLIDER_RADIUS = 2.5f;
    private const float ENEMY_COLLIDER_RADIUS = 0.5f;

    // Tab state
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Player & Core", "Enemies", "Buildings", "Resources", "Decorations", "Cemetery Props", "Tilemap", "Tools" };

    // Pixel Art Top Down prefabs path
    private const string PIXEL_ART_PREFABS = "Assets/Cainos/Pixel Art Top Down - Basic/Prefab";

    // Scroll positions for each tab
    private Vector2 scrollPosition;

    // Preview textures cache
    private Dictionary<string, Texture2D> previewCache = new Dictionary<string, Texture2D>();

    // Button sizes with preview
    private const float BUTTON_SIZE = 64f;
    private const float BUTTON_SIZE_SMALL = 48f;

    [MenuItem("Puppet Master/Level Editor")]
    public static void ShowWindow()
    {
        LevelEditor window = GetWindow<LevelEditor>("Level Editor");
        window.minSize = new Vector2(350, 500);
    }

    private void OnGUI()
    {
        // Header
        EditorGUILayout.Space(5);
        GUILayout.Label("Puppet Master Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Tab bar
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space(10);

        // Scrollable content area
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0: DrawPlayerAndCoreTab(); break;
            case 1: DrawEnemiesTab(); break;
            case 2: DrawBuildingsTab(); break;
            case 3: DrawResourcesTab(); break;
            case 4: DrawDecorationsTab(); break;
            case 5: DrawCemeteryPropsTab(); break;
            case 6: DrawTilemapTab(); break;
            case 7: DrawToolsTab(); break;
        }

        EditorGUILayout.EndScrollView();

        // Footer with quick actions
        EditorGUILayout.Space(10);
        DrawFooter();
    }

    #region Tab: Player & Core

    private void DrawPlayerAndCoreTab()
    {
        GUILayout.Label("Player", EditorStyles.boldLabel);

        // Player button with spider preview
        EditorGUILayout.BeginHorizontal();
        DrawPlayerButtonWithPreview();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Core Systems", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Core systems are automatically created when needed.", MessageType.Info);

        if (GUILayout.Button("Create All Core Systems"))
        {
            CreateCoreSystems();
        }

        EditorGUILayout.Space(5);

        // Individual core systems
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("GameManager")) CreateCoreSystem<GameManager>("GameManager");
        if (GUILayout.Button("EventManager")) CreateCoreSystem<EventManager>("EventManager");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("EffectManager")) CreateCoreSystem<EffectManager>("EffectManager");
        if (GUILayout.Button("ResourceSpawner")) CreateCoreSystem<ResourceSpawner>("ResourceSpawner");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("QuestSystem")) CreateCoreSystem<QuestSystem>("QuestSystem");
        if (GUILayout.Button("UpgradeSystem")) CreateCoreSystem<UpgradeSystem>("UpgradeSystem");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Camera", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Camera Follow"))
        {
            SetupCameraFollow();
        }
        if (GUILayout.Button("Reset Camera Size"))
        {
            if (Camera.main != null) Camera.main.orthographicSize = 24f;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlayerButtonWithPreview()
    {
        Texture2D preview = GetPlayerPreviewTexture();

        EditorGUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE + 20));

        GUIContent content = preview != null
            ? new GUIContent(preview, "Create Player (Spider)")
            : new GUIContent("Spider");

        if (GUILayout.Button(content, GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
        {
            CreatePlayer(GetSpawnPosition());
        }

        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Spider", centeredStyle, GUILayout.Width(BUTTON_SIZE));

        EditorGUILayout.EndVertical();
    }

    private Texture2D GetPlayerPreviewTexture()
    {
        string cacheKey = "player_spider";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        Sprite sprite = LoadSprite($"{SPRITES_ROOT}/{ENEMY_PACK}/Spider/Spider_Idle.png");
        if (sprite != null)
        {
            Texture2D preview = ExtractSpritePreview(sprite, 64);
            previewCache[cacheKey] = preview;
            return preview;
        }

        return null;
    }

    #endregion

    #region Tab: Enemies

    private void DrawEnemiesTab()
    {
        GUILayout.Label("Melee Enemies", EditorStyles.boldLabel);

        // Row 1: Basic melee - with sprite previews
        EditorGUILayout.BeginHorizontal();
        DrawEnemyButtonWithPreview("Pawn", EnemyType.PawnUnarmed);
        DrawEnemyButtonWithPreview("Pawn Axe", EnemyType.PawnAxe);
        DrawEnemyButtonWithPreview("Warrior", EnemyType.Warrior);
        DrawEnemyButtonWithPreview("Lancer", EnemyType.Lancer);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Ranged Enemies", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawEnemyButtonWithPreview("Archer", EnemyType.Archer);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Support Enemies", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawEnemyButtonWithPreview("Monk", EnemyType.Monk);
        DrawEnemyButtonWithPreview("Miner", EnemyType.PawnMiner);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Enemy Groups", EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn Patrol Group (3 Pawns)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(-2, 0, 0));
            CreateEnemy(EnemyType.PawnAxe, center);
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(2, 0, 0));
        }

        if (GUILayout.Button("Spawn Mixed Squad", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateEnemy(EnemyType.Warrior, center);
            CreateEnemy(EnemyType.Archer, center + new Vector3(-3, 2, 0));
            CreateEnemy(EnemyType.Archer, center + new Vector3(3, 2, 0));
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(-2, -1, 0));
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(2, -1, 0));
        }

        if (GUILayout.Button("Spawn Defense Squad (with Monk)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateEnemy(EnemyType.Lancer, center + new Vector3(0, 1, 0));
            CreateEnemy(EnemyType.Lancer, center + new Vector3(-2, 0, 0));
            CreateEnemy(EnemyType.Lancer, center + new Vector3(2, 0, 0));
            CreateEnemy(EnemyType.Monk, center + new Vector3(0, -2, 0));
        }
    }

    /// <summary>
    /// Draws a button with sprite preview for enemy type.
    /// </summary>
    private void DrawEnemyButtonWithPreview(string label, EnemyType type)
    {
        Texture2D preview = GetEnemyPreviewTexture(type);

        EditorGUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE + 10));

        // Preview image as button
        GUIContent content = preview != null
            ? new GUIContent(preview, label)
            : new GUIContent(label);

        if (GUILayout.Button(content, GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
        {
            CreateEnemy(type, GetSpawnPosition());
        }

        // Label under the button
        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 9;
        GUILayout.Label(label, centeredStyle, GUILayout.Width(BUTTON_SIZE));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Gets or loads preview texture for enemy type.
    /// </summary>
    private Texture2D GetEnemyPreviewTexture(EnemyType type)
    {
        string cacheKey = $"enemy_{type}";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        // Get sprite path for this enemy type
        string spritePath = GetEnemySpritePath(type);
        Sprite sprite = LoadSprite(spritePath);

        if (sprite != null && sprite.texture != null)
        {
            // Extract the sprite region from the texture
            Texture2D preview = ExtractSpritePreview(sprite, 64);
            previewCache[cacheKey] = preview;
            return preview;
        }

        return null;
    }

    /// <summary>
    /// Extracts a preview texture from a sprite (first frame only).
    /// </summary>
    private Texture2D ExtractSpritePreview(Sprite sprite, int size)
    {
        if (sprite == null || sprite.texture == null) return null;

        try
        {
            Texture2D source = sprite.texture;
            Rect rect = sprite.textureRect;

            // Calculate UV coordinates for the sprite region
            float uMin = rect.x / source.width;
            float uMax = (rect.x + rect.width) / source.width;
            float vMin = rect.y / source.height;
            float vMax = (rect.y + rect.height) / source.height;

            // Create material to extract just the sprite region
            Material blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
            if (blitMaterial == null)
            {
                blitMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            // Create render texture at preview size
            RenderTexture rt = RenderTexture.GetTemporary(size, size);
            rt.filterMode = FilterMode.Point;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            // Clear to transparent
            GL.Clear(true, true, Color.clear);

            // Draw the sprite region scaled to fill the render texture
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, size, size, 0);

            // Draw quad with proper UVs to extract just the sprite
            Graphics.DrawTexture(
                new Rect(0, 0, size, size),
                source,
                new Rect(uMin, vMin, uMax - uMin, vMax - vMin),
                0, 0, 0, 0,
                Color.white
            );

            GL.PopMatrix();

            // Read pixels
            Texture2D preview = new Texture2D(size, size, TextureFormat.RGBA32, false);
            preview.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            preview.Apply();
            preview.filterMode = FilterMode.Point;

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            if (blitMaterial != null)
                Object.DestroyImmediate(blitMaterial);

            return preview;
        }
        catch
        {
            // Fallback: use Unity's built-in asset preview
            return AssetPreview.GetAssetPreview(sprite);
        }
    }

    /// <summary>
    /// Scales a texture to the specified size.
    /// </summary>
    private Texture2D ScaleTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Point;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        result.filterMode = FilterMode.Point;

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    #endregion

    #region Tab: Buildings

    private void DrawBuildingsTab()
    {
        GUILayout.Label("Enemy Buildings (Blue)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawBuildingButtonWithPreview("House 1", "House1", true);
        DrawBuildingButtonWithPreview("House 2", "House2", true);
        DrawBuildingButtonWithPreview("Tower", "Tower", true);
        DrawBuildingButtonWithPreview("Castle", "Castle", true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Player Buildings (Red)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawBuildingButtonWithPreview("Storage", "House1", false, true);
        DrawBuildingButtonWithPreview("House", "House1", false);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Building Presets", EditorStyles.boldLabel);

        if (GUILayout.Button("Enemy Base (House + Tower + Guards)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateBuilding("House1", center, true);
            CreateBuilding("Tower", center + new Vector3(5, 0, 0), true);
            CreateEnemy(EnemyType.Archer, center + new Vector3(5, 3, 0));
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(-2, -2, 0));
            CreateEnemy(EnemyType.PawnAxe, center + new Vector3(2, -2, 0));
        }

        if (GUILayout.Button("Player Base Starter", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateStorage(center);
            CreateBuilding("House1", center + new Vector3(-4, 0, 0), false);
        }
    }

    private void DrawBuildingButtonWithPreview(string label, string buildingType, bool isEnemy, bool isStorage = false)
    {
        Texture2D preview = GetBuildingPreviewTexture(buildingType, isEnemy);

        EditorGUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE + 10));

        GUIContent content = preview != null
            ? new GUIContent(preview, label)
            : new GUIContent(label);

        if (GUILayout.Button(content, GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
        {
            if (isStorage)
                CreateStorage(GetSpawnPosition());
            else
                CreateBuilding(buildingType, GetSpawnPosition(), isEnemy);
        }

        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 9;
        GUILayout.Label(label, centeredStyle, GUILayout.Width(BUTTON_SIZE));

        EditorGUILayout.EndVertical();
    }

    private Texture2D GetBuildingPreviewTexture(string buildingType, bool isEnemy)
    {
        string cacheKey = $"building_{buildingType}_{(isEnemy ? "blue" : "red")}";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        string path = isEnemy ? BLUE_BUILDINGS : RED_BUILDINGS;
        Sprite sprite = LoadSprite($"{SPRITES_ROOT}/{path}/{buildingType}.png");

        if (sprite != null)
        {
            Texture2D preview = ExtractSpritePreview(sprite, 64);
            previewCache[cacheKey] = preview;
            return preview;
        }

        return null;
    }

    #endregion

    #region Tab: Resources

    private void DrawResourcesTab()
    {
        GUILayout.Label("Resource Pickups", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawResourceButtonWithPreview("Meat", ResourcePickup.ResourceType.Meat);
        DrawResourceButtonWithPreview("Wood", ResourcePickup.ResourceType.Wood);
        DrawResourceButtonWithPreview("Gold", ResourcePickup.ResourceType.Gold);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Resource Clusters", EditorStyles.boldLabel);

        if (GUILayout.Button("Meat Cluster (5)", GUILayout.Height(25)))
        {
            SpawnResourceCluster(ResourcePickup.ResourceType.Meat, 5);
        }
        if (GUILayout.Button("Wood Cluster (5)", GUILayout.Height(25)))
        {
            SpawnResourceCluster(ResourcePickup.ResourceType.Wood, 5);
        }
        if (GUILayout.Button("Gold Cluster (3)", GUILayout.Height(25)))
        {
            SpawnResourceCluster(ResourcePickup.ResourceType.Gold, 3);
        }
        if (GUILayout.Button("Mixed Resources (10)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            SpawnResourceCluster(ResourcePickup.ResourceType.Meat, 4, center + new Vector3(-5, 0, 0));
            SpawnResourceCluster(ResourcePickup.ResourceType.Wood, 4, center + new Vector3(5, 0, 0));
            SpawnResourceCluster(ResourcePickup.ResourceType.Gold, 2, center);
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Resource Nodes (Future)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Gold Mine, Tree, Animal Farm - coming soon.", MessageType.Info);
    }

    private void DrawResourceButtonWithPreview(string label, ResourcePickup.ResourceType type)
    {
        Texture2D preview = GetResourcePreviewTexture(type);

        EditorGUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE + 10));

        GUIContent content = preview != null
            ? new GUIContent(preview, label)
            : new GUIContent(label);

        if (GUILayout.Button(content, GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
        {
            CreateResource(type, GetSpawnPosition());
        }

        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 9;
        GUILayout.Label(label, centeredStyle, GUILayout.Width(BUTTON_SIZE));

        EditorGUILayout.EndVertical();
    }

    private Texture2D GetResourcePreviewTexture(ResourcePickup.ResourceType type)
    {
        string cacheKey = $"resource_{type}";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        string spritePath = GetResourceSpritePath(type);
        if (spritePath != null)
        {
            Sprite sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                Texture2D preview = ExtractSpritePreview(sprite, 64);
                previewCache[cacheKey] = preview;
                return preview;
            }
        }

        return null;
    }

    private void SpawnResourceCluster(ResourcePickup.ResourceType type, int count, Vector3? centerPos = null)
    {
        Vector3 center = centerPos ?? GetSpawnPosition();
        float radius = 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            offset += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            CreateResource(type, center + offset);
        }
    }

    #endregion

    #region Tab: Decorations

    private void DrawDecorationsTab()
    {
        EditorGUILayout.HelpBox("Decorations are visual-only. Trees are animated!", MessageType.Info);

        // Trees (animated)
        GUILayout.Label("Trees (Animated)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawDecorationButtonWithPreview("Tree 1", "Tree1");
        DrawDecorationButtonWithPreview("Tree 2", "Tree2");
        DrawDecorationButtonWithPreview("Tree 3", "Tree3");
        DrawDecorationButtonWithPreview("Tree 4", "Tree4");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Stumps
        GUILayout.Label("Stumps", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawDecorationButtonWithPreview("Stump 1", "Stump1");
        DrawDecorationButtonWithPreview("Stump 2", "Stump2");
        DrawDecorationButtonWithPreview("Stump 3", "Stump3");
        DrawDecorationButtonWithPreview("Stump 4", "Stump4");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Bushes
        GUILayout.Label("Bushes", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawDecorationButtonWithPreview("Bush 1", "Bushe1");
        DrawDecorationButtonWithPreview("Bush 2", "Bushe2");
        DrawDecorationButtonWithPreview("Bush 3", "Bushe3");
        DrawDecorationButtonWithPreview("Bush 4", "Bushe4");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Rocks
        GUILayout.Label("Rocks", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawDecorationButtonWithPreview("Rock 1", "Rock1");
        DrawDecorationButtonWithPreview("Rock 2", "Rock2");
        DrawDecorationButtonWithPreview("Rock 3", "Rock3");
        DrawDecorationButtonWithPreview("Rock 4", "Rock4");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Water & Other
        GUILayout.Label("Water & Other", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawDecorationButtonWithPreview("Water Rock 1", "WaterRock1");
        DrawDecorationButtonWithPreview("Water Rock 2", "WaterRock2");
        DrawDecorationButtonWithPreview("Duck", "Duck");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Decoration Presets", EditorStyles.boldLabel);

        if (GUILayout.Button("Forest (5 trees + 3 bushes)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            string[] trees = { "Tree1", "Tree2", "Tree3", "Tree4" };
            string[] bushes = { "Bushe1", "Bushe2", "Bushe3", "Bushe4" };
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-6f, 6f), Random.Range(-6f, 6f), 0);
                CreateDecoration(trees[Random.Range(0, trees.Length)], center + offset);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-4f, 4f), Random.Range(-4f, 4f), 0);
                CreateDecoration(bushes[Random.Range(0, bushes.Length)], center + offset);
            }
        }

        if (GUILayout.Button("Rocky Area (6 rocks)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            string[] rocks = { "Rock1", "Rock2", "Rock3", "Rock4" };
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
                CreateDecoration(rocks[Random.Range(0, rocks.Length)], center + offset);
            }
        }

        if (GUILayout.Button("Clearing (stumps + bushes)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            string[] stumps = { "Stump1", "Stump2", "Stump3", "Stump4" };
            string[] bushes = { "Bushe1", "Bushe2", "Bushe3", "Bushe4" };
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-4f, 4f), Random.Range(-4f, 4f), 0);
                CreateDecoration(stumps[Random.Range(0, stumps.Length)], center + offset);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                CreateDecoration(bushes[Random.Range(0, bushes.Length)], center + offset);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("For ground/terrain, use Unity's built-in Tilemap system:\nWindow > 2D > Tile Palette", MessageType.Info);
    }

    #endregion

    #region Tab: Cemetery Props (Pixel Art Top Down)

    private void DrawCemeteryPropsTab()
    {
        EditorGUILayout.HelpBox(
            "Pixel Art Top Down - Cemetery/Graveyard themed props.\n" +
            "Some props have special effects (altar glows when player approaches).\n" +
            "These are prefabs - all effects are preserved!",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // Game Buildings - Storage & WorkBench
        GUI.backgroundColor = Color.cyan;
        GUILayout.Label("Game Buildings", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Storage (Chest)", "Props/PF Props - Chest 01.prefab", true);
        DrawPrefabButtonWithPreview("WorkBench (Altar)", "Props/PF Props - Altar 01.prefab", true);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Storage: Opens when spider approaches with resources. Resources fly into chest.\n" +
            "WorkBench: Runes glow when player approaches. Use for crafting/upgrades.",
            MessageType.None);

        EditorGUILayout.Space(10);

        // Cemetery Props
        GUILayout.Label("Gravestones & Coffins", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Grave 1", "Props/PF Props - Gravestone 01.prefab");
        DrawPrefabButtonWithPreview("Grave 2", "Props/PF Props - Gravestone 02.prefab");
        DrawPrefabButtonWithPreview("Grave 3", "Props/PF Props - Gravestone 03.prefab");
        DrawPrefabButtonWithPreview("Coffin H", "Props/PF Props - Stone Coffin 01 H.prefab");
        DrawPrefabButtonWithPreview("Coffin V", "Props/PF Props - Stone Coffin 01 V.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Pillars and Ruins
        GUILayout.Label("Pillars & Ruins", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Pillar 1", "Props/PF Props - Pillar 01.prefab");
        DrawPrefabButtonWithPreview("Pillar 2", "Props/PF Props - Pillar 02.prefab");
        DrawPrefabButtonWithPreview("Rune Pillar", "Props/PF Props - Rune Pillar X3.prefab");
        DrawPrefabButtonWithPreview("Rune Short", "Props/PF Props - Rune Pillar X2.prefab");
        DrawPrefabButtonWithPreview("Broken", "Props/PF Props - Rune Pillar Broken.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Gates and Structures
        GUILayout.Label("Gates & Structures", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Gate 1", "Props/PF Struct - Gate 01.prefab");
        DrawPrefabButtonWithPreview("Gate 2", "Props/PF Struct - Gate 02.prefab");
        DrawPrefabButtonWithPreview("Wood Gate", "Props/PF Props - Wooden Gate 01.prefab");
        DrawPrefabButtonWithPreview("Wood Open", "Props/PF Props - Wooden Gate 01 Opened.prefab");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Stones and Decorations
        GUILayout.Label("Stones & Rubble", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Stone 1", "Props/PF Props - Stone 01.prefab");
        DrawPrefabButtonWithPreview("Stone 2", "Props/PF Props - Stone 02.prefab");
        DrawPrefabButtonWithPreview("Stone 3", "Props/PF Props - Stone 03.prefab");
        DrawPrefabButtonWithPreview("Brick 1", "Props/PF Props - Brick 01.prefab");
        DrawPrefabButtonWithPreview("Brick 2", "Props/PF Props - Brick 02.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Stone 4", "Props/PF Props - Stone 04.prefab");
        DrawPrefabButtonWithPreview("Stone 5", "Props/PF Props - Stone 05.prefab");
        DrawPrefabButtonWithPreview("Stone 6", "Props/PF Props - Stone 06.prefab");
        DrawPrefabButtonWithPreview("Cube", "Props/PF Props - Stone Cube 01.prefab");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Functional Props
        GUILayout.Label("Containers & Objects", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Chest Open", "Props/PF Props - Chest 01 Open.prefab");
        DrawPrefabButtonWithPreview("Barrel", "Props/PF Props - Barrel 01.prefab");
        DrawPrefabButtonWithPreview("Crate 1", "Props/PF Props - Crate 01.prefab");
        DrawPrefabButtonWithPreview("Crate 2", "Props/PF Props - Crate 02.prefab");
        DrawPrefabButtonWithPreview("Well", "Props/PF Props - Well 01.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Furniture
        GUILayout.Label("Furniture & Signs", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Bench E", "Props/PF Props - Stone Bench 01 E.prefab");
        DrawPrefabButtonWithPreview("Bench S", "Props/PF Props - Stone Bench 01 S.prefab");
        DrawPrefabButtonWithPreview("Bench W", "Props/PF Props - Stone Bench 01 W.prefab");
        DrawPrefabButtonWithPreview("Statue", "Props/PF Props - Statue 01.prefab");
        DrawPrefabButtonWithPreview("Lantern", "Props/PF Props - Stone Lantern 01.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Pot 1", "Props/PF Props - Pot 01.prefab");
        DrawPrefabButtonWithPreview("Pot 2", "Props/PF Props - Pot 02.prefab");
        DrawPrefabButtonWithPreview("Pot 3", "Props/PF Props - Pot 03.prefab");
        DrawPrefabButtonWithPreview("Sign E", "Props/PF Props - Road Sign 01 E.prefab");
        DrawPrefabButtonWithPreview("Sign W", "Props/PF Props - Road Sign 01 W.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Plants
        GUILayout.Label("Plants & Trees", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Tree 1", "Plant/PF Plant - Tree 01.prefab");
        DrawPrefabButtonWithPreview("Tree 2", "Plant/PF Plant - Tree 02.prefab");
        DrawPrefabButtonWithPreview("Tree 3", "Plant/PF Plant - Tree 03.prefab");
        DrawPrefabButtonWithPreview("Flower", "Plant/PF Plant - Flower 01.prefab");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawPrefabButtonWithPreview("Bush 1", "Plant/PF Plant - Bush 01.prefab");
        DrawPrefabButtonWithPreview("Bush 2", "Plant/PF Plant - Bush 02.prefab");
        DrawPrefabButtonWithPreview("Bush 3", "Plant/PF Plant - Bush 03.prefab");
        DrawPrefabButtonWithPreview("Bush 4", "Plant/PF Plant - Bush 04.prefab");
        DrawPrefabButtonWithPreview("Bush 5", "Plant/PF Plant - Bush 05.prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Presets
        GUILayout.Label("Cemetery Presets", EditorStyles.boldLabel);

        if (GUILayout.Button("Graveyard Cluster (3 graves + stones)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreatePrefabInstance("Props/PF Props - Gravestone 01.prefab", center);
            CreatePrefabInstance("Props/PF Props - Gravestone 02.prefab", center + new Vector3(2, 0.5f, 0));
            CreatePrefabInstance("Props/PF Props - Gravestone 03.prefab", center + new Vector3(-2, -0.3f, 0));
            CreatePrefabInstance("Props/PF Props - Stone 01.prefab", center + new Vector3(1, -1, 0));
            CreatePrefabInstance("Props/PF Props - Stone 02.prefab", center + new Vector3(-1.5f, 1, 0));
        }

        if (GUILayout.Button("Spider Base (Storage + WorkBench + Pillars)", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreateStorageFromPrefab(center);
            CreateWorkBenchFromPrefab(center + new Vector3(4, 0, 0));
            CreatePrefabInstance("Props/PF Props - Rune Pillar X3.prefab", center + new Vector3(-3, 2, 0));
            CreatePrefabInstance("Props/PF Props - Rune Pillar X3.prefab", center + new Vector3(7, 2, 0));
            CreatePrefabInstance("Props/PF Props - Stone Lantern 01.prefab", center + new Vector3(0, 3, 0));
            CreatePrefabInstance("Props/PF Props - Stone Lantern 01.prefab", center + new Vector3(4, 3, 0));
        }

        if (GUILayout.Button("Ruined Temple Area", GUILayout.Height(25)))
        {
            Vector3 center = GetSpawnPosition();
            CreatePrefabInstance("Props/PF Props - Altar 01.prefab", center);
            CreatePrefabInstance("Props/PF Props - Pillar 01.prefab", center + new Vector3(-3, 1, 0));
            CreatePrefabInstance("Props/PF Props - Pillar 02.prefab", center + new Vector3(3, 1, 0));
            CreatePrefabInstance("Props/PF Props - Rune Pillar Broken.prefab", center + new Vector3(-2, -2, 0));
            CreatePrefabInstance("Props/PF Props - Brick 01.prefab", center + new Vector3(2, -2, 0));
            CreatePrefabInstance("Props/PF Props - Brick 02.prefab", center + new Vector3(0, -3, 0));
        }
    }

    /// <summary>
    /// Draws a button that spawns a prefab from Pixel Art Top Down pack.
    /// </summary>
    private void DrawPrefabButtonWithPreview(string label, string prefabRelativePath, bool isGameBuilding = false)
    {
        string fullPath = $"{PIXEL_ART_PREFABS}/{prefabRelativePath}";
        Texture2D preview = GetPrefabPreviewTexture(fullPath);

        float buttonSize = isGameBuilding ? BUTTON_SIZE : BUTTON_SIZE_SMALL;

        EditorGUILayout.BeginVertical(GUILayout.Width(buttonSize + 10));

        GUIContent content = preview != null
            ? new GUIContent(preview, label)
            : new GUIContent(label);

        if (GUILayout.Button(content, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
        {
            if (isGameBuilding && prefabRelativePath.Contains("Chest"))
            {
                CreateStorageFromPrefab(GetSpawnPosition());
            }
            else if (isGameBuilding && prefabRelativePath.Contains("Altar"))
            {
                CreateWorkBenchFromPrefab(GetSpawnPosition());
            }
            else
            {
                CreatePrefabInstance(prefabRelativePath, GetSpawnPosition());
            }
        }

        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 9;
        GUILayout.Label(label, centeredStyle, GUILayout.Width(buttonSize));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Gets preview texture for a prefab.
    /// </summary>
    private Texture2D GetPrefabPreviewTexture(string prefabPath)
    {
        string cacheKey = $"prefab_{prefabPath}";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            // Get Unity's built-in preview
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview != null)
            {
                previewCache[cacheKey] = preview;
                return preview;
            }
            // Try to get from sprite renderer
            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                preview = ExtractSpritePreview(sr.sprite, 48);
                if (preview != null)
                {
                    previewCache[cacheKey] = preview;
                    return preview;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a prefab instance at position.
    /// </summary>
    public static GameObject CreatePrefabInstance(string prefabRelativePath, Vector3 position)
    {
        string fullPath = $"{PIXEL_ART_PREFABS}/{prefabRelativePath}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

        if (prefab == null)
        {
            Debug.LogError($"[LevelEditor] Prefab not found: {fullPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;

        // Scale up Pixel Art Top Down props (they are small by default)
        instance.transform.localScale = Vector3.one * PIXEL_ART_PROP_SCALE;

        // Parent to Decorations if exists
        GameObject decorParent = GameObject.Find("Decorations");
        if (decorParent != null)
        {
            instance.transform.SetParent(decorParent.transform);
        }

        Selection.activeGameObject = instance;
        Debug.Log($"[LevelEditor] Created {prefab.name} at {position} (scale: {PIXEL_ART_PROP_SCALE}x)");
        return instance;
    }

    /// <summary>
    /// Creates Storage (Chest) with the Storage script from prefab.
    /// </summary>
    public static GameObject CreateStorageFromPrefab(Vector3 position)
    {
        string prefabPath = $"{PIXEL_ART_PREFABS}/Props/PF Props - Chest 01.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[LevelEditor] Chest prefab not found: {prefabPath}");
            return CreateStorage(position); // Fallback to old method
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Storage";
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * PIXEL_ART_PROP_SCALE;
        SafeSetTag(instance, "Storage");

        // Load open chest sprite for the Storage script
        string openPrefabPath = $"{PIXEL_ART_PREFABS}/Props/PF Props - Chest 01 Open.prefab";
        GameObject openPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(openPrefabPath);
        Sprite openSprite = null;
        Sprite closedSprite = null;

        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            closedSprite = sr.sprite;
        }

        if (openPrefab != null)
        {
            SpriteRenderer openSr = openPrefab.GetComponent<SpriteRenderer>();
            if (openSr != null)
            {
                openSprite = openSr.sprite;
            }
        }

        // Make collider a trigger for interaction
        BoxCollider2D col = instance.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            col = instance.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 0.6f);
            col.offset = new Vector2(0, 0.3f);
        }

        // Add Storage script
        Storage storage = instance.GetComponent<Storage>();
        if (storage == null)
        {
            storage = instance.AddComponent<Storage>();
        }

        // Set sprites and interaction range via SerializedObject
        SerializedObject so = new SerializedObject(storage);
        SerializedProperty closedProp = so.FindProperty("chestClosed");
        SerializedProperty openProp = so.FindProperty("chestOpen");
        SerializedProperty rangeProp = so.FindProperty("interactionRange");
        if (closedProp != null) closedProp.objectReferenceValue = closedSprite;
        if (openProp != null) openProp.objectReferenceValue = openSprite;
        if (rangeProp != null) rangeProp.floatValue = 10f; // ~3 spider lengths
        so.ApplyModifiedProperties();

        // Parent to Player Buildings
        GameObject buildingsParent = GameObject.Find("Player Buildings");
        if (buildingsParent != null)
        {
            instance.transform.SetParent(buildingsParent.transform);
        }

        Selection.activeGameObject = instance;
        Debug.Log($"[LevelEditor] Created Storage (Chest) at {position} (scale: {PIXEL_ART_PROP_SCALE}x, range: 10)");
        return instance;
    }

    /// <summary>
    /// Creates WorkBench (Altar) from prefab, preserving the glow effect.
    /// </summary>
    public static GameObject CreateWorkBenchFromPrefab(Vector3 position)
    {
        string prefabPath = $"{PIXEL_ART_PREFABS}/Props/PF Props - Altar 01.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[LevelEditor] Altar prefab not found: {prefabPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "WorkBench";
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * PIXEL_ART_PROP_SCALE;
        SafeSetTag(instance, "Building");

        // The prefab already has PropsAltar script with rune glow effect!
        // Need to enlarge the trigger collider to match the scale
        BoxCollider2D col = instance.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            // Increase collider size for better trigger detection at larger scale
            // Original size is small, we need a bigger trigger area
            col.size = new Vector2(2f, 2f);
            col.offset = new Vector2(0, 0.5f);
        }

        // Add Workbench script if not present (for game functionality)
        Workbench wb = instance.GetComponent<Workbench>();
        if (wb == null)
        {
            wb = instance.AddComponent<Workbench>();
        }

        // Parent to Player Buildings
        GameObject buildingsParent = GameObject.Find("Player Buildings");
        if (buildingsParent != null)
        {
            instance.transform.SetParent(buildingsParent.transform);
        }

        Selection.activeGameObject = instance;
        Debug.Log($"[LevelEditor] Created WorkBench (Altar with glow effect) at {position} (scale: {PIXEL_ART_PROP_SCALE}x)");
        return instance;
    }

    private void DrawDecorationButtonWithPreview(string label, string decorationType)
    {
        Texture2D preview = GetDecorationPreviewTexture(decorationType);

        EditorGUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE + 10));

        GUIContent content = preview != null
            ? new GUIContent(preview, label)
            : new GUIContent(label);

        if (GUILayout.Button(content, GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
        {
            CreateDecoration(decorationType, GetSpawnPosition());
        }

        GUIStyle centeredStyle = new GUIStyle(EditorStyles.miniLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 9;
        GUILayout.Label(label, centeredStyle, GUILayout.Width(BUTTON_SIZE));

        EditorGUILayout.EndVertical();
    }

    private Texture2D GetDecorationPreviewTexture(string decorationType)
    {
        string cacheKey = $"decoration_{decorationType}";

        if (previewCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        string spritePath = GetDecorationSpritePath(decorationType);
        Sprite sprite = LoadSprite(spritePath);

        if (sprite != null)
        {
            Texture2D preview = ExtractSpritePreview(sprite, 64);
            previewCache[cacheKey] = preview;
            return preview;
        }

        return null;
    }

    #endregion

    #region Tab: Tilemap

    private void DrawTilemapTab()
    {
        GUILayout.Label("Tilemap System Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tilemap - система для рисования карты кистью.\n" +
            "Ярусы создаются через Sorting Order и визуальные Elevation тайлы.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // One-click setup
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("CREATE COMPLETE TILEMAP SYSTEM", GUILayout.Height(40)))
        {
            CreateCompleteTilemapSystem();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        GUILayout.Label("Individual Components", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Grid + Tilemaps (Hierarchy)", GUILayout.Height(25)))
        {
            CreateTilemapHierarchy();
        }

        if (GUILayout.Button("Create Tile Palette + Import Tiles", GUILayout.Height(25)))
        {
            CreateTilePaletteWithTiles();
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("REBUILD: Delete All Tiles & Recreate", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Rebuild Tiles",
                "This will DELETE all tiles in Assets/Tiles and Assets/Palettes and recreate them.\n\nContinue?",
                "Yes, Rebuild", "Cancel"))
            {
                RebuildAllTilesAndPalette();
            }
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Setup Organized Scene Hierarchy", GUILayout.Height(25)))
        {
            CreateOrganizedHierarchy();
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Sprite Import for Tiles", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Fix Tileset Sprite Imports", GUILayout.Height(25)))
        {
            FixTilesetSpriteImports();
        }

        if (GUILayout.Button("Fix Tile Colliders (Water/Elevation)", GUILayout.Height(25)))
        {
            FixTileColliders();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Elevation explanation
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Как работают ярусы (Elevation):", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Tilemap Layer 0 (Water) - вода, Sorting Order: -10");
        EditorGUILayout.LabelField("2. Tilemap Layer 1 (Ground) - земля, Sorting Order: 0");
        EditorGUILayout.LabelField("3. Tilemap Layer 2 (Elevation) - стены ярусов, Sorting Order: 5");
        EditorGUILayout.LabelField("4. Tilemap Layer 3 (Upper Ground) - верхний ярус, Sorting Order: 10");
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Игрок и враги используют Sorting Order: 100+");
        EditorGUILayout.LabelField("Коллизии: добавь Tilemap Collider 2D на Elevation слой");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Quick actions
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Tile Palette"))
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
        }
        if (GUILayout.Button("Select Grid"))
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid != null) Selection.activeGameObject = grid.gameObject;
            else Debug.LogWarning("[LevelEditor] No Grid found. Create Tilemap System first.");
        }
        EditorGUILayout.EndHorizontal();

        // Show palette status
        EditorGUILayout.Space(5);
        string palettePath = "Assets/Palettes/TinySwords_Terrain.prefab";
        var paletteAsset = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
        if (paletteAsset != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Palette:", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.ObjectField(paletteAsset, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Palette ready! Open Tile Palette window, select 'TinySwords_Terrain' from dropdown.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No palette found. Click 'Create Tile Palette + Import Tiles' first.", MessageType.Warning);
        }
    }

    /// <summary>
    /// Creates the complete Tilemap system with one click.
    /// </summary>
    private void CreateCompleteTilemapSystem()
    {
        // Step 1: Fix sprite imports
        FixTilesetSpriteImports();

        // Step 2: Create hierarchy
        CreateOrganizedHierarchy();

        // Step 3: Create Grid and Tilemaps
        CreateTilemapHierarchy();

        // Step 4: Create Tile Palette
        CreateTilePaletteWithTiles();

        // Open Tile Palette window
        EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");

        Debug.Log("[LevelEditor] Complete Tilemap system created!");
        EditorUtility.DisplayDialog("Tilemap System Created",
            "Created:\n" +
            "- Organized Hierarchy\n" +
            "- Grid with 4 Tilemap layers\n" +
            "- Tile Palette with all tiles\n\n" +
            "Open Window > 2D > Tile Palette to start painting!",
            "OK");
    }

    /// <summary>
    /// Creates the Grid and Tilemap layers.
    /// </summary>
    private void CreateTilemapHierarchy()
    {
        // Check if Grid already exists
        var existingGrid = Object.FindFirstObjectByType<Grid>();
        if (existingGrid != null)
        {
            Debug.LogWarning("[LevelEditor] Grid already exists. Delete it first if you want to recreate.");
            Selection.activeGameObject = existingGrid.gameObject;
            return;
        }

        // Find or create Environment parent
        GameObject envParent = GameObject.Find("--- ENVIRONMENT ---");
        if (envParent == null)
        {
            envParent = new GameObject("--- ENVIRONMENT ---");
        }

        // Create Grid
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(envParent.transform);
        Grid grid = gridGO.AddComponent<Grid>();
        // Tile = 64px, PPU = 16 -> tile size = 64/16 = 4 Unity units
        // This makes tiles proportional to characters (Spider ~2 units tall)
        grid.cellSize = new Vector3(4f, 4f, 0);
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        // Create Tilemap layers (with colliders where needed)
        // Water has collider - player can't walk on water
        CreateTilemapLayer(gridGO.transform, "Water", -10, addCollider: true);
        // Ground - no collider, walkable
        CreateTilemapLayer(gridGO.transform, "Ground", 0, addCollider: false);
        // Elevation has collider - blocks movement (cliffs/walls)
        CreateTilemapLayer(gridGO.transform, "Elevation", 5, addCollider: true);
        // UpperGround - no collider, walkable upper level
        CreateTilemapLayer(gridGO.transform, "UpperGround", 10, addCollider: false);

        Selection.activeGameObject = gridGO;
        Debug.Log("[LevelEditor] Created Grid with 4 Tilemap layers (Water, Ground, Elevation, UpperGround)");
    }

    private GameObject CreateTilemapLayer(Transform parent, string name, int sortingOrder, bool addCollider = false)
    {
        GameObject tilemapGO = new GameObject(name);
        tilemapGO.transform.SetParent(parent);
        tilemapGO.transform.localPosition = Vector3.zero;

        var tilemap = tilemapGO.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        tilemap.color = Color.white; // Full opacity, no tint

        var renderer = tilemapGO.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        // Add collider if needed (for Water and Elevation)
        if (addCollider)
        {
            var collider = tilemapGO.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.None;
        }

        return tilemapGO;
    }

    /// <summary>
    /// Creates the organized scene hierarchy.
    /// </summary>
    private void CreateOrganizedHierarchy()
    {
        // Create section separators
        string[] sections = {
            "--- ENVIRONMENT ---",
            "--- GAMEPLAY ---",
            "--- BUILDINGS ---",
            "--- SYSTEMS ---"
        };

        foreach (string section in sections)
        {
            if (GameObject.Find(section) == null)
            {
                new GameObject(section);
            }
        }

        // Create subsections
        CreateSubsection("--- ENVIRONMENT ---", "Decorations");
        CreateSubsection("--- GAMEPLAY ---", "Player");
        CreateSubsection("--- GAMEPLAY ---", "Enemies");
        CreateSubsection("--- GAMEPLAY ---", "Resources");
        CreateSubsection("--- BUILDINGS ---", "Enemy Buildings");
        CreateSubsection("--- BUILDINGS ---", "Player Buildings");

        Debug.Log("[LevelEditor] Created organized hierarchy structure");
    }

    private void CreateSubsection(string parentName, string childName)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null) return;

        // Check if child already exists
        Transform existingChild = parent.transform.Find(childName);
        if (existingChild != null) return;

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent.transform);
    }

    /// <summary>
    /// Deletes all tiles and palette, then recreates them from scratch.
    /// </summary>
    private void RebuildAllTilesAndPalette()
    {
        // Delete Assets/Tiles folder
        if (AssetDatabase.IsValidFolder("Assets/Tiles"))
        {
            AssetDatabase.DeleteAsset("Assets/Tiles");
            Debug.Log("[LevelEditor] Deleted Assets/Tiles folder");
        }

        // Delete Assets/Palettes folder
        if (AssetDatabase.IsValidFolder("Assets/Palettes"))
        {
            AssetDatabase.DeleteAsset("Assets/Palettes");
            Debug.Log("[LevelEditor] Deleted Assets/Palettes folder");
        }

        AssetDatabase.Refresh();

        // Fix sprite imports first (sets PPU=64 so tiles = 1 Unity unit)
        FixTilesetSpriteImports();

        // Force refresh to apply PPU changes before creating tiles
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        // Recreate tiles and palette
        CreateTilePaletteWithTiles();
    }

    /// <summary>
    /// Creates Tile Palette and imports all tiles.
    /// </summary>
    private void CreateTilePaletteWithTiles()
    {
        // Create folders
        string palettesFolder = "Assets/Palettes";
        string tilesFolder = "Assets/Tiles";

        if (!AssetDatabase.IsValidFolder(palettesFolder))
            AssetDatabase.CreateFolder("Assets", "Palettes");
        if (!AssetDatabase.IsValidFolder(tilesFolder))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        // Delete existing palette to recreate with tiles
        string palettePath = $"{palettesFolder}/TinySwords_Terrain.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null)
        {
            AssetDatabase.DeleteAsset(palettePath);
        }

        // First, create all tile assets organized by category
        var allTiles = new List<(UnityEngine.Tilemaps.Tile tile, string category)>();

        // Water tiles - WITH COLLIDER (player can't walk on water)
        // Note: Water Foam is animated spritesheet, not suitable for tiles
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Water Background color.png", "1_Water", tilesFolder, hasCollider: true));

        // Ground tiles (5 color variants) - NO collider (walkable)
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color1.png", "3_Grass", tilesFolder, hasCollider: false));
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color2.png", "4_GrassDark", tilesFolder, hasCollider: false));
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color3.png", "5_Dirt", tilesFolder, hasCollider: false));
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color4.png", "6_Sand", tilesFolder, hasCollider: false));
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color5.png", "7_Snow", tilesFolder, hasCollider: false));

        // Update 010 terrain
        string update010Path = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)/Terrain/Ground";
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{update010Path}/Tilemap_Flat.png", "8_Flat", tilesFolder, hasCollider: false));
        // Elevation - WITH COLLIDER (blocks movement - cliffs/walls)
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn($"{update010Path}/Tilemap_Elevation.png", "9_Elevation", tilesFolder, hasCollider: true));

        // Water from Update 010 - WITH COLLIDER
        string waterPath = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)/Terrain/Water/Water.png";
        allTiles.AddRange(CreateTilesFromSpritesheetAndReturn(waterPath, "0_WaterTile", tilesFolder, hasCollider: true));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Now create the palette with tiles already placed
        CreatePaletteWithTilesPlaced(palettePath, allTiles);

        Debug.Log($"[LevelEditor] Created Tile Palette with {allTiles.Count} tiles at {palettePath}");
        Debug.Log("[LevelEditor] Open Window > 2D > Tile Palette, select 'TinySwords_Terrain' from dropdown, and paint!");

        EditorUtility.DisplayDialog("Tile Palette Created",
            $"Created palette with {allTiles.Count} tiles!\n\n" +
            "To use:\n" +
            "1. Window > 2D > Tile Palette\n" +
            "2. Select 'TinySwords_Terrain' from dropdown\n" +
            "3. Select brush and tile\n" +
            "4. Paint on the scene!", "OK");
    }

    /// <summary>
    /// Creates tiles from spritesheet and returns them for palette population.
    /// </summary>
    private List<(UnityEngine.Tilemaps.Tile tile, string category)> CreateTilesFromSpritesheetAndReturn(string spritesheetPath, string tilePrefix, string outputFolder, bool hasCollider = false)
    {
        var result = new List<(UnityEngine.Tilemaps.Tile tile, string category)>();

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritesheetPath);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[LevelEditor] No assets found at: {spritesheetPath}");
            return result;
        }

        // Create subfolder for this tileset
        string subFolder = $"{outputFolder}/{tilePrefix}";
        if (!AssetDatabase.IsValidFolder(subFolder))
        {
            AssetDatabase.CreateFolder(outputFolder, tilePrefix);
        }

        // Sort sprites by name for consistent ordering
        var sprites = assets.OfType<Sprite>().OrderBy(s => s.name, new NaturalStringComparer()).ToList();

        // Determine collider type based on category
        var colliderType = hasCollider
            ? UnityEngine.Tilemaps.Tile.ColliderType.Grid
            : UnityEngine.Tilemaps.Tile.ColliderType.None;

        foreach (var sprite in sprites)
        {
            string tilePath = $"{subFolder}/{sprite.name}.asset";
            UnityEngine.Tilemaps.Tile tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(tilePath);

            if (tile == null)
            {
                // Create Tile asset
                tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                tile.sprite = sprite;
                tile.colliderType = colliderType;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else if (tile.colliderType != colliderType)
            {
                // Update existing tile collider type
                tile.colliderType = colliderType;
                EditorUtility.SetDirty(tile);
            }

            result.Add((tile, tilePrefix));
        }

        if (result.Count > 0)
            Debug.Log($"[LevelEditor] Processed {result.Count} tiles for {tilePrefix} (collider: {hasCollider})");

        return result;
    }

    /// <summary>
    /// Creates the palette prefab with tiles already placed in the tilemap.
    /// </summary>
    private void CreatePaletteWithTilesPlaced(string palettePath, List<(UnityEngine.Tilemaps.Tile tile, string category)> allTiles)
    {
        // Group tiles by category
        var grouped = allTiles.GroupBy(t => t.category).ToList();

        // Create palette GameObject
        GameObject paletteGO = new GameObject("TinySwords_Terrain");
        Grid paletteGrid = paletteGO.AddComponent<Grid>();
        paletteGrid.cellSize = new Vector3(1, 1, 0);

        GameObject tilemapGO = new GameObject("Layer1");
        tilemapGO.transform.SetParent(paletteGO.transform);
        var tilemap = tilemapGO.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var renderer = tilemapGO.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

        // Calculate grid layout - arrange by category in rows
        // Tilemap_color spritesheets are typically 9 columns wide
        int currentRow = 0;
        int tilesPerRow = 9;

        foreach (var group in grouped)
        {
            int col = 0;
            foreach (var (tile, _) in group)
            {
                Vector3Int pos = new Vector3Int(col, -currentRow, 0);
                tilemap.SetTile(pos, tile);

                col++;
                if (col >= tilesPerRow)
                {
                    col = 0;
                    currentRow++;
                }
            }
            // Move to next row section for next category
            if (col > 0) currentRow++;
            currentRow += 2; // Add gap between categories for visual separation
        }

        // Create GridPalette asset for proper Tile Palette window support
        GridPalette gridPalette = ScriptableObject.CreateInstance<GridPalette>();
        gridPalette.name = "GridPalette";
        gridPalette.cellSizing = GridPalette.CellSizing.Automatic;

        // Save as prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(paletteGO, palettePath);
        Object.DestroyImmediate(paletteGO);

        // Add GridPalette as sub-asset for Unity to recognize it as a tile palette
        AssetDatabase.AddObjectToAsset(gridPalette, palettePath);
        AssetDatabase.SaveAssets();

        // Select the palette in project view
        Selection.activeObject = savedPrefab;
        EditorGUIUtility.PingObject(savedPrefab);
    }

    /// <summary>
    /// Natural string comparer for sorting sprites by name (handles numbers correctly).
    /// </summary>
    private class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                {
                    int numX = 0, numY = 0;
                    while (ix < x.Length && char.IsDigit(x[ix]))
                        numX = numX * 10 + (x[ix++] - '0');
                    while (iy < y.Length && char.IsDigit(y[iy]))
                        numY = numY * 10 + (y[iy++] - '0');
                    if (numX != numY) return numX.CompareTo(numY);
                }
                else
                {
                    if (x[ix] != y[iy]) return x[ix].CompareTo(y[iy]);
                    ix++;
                    iy++;
                }
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    /// <summary>
    /// Fixes sprite imports for tileset spritesheets.
    /// </summary>
    private void FixTilesetSpriteImports()
    {
        int fixedCount = 0;

        // Free Pack Tilesets - these are irregular, need manual slicing
        // For now, just fix the basic import settings
        string[] freePackTilesets = {
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color1.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color2.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color3.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color4.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color5.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Water Background color.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Water Foam.png",
            $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Shadow.png",
        };

        foreach (string path in freePackTilesets)
        {
            if (FixTilesetSpriteImport(path, 64)) // 64x64 grid for irregular tilesets
                fixedCount++;
        }

        // Update 010 Tilesets - regular grid
        string update010Path = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)/Terrain/Ground";
        if (FixTilesetSpriteImport($"{update010Path}/Tilemap_Flat.png", 64))
            fixedCount++;
        if (FixTilesetSpriteImport($"{update010Path}/Tilemap_Elevation.png", 64))
            fixedCount++;

        // Update 010 Water
        string waterPath = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)/Terrain/Water/Water.png";
        if (FixTilesetSpriteImport(waterPath, 64))
            fixedCount++;

        AssetDatabase.Refresh();
        Debug.Log($"[LevelEditor] Fixed {fixedCount} tileset sprite imports (PPU=16 for 4x4 unit tiles)");
        EditorUtility.DisplayDialog("Tileset Import", $"Fixed {fixedCount} tileset sprites.\n\nCell size: 64x64, PPU: 16\nEach tile = 4x4 Unity units", "OK");
    }

    /// <summary>
    /// Fixes collider types on existing tile assets.
    /// Water and Elevation tiles get Grid colliders, others get None.
    /// </summary>
    private void FixTileColliders()
    {
        int fixedCount = 0;
        string tilesFolder = "Assets/Tiles";

        if (!AssetDatabase.IsValidFolder(tilesFolder))
        {
            EditorUtility.DisplayDialog("Error", "Assets/Tiles folder not found. Run REBUILD first.", "OK");
            return;
        }

        // Categories that should have colliders
        string[] colliderCategories = { "Water", "Elevation", "WaterTile", "WaterFoam" };

        // Find all tile assets
        string[] tileGuids = AssetDatabase.FindAssets("t:Tile", new[] { tilesFolder });

        foreach (string guid in tileGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(path);

            if (tile == null) continue;

            // Check if this tile should have collider based on folder name
            bool shouldHaveCollider = false;
            foreach (string category in colliderCategories)
            {
                if (path.Contains(category))
                {
                    shouldHaveCollider = true;
                    break;
                }
            }

            var desiredType = shouldHaveCollider
                ? UnityEngine.Tilemaps.Tile.ColliderType.Grid
                : UnityEngine.Tilemaps.Tile.ColliderType.None;

            if (tile.colliderType != desiredType)
            {
                tile.colliderType = desiredType;
                EditorUtility.SetDirty(tile);
                fixedCount++;
                Debug.Log($"[LevelEditor] Fixed collider for: {path} -> {desiredType}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix Tile Colliders",
            $"Fixed {fixedCount} tile colliders.\n\n" +
            "Water & Elevation tiles: Grid collider\n" +
            "Ground tiles: No collider",
            "OK");
    }

    private bool FixTilesetSpriteImport(string assetPath, int cellSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LevelEditor] Could not get importer for: {assetPath}");
            return false;
        }

        // Set basic import settings
        // PPU = 16 so that 64px tile = 4 Unity units (matches Grid cellSize=4)
        // This makes tiles large enough relative to characters
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.maxTextureSize = 8192;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;

        // Apply to get texture dimensions
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // Load texture to get dimensions
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return false;

        int width = tex.width;
        int height = tex.height;
        int cols = width / cellSize;
        int rows = height / cellSize;

        if (cols == 0 || rows == 0)
        {
            Debug.LogWarning($"[LevelEditor] Invalid dimensions for {assetPath}: {width}x{height} with cell size {cellSize}");
            return false;
        }

        // Create sprite sheet metadata
        List<SpriteMetaData> spritesheet = new List<SpriteMetaData>();
        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        int spriteIndex = 0;

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{baseName}_{spriteIndex}";
                meta.rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize);
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                spritesheet.Add(meta);
                spriteIndex++;
            }
        }

        // Apply sprite sheet
        #pragma warning disable 618
        importer.spritesheet = spritesheet.ToArray();
        #pragma warning restore 618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[LevelEditor] Fixed tileset: {assetPath} ({spriteIndex} tiles at {cellSize}x{cellSize})");
        return true;
    }

    #endregion

    #region Tab: Tools

    private void DrawToolsTab()
    {
        GUILayout.Label("Sprite Import", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Fix sprite imports if objects appear as blue squares or animations don't work.", MessageType.Info);

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Fix All Unit Sprites (Blue Units)", GUILayout.Height(30)))
        {
            FixAllUnitSpriteImports();
        }
        if (GUILayout.Button("Fix Decoration Sprites (Trees, Duck)", GUILayout.Height(25)))
        {
            FixDecorationSpriteImports();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        GUILayout.Label("Scene Management", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Enemies")) Selection.objects = GameObject.FindGameObjectsWithTag("Enemy");
        if (GUILayout.Button("Select Resources"))
        {
            var resources = Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);
            GameObject[] gameObjects = new GameObject[resources.Length];
            for (int i = 0; i < resources.Length; i++)
                gameObjects[i] = resources[i].gameObject;
            Selection.objects = gameObjects;
        }
        if (GUILayout.Button("Select Decorations"))
        {
            Selection.objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.StartsWith("Decoration_")).ToArray();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        GUI.backgroundColor = Color.red;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete Enemies"))
        {
            if (EditorUtility.DisplayDialog("Delete", "Delete all enemies?", "Yes", "No"))
                foreach (var e in GameObject.FindGameObjectsWithTag("Enemy")) DestroyImmediate(e);
        }
        if (GUILayout.Button("Delete Resources"))
        {
            if (EditorUtility.DisplayDialog("Delete", "Delete all resources?", "Yes", "No"))
                foreach (var r in Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None)) DestroyImmediate(r.gameObject);
        }
        if (GUILayout.Button("Delete Decorations"))
        {
            if (EditorUtility.DisplayDialog("Delete", "Delete all decorations?", "Yes", "No"))
                foreach (var d in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(go => go.name.StartsWith("Decoration_"))) DestroyImmediate(d);
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        GUILayout.Label("Debug", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle Colliders"))
        {
            var visualizers = Object.FindObjectsByType<ColliderVisualizer>(FindObjectsSortMode.None);
            foreach (var v in visualizers) v.enabled = !v.enabled;
        }
        if (GUILayout.Button("Size Reference"))
        {
            CreateSizeReferenceCircles();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Quick Setup", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("CREATE COMPLETE TEST SCENE", GUILayout.Height(35)))
        {
            CreateCompleteTestScene();
        }
        GUI.backgroundColor = Color.white;
    }

    /// <summary>
    /// Fixes sprite imports for all Blue Units (enemies).
    /// </summary>
    private void FixAllUnitSpriteImports()
    {
        string[] unitFolders = { "Pawn", "Archer", "Warrior", "Lancer", "Monk" };
        int fixedCount = 0;

        foreach (string folder in unitFolders)
        {
            string folderPath = $"{SPRITES_ROOT}/{BLUE_UNITS}/{folder}";
            string[] pngFiles = Directory.GetFiles(folderPath.Replace("/", Path.DirectorySeparatorChar.ToString()), "*.png");

            foreach (string pngFile in pngFiles)
            {
                string assetPath = pngFile.Replace("\\", "/");
                if (assetPath.StartsWith(Application.dataPath))
                    assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);

                if (FixSpriteImport(assetPath, 320))
                    fixedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[LevelEditor] Fixed {fixedCount} unit sprite imports");
        EditorUtility.DisplayDialog("Sprite Import", $"Fixed {fixedCount} sprite sheets.\n\nCell size: 320x320", "OK");
    }

    /// <summary>
    /// Fixes sprite imports for decoration spritesheets (Trees, Duck).
    /// </summary>
    private void FixDecorationSpriteImports()
    {
        int fixedCount = 0;

        // Trees (256x256 cells)
        string treesPath = $"{SPRITES_ROOT}/{TREES_PATH}";
        string[] treeFiles = { "Tree1.png", "Tree2.png", "Tree3.png", "Tree4.png" };
        foreach (string file in treeFiles)
        {
            string assetPath = $"{treesPath}/{file}";
            if (FixSpriteImport(assetPath, 256))
                fixedCount++;
        }

        // Duck (32x32 cells)
        string duckPath = $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rubber Duck/Rubber duck.png";
        if (FixSpriteImport(duckPath, 32))
            fixedCount++;

        AssetDatabase.Refresh();
        Debug.Log($"[LevelEditor] Fixed {fixedCount} decoration sprite imports");
        EditorUtility.DisplayDialog("Sprite Import", $"Fixed {fixedCount} decoration sprites.\n\nTrees: 256x256\nDuck: 32x32", "OK");
    }

    /// <summary>
    /// Fixes a single sprite import with the specified cell size.
    /// </summary>
    private bool FixSpriteImport(string assetPath, int cellSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LevelEditor] Could not get importer for: {assetPath}");
            return false;
        }

        // Set basic import settings
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.maxTextureSize = 8192;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100;

        // Apply to get texture dimensions
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // Load texture to get dimensions
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return false;

        int width = tex.width;
        int height = tex.height;
        int cols = width / cellSize;
        int rows = height / cellSize;

        if (cols == 0 || rows == 0)
        {
            Debug.LogWarning($"[LevelEditor] Invalid dimensions for {assetPath}: {width}x{height} with cell size {cellSize}");
            return false;
        }

        // Create sprite sheet metadata
        List<SpriteMetaData> spritesheet = new List<SpriteMetaData>();
        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        int spriteIndex = 0;

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{baseName}_{spriteIndex}";
                meta.rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize);
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                spritesheet.Add(meta);
                spriteIndex++;
            }
        }

        // Apply sprite sheet
        #pragma warning disable 618
        importer.spritesheet = spritesheet.ToArray();
        #pragma warning restore 618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[LevelEditor] Fixed: {assetPath} ({spriteIndex} sprites at {cellSize}x{cellSize})");
        return true;
    }

    #endregion

    #region Footer

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Spawn at:", GUILayout.Width(60));

        if (GUILayout.Button("Scene View Center", GUILayout.Height(20)))
        {
            // This is the default behavior
        }

        if (GUILayout.Button("Origin (0,0)", GUILayout.Height(20)))
        {
            // Could add spawn point override
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Entity Creation Methods

    /// <summary>
    /// Gets spawn position at center of current scene view (2D, Z=0).
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position;
            return new Vector3(pos.x, pos.y, 0f); // Force Z=0 for 2D
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Creates the player Spider at specified position.
    /// </summary>
    public static GameObject CreatePlayer(Vector3 position)
    {
        // Check if player already exists
        GameObject existing = GameObject.FindGameObjectWithTag("Player");
        if (existing != null)
        {
            Debug.LogWarning("[LevelEditor] Player already exists!");
            Selection.activeGameObject = existing;
            return existing;
        }

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;
        player.transform.localScale = new Vector3(PLAYER_SCALE, PLAYER_SCALE, 1f);

        // Sprite - sortingOrder 100 to be above all tilemap layers
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;
        sr.sprite = LoadSprite($"{SPRITES_ROOT}/{ENEMY_PACK}/Spider/Spider_Idle.png");

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.mass = 100f;

        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.radius = PLAYER_COLLIDER_RADIUS;

        // Debug visualizer
        player.AddComponent<ColliderVisualizer>();

        // Components
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<SkullCollector>();
        player.AddComponent<ResourceMagnet>();

        // Animator setup would go here
        SetupPlayerAnimator(player);

        Selection.activeGameObject = player;
        Debug.Log($"[LevelEditor] Created Player at {position}");
        return player;
    }

    /// <summary>
    /// Creates an enemy of specified type at position.
    /// </summary>
    public static GameObject CreateEnemy(EnemyType type, Vector3 position)
    {
        GameObject enemy = new GameObject($"Enemy_{type}");
        enemy.tag = "Enemy";
        enemy.transform.position = position;
        enemy.transform.localScale = new Vector3(UNIT_SCALE, UNIT_SCALE, 1f);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) enemy.layer = enemyLayer;

        // Sprite
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = GetEnemySprite(type);
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(GetEnemyColor(type));
        }

        // Physics
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 100f;
        rb.linearDamping = 10f;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = ENEMY_COLLIDER_RADIUS;

        // Debug visualizer
        enemy.AddComponent<ColliderVisualizer>();

        // Enemy component with config
        HumanEnemy humanEnemy = enemy.AddComponent<HumanEnemy>();
        EnemyConfig config = CreateEnemyConfig(type);

        SerializedObject so = new SerializedObject(humanEnemy);
        SerializedProperty configProp = so.FindProperty("config");
        if (configProp != null)
        {
            configProp.objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        // Animator
        SetupEnemyAnimator(enemy, type);

        Selection.activeGameObject = enemy;
        Debug.Log($"[LevelEditor] Created {type} at {position}");
        return enemy;
    }

    /// <summary>
    /// Creates a building at position.
    /// </summary>
    public static GameObject CreateBuilding(string buildingType, Vector3 position, bool isEnemy)
    {
        string faction = isEnemy ? "Blue" : "Red";
        GameObject building = new GameObject($"Building_{faction}_{buildingType}");
        SafeSetTag(building, "Building");
        building.transform.position = position;
        building.transform.localScale = new Vector3(BUILDING_SCALE, BUILDING_SCALE, 1f);

        SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;

        string path = isEnemy ? BLUE_BUILDINGS : RED_BUILDINGS;
        sr.sprite = LoadSprite($"{SPRITES_ROOT}/{path}/{buildingType}.png");
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(isEnemy ? Color.blue : Color.red, 64);
        }

        BoxCollider2D col = building.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 1.5f);

        if (isEnemy)
        {
            building.AddComponent<EnemyHouse>();
        }

        Selection.activeGameObject = building;
        Debug.Log($"[LevelEditor] Created {faction} {buildingType} at {position}");
        return building;
    }

    /// <summary>
    /// Creates a storage building (player base).
    /// </summary>
    public static GameObject CreateStorage(Vector3 position)
    {
        GameObject storage = new GameObject("Storage");
        SafeSetTag(storage, "Storage");
        storage.transform.position = position;
        storage.transform.localScale = new Vector3(BUILDING_SCALE, BUILDING_SCALE, 1f);

        SpriteRenderer sr = storage.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        sr.sprite = LoadSprite($"{SPRITES_ROOT}/{RED_BUILDINGS}/House1.png");
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(new Color(0.4f, 0.6f, 0.4f), 48);
        }

        BoxCollider2D col = storage.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1.2f);
        col.isTrigger = true;

        storage.AddComponent<Storage>();

        Selection.activeGameObject = storage;
        Debug.Log($"[LevelEditor] Created Storage at {position}");
        return storage;
    }

    /// <summary>
    /// Creates a resource pickup at position.
    /// </summary>
    public static GameObject CreateResource(ResourcePickup.ResourceType type, Vector3 position)
    {
        GameObject pickup = new GameObject($"Resource_{type}");
        pickup.transform.position = position;
        pickup.transform.localScale = new Vector3(RESOURCE_SCALE, RESOURCE_SCALE, 1f);

        SpriteRenderer sr = pickup.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        string spritePath = GetResourceSpritePath(type);
        sr.sprite = spritePath != null ? LoadSprite(spritePath) : null;
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(GetResourceColor(type), 16);
        }

        CircleCollider2D col = pickup.AddComponent<CircleCollider2D>();
        col.radius = 0.6f;
        col.isTrigger = true;

        ResourcePickup rp = pickup.AddComponent<ResourcePickup>();
        SerializedObject so = new SerializedObject(rp);
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("amount").intValue = Random.Range(1, 5);
        so.ApplyModifiedProperties();

        return pickup;
    }

    /// <summary>
    /// Creates a decoration (visual only) at position.
    /// </summary>
    public static GameObject CreateDecoration(string decorationType, Vector3 position)
    {
        GameObject decoration = new GameObject($"Decoration_{decorationType}");
        decoration.transform.position = position;

        // Trees are larger, use special scale
        bool isTree = decorationType.StartsWith("Tree");
        float scale = isTree ? TREE_SCALE : DECORATION_SCALE;
        decoration.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = decoration.AddComponent<SpriteRenderer>();
        sr.sortingOrder = isTree ? 2 : 1; // Trees above bushes/rocks

        // Get sprite path based on decoration type
        string spritePath = GetDecorationSpritePath(decorationType);
        sr.sprite = LoadSprite(spritePath);
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(Color.green, 32);
        }

        // Add animation for animated decorations (trees, duck)
        if (IsAnimatedDecoration(decorationType))
        {
            SetupDecorationAnimation(decoration, decorationType, spritePath);
        }

        return decoration;
    }

    /// <summary>
    /// Sets up animation for animated decorations like trees and duck.
    /// </summary>
    private static void SetupDecorationAnimation(GameObject decoration, string decorationType, string spritePath)
    {
        int cellSize = GetDecorationCellSize(decorationType);

        // Load all sprites from the spritesheet
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count <= 1)
        {
            Debug.LogWarning($"[LevelEditor] No animation frames found for {decorationType}. Make sure sprite is sliced.");
            return;
        }

        // Sort sprites by name for correct frame order
        sprites.Sort((a, b) => NaturalCompare(a.name, b.name));

        // Create animation clip
        string animFolder = "Assets/Animations";
        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        string clipPath = $"{animFolder}/Decoration_{decorationType}_Idle.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            clip.frameRate = 12; // Slower for ambient decoration animation

            // Create keyframes for sprite animation
            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count + 1];
            float frameTime = 1f / clip.frameRate;

            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = i * frameTime;
                keyframes[i].value = sprites[i];
            }

            // Loop back to first frame
            keyframes[sprites.Count] = new ObjectReferenceKeyframe();
            keyframes[sprites.Count].time = sprites.Count * frameTime;
            keyframes[sprites.Count].value = sprites[0];

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            // Set loop
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, clipPath);
        }

        // Create or load animator controller
        string controllerPath = $"{animFolder}/Decoration_{decorationType}_Controller.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = clip;
            rootStateMachine.defaultState = idleState;
        }

        // Add Animator component
        Animator animator = decoration.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Debug.Log($"[LevelEditor] Created animated decoration: {decorationType} ({sprites.Count} frames)");
    }

    private static string GetDecorationSpritePath(string decorationType)
    {
        // Map decoration types to actual file paths in Tiny Swords asset pack
        return decorationType switch
        {
            // Trees (animated spritesheets - 256x256 cells)
            "Tree1" => $"{SPRITES_ROOT}/{TREES_PATH}/Tree1.png",
            "Tree2" => $"{SPRITES_ROOT}/{TREES_PATH}/Tree2.png",
            "Tree3" => $"{SPRITES_ROOT}/{TREES_PATH}/Tree3.png",
            "Tree4" => $"{SPRITES_ROOT}/{TREES_PATH}/Tree4.png",

            // Stumps (static)
            "Stump1" => $"{SPRITES_ROOT}/{TREES_PATH}/Stump 1.png",
            "Stump2" => $"{SPRITES_ROOT}/{TREES_PATH}/Stump 2.png",
            "Stump3" => $"{SPRITES_ROOT}/{TREES_PATH}/Stump 3.png",
            "Stump4" => $"{SPRITES_ROOT}/{TREES_PATH}/Stump 4.png",

            // Bushes - note the typo in asset pack: "Bushe" not "Bush"
            "Bush" or "Bush1" or "Bushe1" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Bushes/Bushe1.png",
            "Bush2" or "Bushe2" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Bushes/Bushe2.png",
            "Bush3" or "Bushe3" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Bushes/Bushe3.png",
            "Bush4" or "Bushe4" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Bushes/Bushe4.png",

            // Rocks
            "Rock1" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks/Rock1.png",
            "Rock2" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks/Rock2.png",
            "Rock3" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks/Rock3.png",
            "Rock4" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks/Rock4.png",

            // Clouds
            "Cloud1" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Clouds/Clouds_01.png",
            "Cloud2" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Clouds/Clouds_02.png",

            // Water rocks
            "WaterRock1" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks in the Water/Water Rocks_01.png",
            "WaterRock2" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rocks in the Water/Water Rocks_02.png",

            // Rubber duck (animated - 32x32 cells) - note lowercase 'd' in filename
            "Duck" => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Rubber Duck/Rubber duck.png",

            // Default fallback
            _ => $"{SPRITES_ROOT}/{DECORATIONS_PATH}/Bushes/Bushe1.png"
        };
    }

    /// <summary>
    /// Checks if a decoration type has animation (spritesheet with multiple frames).
    /// </summary>
    private static bool IsAnimatedDecoration(string decorationType)
    {
        return decorationType switch
        {
            "Tree1" or "Tree2" or "Tree3" or "Tree4" => true,
            "Duck" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the cell size for animated decoration spritesheets.
    /// </summary>
    private static int GetDecorationCellSize(string decorationType)
    {
        return decorationType switch
        {
            "Tree1" or "Tree2" or "Tree3" or "Tree4" => 256,
            "Duck" => 32,
            _ => 64
        };
    }

    /// <summary>
    /// Creates a ground tile at position.
    /// </summary>
    public static GameObject CreateGroundTile(string tileType, Vector3 position)
    {
        GameObject tile = new GameObject($"Ground_{tileType}");
        tile.transform.position = position;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0; // Ground layer

        // Get sprite path based on tile type
        string spritePath = GetTileSpritePath(tileType);
        sr.sprite = LoadSprite(spritePath);
        if (sr.sprite == null)
        {
            sr.sprite = CreateFallbackSprite(Color.gray, 64);
        }

        return tile;
    }

    private static string GetTileSpritePath(string tileType)
    {
        // Map tile types to Tileset sprites
        return tileType switch
        {
            "Grass" or "Grass1" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color1.png",
            "Grass2" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color2.png",
            "Dirt" or "Dirt1" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color3.png",
            "Sand" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color4.png",
            "Snow" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color5.png",
            "Water" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Water Background color.png",
            "WaterFoam" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Water Foam.png",
            "Shadow" => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Shadow.png",
            _ => $"{SPRITES_ROOT}/{TERRAIN_PATH}/Tileset/Tilemap_color1.png"
        };
    }

    #endregion

    #region Core Systems

    private void CreateCoreSystems()
    {
        CreateCoreSystem<GameManager>("GameManager");
        CreateCoreSystem<EventManager>("EventManager");
        CreateCoreSystem<EffectManager>("EffectManager");
        CreateCoreSystem<ResourceSpawner>("ResourceSpawner");
        CreateCoreSystem<QuestSystem>("QuestSystem");
        CreateCoreSystem<UpgradeSystem>("UpgradeSystem");
        Debug.Log("[LevelEditor] All core systems created!");
    }

    private void CreateCoreSystem<T>(string name) where T : Component
    {
        if (Object.FindAnyObjectByType<T>() == null)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<T>();
            Debug.Log($"[LevelEditor] Created {name}");
        }
    }

    private void SetupCameraFollow()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            camFollow.SetTarget(player.transform);
            Debug.Log("[LevelEditor] Camera follow set to Player");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Safely sets a tag on a GameObject. If the tag doesn't exist, creates it.
    /// </summary>
    private static void SafeSetTag(GameObject go, string tag)
    {
        // First try to create the tag if it doesn't exist
        EnsureTagExists(tag);

        try
        {
            go.tag = tag;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[LevelEditor] Tag '{tag}' could not be set. Add it manually in Edit > Project Settings > Tags and Layers");
        }
    }

    /// <summary>
    /// Ensures a tag exists in the project. Creates it if missing.
    /// </summary>
    private static void EnsureTagExists(string tag)
    {
        // Check if tag already exists
        try
        {
            // This will throw if tag doesn't exist
            var testGo = new GameObject();
            testGo.tag = tag;
            Object.DestroyImmediate(testGo);
            return; // Tag exists
        }
        catch (UnityException)
        {
            // Tag doesn't exist, need to create it
        }

        // Open the TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if tag already in list
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return; // Already exists
        }

        // Add new tag
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"[LevelEditor] Created tag: {tag}");
    }

    private static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[LevelEditor] No assets found at: {path}");
            return null;
        }

        // Collect all sprites
        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[LevelEditor] No sprites found at: {path}");
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // If only one sprite, return it
        if (sprites.Count == 1)
        {
            return sprites[0];
        }

        // Multiple sprites (sliced spritesheet) - find the first frame (_0)
        // First, try to find sprite ending with "_0"
        foreach (var sprite in sprites)
        {
            if (sprite.name.EndsWith("_0"))
            {
                return sprite;
            }
        }

        // If no "_0" found, sort by name and return first
        sprites.Sort((a, b) => NaturalCompare(a.name, b.name));
        return sprites[0];
    }

    // Natural string comparison for proper numeric sorting (e.g., "Idle_2" before "Idle_10")
    private static int NaturalCompare(string a, string b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                // Extract numbers
                int numA = 0, numB = 0;
                while (i < a.Length && char.IsDigit(a[i]))
                {
                    numA = numA * 10 + (a[i] - '0');
                    i++;
                }
                while (j < b.Length && char.IsDigit(b[j]))
                {
                    numB = numB * 10 + (b[j] - '0');
                    j++;
                }
                if (numA != numB) return numA.CompareTo(numB);
            }
            else
            {
                if (a[i] != b[j]) return a[i].CompareTo(b[j]);
                i++;
                j++;
            }
        }
        return a.Length.CompareTo(b.Length);
    }

    private static Sprite CreateFallbackSprite(Color color, int size = 32)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static string GetEnemySpritePath(EnemyType type)
    {
        return type switch
        {
            EnemyType.PawnUnarmed => $"{SPRITES_ROOT}/{BLUE_UNITS}/Pawn/Pawn_Idle.png",
            EnemyType.PawnAxe => $"{SPRITES_ROOT}/{BLUE_UNITS}/Pawn/Pawn_Idle Axe.png",
            EnemyType.Archer => $"{SPRITES_ROOT}/{BLUE_UNITS}/Archer/Archer_Idle.png",
            EnemyType.Warrior => $"{SPRITES_ROOT}/{BLUE_UNITS}/Warrior/Warrior_Idle.png",
            EnemyType.Lancer => $"{SPRITES_ROOT}/{BLUE_UNITS}/Lancer/Lancer_Idle.png",
            EnemyType.Monk => $"{SPRITES_ROOT}/{BLUE_UNITS}/Monk/Idle.png",
            EnemyType.PawnMiner or EnemyType.Miner => $"{SPRITES_ROOT}/{BLUE_UNITS}/Pawn/Pawn_Idle Pickaxe.png",
            _ => $"{SPRITES_ROOT}/{BLUE_UNITS}/Pawn/Pawn_Idle.png"
        };
    }

    private static Sprite GetEnemySprite(EnemyType type)
    {
        return LoadSprite(GetEnemySpritePath(type));
    }

    private static Color GetEnemyColor(EnemyType type)
    {
        return type switch
        {
            EnemyType.PawnUnarmed => new Color(0.4f, 0.4f, 0.8f),
            EnemyType.PawnAxe => new Color(0.4f, 0.4f, 0.9f),
            EnemyType.Lancer => new Color(0.3f, 0.3f, 0.8f),
            EnemyType.Archer => new Color(0.2f, 0.5f, 0.8f),
            EnemyType.Warrior => new Color(0.2f, 0.2f, 0.9f),
            EnemyType.Monk => new Color(0.5f, 0.5f, 0.9f),
            _ => Color.blue
        };
    }

    private static string GetResourceSpritePath(ResourcePickup.ResourceType type)
    {
        return type switch
        {
            ResourcePickup.ResourceType.Meat => $"{SPRITES_ROOT}/{RESOURCES_PATH}/Meat/Meat Resource/Meat Resource.png",
            ResourcePickup.ResourceType.Wood => $"{SPRITES_ROOT}/{RESOURCES_PATH}/Wood/Wood Resource/Wood Resource.png",
            ResourcePickup.ResourceType.Gold => $"{SPRITES_ROOT}/{RESOURCES_PATH}/Gold/Gold Resource/Gold_Resource.png",
            _ => null
        };
    }

    private static Color GetResourceColor(ResourcePickup.ResourceType type)
    {
        return type switch
        {
            ResourcePickup.ResourceType.Meat => new Color(0.9f, 0.3f, 0.3f),
            ResourcePickup.ResourceType.Wood => new Color(0.6f, 0.4f, 0.2f),
            ResourcePickup.ResourceType.Gold => new Color(1f, 0.85f, 0f),
            _ => Color.white
        };
    }

    private static EnemyConfig CreateEnemyConfig(EnemyType type)
    {
        return type switch
        {
            EnemyType.PawnUnarmed => EnemyConfig.CreatePawnUnarmedConfig(),
            EnemyType.PawnAxe => EnemyConfig.CreatePawnAxeConfig(),
            EnemyType.Lancer => EnemyConfig.CreateLancerConfig(),
            EnemyType.Archer => EnemyConfig.CreateArcherConfig(),
            EnemyType.Warrior => EnemyConfig.CreateWarriorConfig(),
            EnemyType.Monk => EnemyConfig.CreateMonkConfig(),
            EnemyType.PawnMiner => EnemyConfig.CreatePawnMinerConfig(),
            _ => EnemyConfig.CreatePawnUnarmedConfig()
        };
    }

    private static void SetupPlayerAnimator(GameObject player)
    {
        // Add Animator component for player animations
        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
        }

        // Load and assign the Spider AnimatorController
        string controllerPath = "Assets/Animations/Player_Spider_Controller.controller";
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log("[LevelEditor] Assigned Spider AnimatorController to player");
        }
        else
        {
            Debug.LogWarning($"[LevelEditor] AnimatorController not found at: {controllerPath}");
        }
    }

    private static void SetupEnemyAnimator(GameObject enemy, EnemyType type)
    {
        if (enemy.GetComponent<Animator>() == null)
        {
            enemy.AddComponent<Animator>();
        }
    }

    private void CreateSizeReferenceCircles()
    {
        // Create visual reference circles for different entity sizes
        GameObject parent = new GameObject("Size Reference Circles");
        parent.transform.position = GetSpawnPosition();

        // Player size reference (scale 1)
        CreateReferenceCircle(parent.transform, "Player (scale 1)", 1f, Color.green);

        // Enemy size reference (scale 4)
        CreateReferenceCircle(parent.transform, "Enemy (scale 4)", 4f, Color.red);

        // Building size reference (scale 1)
        CreateReferenceCircle(parent.transform, "Building (scale 1)", 3.2f, Color.blue);

        Selection.activeGameObject = parent;
        Debug.Log("[LevelEditor] Size reference circles created. Delete when done.");
    }

    private void CreateReferenceCircle(Transform parent, string name, float radius, Color color)
    {
        GameObject circle = new GameObject(name);
        circle.transform.SetParent(parent);
        circle.transform.localPosition = Vector3.right * radius * 2;

        var sr = circle.AddComponent<SpriteRenderer>();
        sr.color = new Color(color.r, color.g, color.b, 0.3f);

        // Use Unity's built-in white texture as placeholder
        var tex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                tex.SetPixel(x, y, dist < 30 ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
        circle.transform.localScale = Vector3.one * radius;
    }

    private void CreateCompleteTestScene()
    {
        // Core systems
        CreateCoreSystems();

        // Player
        CreatePlayer(Vector3.zero);

        // Enemies spread out
        CreateEnemy(EnemyType.PawnAxe, new Vector3(10, 0, 0));
        CreateEnemy(EnemyType.Archer, new Vector3(0, 12, 0));
        CreateEnemy(EnemyType.Warrior, new Vector3(-10, 0, 0));
        CreateEnemy(EnemyType.Lancer, new Vector3(8, -8, 0));
        CreateEnemy(EnemyType.PawnUnarmed, new Vector3(-8, 8, 0));

        // Resources
        SpawnResourceCluster(ResourcePickup.ResourceType.Meat, 5, new Vector3(5, 5, 0));
        SpawnResourceCluster(ResourcePickup.ResourceType.Wood, 5, new Vector3(-5, 5, 0));
        SpawnResourceCluster(ResourcePickup.ResourceType.Gold, 3, new Vector3(0, -8, 0));

        // Buildings
        CreateBuilding("House1", new Vector3(15, 5, 0), true);
        CreateStorage(new Vector3(-15, -8, 0));

        // Camera
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = 24f;
            Camera.main.backgroundColor = new Color(0.15f, 0.2f, 0.15f);
        }
        SetupCameraFollow();

        Debug.Log("[LevelEditor] Complete test scene created!");
    }

    #endregion
}
