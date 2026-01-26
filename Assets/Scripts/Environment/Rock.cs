using UnityEngine;

/// <summary>
/// A rock obstacle with different sizes.
/// Simply blocks movement with a collider.
/// </summary>
public class Rock : MonoBehaviour
{
    public enum RockSize
    {
        Small,
        Medium,
        Large
    }

    [Header("Configuration")]
    [SerializeField] private RockSize size = RockSize.Medium;

    [Header("Size Settings")]
    [SerializeField] private Vector3 smallScale = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] private Vector3 mediumScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 largeScale = new Vector3(2f, 2f, 1f);

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite smallSprite;
    [SerializeField] private Sprite mediumSprite;
    [SerializeField] private Sprite largeSprite;
    [SerializeField] private int sortingOrder = 0;

    [Header("Collision")]
    [SerializeField] private Collider2D rockCollider;
    [SerializeField] private bool blockProjectiles = true;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (rockCollider == null)
        {
            rockCollider = GetComponent<Collider2D>();
        }
    }

    private void Start()
    {
        ApplySize();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        // Ensure collider is not a trigger
        if (rockCollider != null)
        {
            rockCollider.isTrigger = false;
        }

        // Set layer for projectile collision
        if (blockProjectiles)
        {
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer != -1)
            {
                gameObject.layer = obstacleLayer;
            }
        }
    }

    /// <summary>
    /// Apply the configured size to the rock.
    /// </summary>
    public void ApplySize()
    {
        ApplySize(size);
    }

    /// <summary>
    /// Set and apply a specific size.
    /// </summary>
    public void ApplySize(RockSize newSize)
    {
        size = newSize;

        // Apply scale
        switch (size)
        {
            case RockSize.Small:
                transform.localScale = smallScale;
                if (spriteRenderer != null && smallSprite != null)
                {
                    spriteRenderer.sprite = smallSprite;
                }
                break;

            case RockSize.Medium:
                transform.localScale = mediumScale;
                if (spriteRenderer != null && mediumSprite != null)
                {
                    spriteRenderer.sprite = mediumSprite;
                }
                break;

            case RockSize.Large:
                transform.localScale = largeScale;
                if (spriteRenderer != null && largeSprite != null)
                {
                    spriteRenderer.sprite = largeSprite;
                }
                break;
        }

        Debug.Log($"[Rock] {gameObject.name} set to {size} size");
    }

    /// <summary>
    /// Get the current rock size.
    /// </summary>
    public RockSize Size => size;

    /// <summary>
    /// Set whether this rock blocks projectiles.
    /// </summary>
    public void SetBlockProjectiles(bool block)
    {
        blockProjectiles = block;
    }

    /// <summary>
    /// Check if this rock blocks projectiles.
    /// </summary>
    public bool BlocksProjectiles => blockProjectiles;

    private void OnDrawGizmosSelected()
    {
        // Draw collider bounds
        if (rockCollider != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(rockCollider.bounds.center, rockCollider.bounds.size);
        }
    }

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Apply size in editor when changed
        if (Application.isPlaying)
        {
            ApplySize();
        }
    }
#endif

    #endregion
}
