using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click project setup tool.
/// Configures layers, tags, and scene objects automatically.
/// Access via menu: Tools > Project Setup > Setup All
/// </summary>
public class ProjectSetup : EditorWindow
{
    [MenuItem("Tools/Project Setup/Setup All (Run This!)")]
    public static void SetupAll()
    {
        SetupLayers();
        SetupTags();
        SetupPlayerInScene();
        SetupGameManager();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Project Setup Complete",
            "Configured:\n\n" +
            "- Layers: Enemy, Projectile, Pickup\n" +
            "- Tags: Player (assigned to Player object)\n" +
            "- Player: Added PlayerHealth, PlayerCombat\n" +
            "- GameManager: Created if missing\n\n" +
            "You can now:\n" +
            "1. Create enemies via Tools > Create > Test Enemy\n" +
            "2. Press Play to test",
            "OK");
    }

    [MenuItem("Tools/Project Setup/Setup Layers")]
    public static void SetupLayers()
    {
        CreateLayerIfNotExists("Enemy");
        CreateLayerIfNotExists("Projectile");
        CreateLayerIfNotExists("Pickup");
        CreateLayerIfNotExists("Player");
        Debug.Log("[ProjectSetup] Layers configured");
    }

    [MenuItem("Tools/Project Setup/Setup Tags")]
    public static void SetupTags()
    {
        // Player tag already exists by default in Unity
        Debug.Log("[ProjectSetup] Tags configured (Player tag is built-in)");
    }

    [MenuItem("Tools/Project Setup/Setup Player In Scene")]
    public static void SetupPlayerInScene()
    {
        // Find Player object
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[ProjectSetup] No 'Player' object found in scene!");
            return;
        }

        // Set tag
        player.tag = "Player";

        // Set layer
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            player.layer = playerLayer;
        }

        // Add PlayerHealth if missing
        if (player.GetComponent<PlayerHealth>() == null)
        {
            player.AddComponent<PlayerHealth>();
            Debug.Log("[ProjectSetup] Added PlayerHealth to Player");
        }

        // Add PlayerCombat if missing
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            combat = player.AddComponent<PlayerCombat>();
            Debug.Log("[ProjectSetup] Added PlayerCombat to Player");
        }

        // Configure PlayerCombat enemy layer
        SerializedObject combatSO = new SerializedObject(combat);
        SerializedProperty enemyLayerProp = combatSO.FindProperty("enemyLayer");
        if (enemyLayerProp != null)
        {
            // Set to Enemy layer mask
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                enemyLayerProp.intValue = 1 << enemyLayer;
                combatSO.ApplyModifiedProperties();
                Debug.Log("[ProjectSetup] Configured PlayerCombat enemy layer");
            }
        }

        // Select player
        Selection.activeGameObject = player;
        Debug.Log("[ProjectSetup] Player configured with tag and components");
    }

    [MenuItem("Tools/Project Setup/Setup GameManager")]
    public static void SetupGameManager()
    {
        // Check if GameManager exists
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        if (existingGM != null)
        {
            Debug.Log("[ProjectSetup] GameManager already exists");
            return;
        }

        // Create GameManager
        GameObject gmObject = new GameObject("GameManager");
        gmObject.AddComponent<GameManager>();
        gmObject.AddComponent<EventManager>();

        Debug.Log("[ProjectSetup] Created GameManager with EventManager");
    }

    private static void CreateLayerIfNotExists(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1) return;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[ProjectSetup] Created layer '{layerName}' at index {i}");
                return;
            }
        }
    }

    [MenuItem("Tools/Project Setup/Configure Player Combat Layer")]
    public static void ConfigurePlayerCombatLayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[ProjectSetup] No Player found!");
            return;
        }

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            Debug.LogError("[ProjectSetup] Player has no PlayerCombat component!");
            return;
        }

        SerializedObject combatSO = new SerializedObject(combat);
        SerializedProperty enemyLayerProp = combatSO.FindProperty("enemyLayer");

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && enemyLayerProp != null)
        {
            enemyLayerProp.intValue = 1 << enemyLayer;
            combatSO.ApplyModifiedProperties();
            Debug.Log("[ProjectSetup] PlayerCombat enemy layer configured!");
        }
    }
}
