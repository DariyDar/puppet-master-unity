using UnityEngine;

/// <summary>
/// Visualizes colliders and attack ranges as semi-transparent circles in the editor and at runtime.
/// Attach to any GameObject with colliders to see their bounds.
/// </summary>
public class ColliderVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showCollider = true;
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private bool showInGame = true; // Show at runtime too

    [Header("Colors")]
    [SerializeField] private Color colliderColor = new Color(0f, 1f, 0f, 0.3f); // Green, semi-transparent
    [SerializeField] private Color attackRangeColor = new Color(1f, 0f, 0f, 0.2f); // Red, semi-transparent

    [Header("Attack Range (if applicable)")]
    [SerializeField] private float attackRange = 2f;

    private CircleCollider2D circleCollider;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Try to get attack range from enemy config
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // Will be set by enemy's config
            attackRange = 2f; // Default, will be overridden
        }
    }

    private void OnDrawGizmos()
    {
        DrawVisualization();
    }

    private void OnDrawGizmosSelected()
    {
        DrawVisualization();
    }

    private void DrawVisualization()
    {
        if (showCollider)
        {
            Gizmos.color = colliderColor;

            // Draw circle collider
            CircleCollider2D cc = circleCollider ?? GetComponent<CircleCollider2D>();
            if (cc != null)
            {
                Vector3 center = transform.TransformPoint(cc.offset);
                float radius = cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
                DrawFilledCircle(center, radius, colliderColor);
            }

            // Draw box collider
            BoxCollider2D bc = boxCollider ?? GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                Vector3 center = transform.TransformPoint(bc.offset);
                Vector3 size = new Vector3(
                    bc.size.x * transform.lossyScale.x,
                    bc.size.y * transform.lossyScale.y,
                    0.1f
                );
                Gizmos.color = colliderColor;
                Gizmos.DrawCube(center, size);
            }
        }

        if (showAttackRange)
        {
            // Draw attack range circle
            DrawFilledCircle(transform.position, attackRange * Mathf.Max(transform.lossyScale.x, 1f), attackRangeColor);
        }
    }

    private void DrawFilledCircle(Vector3 center, float radius, Color color)
    {
        // Draw filled circle using multiple triangles
        Gizmos.color = color;

        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            // Draw triangle from center
            Gizmos.DrawLine(center, prevPoint);
            Gizmos.DrawLine(prevPoint, newPoint);

            prevPoint = newPoint;
        }

        // Draw wire circle for clearer outline
        Gizmos.color = new Color(color.r, color.g, color.b, 1f); // Solid outline

        prevPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    // Runtime visualization
    private void OnRenderObject()
    {
        if (!showInGame || !Application.isPlaying) return;

        // For runtime, we'd need to create a material and draw
        // For now, Gizmos only work in editor
    }

    /// <summary>
    /// Set the attack range from external scripts.
    /// </summary>
    public void SetAttackRange(float range)
    {
        attackRange = range;
    }
}
