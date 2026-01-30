using UnityEngine;

/// <summary>
/// Helper for drawing standardized gizmos across the project.
///
/// COLOR LEGEND (Radius Types):
/// - Red (#FF0000)         — Attack range (melee solid, ranged dashed)
/// - Yellow (#FFFF00)      — Chase range (max pursuit distance)
/// - Cyan (#00FFFF)        — Fear range (triggers fleeing)
/// - Green (#00FF00)       — Heal range (Monk only)
/// - Pink (#FF69B4)        — Desperate attack / knife (Pawn only)
/// - Brown (#8B4513)       — Resource detection (Pawn=any, Miner=gold)
/// - Gray (#888888)        — Wander radius
/// - Black (#000000)       — Collider
/// </summary>
public static class GizmosHelper
{
    // Standard colors
    public static readonly Color ColliderColor = Color.black;
    public static readonly Color MeleeAttackColor = Color.red;                      // Solid circle
    public static readonly Color RangedAttackColor = Color.red;                     // Dashed circle
    public static readonly Color ChaseColor = Color.yellow;                         // Dashed circle
    public static readonly Color DetectionColor = Color.yellow;                     // Same as Chase (legacy alias)
    public static readonly Color HealRangeColor = Color.green;                      // Monk only
    public static readonly Color DesperateAttackColor = new Color(1f, 0.41f, 0.71f); // #FF69B4 pink
    public static readonly Color ResourceDetectionColor = new Color(0.55f, 0.27f, 0.07f); // #8B4513 brown
    public static readonly Color FearColor = Color.cyan;                             // Triggers fleeing (light blue)
    public static readonly Color WanderColor = new Color(0.53f, 0.53f, 0.53f);     // #888888 gray

    /// <summary>
    /// Draw a dashed wire circle in the XY plane (for 2D games).
    /// </summary>
    public static void DrawDashedWireCircle(Vector3 center, float radius, int segments = 64)
    {
        if (radius <= 0f) return;

        // Draw every other segment to create dashed effect
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i += 2)
        {
            float startAngle = i * angleStep * Mathf.Deg2Rad;
            float endAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 start = center + new Vector3(Mathf.Cos(startAngle), Mathf.Sin(startAngle), 0f) * radius;
            Vector3 end = center + new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle), 0f) * radius;

            Gizmos.DrawLine(start, end);
        }
    }
}
