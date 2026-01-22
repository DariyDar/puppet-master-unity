using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates Animator Controller for Spider with 16-directional Blend Tree.
/// Access via menu: Tools > Animation > Generate Spider Animator
/// </summary>
public class SpiderAnimatorGenerator : EditorWindow
{
    // Direction angles matching Tiny Swords sprite naming
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

    [MenuItem("Tools/Animation/Generate Spider Animator")]
    public static void GenerateSpiderAnimator()
    {
        string spritesPath = "Assets/Sprites/Spider";
        string animationsPath = "Assets/Animations/Spider";
        string controllerPath = "Assets/Animations/Spider/SpiderAnimator.controller";

        // Create animations folder
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(animationsPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Spider");
        }

        // Create Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Get root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Create Idle blend tree state
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        BlendTree idleBlendTree = CreateDirectionalBlendTree(controller, "Idle Blend", spritesPath, "Idle", animationsPath);
        idleState.motion = idleBlendTree;

        // Create Walk blend tree state
        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        BlendTree walkBlendTree = CreateDirectionalBlendTree(controller, "Walk Blend", spritesPath, "Walk", animationsPath);
        walkState.motion = walkBlendTree;

        // Set Idle as default state
        rootStateMachine.defaultState = idleState;

        // Create transitions
        // Idle -> Walk (when IsMoving becomes true)
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;

        // Walk -> Idle (when IsMoving becomes false)
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Spider Animator Generated",
            "Created SpiderAnimator.controller with:\n" +
            "- Idle state (16-directional blend tree)\n" +
            "- Walk state (16-directional blend tree)\n" +
            "- Automatic transitions based on IsMoving\n\n" +
            "Next steps:\n" +
            "1. Select Player in scene\n" +
            "2. Add Animator component\n" +
            "3. Assign SpiderAnimator controller",
            "OK");

        Debug.Log("[SpiderAnimatorGenerator] Created Spider Animator Controller with 16-directional blend trees.");
    }

    private static BlendTree CreateDirectionalBlendTree(AnimatorController controller, string name, string spritesPath, string animType, string animationsPath)
    {
        BlendTree blendTree = new BlendTree();
        blendTree.name = name;
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "MoveX";
        blendTree.blendParameterY = "MoveY";

        // Create animation clips for each direction
        foreach (int angle in Angles)
        {
            // File naming: "0" for angle 0, "022", "045" etc for others
            string angleStr = angle == 0 ? "0" : angle.ToString("D3");
            string spriteFile = $"{animType} Body {angleStr}.png";
            string spritePath = $"{spritesPath}/{animType}/{spriteFile}";

            // Load sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite != null)
            {
                // Create animation clip with single frame (for now)
                AnimationClip clip = new AnimationClip();
                clip.name = $"Spider_{animType}_{angle}";
                clip.frameRate = 12;

                // Create keyframe curve for sprite
                EditorCurveBinding binding = new EditorCurveBinding();
                binding.type = typeof(SpriteRenderer);
                binding.path = "";
                binding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[2];
                keyframes[0] = new ObjectReferenceKeyframe { time = 0, value = sprite };
                keyframes[1] = new ObjectReferenceKeyframe { time = 1f / 12f, value = sprite };

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                // Make it loop
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Save clip
                string clipPath = $"{animationsPath}/Spider_{animType}_{angle}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);

                // Add to blend tree
                Vector2 direction = AngleToDirection[angle];
                blendTree.AddChild(clip, direction);
            }
            else
            {
                Debug.LogWarning($"[SpiderAnimatorGenerator] Sprite not found: {spritePath}");
            }
        }

        // Add blend tree as sub-asset
        if (AssetDatabase.GetAssetPath(controller) != "")
        {
            AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(controller));
        }

        return blendTree;
    }
}
