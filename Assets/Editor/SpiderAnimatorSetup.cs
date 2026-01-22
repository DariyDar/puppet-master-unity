using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Sets up Spider animations using sprite sheets.
/// Uses 4 or 8 direction approach with sprite flipping for efficiency.
/// Access via menu: Tools > Animation > Setup Spider Animations (Sprite Sheet)
/// </summary>
public class SpiderAnimatorSetup : EditorWindow
{
    private static readonly string SpriteSheetsPath = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Spider";
    private static readonly string AnimationsPath = "Assets/Animations/Spider";

    // Spider_Idle.png: 1536x192 = 8 frames of 192x192
    // Spider_Run.png: 960x192 = 5 frames of 192x192
    private static readonly int FrameSize = 192;

    [MenuItem("Tools/Animation/Setup Spider Animations (Sprite Sheet)")]
    public static void SetupAnimations()
    {
        // Create animations folder
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(AnimationsPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Spider");
        }

        // Step 1: Slice sprite sheets
        SliceSpriteSheet("Spider_Idle", 8);
        SliceSpriteSheet("Spider_Run", 5);
        SliceSpriteSheet("Spider_Attack", 8);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 2: Create animation clips
        AnimationClip idleClip = CreateAnimationClip("Spider_Idle", "Idle", 8, 10, true);
        AnimationClip walkClip = CreateAnimationClip("Spider_Run", "Walk", 5, 12, true);
        AnimationClip attackClip = CreateAnimationClip("Spider_Attack", "Attack", 8, 12, false);

        // Step 3: Create Animator Controller
        CreateAnimatorController(idleClip, walkClip, attackClip);

        EditorUtility.DisplayDialog("Spider Animations Setup Complete",
            "Created animations from sprite sheets:\n\n" +
            "- Idle animation (8 frames, looping)\n" +
            "- Walk animation (5 frames, looping)\n" +
            "- Attack animation (8 frames, plays once)\n" +
            "- SpiderAnimator.controller\n\n" +
            "Note: This version uses single-direction sprites.\n" +
            "The PlayerController will handle sprite flipping based on direction.\n\n" +
            "Next: Assign SpiderAnimator to Player's Animator component",
            "OK");
    }

    private static void SliceSpriteSheet(string fileName, int frameCount)
    {
        string path = $"{SpriteSheetsPath}/{fileName}.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[SpiderAnimatorSetup] Could not find: {path}");
            return;
        }

        // Configure for pixel art
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;
        importer.mipmapEnabled = false;

        // Create sprite rects
        List<SpriteMetaData> sprites = new List<SpriteMetaData>();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int width = tex.width;
        int height = tex.height;

        for (int i = 0; i < frameCount; i++)
        {
            SpriteMetaData sprite = new SpriteMetaData();
            sprite.name = $"{fileName}_{i}";
            sprite.rect = new Rect(i * FrameSize, 0, FrameSize, height);
            sprite.pivot = new Vector2(0.5f, 0.5f);
            sprite.alignment = (int)SpriteAlignment.Center;
            sprites.Add(sprite);
        }

        importer.spritesheet = sprites.ToArray();
        importer.SaveAndReimport();

        Debug.Log($"[SpiderAnimatorSetup] Sliced {fileName} into {frameCount} frames");
    }

    private static AnimationClip CreateAnimationClip(string spriteSheetName, string animName, int frameCount, int fps, bool loop)
    {
        string spritePath = $"{SpriteSheetsPath}/{spriteSheetName}.png";

        // Load all sprites
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < frameCount; i++)
        {
            string spriteName = $"{spriteSheetName}_{i}";
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    sprites.Add(sprite);
                    break;
                }
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogError($"[SpiderAnimatorSetup] No sprites found for {spriteSheetName}");
            return null;
        }

        // Create animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = $"Spider_{animName}";
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
        string clipPath = $"{AnimationsPath}/Spider_{animName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
        {
            AssetDatabase.DeleteAsset(clipPath);
        }
        AssetDatabase.CreateAsset(clip, clipPath);

        Debug.Log($"[SpiderAnimatorSetup] Created {animName} animation with {sprites.Count} frames at {fps} FPS");

        return clip;
    }

    private static void CreateAnimatorController(AnimationClip idleClip, AnimationClip walkClip, AnimationClip attackClip)
    {
        string controllerPath = $"{AnimationsPath}/SpiderAnimator.controller";

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

        // Create states
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        idleState.motion = idleClip;

        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        walkState.motion = walkClip;

        AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(500, 150, 0));
        attackState.motion = attackClip;

        // Set default
        rootStateMachine.defaultState = idleState;

        // Idle <-> Walk transitions
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0;

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0;

        // Attack transitions (from any state)
        AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0;

        AnimatorStateTransition walkToAttack = walkState.AddTransition(attackState);
        walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        walkToAttack.hasExitTime = false;
        walkToAttack.duration = 0;

        // Attack -> Idle (after animation finishes)
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0;

        AssetDatabase.SaveAssets();
        Debug.Log("[SpiderAnimatorSetup] Created SpiderAnimator.controller with Attack state");
    }
}
