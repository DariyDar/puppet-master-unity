using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor script for setting up animations for all Red Unit types from Tiny Swords asset pack.
/// Uses Unity 6 ISpriteEditorDataProvider API for sprite slicing.
/// </summary>
public class UnitAnimatorSetup : EditorWindow
{
    private const int PPU = 16;
    private const float FRAME_RATE = 12f;

    private static readonly string RedUnitsPath = "Assets/Sprites/Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Units/Red Units";
    private static readonly string AnimationsOutputPath = "Assets/Animations/Units";

    /// <summary>
    /// Defines a sprite sheet with its properties for animation creation
    /// </summary>
    private class SpriteSheetDefinition
    {
        public string FileName;
        public string AnimationName;
        public int FrameWidth;
        public int FrameHeight;
        public int FrameCount;
        public bool IsLooping;

        public SpriteSheetDefinition(string fileName, string animName, int frameWidth, int frameHeight, int frameCount, bool isLooping = true)
        {
            FileName = fileName;
            AnimationName = animName;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameCount = frameCount;
            IsLooping = isLooping;
        }
    }

    /// <summary>
    /// Defines a unit type with all its sprite sheets
    /// </summary>
    private class UnitDefinition
    {
        public string UnitName;
        public string FolderName;
        public List<SpriteSheetDefinition> SpriteSheets;
        public string DefaultState;

        public UnitDefinition(string unitName, string folderName, string defaultState)
        {
            UnitName = unitName;
            FolderName = folderName;
            DefaultState = defaultState;
            SpriteSheets = new List<SpriteSheetDefinition>();
        }
    }

    [MenuItem("Tools/Animation/Setup All Unit Animations")]
    public static void SetupAllUnitAnimations()
    {
        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(AnimationsOutputPath))
        {
            CreateFolderRecursive(AnimationsOutputPath);
        }

        // Define all units and their sprite sheets
        List<UnitDefinition> units = GetUnitDefinitions();

        int totalProcessed = 0;
        int totalFailed = 0;

        foreach (var unit in units)
        {
            Debug.Log($"Processing unit: {unit.UnitName}");

            // Create unit-specific animation folder
            string unitAnimFolder = $"{AnimationsOutputPath}/{unit.UnitName}";
            if (!AssetDatabase.IsValidFolder(unitAnimFolder))
            {
                AssetDatabase.CreateFolder(AnimationsOutputPath, unit.UnitName);
            }

            List<AnimationClip> clips = new List<AnimationClip>();

            foreach (var sheet in unit.SpriteSheets)
            {
                string spritePath = $"{RedUnitsPath}/{unit.FolderName}/{sheet.FileName}";

                // Use AssetDatabase to check if file exists (more reliable than File.Exists)
                TextureImporter testImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (testImporter == null)
                {
                    Debug.LogWarning($"Sprite sheet not found: {spritePath}");
                    totalFailed++;
                    continue;
                }

                // Configure and slice sprite sheet
                ConfigureSpriteSheet(spritePath, sheet.FrameWidth, sheet.FrameHeight, sheet.FrameCount);

                // Force refresh to ensure sliced sprites are available
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                // Create animation clip
                AnimationClip clip = CreateAnimationClip(spritePath, sheet, unitAnimFolder, unit.UnitName);
                if (clip != null)
                {
                    clips.Add(clip);
                    totalProcessed++;
                }
                else
                {
                    totalFailed++;
                }
            }

            // Create animator controller for this unit
            if (clips.Count > 0)
            {
                CreateAnimatorController(unit, clips, unitAnimFolder);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Unit Animation Setup Complete! Processed: {totalProcessed}, Failed: {totalFailed}");
        EditorUtility.DisplayDialog("Unit Animation Setup",
            $"Setup complete!\n\nProcessed: {totalProcessed} animations\nFailed: {totalFailed}", "OK");
    }

    private static List<UnitDefinition> GetUnitDefinitions()
    {
        List<UnitDefinition> units = new List<UnitDefinition>();

        // PAWN - Worker unit
        var pawn = new UnitDefinition("Pawn", "Pawn", "Idle");
        pawn.SpriteSheets.Add(new SpriteSheetDefinition("Pawn_Idle.png", "Idle", 192, 192, 8, true));
        pawn.SpriteSheets.Add(new SpriteSheetDefinition("Pawn_Run.png", "Run", 192, 192, 6, true));
        pawn.SpriteSheets.Add(new SpriteSheetDefinition("Pawn_Interact Axe.png", "Attack", 192, 192, 6, false));
        units.Add(pawn);

        // WARRIOR - Melee fighter with sword and shield
        var warrior = new UnitDefinition("Warrior", "Warrior", "Idle");
        warrior.SpriteSheets.Add(new SpriteSheetDefinition("Warrior_Idle.png", "Idle", 192, 192, 8, true));
        warrior.SpriteSheets.Add(new SpriteSheetDefinition("Warrior_Run.png", "Run", 192, 192, 6, true));
        warrior.SpriteSheets.Add(new SpriteSheetDefinition("Warrior_Attack1.png", "Attack", 192, 192, 4, false));
        units.Add(warrior);

        // ARCHER - Ranged unit with bow
        var archer = new UnitDefinition("Archer", "Archer", "Idle");
        archer.SpriteSheets.Add(new SpriteSheetDefinition("Archer_Idle.png", "Idle", 192, 192, 6, true));
        archer.SpriteSheets.Add(new SpriteSheetDefinition("Archer_Run.png", "Run", 192, 192, 4, true));
        archer.SpriteSheets.Add(new SpriteSheetDefinition("Archer_Shoot.png", "Attack", 192, 192, 8, false));
        units.Add(archer);

        // LANCER - Spear unit
        var lancer = new UnitDefinition("Lancer", "Lancer", "Idle");
        lancer.SpriteSheets.Add(new SpriteSheetDefinition("Lancer_Idle.png", "Idle", 128, 128, 12, true));
        lancer.SpriteSheets.Add(new SpriteSheetDefinition("Lancer_Run.png", "Run", 192, 192, 6, true));
        lancer.SpriteSheets.Add(new SpriteSheetDefinition("Lancer_Right_Attack.png", "Attack", 256, 192, 3, false));
        units.Add(lancer);

        // MONK - Healer unit
        var monk = new UnitDefinition("Monk", "Monk", "Idle");
        monk.SpriteSheets.Add(new SpriteSheetDefinition("Idle.png", "Idle", 192, 192, 6, true));
        monk.SpriteSheets.Add(new SpriteSheetDefinition("Run.png", "Run", 192, 192, 4, true));
        monk.SpriteSheets.Add(new SpriteSheetDefinition("Heal.png", "Attack", 192, 192, 12, false));
        units.Add(monk);

        return units;
    }

    private static void ConfigureSpriteSheet(string assetPath, int frameWidth, int frameHeight, int frameCount)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Failed to get TextureImporter for: {assetPath}");
            return;
        }

