using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Sets up enemy animations from Tiny Swords Enemy Pack sprite sheets.
/// Uses Unity 6 ISpriteEditorDataProvider API for slicing.
/// Access via menu: Tools > Animation > Setup All Enemy Animations
/// </summary>
public class EnemyAnimatorSetup : EditorWindow
{
    private static readonly string SpriteSheetsBasePath = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack";
    private static readonly string AnimationsBasePath = "Assets/Animations/Enemies";

    private static readonly int DefaultFPS = 12;

    // Enemy definitions with their sprite sheets
    // Frame sizes: Gnome/Gnoll/Shaman/Lizard/Skull=192, Bear=256, Minotaur=320
    private static readonly EnemyDefinition[] Enemies = new EnemyDefinition[]
    {
        // Gnome: 192x192 frames
        new EnemyDefinition("Gnome", 192, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Gnome_Idle", "Idle", 8, true),
            new SpriteSheetConfig("Gnome_Run", "Walk", 6, true),
            new SpriteSheetConfig("Gnome_Attack", "Attack", 7, false)
        }),

        // Bear: 256x256 frames
        new EnemyDefinition("Bear", 256, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Bear_Idle", "Idle", 8, true),
            new SpriteSheetConfig("Bear_Run", "Walk", 5, true),
            new SpriteSheetConfig("Bear_Attack", "Attack", 9, false)
        }),

        // Gnoll: 192x192 frames
        new EnemyDefinition("Gnoll", 192, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Gnoll_Idle", "Idle", 6, true),
            new SpriteSheetConfig("Gnoll_Walk", "Walk", 8, true),
            new SpriteSheetConfig("Gnoll_Throw", "Attack", 8, false),
            new SpriteSheetConfig("Gnoll_Hit", "Hit", 4, false)
        }),

        // Shaman: 192x192 frames
        new EnemyDefinition("Shaman", 192, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Shaman_Idle", "Idle", 8, true),
            new SpriteSheetConfig("Shaman_Run", "Walk", 4, true),
            new SpriteSheetConfig("Shaman_Attack", "Attack", 10, false)
        }),

        // Lizard: 192x192 frames
        new EnemyDefinition("Lizard", 192, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Lizard_Idle", "Idle", 7, true),
            new SpriteSheetConfig("Lizard_Run", "Walk", 6, true),
            new SpriteSheetConfig("Lizard_Attack", "Attack", 9, false),
            new SpriteSheetConfig("Lizard_Hit", "Hit", 4, false)
        }),

        // Skull: 192x192 frames
        new EnemyDefinition("Skull", 192, new SpriteSheetConfig[]
        {
            new SpriteSheetConfig("Skull_Idle", "Idle", 8, true),
            new SpriteSheetConfig("Skull_Run", "Walk", 6, true),
            new SpriteSheetConfig("Skull_Attack", "Attack", 7, false),
            new SpriteSheetConfig("Skull_Guard", "Guard", 4, false)
        })

        // NOTE: Minotaur has non-standard sprite layout (sprites not aligned to texture edges)
        // Configure Minotaur manually via Unity Sprite Editor if needed
    };

    private struct EnemyDefinition
    {
        public string Name;
        public int FrameSize;
        public SpriteSheetConfig[] Configs;

        public EnemyDefinition(string name, int frameSize, SpriteSheetConfig[] configs)
        {
            Name = name;
            FrameSize = frameSize;
            Configs = configs;
        }
    }

    private struct SpriteSheetConfig
    {
        public string FileName;
        public string AnimationName;
        public int FrameCount;
        public bool Loop;

        public SpriteSheetConfig(string fileName, string animationName, int frameCount, bool loop)
        {
            FileName = fileName;
            AnimationName = animationName;
            FrameCount = frameCount;
            Loop = loop;
        }
    }

    [MenuItem("Tools/Animation/Setup All Enemy Animations")]
    public static void SetupAllEnemyAnimations()
    {
        // Create base animations folder
        EnsureFolderExists("Assets/Animations");
        EnsureFolderExists(AnimationsBasePath);

        int successCount = 0;
        int failCount = 0;
        int totalAnimationsCreated = 0;
        List<string> processedEnemies = new List<string>();

        foreach (var enemy in Enemies)
        {
            int animCount = SetupEnemyAnimations(enemy);
            if (animCount > 0)
            {
                successCount++;
                totalAnimationsCreated += animCount;
                processedEnemies.Add($"{enemy.Name} ({animCount} anims)");
            }
            else
            {
                failCount++;
                processedEnemies.Add($"{enemy.Name} (FAILED)");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"Setup complete!\n";
        message += $"Total animations created: {totalAnimationsCreated}\n";
        message += $"Enemies processed: {successCount}, Failed: {failCount}\n\n";
        foreach (var name in processedEnemies)
        {
            message += $"- {name}\n";
        }
        message += $"\nOutput: {AnimationsBasePath}/[EnemyName]/";
        message += $"\n\nCheck Console for detailed logs.";

        EditorUtility.DisplayDialog("Enemy Animations Setup Complete", message, "OK");
    }

    private static int SetupEnemyAnimations(EnemyDefinition enemy)
    {
        string enemySpritePath = $"{SpriteSheetsBasePath}/{enemy.Name}";
        string enemyAnimPath = $"{AnimationsBasePath}/{enemy.Name}";

        // Create enemy animations folder
        EnsureFolderExists(enemyAnimPath);

        Debug.Log($"[EnemyAnimatorSetup] Setting up {enemy.Name} animations...");

        // Step 1: Slice all sprite sheets
        foreach (var config in enemy.Configs)
        {
            SliceSpriteSheet(enemySpritePath, config.FileName, config.FrameCount, enemy.FrameSize);
        }

        // IMPORTANT: Must save and refresh before loading sliced sprites
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Force import each sprite sheet individually to ensure slices are available
        foreach (var config in enemy.Configs)
        {
            string spritePath = $"{enemySpritePath}/{config.FileName}.png";
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        }

        // Additional refresh to ensure all assets are loaded
        AssetDatabase.Refresh();

        // Step 2: Create animation clips
        List<AnimationClip> clips = new List<AnimationClip>();
        AnimationClip idleClip = null;
        AnimationClip walkClip = null;
        AnimationClip attackClip = null;

        foreach (var config in enemy.Configs)
        {
            AnimationClip clip = CreateAnimationClip(
                enemySpritePath,
                enemyAnimPath,
                enemy.Name,
                config.FileName,
                config.AnimationName,
                config.FrameCount,
                DefaultFPS,
                config.Loop
            );

            if (clip != null)
            {
                clips.Add(clip);

                // Track main clips for animator controller
                if (config.AnimationName == "Idle") idleClip = clip;
                else if (config.AnimationName == "Walk") walkClip = clip;
                else if (config.AnimationName == "Attack") attackClip = clip;
            }
        }

        if (clips.Count == 0)
        {
            Debug.LogError($"[EnemyAnimatorSetup] No clips created for {enemy.Name}");
            return 0;
        }

        // Step 3: Create Animator Controller
        CreateAnimatorController(enemyAnimPath, enemy.Name, idleClip, walkClip, attackClip, clips);

        Debug.Log($"[EnemyAnimatorSetup] Completed {enemy.Name} with {clips.Count} animations");
        return clips.Count;
    }

    private static void SliceSpriteSheet(string basePath, string fileName, int frameCount, int frameSize)
    {
        string path = $"{basePath}/{fileName}.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[EnemyAnimatorSetup] Could not find: {path}");
            return;
        }

        // Load texture to get dimensions
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning($"[EnemyAnimatorSetup] Could not load texture: {path}");
            return;
        }

        int textureWidth = tex.width;
        int textureHeight = tex.height;

        Debug.Log($"[EnemyAnimatorSetup] Processing {fileName}: texture {textureWidth}x{textureHeight}, frameSize={frameSize}, frameCount={frameCount}");

        // STEP 1: Reset to Single mode to clear ALL existing slices
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        // STEP 2: Now set to Multiple mode
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        // STEP 3: Use ISpriteEditorDataProvider to set sprite rects
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Create new sprite rects - use frameSize for both width AND height
        // For horizontal sprite strips, Y is always 0 (frames are at the bottom in Unity coordinates)
        // The frame height equals textureHeight (single row of frames)
        var spriteRects = new List<SpriteRect>();
        int frameHeight = textureHeight; // Use actual texture height for frame height

        for (int i = 0; i < frameCount; i++)
        {
            var rect = new SpriteRect();
            rect.name = $"{fileName}_{i}";
            rect.rect = new Rect(i * frameSize, 0, frameSize, frameHeight);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.alignment = SpriteAlignment.Center;
            rect.spriteID = GUID.Generate();
            spriteRects.Add(rect);

            Debug.Log($"[EnemyAnimatorSetup] Frame {i}: x={i * frameSize}, y=0, w={frameSize}, h={frameHeight}");
        }

        // Apply new sprite rects (this replaces all existing ones)
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        // Save and reimport
        var assetImporter = dataProvider.targetObject as AssetImporter;
        assetImporter.SaveAndReimport();

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[EnemyAnimatorSetup] Sliced {fileName} into {frameCount} frames ({frameSize}x{frameSize})");
    }

    private static AnimationClip CreateAnimationClip(
        string spritePath,
        string animPath,
        string enemyName,
        string spriteSheetName,
        string animName,
        int frameCount,
        int fps,
        bool loop)
    {
        string fullSpritePath = $"{spritePath}/{spriteSheetName}.png";

        // Load all sprites
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fullSpritePath);

        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[EnemyAnimatorSetup] No assets loaded from: {fullSpritePath}");
            return null;
        }

        Debug.Log($"[EnemyAnimatorSetup] Loaded {assets.Length} assets from {fullSpritePath}");

        // List all sprites found for debugging
        List<string> foundSpriteNames = new List<string>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                foundSpriteNames.Add(s.name);
            }
        }
        Debug.Log($"[EnemyAnimatorSetup] Found sprites: {string.Join(", ", foundSpriteNames)}");

        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < frameCount; i++)
        {
            string spriteName = $"{spriteSheetName}_{i}";
            bool found = false;
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    sprites.Add(sprite);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.LogWarning($"[EnemyAnimatorSetup] Sprite not found: {spriteName}");
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[EnemyAnimatorSetup] No sprites matched for {spriteSheetName}. Expected names like {spriteSheetName}_0, {spriteSheetName}_1, etc.");
            return null;
        }

        // Create animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = $"{enemyName}_{animName}";
        clip.frameRate = fps;

        // Create keyframes
        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)fps,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Set loop mode
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Save clip
        string clipPath = $"{animPath}/{enemyName}_{animName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
        {
            AssetDatabase.DeleteAsset(clipPath);
        }
        AssetDatabase.CreateAsset(clip, clipPath);

        Debug.Log($"[EnemyAnimatorSetup] Created {enemyName}_{animName} animation with {sprites.Count} frames at {fps} FPS");

        return clip;
    }

    private static void CreateAnimatorController(
        string animPath,
        string enemyName,
        AnimationClip idleClip,
        AnimationClip walkClip,
        AnimationClip attackClip,
        List<AnimationClip> allClips)
    {
        string controllerPath = $"{animPath}/{enemyName}Animator.controller";

        // Delete old controller
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        // Get state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Create main states
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState attackState = null;

        float yPos = 100;

        if (idleClip != null)
        {
            idleState = rootStateMachine.AddState("Idle", new Vector3(300, yPos, 0));
            idleState.motion = idleClip;
            yPos += 100;
        }

        if (walkClip != null)
        {
            walkState = rootStateMachine.AddState("Walk", new Vector3(300, yPos, 0));
            walkState.motion = walkClip;
            yPos += 100;
        }

        if (attackClip != null)
        {
            attackState = rootStateMachine.AddState("Attack", new Vector3(500, 150, 0));
            attackState.motion = attackClip;
        }

        // Add any additional clips as states
        foreach (var clip in allClips)
        {
            if (clip == idleClip || clip == walkClip || clip == attackClip)
                continue;

            AnimatorState extraState = rootStateMachine.AddState(clip.name.Replace($"{enemyName}_", ""), new Vector3(500, yPos, 0));
            extraState.motion = clip;
            yPos += 100;
        }

        // Set default state
        if (idleState != null)
        {
            rootStateMachine.defaultState = idleState;
        }

        // Create transitions
        if (idleState != null && walkState != null)
        {
            // Idle -> Walk
            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toWalk.hasExitTime = false;
            toWalk.duration = 0;

            // Walk -> Idle
            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            toIdle.hasExitTime = false;
            toIdle.duration = 0;
        }

        if (attackState != null)
        {
            // Idle -> Attack
            if (idleState != null)
            {
                AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
                idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                idleToAttack.hasExitTime = false;
                idleToAttack.duration = 0;
            }

            // Walk -> Attack
            if (walkState != null)
            {
                AnimatorStateTransition walkToAttack = walkState.AddTransition(attackState);
                walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                walkToAttack.hasExitTime = false;
                walkToAttack.duration = 0;
            }

            // Attack -> Idle (after animation finishes)
            if (idleState != null)
            {
                AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
                attackToIdle.hasExitTime = true;
                attackToIdle.exitTime = 1f;
                attackToIdle.duration = 0;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyAnimatorSetup] Created {enemyName}Animator.controller");
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolderExists(parentPath);
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
        Debug.Log($"[EnemyAnimatorSetup] Created folder: {path}");
    }
}
