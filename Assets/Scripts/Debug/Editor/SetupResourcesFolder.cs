#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to setup Resources folder with sprites from Tiny Swords.
/// Run from menu: Tools > Puppet Master > Setup Resources Folder
/// Updated for GDD v5 - includes terrain, UI, and all required assets.
/// </summary>
public class SetupResourcesFolder : EditorWindow
{
    private const string RESOURCES_PATH = "Assets/Resources/Sprites";
    private const string FREE_PACK = "Assets/Sprites/Tiny Swords (Free Pack)/Tiny Swords (Free Pack)";
    private const string ENEMY_PACK = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private const string UPDATE_010 = "Assets/Sprites/Tiny Swords/Tiny Swords (Update 010)";
    private const string PIXEL_ART = "Assets/Sprites/Pixel Art Top Down - Basic v1.2.3/Texture/Extra";

    [MenuItem("Tools/Puppet Master/Setup Resources Folder")]
    public static void ShowWindow()
    {
        GetWindow<SetupResourcesFolder>("Setup Resources");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Puppet Master - Resource Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will copy sprites from Tiny Swords assets to Resources folder " +
            "so they can be loaded at runtime.\n\n" +
            "Per GDD v5:\n" +
            "- Player: Spider (Enemy Pack)\n" +
            "- Enemies: Blue Units (not Red)\n" +
            "- Army: Skull, Gnoll, Gnome, TNT, Shaman\n" +
            "- Terrain: Tilesets, Water, Foam\n" +
            "- UI: Enemy Avatars\n\n" +
            "Sprites will be copied to: Assets/Resources/Sprites/",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Setup Resources Folder", GUILayout.Height(40)))
        {
            SetupResources();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open Resources Folder", GUILayout.Height(25)))
        {
            if (Directory.Exists(RESOURCES_PATH))
            {
                EditorUtility.RevealInFinder(RESOURCES_PATH);
            }
            else
            {
                Debug.LogWarning("Resources folder doesn't exist yet. Click 'Setup Resources Folder' first.");
            }
        }
    }

    private void SetupResources()
    {
        // Create directories
        CreateDirectory($"{RESOURCES_PATH}/Player");
        CreateDirectory($"{RESOURCES_PATH}/Units");
        CreateDirectory($"{RESOURCES_PATH}/Buildings");
        CreateDirectory($"{RESOURCES_PATH}/Environment");
        CreateDirectory($"{RESOURCES_PATH}/Enemies");
        CreateDirectory($"{RESOURCES_PATH}/Resources");
        CreateDirectory($"{RESOURCES_PATH}/Terrain");
        CreateDirectory($"{RESOURCES_PATH}/UI");
        CreateDirectory($"{RESOURCES_PATH}/Projectiles");

        int copied = 0;

        // === PLAYER (Spider) - Per GDD ===
        copied += CopySprite($"{ENEMY_PACK}/Spider/Spider_Idle.png", $"{RESOURCES_PATH}/Player/Spider_Idle.png");
        copied += CopySprite($"{ENEMY_PACK}/Spider/Spider_Run.png", $"{RESOURCES_PATH}/Player/Spider_Run.png");
        copied += CopySprite($"{ENEMY_PACK}/Spider/Spider_Attack.png", $"{RESOURCES_PATH}/Player/Spider_Attack.png");

        // === ARMY UNITS (Player's Army) - Per GDD ===
        // Skull (Dead/Creepy Clown)
        copied += CopySprite($"{UPDATE_010}/Factions/Knights/Troops/Dead/Dead.png", $"{RESOURCES_PATH}/Units/Skull.png");
        // Gnoll (Bonnie)
        copied += CopySprite($"{ENEMY_PACK}/Gnoll/Gnoll_Idle.png", $"{RESOURCES_PATH}/Units/Gnoll_Idle.png");
        copied += CopySprite($"{ENEMY_PACK}/Gnoll/Gnoll_Run.png", $"{RESOURCES_PATH}/Units/Gnoll_Run.png");
        copied += CopySprite($"{ENEMY_PACK}/Gnoll/Gnoll_Attack.png", $"{RESOURCES_PATH}/Units/Gnoll_Attack.png");
        // Gnome (Foxy)
        copied += CopySprite($"{ENEMY_PACK}/Gnome/Gnome_Idle.png", $"{RESOURCES_PATH}/Units/Gnome_Idle.png");
        copied += CopySprite($"{ENEMY_PACK}/Gnome/Gnome_Run.png", $"{RESOURCES_PATH}/Units/Gnome_Run.png");
        copied += CopySprite($"{ENEMY_PACK}/Gnome/Gnome_Attack.png", $"{RESOURCES_PATH}/Units/Gnome_Attack.png");
        // TNT (Chica) - Blue TNT
        copied += CopySprite($"{UPDATE_010}/Factions/Goblins/Troops/TNT/Blue/TNT_Blue_Idle.png", $"{RESOURCES_PATH}/Units/TNT_Idle.png");
        copied += CopySprite($"{UPDATE_010}/Factions/Goblins/Troops/TNT/Blue/TNT_Blue_Run.png", $"{RESOURCES_PATH}/Units/TNT_Run.png");
        // Shaman (Marionette)
        copied += CopySprite($"{ENEMY_PACK}/Shaman/Shaman_Idle.png", $"{RESOURCES_PATH}/Units/Shaman_Idle.png");
        copied += CopySprite($"{ENEMY_PACK}/Shaman/Shaman_Run.png", $"{RESOURCES_PATH}/Units/Shaman_Run.png");
        copied += CopySprite($"{ENEMY_PACK}/Shaman/Shaman_Attack.png", $"{RESOURCES_PATH}/Units/Shaman_Attack.png");

        // === ENEMIES (Blue Units - Per GDD) ===
        // All enemies are from BLUE faction per GDD
        // Pawn (unarmed)
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle.png", $"{RESOURCES_PATH}/Enemies/Pawn_Idle.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Run.png", $"{RESOURCES_PATH}/Enemies/Pawn_Run.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Knife.png", $"{RESOURCES_PATH}/Enemies/Pawn_Idle_Knife.png");
        // Pawn (with Axe)
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Axe.png", $"{RESOURCES_PATH}/Enemies/Pawn_Idle_Axe.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Run Axe.png", $"{RESOURCES_PATH}/Enemies/Pawn_Run_Axe.png");
        // Pawn (with Pickaxe - Miner)
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Pickaxe.png", $"{RESOURCES_PATH}/Enemies/Pawn_Idle_Pickaxe.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Run Pickaxe.png", $"{RESOURCES_PATH}/Enemies/Pawn_Run_Pickaxe.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Idle Gold.png", $"{RESOURCES_PATH}/Enemies/Pawn_Idle_Gold.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Pawn/Pawn_Run Gold.png", $"{RESOURCES_PATH}/Enemies/Pawn_Run_Gold.png");
        // Lancer
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Lancer/Lancer_Idle.png", $"{RESOURCES_PATH}/Enemies/Lancer_Idle.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Lancer/Lancer_Run.png", $"{RESOURCES_PATH}/Enemies/Lancer_Run.png");
        // Archer
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Archer/Archer_Idle.png", $"{RESOURCES_PATH}/Enemies/Archer_Idle.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Archer/Archer_Run.png", $"{RESOURCES_PATH}/Enemies/Archer_Run.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Archer/Arrow.png", $"{RESOURCES_PATH}/Projectiles/Arrow.png");
        // Warrior (Knight)
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Warrior/Warrior_Idle.png", $"{RESOURCES_PATH}/Enemies/Warrior_Idle.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Warrior/Warrior_Run.png", $"{RESOURCES_PATH}/Enemies/Warrior_Run.png");
        // Monk
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Monk/Idle.png", $"{RESOURCES_PATH}/Enemies/Monk_Idle.png");
        copied += CopySprite($"{FREE_PACK}/Units/Blue Units/Monk/Run.png", $"{RESOURCES_PATH}/Enemies/Monk_Run.png");

        // === BUILDINGS - Per GDD ===
        // Enemy buildings (Red)
        copied += CopySprite($"{FREE_PACK}/Buildings/Red Buildings/House1.png", $"{RESOURCES_PATH}/Buildings/House.png");
        copied += CopySprite($"{FREE_PACK}/Buildings/Red Buildings/Tower.png", $"{RESOURCES_PATH}/Buildings/Tower.png");
        copied += CopySprite($"{FREE_PACK}/Buildings/Red Buildings/Castle.png", $"{RESOURCES_PATH}/Buildings/Castle.png");
        // Player buildings (Blue)
        copied += CopySprite($"{FREE_PACK}/Buildings/Blue Buildings/House1.png", $"{RESOURCES_PATH}/Buildings/Storage.png");
        copied += CopySprite($"{FREE_PACK}/Buildings/Blue Buildings/House2.png", $"{RESOURCES_PATH}/Buildings/Workbench.png");
        copied += CopySprite($"{FREE_PACK}/Buildings/Blue Buildings/House3.png", $"{RESOURCES_PATH}/Buildings/Farm.png");

        // === GOLD MINE - Per GDD ===
        copied += CopySprite($"{UPDATE_010}/Resources/Gold Mine/GoldMine_Active.png", $"{RESOURCES_PATH}/Buildings/GoldMine_Active.png");
        copied += CopySprite($"{UPDATE_010}/Resources/Gold Mine/GoldMine_Inactive.png", $"{RESOURCES_PATH}/Buildings/GoldMine_Inactive.png");

        // === RESOURCES - Per GDD ===
        copied += CopySprite($"{FREE_PACK}/Terrain/Resources/Meat/Meat.png", $"{RESOURCES_PATH}/Resources/Meat.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Resources/Wood/Wood Resource/Wood Resource.png", $"{RESOURCES_PATH}/Resources/Wood.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png", $"{RESOURCES_PATH}/Resources/Gold.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Resources/Gold/Gold Resource/Gold_Resource_Highlight.png", $"{RESOURCES_PATH}/Resources/Gold_Highlight.png");

        // === SHEEP (Resource source, not enemy) ===
        copied += CopySprite($"{FREE_PACK}/Terrain/Resources/Meat/Sheep/Sheep_Idle.png", $"{RESOURCES_PATH}/Environment/Sheep_Idle.png");

        // === TERRAIN - Per GDD ===
        // Free Pack terrain
        copied += CopySprite($"{FREE_PACK}/Terrain/Tileset/Tilemap_color1.png", $"{RESOURCES_PATH}/Terrain/Tilemap_color1.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Tileset/Tilemap_color2.png", $"{RESOURCES_PATH}/Terrain/Tilemap_color2.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Tileset/Water Background color.png", $"{RESOURCES_PATH}/Terrain/Water_Background.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Tileset/Water Foam.png", $"{RESOURCES_PATH}/Terrain/Water_Foam.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Tileset/Shadow.png", $"{RESOURCES_PATH}/Terrain/Shadow.png");
        // Decorations
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Bushes/Bushe1.png", $"{RESOURCES_PATH}/Environment/Bush1.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Bushes/Bushe2.png", $"{RESOURCES_PATH}/Environment/Bush2.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Rocks/Rock1.png", $"{RESOURCES_PATH}/Environment/Rock1.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Rocks/Rock2.png", $"{RESOURCES_PATH}/Environment/Rock2.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Clouds/Clouds_01.png", $"{RESOURCES_PATH}/Environment/Cloud1.png");
        copied += CopySprite($"{FREE_PACK}/Terrain/Decorations/Clouds/Clouds_02.png", $"{RESOURCES_PATH}/Environment/Cloud2.png");
        // Update 010 terrain
        copied += CopySprite($"{UPDATE_010}/Terrain/Ground/Tilemap_Flat.png", $"{RESOURCES_PATH}/Terrain/Tilemap_Flat.png");
        copied += CopySprite($"{UPDATE_010}/Terrain/Ground/Tilemap_Elevation.png", $"{RESOURCES_PATH}/Terrain/Tilemap_Elevation.png");
        copied += CopySprite($"{UPDATE_010}/Terrain/Ground/Shadows.png", $"{RESOURCES_PATH}/Terrain/Ground_Shadows.png");
        copied += CopySprite($"{UPDATE_010}/Terrain/Water/Water.png", $"{RESOURCES_PATH}/Terrain/Water.png");
        copied += CopySprite($"{UPDATE_010}/Terrain/Water/Foam/Foam.png", $"{RESOURCES_PATH}/Terrain/Foam.png");
        copied += CopySprite($"{UPDATE_010}/Terrain/Bridge/Bridge_All.png", $"{RESOURCES_PATH}/Terrain/Bridge.png");

        // === UI - Enemy Avatars for unit icons ===
        copied += CopySprite($"{ENEMY_PACK}/Enemy Avatars/Enemy Avatars.png", $"{RESOURCES_PATH}/UI/Enemy_Avatars.png");

        // Refresh asset database
        AssetDatabase.Refresh();

        Debug.Log($"[SetupResourcesFolder] Copied {copied} sprites to Resources folder!");
        EditorUtility.DisplayDialog("Setup Complete",
            $"Copied {copied} sprites to Resources folder.\n\n" +
            "Assets organized per GDD v5:\n" +
            "- Player: Spider\n" +
            "- Enemies: Blue Units\n" +
            "- Army: Skull, Gnoll, Gnome, TNT, Shaman\n" +
            "- Terrain: Tilesets, Water, Foam\n" +
            "- UI: Enemy Avatars\n\n" +
            "You can now use PrefabCreator!", "OK");
    }

    private void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"[Setup] Created directory: {path}");
        }
    }

    private int CopySprite(string source, string dest)
    {
        if (!File.Exists(source))
        {
            Debug.LogWarning($"[Setup] Source not found: {source}");
            return 0;
        }

        try
        {
            // Create destination directory if needed
            string destDir = Path.GetDirectoryName(dest);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy file
            File.Copy(source, dest, true);

            // Copy meta file if exists
            string sourceMeta = source + ".meta";
            if (File.Exists(sourceMeta))
            {
                File.Copy(sourceMeta, dest + ".meta", true);
            }

            Debug.Log($"[Setup] Copied: {Path.GetFileName(source)} -> {dest}");
            return 1;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Setup] Error copying {source}: {e.Message}");
            return 0;
        }
    }
}
#endif
