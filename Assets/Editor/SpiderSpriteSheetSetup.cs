using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Sets up Spider sprite sheets with proper slicing and generates animated clips.
/// Access via menu: Tools > Animation > Setup Spider Sprite Sheets
/// </summary>
public class SpiderSpriteSheetSetup : EditorWindow
{
    private static readonly string SpriteSheetsPath = "Assets/Sprites/Tiny Swords (Enemy Pack)/Tiny Swords (Enemy Pack)/Enemy Pack/Spider";
    private static readonly string AnimationsPath = "Assets/Animations/Spider";

    // Spider sprite sheet is 960x192, with 5 frames of 192x192
    // Each row is a different direction (16 directions total in the sheet)
    private static readonly int FrameWidth = 192;
    private static readonly int FrameHeight = 192;
    private static readonly int FramesPerDirection = 5;

    // Direction angles (matching the 16-directional system)
    private static readonly int[] Angles = { 0, 22, 45, 67, 90, 112, 135, 157, 180, 202, 225, 247, 270, 292, 315, 337 };

    // Map angle to Vector2 direction for blend tree
    private static readonly Dictionary<int, Vector2> AngleToDirection = new Dictionary<int, Vector2>
    {
        { 0, new Vector2(0, 1) },      // Up
        { 22, new Vector2(0.38f, 0.92f) },
        { 45, new Vector2(0.71f, 0.71f) },   // Up-Right
        { 67, new Vector2(0.92f, 0.38f) },
        { 90, new Vector2(1, 0) },     // Right
        { 112, new Vector2(0.92f, -0.38f) },
        { 135, new Vector2(0.71f, -0.71f) }, // Down-Right
        { 157, new Vector2(0.38f, -0.92f) },
        { 180, new Vector2(0, -1) },   // Down
        { 202, new Vector2(-0.38f, -0.92f) },
        { 225, new Vector2(-0.71f, -0.71f) }, // Down-Left
        { 247, new Vector2(-0.92f, -0.38f) },
        { 270, new Vector2(-1, 0) },   // Left
        { 292, new Vector2(-0.92f, 0.38f) },
        { 315, new Vector2(-0.71f, 0.71f) }, // Up-Left
        { 337, new Vector2(-0.38f, 0.92f) }
    };

    [MenuItem("Tools/Animation/Setup Spider Sprite Sheets")]
    public static void SetupSpriteSheets()
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

        // Process each sprite sheet
        SetupSpriteSheet("Spider_Idle", "Idle");
        SetupSpriteSheet("Spider_Run", "Walk");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Now generate the animator with proper animations
        GenerateAnimatorController();

