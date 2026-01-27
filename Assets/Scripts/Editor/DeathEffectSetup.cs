using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to setup death effect and skull pickup sprites on EffectManager.
/// Slices Dust_01.png, Explosion_01.png, and Dead.png sprite sheets,
/// then assigns the frames to the EffectManager component in the scene.
/// Run from: Puppet Master → Setup Death Effects
/// </summary>
public class DeathEffectSetup : Editor
{
    // Sprite sheet paths
    private const string DUST_PATH = "Assets/Sprites/Effects/Dust_01.png";
    private const string EXPLOSION_PATH = "Assets/Sprites/Effects/Explosion_01.png";
    private const string DEAD_PATH = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)/Factions/Knights/Troops/Dead/Dead.png";

    [MenuItem("Puppet Master/Setup Death Effects")]
    public static void SetupDeathEffects()
    {
        // Step 1: Ensure sprite sheets are sliced
        bool dustOk = EnsureSpriteSheetSliced(DUST_PATH, "Dust_01", 64, 64);
        bool explosionOk = EnsureSpriteSheetSliced(EXPLOSION_PATH, "Explosion_01", 192, 192);
        // Dead.png: re-slice as 128x128 grid (7 cols x 2 rows = 14 animation frames)
        bool deadOk = EnsureSpriteSheetSliced(DEAD_PATH, "Dead", 128, 128, true);

        if (!dustOk && !explosionOk && !deadOk)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "Could not find any sprite sheets. Check that the following exist:\n" +
                $"- {DUST_PATH}\n" +
                $"- {EXPLOSION_PATH}\n" +
                $"- {DEAD_PATH}",
                "OK");
            return;
        }

        // Step 2: Load sprite frames
        Sprite[] dustFrames = LoadSpriteFrames(DUST_PATH);
        Sprite[] explosionFrames = LoadSpriteFrames(EXPLOSION_PATH);
        Sprite[] deadFrames = LoadSkullFrames(DEAD_PATH);

        // Step 3: Find or create EffectManager in scene
        EffectManager effectManager = FindOrCreateEffectManager();
        if (effectManager == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "Could not find or create EffectManager in the scene.", "OK");
            return;
        }

        // Step 4: Assign sprites via SerializedObject
        SerializedObject so = new SerializedObject(effectManager);

        if (dustFrames != null && dustFrames.Length > 0)
        {
            AssignSpriteArray(so, "deathDustFrames", dustFrames);
            Debug.Log($"[DeathEffectSetup] Assigned {dustFrames.Length} dust frames from Dust_01.png");
        }

        if (explosionFrames != null && explosionFrames.Length > 0)
        {
            AssignSpriteArray(so, "tntExplosionFrames", explosionFrames);
            Debug.Log($"[DeathEffectSetup] Assigned {explosionFrames.Length} explosion frames from Explosion_01.png");
        }

        if (deadFrames != null && deadFrames.Length > 0)
        {
            AssignSpriteArray(so, "skullPickupFrames", deadFrames);
            Debug.Log($"[DeathEffectSetup] Assigned {deadFrames.Length} skull frames from Dead.png (128x128 grid)");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(effectManager);

        // Step 5: Ensure player has SkullCollector
        EnsurePlayerHasSkullCollector();

        // Summary
        string summary = "Death Effects Setup Complete!\n\n";
        summary += $"Dust frames (Dust_01.png): {(dustFrames != null ? dustFrames.Length : 0)}\n";
        summary += $"Explosion frames (Explosion_01.png): {(explosionFrames != null ? explosionFrames.Length : 0)}\n";
        summary += $"Skull frames (Dead.png, 128x128 grid): {(deadFrames != null ? deadFrames.Length : 0)}\n";
        summary += $"\nEffectManager: {effectManager.gameObject.name}";

        EditorUtility.DisplayDialog("Death Effects Setup", summary, "OK");
        Debug.Log($"[DeathEffectSetup] {summary}");
    }

    /// <summary>
    /// Ensure a sprite sheet is sliced into grid cells.
    /// Returns true if the texture exists.
    /// </summary>
    private static bool EnsureSpriteSheetSliced(string assetPath, string namePrefix, int cellWidth, int cellHeight, bool forceReslice = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[DeathEffectSetup] Texture not found: {assetPath}");
            return false;
        }

        // Check if already sliced (skip if force reslice)
        if (!forceReslice)
        {
            Sprite[] existing = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
            if (existing.Length > 1)
            {
                Debug.Log($"[DeathEffectSetup] {assetPath} already sliced ({existing.Length} sprites)");
                return true;
            }
        }

        // Need to slice
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null) return false;

        // Make texture readable for slicing
        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Calculate grid
        int cols = texture.width / cellWidth;
        int rows = texture.height / cellHeight;

        List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();
        int index = 0;

        // Unity texture coords: bottom-left origin
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{namePrefix}_{index}";
                meta.rect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                spriteSheet.Add(meta);
                index++;
            }
        }

        importer.spritesheet = spriteSheet.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[DeathEffectSetup] Sliced {assetPath} into {index} sprites ({cols}x{rows} grid, {cellWidth}x{cellHeight} cells)");
        return true;
    }

    /// <summary>
    /// Load all sprite frames from a sliced sprite sheet, sorted by name index.
    /// </summary>
    private static Sprite[] LoadSpriteFrames(string assetPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s => GetSpriteIndex(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[DeathEffectSetup] No sprites found in {assetPath}");
            return null;
        }

        return sprites;
    }

    /// <summary>
    /// Load all skull animation frames from Dead.png (14 frames from 128x128 grid).
    /// </summary>
    private static Sprite[] LoadSkullFrames(string assetPath)
    {
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s => GetSpriteIndex(s.name))
            .ToArray();

        if (allSprites.Length == 0)
        {
            Debug.LogWarning($"[DeathEffectSetup] No sprites found in {assetPath}");
            return null;
        }

        Debug.Log($"[DeathEffectSetup] Loaded {allSprites.Length} skull frames from {assetPath}");
        return allSprites;
    }

    /// <summary>
    /// Extract numeric index from sprite name (e.g. "Dead_9" → 9, "Dust_01_3" → 3).
    /// </summary>
    private static int GetSpriteIndex(string name)
    {
        // Find last underscore and parse the number after it
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
        {
            string numStr = name.Substring(lastUnderscore + 1);
            if (int.TryParse(numStr, out int index))
            {
                return index;
            }
        }
        return 0;
    }

    /// <summary>
    /// Find existing EffectManager in scene, or create one.
    /// </summary>
    private static EffectManager FindOrCreateEffectManager()
    {
        EffectManager existing = Object.FindFirstObjectByType<EffectManager>();
        if (existing != null)
        {
            Debug.Log($"[DeathEffectSetup] Found existing EffectManager on '{existing.gameObject.name}'");
            return existing;
        }

        // Create new EffectManager
        GameObject go = new GameObject("EffectManager");
        EffectManager manager = go.AddComponent<EffectManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create EffectManager");
        Debug.Log("[DeathEffectSetup] Created new EffectManager GameObject");
        return manager;
    }

    /// <summary>
    /// Ensure the player GameObject has a SkullCollector component.
    /// </summary>
    private static void EnsurePlayerHasSkullCollector()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[DeathEffectSetup] No Player found in scene. SkullCollector not added.");
            return;
        }

        SkullCollector collector = player.GetComponent<SkullCollector>();
        if (collector == null)
        {
            collector = Undo.AddComponent<SkullCollector>(player);
            Debug.Log($"[DeathEffectSetup] Added SkullCollector to player '{player.name}'");
        }

        // Force-update serialized collectionRange to current default (4.5)
        SerializedObject collectorSO = new SerializedObject(collector);
        SerializedProperty rangeProp = collectorSO.FindProperty("collectionRange");
        if (rangeProp != null)
        {
            rangeProp.floatValue = 4.5f;
            collectorSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(collector);
            Debug.Log($"[DeathEffectSetup] Updated SkullCollector collectionRange to 4.5");
        }
    }

    /// <summary>
    /// Assign a Sprite array to a SerializedProperty.
    /// </summary>
    private static void AssignSpriteArray(SerializedObject so, string propertyName, Sprite[] sprites)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogError($"[DeathEffectSetup] Property '{propertyName}' not found on EffectManager!");
            return;
        }

        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }
}