        // Configure for pixel art
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        // Apply initial settings
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // Use ISpriteEditorDataProvider API for slicing (Unity 6 approach)
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);

        if (dataProvider == null)
        {
            Debug.LogError($"Failed to get ISpriteEditorDataProvider for: {assetPath}");
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();

        // Get texture dimensions
        var textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
        int textureWidth, textureHeight;
        textureProvider.GetTextureActualWidthAndHeight(out textureWidth, out textureHeight);

        // Create sprite rects for each frame
        var spriteRects = new List<SpriteRect>();
        string baseName = Path.GetFileNameWithoutExtension(assetPath);

        for (int i = 0; i < frameCount; i++)
        {
            int x = i * frameWidth;
            int y = textureHeight - frameHeight; // Unity uses bottom-left origin

            // Ensure we don't go out of bounds
            if (x + frameWidth > textureWidth)
            {
                Debug.LogWarning($"Frame {i} exceeds texture width for {assetPath}");
                break;
            }

            var spriteRect = new SpriteRect
            {
                name = $"{baseName}_{i}",
                rect = new Rect(x, y, frameWidth, frameHeight),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
            spriteRects.Add(spriteRect);
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        // Reimport to apply changes
        var assetImporterInstance = dataProvider.targetObject as AssetImporter;
        if (assetImporterInstance != null)
        {
            assetImporterInstance.SaveAndReimport();
        }

        Debug.Log($"Sliced {assetPath} into {spriteRects.Count} frames ({frameWidth}x{frameHeight})");
    }

    private static AnimationClip CreateAnimationClip(string spritePath, SpriteSheetDefinition sheet, string outputFolder, string unitName)
    {
        // Load all sprites from the sprite sheet
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        var spriteList = sprites.OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToList();

        if (spriteList.Count == 0)
        {
            Debug.LogError($"No sprites found in: {spritePath}");
            return null;
        }

        // Create animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = $"{unitName}_{sheet.AnimationName}";
        clip.frameRate = FRAME_RATE;

        // Create keyframes
        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteList.Count];
        float frameTime = 1f / FRAME_RATE;

        for (int i = 0; i < spriteList.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = spriteList[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Set loop time
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = sheet.IsLooping;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Save the clip
        string clipPath = $"{outputFolder}/{clip.name}.anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        Debug.Log($"Created animation clip: {clipPath}");

        return clip;
    }

    private static void CreateAnimatorController(UnitDefinition unit, List<AnimationClip> clips, string outputFolder)
    {
        string controllerPath = $"{outputFolder}/{unit.UnitName}_Animator.controller";

        // Create animator controller
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Add parameters for transitions
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        // Add states for each animation
        Dictionary<string, UnityEditor.Animations.AnimatorState> states = new Dictionary<string, UnityEditor.Animations.AnimatorState>();

        foreach (var clip in clips)
        {
            string stateName = clip.name.Replace($"{unit.UnitName}_", "");
            var state = rootStateMachine.AddState(stateName);
            state.motion = clip;
            states[stateName] = state;

            // Set default state
            if (stateName == unit.DefaultState)
            {
                rootStateMachine.defaultState = state;
            }
        }

        // Create transitions
        if (states.ContainsKey("Idle") && states.ContainsKey("Run"))
        {
            // Idle -> Run (when IsMoving = true)
            var idleToRun = states["Idle"].AddTransition(states["Run"]);
            idleToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;

            // Run -> Idle (when IsMoving = false)
            var runToIdle = states["Run"].AddTransition(states["Idle"]);
            runToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.1f;
        }

        if (states.ContainsKey("Idle") && states.ContainsKey("Attack"))
        {
            // Idle -> Attack (when Attack trigger)
            var idleToAttack = states["Idle"].AddTransition(states["Attack"]);
            idleToAttack.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
            idleToAttack.hasExitTime = false;
            idleToAttack.duration = 0.05f;

            // Attack -> Idle (exit time)
            var attackToIdle = states["Attack"].AddTransition(states["Idle"]);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0.1f;
        }

        if (states.ContainsKey("Run") && states.ContainsKey("Attack"))
        {
            // Run -> Attack (when Attack trigger)
            var runToAttack = states["Run"].AddTransition(states["Attack"]);
            runToAttack.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
            runToAttack.hasExitTime = false;
            runToAttack.duration = 0.05f;
        }

        EditorUtility.SetDirty(controller);
        Debug.Log($"Created animator controller: {controllerPath}");
    }

    private static void CreateFolderRecursive(string folderPath)
    {
        string[] folders = folderPath.Split('/');
        string currentPath = folders[0];

        for (int i = 1; i < folders.Length; i++)
        {
            string parentPath = currentPath;
            currentPath = $"{currentPath}/{folders[i]}";

            if (!AssetDatabase.IsValidFolder(currentPath))
            {
                AssetDatabase.CreateFolder(parentPath, folders[i]);
            }
        }
    }

    [MenuItem("Tools/Animation/Setup Pawn Animations")]
    public static void SetupPawnAnimations()
    {
        SetupSingleUnit("Pawn");
    }

    [MenuItem("Tools/Animation/Setup Warrior Animations")]
    public static void SetupWarriorAnimations()
    {
        SetupSingleUnit("Warrior");
    }

    [MenuItem("Tools/Animation/Setup Archer Animations")]
    public static void SetupArcherAnimations()
    {
        SetupSingleUnit("Archer");
    }

    [MenuItem("Tools/Animation/Setup Lancer Animations")]
    public static void SetupLancerAnimations()
    {
        SetupSingleUnit("Lancer");
    }

    [MenuItem("Tools/Animation/Setup Monk Animations")]
    public static void SetupMonkAnimations()
    {
        SetupSingleUnit("Monk");
    }

    private static void SetupSingleUnit(string unitName)
    {
        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(AnimationsOutputPath))
        {
            CreateFolderRecursive(AnimationsOutputPath);
        }

        var units = GetUnitDefinitions();
        var unit = units.Find(u => u.UnitName == unitName);

        if (unit == null)
        {
            Debug.LogError($"Unit not found: {unitName}");
            return;
        }

        // Create unit-specific animation folder
        string unitAnimFolder = $"{AnimationsOutputPath}/{unit.UnitName}";
        if (!AssetDatabase.IsValidFolder(unitAnimFolder))
        {
            AssetDatabase.CreateFolder(AnimationsOutputPath, unit.UnitName);
        }

        List<AnimationClip> clips = new List<AnimationClip>();
        int processed = 0;
        int failed = 0;

        foreach (var sheet in unit.SpriteSheets)
        {
            string spritePath = $"{RedUnitsPath}/{unit.FolderName}/{sheet.FileName}";

            // Use AssetDatabase to check if file exists
            TextureImporter testImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (testImporter == null)
            {
                Debug.LogWarning($"Sprite sheet not found: {spritePath}");
                failed++;
                continue;
            }

            ConfigureSpriteSheet(spritePath, sheet.FrameWidth, sheet.FrameHeight, sheet.FrameCount);

            // Force refresh to ensure sliced sprites are available
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            AnimationClip clip = CreateAnimationClip(spritePath, sheet, unitAnimFolder, unit.UnitName);
            if (clip != null)
            {
                clips.Add(clip);
                processed++;
            }
            else
            {
                failed++;
            }
        }

        if (clips.Count > 0)
        {
            CreateAnimatorController(unit, clips, unitAnimFolder);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"{unitName} Animation Setup Complete! Processed: {processed}, Failed: {failed}");
        EditorUtility.DisplayDialog($"{unitName} Animation Setup",
            $"Setup complete!\n\nProcessed: {processed} animations\nFailed: {failed}", "OK");
    }
}
