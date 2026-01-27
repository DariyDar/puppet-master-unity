using UnityEngine;

/// <summary>
/// Y-based sorting for 2D sprites.
/// Objects lower on the Y axis (closer to camera/bottom of screen) render on top.
/// This creates the illusion of depth in top-down games.
/// </summary>
public class YSortingRenderer : MonoBehaviour
{
    [Header("Sorting Configuration")]
    [Tooltip("The SpriteRenderer to sort. If not assigned, will try to find one on this object.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Offset added to final sorting order for fine-tuning.")]
    [SerializeField] private int sortingOffset = 0;

    [Tooltip("Y offset for sorting calculation. Use negative values for objects like trees where the sorting point should be at the base/trunk.")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Multiplier for Y-to-sorting conversion. Higher = more precision, but limited range.")]
    [SerializeField] private float sortingPrecision = 100f;

    [Tooltip("If true, sorting updates every frame. If false, only updates once on Start.")]
    [SerializeField] private bool dynamicSorting = true;

    [Header("Debug")]
    [SerializeField] private bool showSortingGizmo = false;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[YSortingRenderer] No SpriteRenderer found on {gameObject.name}");
        }
    }

    private void Start()
    {
        UpdateSorting();
    }

    private void LateUpdate()
    {
        if (dynamicSorting)
        {
            UpdateSorting();
        }
    }

    /// <summary>
    /// Update the sorting order based on Y position.
    /// Lower Y = higher sorting order = renders on top.
    /// </summary>
    public void UpdateSorting()
    {
        if (spriteRenderer == null) return;

        float sortY = transform.position.y + yOffset;
        int newSortingOrder = Mathf.RoundToInt(-sortY * sortingPrecision) + sortingOffset;

        spriteRenderer.sortingOrder = newSortingOrder;
    }

    /// <summary>
    /// Force update sorting (useful for static objects that moved).
    /// </summary>
    public void RefreshSorting()
    {
        UpdateSorting();
    }

    /// <summary>
    /// Set Y offset at runtime.
    /// </summary>
    public void SetYOffset(float offset)
    {
        yOffset = offset;
        UpdateSorting();
    }

    /// <summary>
    /// Set sorting offset at runtime.
    /// </summary>
    public void SetSortingOffset(int offset)
    {
        sortingOffset = offset;
        UpdateSorting();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showSortingGizmo) return;

        // Draw the sorting point
        Vector3 sortPoint = transform.position + new Vector3(0, yOffset, 0);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sortPoint, 0.1f);
        Gizmos.DrawLine(transform.position, sortPoint);

        // Draw sorting value
#if UNITY_EDITOR
        if (spriteRenderer != null)
        {
            UnityEditor.Handles.Label(sortPoint + Vector3.up * 0.2f, $"Order: {spriteRenderer.sortingOrder}");
        }
#endif
    }
}
