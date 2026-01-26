using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

/// <summary>
/// Helper tool for setting up Tile Palettes from Pixel Art Top Down asset.
/// Access via menu: Tools > Tiles > Create Tile Palette
/// </summary>
public class TilePaletteSetup : EditorWindow
{
    private static readonly string AssetPath = "Assets/Pixel Art Top Down - Basic v1.2.3/Texture";
    private static readonly string TilesOutputPath = "Assets/Tiles";

    [MenuItem("Tools/Tiles/Setup Tiles from Pixel Art")]
    public static void SetupTiles()
    {
        // Create Tiles folder
        if (!AssetDatabase.IsValidFolder(TilesOutputPath))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }

        // Create tiles from each tileset
        CreateTilesFromTexture("TX Tileset Grass", "Grass");
        CreateTilesFromTexture("TX Tileset Stone Ground", "Stone");
        CreateTilesFromTexture("TX Tileset Wall", "Wall");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Tiles Created!",
            "Created tile assets in Assets/Tiles/\n\n" +
            "Next steps:\n" +
            "1. Window > 2D > Tile Palette\n" +
            "2. Create New Palette\n" +
            "3. Drag tiles from Assets/Tiles into palette\n" +
            "4. Select brush and paint on Tilemap!",
            "OK");
    }

    [MenuItem("Tools/Tiles/Create Empty Tilemap Grid")]
    public static void CreateTilemapGrid()
    {
        // Check if Grid exists
        Grid existingGrid = Object.FindFirstObjectByType<Grid>();
        if (existingGrid != null)
        {
            Selection.activeGameObject = existingGrid.gameObject;
            EditorUtility.DisplayDialog("Grid Exists",
                "A Grid already exists in the scene.\n" +
                "Selected it for you.",
                "OK");
            return;
        }

        // Create Grid
        GameObject grid = new GameObject("BaseGrid");
        Grid gridComponent = grid.AddComponent<Grid>();
        gridComponent.cellSize = new Vector3(1, 1, 0);

        // Create Ground layer
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(grid.transform);
        ground.transform.localPosition = Vector3.zero;
        Tilemap groundTilemap = ground.AddComponent<Tilemap>();
        TilemapRenderer groundRenderer = ground.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = -10;

        // Create Paths layer
        GameObject paths = new GameObject("Paths");
        paths.transform.SetParent(grid.transform);
        paths.transform.localPosition = Vector3.zero;
        Tilemap pathsTilemap = paths.AddComponent<Tilemap>();
        TilemapRenderer pathsRenderer = paths.AddComponent<TilemapRenderer>();
        pathsRenderer.sortingOrder = -5;

        // Create Walls layer
        GameObject walls = new GameObject("Walls");
        walls.transform.SetParent(grid.transform);
        walls.transform.localPosition = Vector3.zero;
        Tilemap wallsTilemap = walls.AddComponent<Tilemap>();
        TilemapRenderer wallsRenderer = walls.AddComponent<TilemapRenderer>();
        wallsRenderer.sortingOrder = 0;
        walls.AddComponent<TilemapCollider2D>();

        // Create Props layer (for objects)
        GameObject props = new GameObject("Props");
        props.transform.SetParent(grid.transform);
        props.transform.localPosition = Vector3.zero;

        Selection.activeGameObject = grid;

        EditorUtility.DisplayDialog("Tilemap Grid Created!",
            "Created grid with layers:\n\n" +
            "• Ground (sorting: -10) - for grass\n" +
            "• Paths (sorting: -5) - for stone paths\n" +
            "• Walls (sorting: 0) - with collider\n" +
            "• Props - parent for objects\n\n" +
            "Now open Tile Palette and start painting!",
            "OK");
    }

    private static void CreateTilesFromTexture(string textureName, string folderName)
    {
        string texturePath = $"{AssetPath}/{textureName}.png";

        // Create subfolder
        string subFolder = $"{TilesOutputPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(subFolder))
        {
            AssetDatabase.CreateFolder(TilesOutputPath, folderName);
        }

        // Load all sprites from texture
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        int tileCount = 0;

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name != textureName)
            {
                // Create Tile asset
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;

                string tilePath = $"{subFolder}/{sprite.name}.asset";

                // Delete if exists
                if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
                {
                    AssetDatabase.DeleteAsset(tilePath);
                }

                AssetDatabase.CreateAsset(tile, tilePath);
                tileCount++;
            }
        }

        Debug.Log($"[TilePaletteSetup] Created {tileCount} tiles from {textureName}");
    }

    [MenuItem("Tools/Tiles/Quick Add Storage Building")]
    public static void AddStorageBuilding()
    {
        // Find Props parent or create one
        GameObject propsParent = GameObject.Find("Props");
        if (propsParent == null)
        {
            propsParent = new GameObject("Props");
        }

        // Create Storage
        GameObject storage = new GameObject("Storage");
        storage.transform.SetParent(propsParent.transform);
        storage.transform.position = Vector3.zero;

        // Add sprite renderer
        SpriteRenderer sr = storage.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Try to load a crate sprite
        Sprite crateSprite = LoadSpriteFromProps("crate_large");
        if (crateSprite == null)
        {
            // Create placeholder
            Texture2D tex = new Texture2D(32, 32);
            Color wood = new Color(0.55f, 0.38f, 0.22f);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, wood);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            crateSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
        }
        sr.sprite = crateSprite;

        // Add collider
        BoxCollider2D col = storage.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);

        // Add Storage component
        Storage storageComp = storage.AddComponent<Storage>();
        SerializedObject so = new SerializedObject(storageComp);
        so.FindProperty("buildingName").stringValue = "Storage";
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("interactionRange").floatValue = 3f;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = storage;

        EditorUtility.DisplayDialog("Storage Added!",
            "Created Storage building.\n\n" +
            "• Move it where you want in the Scene\n" +
            "• Assign a sprite in Inspector if needed\n" +
            "• Players deposit cargo when near it",
            "OK");
    }

    private static Sprite LoadSpriteFromProps(string spriteName)
    {
        string propsPath = $"{AssetPath}/TX Props.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(propsPath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }
        return null;
    }
}
