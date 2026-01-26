using UnityEngine;

/// <summary>
/// Base class for all decorative environment objects.
/// Provides sprite rendering, optional collision, and player interaction.
/// </summary>
public class Decoration : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected int sortingOrder = 0;
    [SerializeField] protected string sortingLayerName = "Default";

    [Header("Collision Settings")]
    [SerializeField] protected bool hasCollider = false;
    [SerializeField] protected Collider2D decorationCollider;

    [Header("Player Interaction")]
    [SerializeField] protected float playerDetectionRadius = 2f;
    [SerializeField] protected bool detectPlayer = false;

    protected Transform playerTransform;
    protected bool playerNear = false;

    protected virtual void Awake()
    {
        // Get components if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (decorationCollider == null && hasCollider)
        {
            decorationCollider = GetComponent<Collider2D>();
        }
    }

    protected virtual void Start()
    {
        // Apply sorting settings
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                spriteRenderer.sortingLayerName = sortingLayerName;
            }
        }

        // Find player
        if (detectPlayer)
        {
            FindPlayer();
        }
    }

    protected virtual void Update()
    {
        if (detectPlayer && playerTransform != null)
        {
            CheckPlayerProximity();
        }
    }

    /// <summary>
    /// Find and cache the player transform.
    /// </summary>
    protected virtual void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    /// <summary>
    /// Check if player is within detection radius.
    /// </summary>
    protected virtual void CheckPlayerProximity()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool isNear = distance <= playerDetectionRadius;

        if (isNear && !playerNear)
        {
            playerNear = true;
            OnPlayerNear();
        }
        else if (!isNear && playerNear)
        {
            playerNear = false;
            OnPlayerFar();
        }
    }

    /// <summary>
    /// Called when player enters detection radius.
    /// Override in derived classes for custom behavior.
    /// </summary>
    public virtual void OnPlayerNear()
    {
        // Base implementation - can be overridden
        Debug.Log($"[Decoration] Player near {gameObject.name}");
    }

    /// <summary>
    /// Called when player leaves detection radius.
    /// Override in derived classes for custom behavior.
    /// </summary>
    public virtual void OnPlayerFar()
    {
        // Base implementation - can be overridden
    }

    /// <summary>
    /// Set the sorting order for the sprite renderer.
    /// </summary>
    public void SetSortingOrder(int order)
    {
        sortingOrder = order;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    /// <summary>
    /// Set the sorting layer for the sprite renderer.
    /// </summary>
    public void SetSortingLayer(string layerName)
    {
        sortingLayerName = layerName;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }
    }

    /// <summary>
    /// Enable or disable the collider.
    /// </summary>
    public void SetColliderEnabled(bool enabled)
    {
        if (decorationCollider != null)
        {
            decorationCollider.enabled = enabled;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (detectPlayer)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
        }
    }
}
