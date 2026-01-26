#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Editor script that auto-assigns sprites from Tiny Swords asset to FullSceneBuilder.
/// </summary>
[CustomEditor(typeof(FullSceneBuilder))]
public class FullSceneBuilderEditor : Editor
{
    // Base paths for sprites
    private const string FREE_PACK_PATH = "Assets/Sprites/Tiny Swords (Free Pack)/Tiny Swords (Free Pack)";
    private const string ENEMY_PACK_PATH = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private const string UPDATE_PACK_PATH = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("=== AUTO SPRITE ASSIGNMENT ===", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click the button below to automatically find and assign sprites from Tiny Swords asset pack.",
            MessageType.Info
        );

        if (GUILayout.Button("Auto-Assign Sprites from Tiny Swords", GUILayout.Height(40)))
        {
            AutoAssignSprites();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Build Scene Now (Editor)", GUILayout.Height(30)))
        {
            FullSceneBuilder builder = (FullSceneBuilder)target;
            builder.BuildFullScene();
        }
    }

    private void AutoAssignSprites()
    {
        FullSceneBuilder builder = (FullSceneBuilder)target;
        SerializedObject so = new SerializedObject(builder);

        int assigned = 0;

        // Player - Blue Pawn (player is blue/cyan colored)
        assigned += TryAssignSprite(so, "playerSprite", $"{FREE_PACK_PATH}/Units/Blue Units/Pawn/Pawn_Idle.png");

        // Ground - use terrain tile
        assigned += TryAssignFirstMatch(so, "groundTileSprite", "Assets/Sprites", "*Tileset_Grass*.png", "*Grass*.png");

        // Tree
        assigned += TryAssignFirstMatch(so, "treeSprite", $"{FREE_PACK_PATH}/Terrain/Trees", "*.png");

        // Bush
        assigned += TryAssignFirstMatch(so, "bushSprite", $"{FREE_PACK_PATH}/Terrain/Props/Shrubs", "*.png");

        // Rock
        assigned += TryAssignFirstMatch(so, "rockSprite", $"{FREE_PACK_PATH}/Terrain/Props/Rocks", "*.png");

        // Buildings - Red faction (enemies)
        assigned += TryAssignSprite(so, "houseSprite", $"{FREE_PACK_PATH}/Buildings/Red Buildings/House1.png");
        assigned += TryAssignSprite(so, "castleSprite", $"{FREE_PACK_PATH}/Buildings/Red Buildings/Castle.png");

        // Storage - use a chest or resource sprite
        assigned += TryAssignFirstMatch(so, "storageSprite", $"{FREE_PACK_PATH}/Terrain/Resources", "*Chest*.png", "*Gold*.png");

        // Farm
        assigned += TryAssignFirstMatch(so, "farmSprite", $"{FREE_PACK_PATH}/Buildings/Red Buildings", "*Farm*.png", "*House2*.png");

        // Sheep
        assigned += TryAssignSprite(so, "sheepSprite", $"{FREE_PACK_PATH}/Terrain/Resources/Meat/Sheep/Sheep_Idle.png");

        // Enemy units - Red faction
        assigned += TryAssignSprite(so, "peasantSprite", $"{FREE_PACK_PATH}/Units/Red Units/Pawn/Pawn_Idle.png");
        assigned += TryAssignSprite(so, "warriorSprite", $"{FREE_PACK_PATH}/Units/Red Units/Warrior/Warrior_Idle.png");
        assigned += TryAssignSprite(so, "archerSprite", $"{FREE_PACK_PATH}/Units/Red Units/Archer/Archer_Idle.png");
        assigned += TryAssignSprite(so, "lancerSprite", $"{FREE_PACK_PATH}/Units/Red Units/Lancer/Lancer_Idle.png");
        assigned += TryAssignSprite(so, "monkSprite", $"{FREE_PACK_PATH}/Units/Red Units/Monk/Idle.png");

        // Animals from Enemy Pack
        assigned += TryAssignSprite(so, "bearSprite", $"{ENEMY_PACK_PATH}/Bear/Bear_Idle.png");
        assigned += TryAssignFirstMatch(so, "snakeSprite", ENEMY_PACK_PATH, "*Snake*Idle*.png", "*Lizard*Idle*.png");
        assigned += TryAssignFirstMatch(so, "lizardSprite", ENEMY_PACK_PATH, "*Lizard*Idle*.png");
        assigned += TryAssignFirstMatch(so, "turtleSprite", ENEMY_PACK_PATH, "*Turtle*Idle*.png");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(builder);

        Debug.Log($"[FullSceneBuilderEditor] Auto-assigned {assigned} sprites from Tiny Swords!");

        if (assigned < 10)
        {
            Debug.LogWarning("[FullSceneBuilderEditor] Some sprites were not found. Check that Tiny Swords asset is imported correctly.");
        }
    }

    private int TryAssignSprite(SerializedObject so, string propertyName, string path)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"Property not found: {propertyName}");
            return 0;
        }

        // Load sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // If direct path doesn't work, try to find sub-sprite (for sprite sheets)
        if (sprite == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            sprite = assets?.OfType<Sprite>().FirstOrDefault();
        }

        if (sprite != null)
        {
            prop.objectReferenceValue = sprite;
            Debug.Log($"[Sprite] Assigned {propertyName} = {sprite.name}");
            return 1;
        }
        else
        {
            Debug.LogWarning($"[Sprite] Not found: {path}");
            return 0;
        }
    }

    private int TryAssignFirstMatch(SerializedObject so, string propertyName, string folder, params string[] patterns)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null) return 0;

        foreach (string pattern in patterns)
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { folder });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(assetPath);

                if (MatchesPattern(fileName, pattern))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null)
                    {
                        // Try loading sub-sprites
                        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        sprite = assets?.OfType<Sprite>().FirstOrDefault();
                    }

                    if (sprite != null)
                    {
                        prop.objectReferenceValue = sprite;
                        Debug.Log($"[Sprite] Assigned {propertyName} = {sprite.name} (from pattern {pattern})");
                        return 1;
                    }
                }
            }
        }

        Debug.LogWarning($"[Sprite] No match found for {propertyName} in {folder}");
        return 0;
    }

    private bool MatchesPattern(string fileName, string pattern)
    {
        // Simple wildcard matching
        if (pattern.StartsWith("*") && pattern.EndsWith("*"))
        {
            string middle = pattern.Substring(1, pattern.Length - 2);
            return fileName.ToLower().Contains(middle.ToLower());
        }
        else if (pattern.StartsWith("*"))
        {
            string end = pattern.Substring(1);
            return fileName.ToLower().EndsWith(end.ToLower());
        }
        else if (pattern.EndsWith("*"))
        {
            string start = pattern.Substring(0, pattern.Length - 1);
            return fileName.ToLower().StartsWith(start.ToLower());
        }
        else
        {
            return fileName.ToLower() == pattern.ToLower();
        }
    }
}
#endif