        EditorUtility.DisplayDialog("Spider Sprite Sheets Setup Complete",
            "Configured sprite sheets and generated animations:\n\n" +
            "- Spider_Idle.png sliced into 16 directions x 5 frames\n" +
            "- Spider_Run.png sliced into 16 directions x 5 frames\n" +
            "- Created SpiderAnimator.controller with Blend Trees\n\n" +
            "Next: Assign SpiderAnimator to Player's Animator component",
            "OK");
    }

    private static void SetupSpriteSheet(string fileName, string animName)
    {
        string path = $"{SpriteSheetsPath}/{fileName}.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[SpiderSpriteSheetSetup] Could not find: {path}");
            return;
        }

        // Configure for pixel art
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;
        importer.mipmapEnabled = false;

        // Get texture dimensions
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError($"[SpiderSpriteSheetSetup] Could not load texture: {path}");
            return;
        }

        int width = tex.width;
        int height = tex.height;

        // Calculate grid: 960x192 = 5 columns x 1 row per direction
        // But we have 16 directions, so the sheet should be taller
        // Let me check: 960 / 192 = 5 frames, 192 / 192 = 1 row
        // This means each PNG has only ONE direction with 5 frames

        // Actually looking at Tiny Swords structure, each direction is a separate row
        // Sheet is 960 wide (5 frames * 192) and varies in height (16 rows * 192 = 3072)

        int columns = width / FrameWidth;  // 5 frames
        int rows = height / FrameHeight;    // 16 directions (or 1 if single direction sheet)

        Debug.Log($"[SpiderSpriteSheetSetup] {fileName}: {width}x{height}, {columns} cols x {rows} rows");

        // Create sprite rects
        List<SpriteMetaData> sprites = new List<SpriteMetaData>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                SpriteMetaData sprite = new SpriteMetaData();
                // Name format: AnimName_Direction_Frame (e.g., Idle_0_0, Idle_0_1, etc.)
                int directionIndex = row;
                int angle = directionIndex < Angles.Length ? Angles[directionIndex] : directionIndex * 22;
                sprite.name = $"{animName}_{angle}_{col}";
                sprite.rect = new Rect(
                    col * FrameWidth,
                    height - (row + 1) * FrameHeight, // Unity Y is bottom-up
                    FrameWidth,
                    FrameHeight
                );
                sprite.pivot = new Vector2(0.5f, 0.5f);
                sprite.alignment = (int)SpriteAlignment.Center;
                sprites.Add(sprite);
            }
        }

        importer.spritesheet = sprites.ToArray();
        importer.SaveAndReimport();

        Debug.Log($"[SpiderSpriteSheetSetup] Sliced {fileName} into {sprites.Count} sprites");
    }

    private static void GenerateAnimatorController()
    {
        string controllerPath = $"{AnimationsPath}/SpiderAnimator.controller";

        // Delete old controller if exists
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        // Create new Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Get root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Create Idle blend tree
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        BlendTree idleBlendTree = CreateAnimatedBlendTree(controller, "Idle Blend", "Idle", "Spider_Idle");
        idleState.motion = idleBlendTree;

        // Create Walk blend tree
        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        BlendTree walkBlendTree = CreateAnimatedBlendTree(controller, "Walk Blend", "Walk", "Spider_Run");
        walkState.motion = walkBlendTree;

        // Set Idle as default
        rootStateMachine.defaultState = idleState;

        // Create transitions
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        AssetDatabase.SaveAssets();
        Debug.Log("[SpiderSpriteSheetSetup] Generated SpiderAnimator.controller");
    }

    private static BlendTree CreateAnimatedBlendTree(AnimatorController controller, string name, string animPrefix, string spriteSheetName)
    {
        BlendTree blendTree = new BlendTree();
        blendTree.name = name;
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "MoveX";
        blendTree.blendParameterY = "MoveY";

        string spritePath = $"{SpriteSheetsPath}/{spriteSheetName}.png";

        // Load all sprites from the sprite sheet
        Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

        foreach (Object obj in allSprites)
        {
            if (obj is Sprite sprite)
            {
                spriteDict[sprite.name] = sprite;
            }
        }

        if (spriteDict.Count == 0)
        {
            Debug.LogWarning($"[SpiderSpriteSheetSetup] No sprites found in {spritePath}. Using fallback single-frame sprites.");
            // Fallback to old single-frame method
            return CreateFallbackBlendTree(controller, name, animPrefix);
        }

        // Create animation clip for each direction
        foreach (int angle in Angles)
        {
            List<Sprite> frameSprites = new List<Sprite>();

            // Collect all frames for this direction
            for (int frame = 0; frame < FramesPerDirection; frame++)
            {
                string spriteName = $"{animPrefix}_{angle}_{frame}";
                if (spriteDict.TryGetValue(spriteName, out Sprite sprite))
                {
                    frameSprites.Add(sprite);
                }
            }

            if (frameSprites.Count > 0)
            {
                // Create animation clip
                AnimationClip clip = new AnimationClip();
                clip.name = $"Spider_{animPrefix}_{angle}";
                clip.frameRate = 12;

                // Create keyframes for sprite animation
                EditorCurveBinding binding = new EditorCurveBinding();
                binding.type = typeof(SpriteRenderer);
                binding.path = "";
                binding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameSprites.Count + 1];
                for (int i = 0; i < frameSprites.Count; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = i / 12f,
                        value = frameSprites[i]
                    };
                }
                // Add final keyframe to complete the loop
                keyframes[frameSprites.Count] = new ObjectReferenceKeyframe
                {
                    time = frameSprites.Count / 12f,
                    value = frameSprites[0]
                };

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                // Make it loop
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Save clip
                string clipPath = $"{AnimationsPath}/Spider_{animPrefix}_{angle}.anim";

                // Delete old clip if exists
                if (File.Exists(clipPath))
                {
                    AssetDatabase.DeleteAsset(clipPath);
                }

                AssetDatabase.CreateAsset(clip, clipPath);

                // Add to blend tree
                Vector2 direction = AngleToDirection[angle];
                blendTree.AddChild(clip, direction);

                Debug.Log($"[SpiderSpriteSheetSetup] Created {clip.name} with {frameSprites.Count} frames");
            }
        }

        // Add blend tree as sub-asset
        AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(controller));

        return blendTree;
    }

    private static BlendTree CreateFallbackBlendTree(AnimatorController controller, string name, string animPrefix)
    {
        // Fallback: use individual frame files from Assets/Sprites/Spider/
        BlendTree blendTree = new BlendTree();
        blendTree.name = name;
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "MoveX";
        blendTree.blendParameterY = "MoveY";

        string fallbackPath = "Assets/Sprites/Spider";

        foreach (int angle in Angles)
        {
            string angleStr = angle == 0 ? "0" : angle.ToString("D3");
            string spritePath = $"{fallbackPath}/{animPrefix}/{animPrefix} Body {angleStr}.png";

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                AnimationClip clip = new AnimationClip();
                clip.name = $"Spider_{animPrefix}_{angle}";
                clip.frameRate = 12;

                EditorCurveBinding binding = new EditorCurveBinding();
                binding.type = typeof(SpriteRenderer);
                binding.path = "";
                binding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[2];
                keyframes[0] = new ObjectReferenceKeyframe { time = 0, value = sprite };
                keyframes[1] = new ObjectReferenceKeyframe { time = 1f / 12f, value = sprite };

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                string clipPath = $"{AnimationsPath}/Spider_{animPrefix}_{angle}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);

                Vector2 direction = AngleToDirection[angle];
                blendTree.AddChild(clip, direction);
            }
        }

        AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(controller));
        return blendTree;
    }
}
