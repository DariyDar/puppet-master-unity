using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility to fix Pixels Per Unit for Pixel Art Top Down TILESET assets ONLY.
/// Changes PPU from 32 to 16 to match Tiny Swords scale.
/// IMPORTANT: Only affects TX Tileset files, NOT TX Props, TX Plant, TX Player, etc.
/// </summary>
public class SpriteScaleUtility : EditorWindow
{
    private const float TARGET_PPU = 16f;

    [MenuItem("Tools/Puppet Master/Fix Pixel Art Top Down Tileset PPU")]
    public static void FixPixelArtTopDownPPU()
    {
        // ONLY tileset files - NOT Props, Plant, Player, Struct, Shadow, etc.
        string[] tilesetFiles = new string[]
        {
            // Cainos folder
            "Assets/Cainos/Pixel Art Top Down - Basic/Texture/TX Tileset Grass.png",
            "Assets/Cainos/Pixel Art Top Down - Basic/Texture/TX Tileset Stone Ground.png",
            "Assets/Cainos/Pixel Art Top Down - Basic/Texture/TX Tileset Wall.png",
            // Alternative folder locations
            "Assets/Pixel Art Top Down - Basic v1.2.3/Texture/TX Tileset Grass.png",
            "Assets/Pixel Art Top Down - Basic v1.2.3/Texture/TX Tileset Stone Ground.png",
            "Assets/Pixel Art Top Down - Basic v1.2.3/Texture/TX Tileset Wall.png",
            // Sprites folder
            "Assets/Sprites/Pixel Art Top Down/TX Tileset Grass.png",
            "Assets/Sprites/Pixel Art Top Down/TX Tileset Stone Ground.png",
            "Assets/Sprites/Pixel Art Top Down/TX Tileset Wall.png"
        };

        int fixedCount = 0;
        int skippedCount = 0;

        foreach (string assetPath in tilesetFiles)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                // File doesn't exist at this path
                continue;
            }

            if (Mathf.Abs(importer.spritePixelsPerUnit - TARGET_PPU) > 0.01f)
            {
                float oldPPU = importer.spritePixelsPerUnit;
                importer.spritePixelsPerUnit = TARGET_PPU;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                fixedCount++;
                Debug.Log($"[SpriteScaleUtility] Fixed {assetPath}: PPU {oldPPU} -> {TARGET_PPU}");
            }
            else
            {
                skippedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SpriteScaleUtility] Fixed {fixedCount} tileset textures to PPU={TARGET_PPU}, skipped {skippedCount} (already correct)");
        EditorUtility.DisplayDialog("Tileset PPU Fix Complete",
            $"Fixed {fixedCount} tileset textures to PPU={TARGET_PPU}\nSkipped {skippedCount} (already correct)\n\nNOTE: Only TX Tileset files were modified.", "OK");
    }
}
