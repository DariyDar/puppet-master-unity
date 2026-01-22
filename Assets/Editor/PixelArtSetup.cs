using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to configure all sprites for pixel art rendering.
/// Access via menu: Tools > Pixel Art > Setup All Sprites
/// </summary>
public class PixelArtSetup : EditorWindow
{
    [MenuItem("Tools/Pixel Art/Setup All Sprites (Run This First!)")]
    public static void SetupAllSprites()
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

                // Set texture type to Sprite
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                // Set filter mode to Point (pixel perfect)
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }

                // Disable compression for crisp pixels
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                // Set pixels per unit (16 is common for pixel art)
                if (importer.spritePixelsPerUnit != 16)
                {
                    importer.spritePixelsPerUnit = 16;
                    changed = true;
                }

                // Disable mipmaps for 2D
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

        EditorUtility.DisplayDialog("Pixel Art Setup Complete",
            $"Configured {count} sprites for pixel art rendering.\n\n" +
            "Settings applied:\n" +
            "- Filter Mode: Point (no filter)\n" +
            "- Compression: None\n" +
            "- Pixels Per Unit: 16\n" +
            "- Mipmaps: Disabled",
            "OK");

        Debug.Log($"[PixelArtSetup] Configured {count} sprites for pixel art.");
    }

    [MenuItem("Tools/Pixel Art/Setup Single Sprite")]
    public static void SetupSelectedSprite()
    {
        Object[] selectedObjects = Selection.objects;
        int count = 0;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 16;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"[PixelArtSetup] Configured {count} selected sprites.");
    }

    [MenuItem("Tools/Pixel Art/Create Game Scene")]
    public static void CreateGameScene()
    {
        // Create GameManager object
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<EventManager>();

        // Create Main Camera with pixel perfect settings
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.AddComponent<CameraFollow>();
            mainCam.orthographicSize = 8; // Good size for pixel art
        }

        // Create Player object
        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;

        // Add required components
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        player.AddComponent<PlayerController>();

        // Set camera target
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                // Use reflection or serialize field to set target
                var targetField = typeof(CameraFollow).GetField("target",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    targetField.SetValue(camFollow, player.transform);
                }
            }
        }

        // Select the player
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog("Game Scene Created",
            "Created:\n" +
            "- GameManager (with EventManager)\n" +
            "- Player (with Rigidbody2D, Collider, PlayerController)\n" +
            "- Camera Follow script on Main Camera\n\n" +
            "Next steps:\n" +
            "1. Assign a sprite to the Player's SpriteRenderer\n" +
            "2. Add PlayerInput component to Player\n" +
            "3. Press Play to test movement with WASD",
            "OK");

        Debug.Log("[PixelArtSetup] Game scene created with Player and GameManager.");
    }
}
