using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Balancer — runtime balance tuning tool for Puppet Master.
/// Puppet Master > Balancer
///
/// Categories:
/// - Spider (player) — base stats
/// - Units (player army units)
/// - Enemies (all enemy types)
/// - Buildings (towers, houses, castles)
///
/// In Play Mode: changes apply immediately to live instances.
/// Changes are saved permanently via SerializedObject when possible.
/// </summary>
public class Balancer : EditorWindow
{
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Spider", "Units", "Enemies", "Buildings" };

    // Foldout states
    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    [MenuItem("Puppet Master/Balancer")]
    public static void ShowWindow()
    {
        var window = GetWindow<Balancer>("Balancer");
        window.minSize = new Vector2(420, 550);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Balancer", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("PLAY MODE: Changes apply immediately. Use SerializedObject fields to persist.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("EDIT MODE: Select objects in scene to edit via Inspector, or enter Play Mode for live tuning.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space(5);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0: DrawSpiderTab(); break;
            case 1: DrawUnitsTab(); break;
            case 2: DrawEnemiesTab(); break;
            case 3: DrawBuildingsTab(); break;
        }

        EditorGUILayout.EndScrollView();

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    #region Spider Tab

    private void DrawSpiderTab()
    {
        EditorGUILayout.LabelField("Spider (Player)", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        if (Application.isPlaying)
        {
            DrawLiveSpider();
        }
        else
        {
            DrawSpiderFromScene();
        }
    }

    private void DrawLiveSpider()
    {
        // Find player objects
        var playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        var playerController = Object.FindFirstObjectByType<PlayerController>();
        var playerCombat = Object.FindFirstObjectByType<PlayerCombat>();

        if (playerHealth == null)
        {
            EditorGUILayout.LabelField("No Spider (Player) found in scene.");
            return;
        }

        // Health
        EditorGUILayout.LabelField("Health", EditorStyles.miniBoldLabel);
        SerializedObject soHealth = new SerializedObject(playerHealth);
        soHealth.Update();
        EditorGUILayout.PropertyField(soHealth.FindProperty("maxHealth"));
        EditorGUILayout.PropertyField(soHealth.FindProperty("invincibilityDuration"));
        EditorGUILayout.PropertyField(soHealth.FindProperty("armor"));
        EditorGUILayout.PropertyField(soHealth.FindProperty("regenRate"));
        soHealth.ApplyModifiedProperties();

        // Movement
        if (playerController != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            SerializedObject soCtrl = new SerializedObject(playerController);
            soCtrl.Update();
            EditorGUILayout.PropertyField(soCtrl.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(soCtrl.FindProperty("autoAttackRange"));
            EditorGUILayout.PropertyField(soCtrl.FindProperty("autoAttackCooldown"));
            soCtrl.ApplyModifiedProperties();
        }

        // Combat
        if (playerCombat != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Combat", EditorStyles.miniBoldLabel);
            SerializedObject soCombat = new SerializedObject(playerCombat);
            soCombat.Update();
            EditorGUILayout.PropertyField(soCombat.FindProperty("attackDamage"));
            EditorGUILayout.PropertyField(soCombat.FindProperty("attackRange"));
            EditorGUILayout.PropertyField(soCombat.FindProperty("attackCooldown"));
            soCombat.ApplyModifiedProperties();
        }
    }

    private void DrawSpiderFromScene()
    {
        // Try to find player in scene hierarchy
        var playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            DrawLiveSpider(); // Same logic works in edit mode with SerializedObject
        }
        else
        {
            EditorGUILayout.LabelField("No Spider found. Open a scene with the Player object.");
        }
    }

    #endregion

    #region Units Tab (Player Army)

    private void DrawUnitsTab()
    {
        EditorGUILayout.LabelField("Player Army Units", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        if (Application.isPlaying)
        {
            DrawLiveUnits();
        }
        else
        {
            EditorGUILayout.LabelField("Unit configs are ScriptableObjects. Enter Play Mode for live tuning.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            DrawUnitConfigAssets();
        }
    }

    private void DrawLiveUnits()
    {
        UnitBase[] units = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        if (units.Length == 0)
        {
            EditorGUILayout.LabelField("No player units in scene.");
            return;
        }

        var grouped = units
            .Where(u => u.Config != null && !u.IsDead)
            .GroupBy(u => u.Config.unitType)
            .OrderBy(g => g.Key.ToString());

        foreach (var group in grouped)
        {
            string key = $"unit_{group.Key}";
            if (!foldouts.ContainsKey(key)) foldouts[key] = true;

            var first = group.First();
            foldouts[key] = EditorGUILayout.Foldout(foldouts[key],
                $"{first.Config.displayName} ({group.Count()} alive)", true, EditorStyles.foldoutHeader);

            if (foldouts[key])
            {
                EditorGUI.indentLevel++;

                // Edit via SerializedObject on the unit instance
                SerializedObject so = new SerializedObject(first);
                so.Update();

                EditorGUILayout.PropertyField(so.FindProperty("maxHealth"));
                EditorGUILayout.PropertyField(so.FindProperty("damage"));
                EditorGUILayout.PropertyField(so.FindProperty("moveSpeed"));
                EditorGUILayout.PropertyField(so.FindProperty("attackRange"));
                EditorGUILayout.PropertyField(so.FindProperty("attackCooldown"));

                so.ApplyModifiedProperties();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }
    }

    private void DrawUnitConfigAssets()
    {
        // Find all UnitConfig assets
        string[] guids = AssetDatabase.FindAssets("t:UnitConfig");
        if (guids.Length == 0)
        {
            EditorGUILayout.LabelField("No UnitConfig assets found.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitConfig config = AssetDatabase.LoadAssetAtPath<UnitConfig>(path);
            if (config == null) continue;

            string key = $"unitAsset_{config.displayName}";
            if (!foldouts.ContainsKey(key)) foldouts[key] = false;

            foldouts[key] = EditorGUILayout.Foldout(foldouts[key],
                $"{config.displayName} ({config.unitType})", true);

            if (foldouts[key])
            {
                EditorGUI.indentLevel++;
                SerializedObject so = new SerializedObject(config);
                so.Update();

                EditorGUILayout.PropertyField(so.FindProperty("maxHealth"));
                EditorGUILayout.PropertyField(so.FindProperty("damage"));
                EditorGUILayout.PropertyField(so.FindProperty("attackSpeed"));
                EditorGUILayout.PropertyField(so.FindProperty("attackRange"));
                EditorGUILayout.PropertyField(so.FindProperty("moveSpeed"));
                EditorGUILayout.PropertyField(so.FindProperty("detectionRange"));
                EditorGUILayout.PropertyField(so.FindProperty("skullCost"));
                EditorGUILayout.PropertyField(so.FindProperty("meatCost"));

                if (so.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(config);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(3);
        }
    }

    #endregion

    #region Enemies Tab

    private void DrawEnemiesTab()
    {
        EditorGUILayout.LabelField("Enemy Units", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        if (Application.isPlaying)
        {
            DrawLiveEnemies();
        }
        else
        {
            DrawEnemyConfigDefaults();
        }
    }

    private void DrawLiveEnemies()
    {
        HumanEnemy[] enemies = Object.FindObjectsByType<HumanEnemy>(FindObjectsSortMode.None);
        if (enemies.Length == 0)
        {
            EditorGUILayout.LabelField("No enemies in scene.");
            return;
        }

        var grouped = enemies
            .Where(e => e.Config != null && !e.IsDead)
            .GroupBy(e => e.Config.enemyType)
            .OrderBy(g => g.Key.ToString());

        foreach (var group in grouped)
        {
            string key = $"enemy_{group.Key}";
            if (!foldouts.ContainsKey(key)) foldouts[key] = true;

            var first = group.First();
            foldouts[key] = EditorGUILayout.Foldout(foldouts[key],
                $"{first.Config.displayName} ({group.Count()} alive)", true, EditorStyles.foldoutHeader);

            if (foldouts[key])
            {
                EditorGUI.indentLevel++;
                var config = first.Config;

                EditorGUILayout.LabelField("Combat", EditorStyles.miniBoldLabel);
                config.maxHealth = EditorGUILayout.IntField("Max HP", config.maxHealth);
                config.damage = EditorGUILayout.IntField("Damage", config.damage);
                config.attackSpeed = EditorGUILayout.FloatField("Attack Speed", config.attackSpeed);
                config.attackRange = EditorGUILayout.FloatField("Attack Range", config.attackRange);
                config.moveSpeed = EditorGUILayout.FloatField("Move Speed", config.moveSpeed);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Behavior", EditorStyles.miniBoldLabel);
                config.detectionRange = EditorGUILayout.FloatField("Detection Range", config.detectionRange);
                config.chaseRange = EditorGUILayout.FloatField("Chase Range", config.chaseRange);
                config.fleeRange = EditorGUILayout.FloatField("Flee Range", config.fleeRange);
                config.preferredRange = EditorGUILayout.FloatField("Preferred Range", config.preferredRange);
                config.fearRange = EditorGUILayout.FloatField("Fear Range", config.fearRange);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Rewards", EditorStyles.miniBoldLabel);
                config.xpReward = EditorGUILayout.IntField("XP Reward", config.xpReward);

                if (GUI.changed)
                {
                    foreach (var enemy in group)
                    {
                        enemy.InitializeFromConfig();
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }
    }

    private void DrawEnemyConfigDefaults()
    {
        EditorGUILayout.LabelField("Default Config Values (read-only, from code)", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        // Check for EnemyConfig .asset files first
        string[] guids = AssetDatabase.FindAssets("t:EnemyConfig");
        if (guids.Length > 0)
        {
            EditorGUILayout.LabelField("EnemyConfig Assets:", EditorStyles.miniBoldLabel);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
                if (config == null) continue;

                string key = $"enemyAsset_{config.displayName}";
                if (!foldouts.ContainsKey(key)) foldouts[key] = false;

                foldouts[key] = EditorGUILayout.Foldout(foldouts[key], config.displayName, true);
                if (foldouts[key])
                {
                    EditorGUI.indentLevel++;
                    SerializedObject so = new SerializedObject(config);
                    so.Update();

                    EditorGUILayout.PropertyField(so.FindProperty("maxHealth"));
                    EditorGUILayout.PropertyField(so.FindProperty("damage"));
                    EditorGUILayout.PropertyField(so.FindProperty("attackSpeed"));
                    EditorGUILayout.PropertyField(so.FindProperty("attackRange"));
                    EditorGUILayout.PropertyField(so.FindProperty("moveSpeed"));
                    EditorGUILayout.PropertyField(so.FindProperty("behavior"));
                    EditorGUILayout.PropertyField(so.FindProperty("detectionRange"));
                    EditorGUILayout.PropertyField(so.FindProperty("chaseRange"));
                    EditorGUILayout.PropertyField(so.FindProperty("fearRange"));
                    EditorGUILayout.PropertyField(so.FindProperty("preferredRange"));
                    EditorGUILayout.PropertyField(so.FindProperty("xpReward"));

                    if (so.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(config);
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(3);
            }
            EditorGUILayout.Space(5);
        }

        // Show code defaults
        EditorGUILayout.LabelField("Code Defaults (factory methods):", EditorStyles.miniBoldLabel);
        DrawReadOnlyConfig("PawnUnarmed", EnemyConfig.CreatePawnUnarmedConfig());
        DrawReadOnlyConfig("PawnAxe", EnemyConfig.CreatePawnAxeConfig());
        DrawReadOnlyConfig("Lancer", EnemyConfig.CreateLancerConfig());
        DrawReadOnlyConfig("Archer", EnemyConfig.CreateArcherConfig());
        DrawReadOnlyConfig("Warrior", EnemyConfig.CreateWarriorConfig());
        DrawReadOnlyConfig("Monk", EnemyConfig.CreateMonkConfig());
        DrawReadOnlyConfig("Miner", EnemyConfig.CreatePawnMinerConfig());
    }

    private void DrawReadOnlyConfig(string name, EnemyConfig config)
    {
        string key = $"ro_{name}";
        if (!foldouts.ContainsKey(key)) foldouts[key] = false;
        foldouts[key] = EditorGUILayout.Foldout(foldouts[key], $"{config.displayName}", true);

        if (foldouts[key])
        {
            EditorGUI.indentLevel++;
            GUI.enabled = false;
            EditorGUILayout.IntField("Max HP", config.maxHealth);
            EditorGUILayout.IntField("Damage", config.damage);
            EditorGUILayout.FloatField("Attack Speed", config.attackSpeed);
            EditorGUILayout.FloatField("Cooldown", config.AttackCooldown);
            EditorGUILayout.FloatField("Attack Range", config.attackRange);
            EditorGUILayout.FloatField("Move Speed", config.moveSpeed);
            EditorGUILayout.EnumPopup("Behavior", config.behavior);
            EditorGUILayout.FloatField("Detection", config.detectionRange);
            EditorGUILayout.FloatField("Chase", config.chaseRange);
            EditorGUILayout.FloatField("Fear", config.fearRange);
            EditorGUILayout.FloatField("Preferred", config.preferredRange);
            EditorGUILayout.IntField("XP", config.xpReward);
            GUI.enabled = true;
            EditorGUI.indentLevel--;
            DestroyImmediate(config);
        }
        EditorGUILayout.Space(2);
    }

    #endregion

    #region Buildings Tab

    private void DrawBuildingsTab()
    {
        EditorGUILayout.LabelField("Buildings", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        // Towers
        DrawBuildingSection<Watchtower>("Towers (Enemy)");
        EditorGUILayout.Space(5);

        // Enemy Houses
        DrawBuildingSection<EnemyHouse>("Enemy Houses");
        EditorGUILayout.Space(5);

        // Castles
        DrawBuildingSection<Castle>("Castles");
        EditorGUILayout.Space(5);

        // Gold Mines
        DrawBuildingSection<GoldMine>("Gold Mines");
    }

    private void DrawBuildingSection<T>(string label) where T : MonoBehaviour
    {
        T[] buildings = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        string key = $"bldg_{label}";
        if (!foldouts.ContainsKey(key)) foldouts[key] = true;

        foldouts[key] = EditorGUILayout.Foldout(foldouts[key],
            $"{label} ({buildings.Length})", true, EditorStyles.foldoutHeader);

        if (!foldouts[key]) return;

        EditorGUI.indentLevel++;

        if (buildings.Length == 0)
        {
            EditorGUILayout.LabelField(Application.isPlaying ? "None in scene." : "Open a scene or enter Play Mode.");
            EditorGUI.indentLevel--;
            return;
        }

        foreach (var bldg in buildings)
        {
            EditorGUILayout.LabelField(bldg.name, EditorStyles.miniLabel);
            SerializedObject so = new SerializedObject(bldg);
            so.Update();

            // Show common fields based on type
            if (typeof(T) == typeof(Watchtower))
            {
                EditorGUILayout.PropertyField(so.FindProperty("maxHealth"));
                EditorGUILayout.PropertyField(so.FindProperty("damage"));
                EditorGUILayout.PropertyField(so.FindProperty("attackSpeed"));
                EditorGUILayout.PropertyField(so.FindProperty("attackRange"));
                EditorGUILayout.PropertyField(so.FindProperty("guardCount"));
                EditorGUILayout.PropertyField(so.FindProperty("baseColliderSize"));
                EditorGUILayout.PropertyField(so.FindProperty("baseColliderOffset"));
            }
            else
            {
                // Generic — show all serialized fields
                SerializedProperty prop = so.GetIterator();
                prop.NextVisible(true); // skip script
                while (prop.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(prop);
                }
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.Space(3);
        }

        EditorGUI.indentLevel--;
    }

    #endregion
}
