using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to create test enemies.
/// Access via menu: Tools > Create > Test Enemy
/// </summary>
public class EnemyCreator : EditorWindow
{
    [MenuItem("Tools/Create/Test Enemy (Gnome)")]
    public static void CreateGnomeEnemy()
    {
        CreateEnemy("Gnome", "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Gnome/Gnome_Idle.png");
    }

    [MenuItem("Tools/Create/Test Enemy (Bear)")]
    public static void CreateBearEnemy()
    {
        CreateEnemy("Bear", "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Bear/Bear_Idle.png");
    }

    [MenuItem("Tools/Create/Test Enemy (Skull)")]
    public static void CreateSkullEnemy()
    {
        CreateEnemy("Skull", "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Skull/Skull_Idle.png");
    }

    private static void CreateEnemy(string enemyName, string spritePath)
    {
        // Ensure Enemy layer exists
        CreateLayerIfNotExists("Enemy");

        // Create enemy GameObject
        GameObject enemy = new GameObject($"Enemy_{enemyName}");

        // Position in front of camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            enemy.transform.position = cam.transform.position + new Vector3(3, 0, 10);
        }
        else
        {
            enemy.transform.position = new Vector3(3, 0, 0);
        }

        // Add SpriteRenderer
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Try to load sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            // Try loading first sub-sprite if it's a spritesheet
            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (Object obj in sprites)
            {
                if (obj is Sprite s)
                {
                    sprite = s;
                    break;
                }
            }
        }

        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            Debug.LogWarning($"[EnemyCreator] Could not find sprite at: {spritePath}");
        }

        // Add Rigidbody2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Add BoxCollider2D
        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // Add EnemyAI
        enemy.AddComponent<EnemyAI>();

        // Set layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            enemy.layer = enemyLayer;
        }

        // Select the new enemy
        Selection.activeGameObject = enemy;

        Debug.Log($"[EnemyCreator] Created {enemyName} enemy. Don't forget to set Enemy layer in PlayerCombat!");

        EditorUtility.DisplayDialog("Enemy Created",
            $"Created {enemyName} enemy with:\n" +
            "- SpriteRenderer\n" +
            "- Rigidbody2D (no gravity)\n" +
            "- BoxCollider2D\n" +
            "- EnemyAI script\n\n" +
            "Make sure:\n" +
            "1. Player has tag 'Player'\n" +
            "2. PlayerCombat has Enemy Layer set",
            "OK");
    }

    private static void CreateLayerIfNotExists(string layerName)
    {
        // Check if layer exists
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1) return;

        // Open TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Find first empty user layer (8-31)
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[EnemyCreator] Created layer '{layerName}' at index {i}");
                return;
            }
        }

        Debug.LogWarning("[EnemyCreator] No empty layer slots available!");
    }
}
