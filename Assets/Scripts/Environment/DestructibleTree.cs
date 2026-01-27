using UnityEngine;
using System.Collections;

/// <summary>
/// A tree that can be destroyed by the player or units.
/// Drops Wood resources when destroyed.
/// </summary>
public class DestructibleTree : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int currentHealth;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite treeSprite;
    [SerializeField] private Sprite stumpSprite;
    [SerializeField] private int sortingOrder = 0;

    [Header("Loot Settings - Per GDD v5")]
    [SerializeField] private int minWoodDrop = 5;
    [SerializeField] private int maxWoodDrop = 15;

    [Header("Effects")]
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float stumpDuration = 5f;
    [SerializeField] private AudioClip chopSound;
    [SerializeField] private AudioClip fallSound;

    [Header("Collision")]
    [SerializeField] private Collider2D treeCollider;

    [Header("Trunk Collider Settings")]
    [Tooltip("If true, collider will be resized to only cover the trunk (player can walk behind tree crown)")]
    [SerializeField] private bool useTrunkCollider = true;
    [Tooltip("Size of the trunk collider (width, height)")]
    [SerializeField] private Vector2 trunkColliderSize = new Vector2(0.5f, 0.4f);
    [Tooltip("Offset of trunk collider from sprite center (usually negative Y to place at bottom)")]
    [SerializeField] private Vector2 trunkColliderOffset = new Vector2(0f, -0.8f);

    private AudioSource audioSource;
    private bool isDestroyed = false;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (treeCollider == null)
        {
            treeCollider = GetComponent<Collider2D>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = sortingOrder;

            if (treeSprite != null)
            {
                spriteRenderer.sprite = treeSprite;
            }
        }

        // Setup trunk collider for proper layering (player can walk behind crown)
        if (useTrunkCollider)
        {
            SetupTrunkCollider();
        }
    }

    /// <summary>
    /// Setup a small collider at the trunk base so player can walk behind the crown.
    /// </summary>
    private void SetupTrunkCollider()
    {
        BoxCollider2D boxCollider = treeCollider as BoxCollider2D;
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (boxCollider == null)
        {
            // Create a new BoxCollider2D if none exists
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
            treeCollider = boxCollider;
        }

        // Set size and offset for trunk only
        boxCollider.size = trunkColliderSize;
        boxCollider.offset = trunkColliderOffset;

        Debug.Log($"[DestructibleTree] {gameObject.name}: Trunk collider set - Size: {trunkColliderSize}, Offset: {trunkColliderOffset}");
    }

    /// <summary>
    /// Deal damage to the tree.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Play chop sound
        if (chopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(chopSound, 0.5f);
        }

        Debug.Log($"[DestructibleTree] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyTree();
        }
    }

    /// <summary>
    /// Flash when taking damage.
    /// </summary>
    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (spriteRenderer != null && !isDestroyed)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Destroy the tree and drop resources.
    /// </summary>
    private void DestroyTree()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[DestructibleTree] {gameObject.name} destroyed!");

        // Disable collider
        if (treeCollider != null)
        {
            treeCollider.enabled = false;
        }

        // Drop wood
        DropWood();

        // Start falling animation
        StartCoroutine(FallAndDestroy());
    }

    /// <summary>
    /// Drop wood resources at tree position - Per GDD v5.
    /// </summary>
    private void DropWood()
    {
        int amount = Random.Range(minWoodDrop, maxWoodDrop + 1);

        if (ResourceSpawner.Instance != null)
        {
            // Spawn multiple wood pickups
            int pickupCount = Mathf.Min(amount, 5); // Max 5 pickups for performance
            int amountPerPickup = amount / pickupCount;
            int remainder = amount % pickupCount;

            for (int i = 0; i < pickupCount; i++)
            {
                int pickupAmount = amountPerPickup + (i < remainder ? 1 : 0);
                ResourceSpawner.Instance.SpawnWood(transform.position, pickupAmount);
            }

            Debug.Log($"[DestructibleTree] Dropped {amount} Wood");
        }
        else
        {
            Debug.LogWarning("[DestructibleTree] ResourceSpawner.Instance is null, cannot spawn wood");
        }
    }

    /// <summary>
    /// Animate tree falling and show stump.
    /// </summary>
    private IEnumerator FallAndDestroy()
    {
        // Play fall sound
        if (fallSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fallSound);
        }

        // Animate falling (rotate and fade)
        float elapsed = 0f;
        float startRotation = transform.eulerAngles.z;
        float targetRotation = startRotation + (Random.value > 0.5f ? 90f : -90f); // Fall left or right
        Vector3 startScale = transform.localScale;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;

            // Rotate
            float rotation = Mathf.Lerp(startRotation, targetRotation, t);
            transform.eulerAngles = new Vector3(0, 0, rotation);

            // Squash slightly
            float scaleY = Mathf.Lerp(1f, 0.5f, t);
            transform.localScale = new Vector3(startScale.x, startScale.y * scaleY, startScale.z);

            yield return null;
        }

        // Fade out the fallen tree
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            if (spriteRenderer != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        // Show stump if available
        if (stumpSprite != null)
        {
            ShowStump();
            yield return new WaitForSeconds(stumpDuration);
        }

        // Destroy the game object
        Destroy(gameObject);
    }

    /// <summary>
    /// Show the stump sprite.
    /// </summary>
    private void ShowStump()
    {
        if (spriteRenderer == null || stumpSprite == null) return;

        // Reset transform
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Set stump sprite
        spriteRenderer.sprite = stumpSprite;
        spriteRenderer.color = originalColor;

        Debug.Log($"[DestructibleTree] Showing stump for {gameObject.name}");
    }

    /// <summary>
    /// Get current health percentage.
    /// </summary>
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Check if tree is destroyed.
    /// </summary>
    public bool IsDestroyed => isDestroyed;

    private void OnDrawGizmosSelected()
    {
        // Draw health bar above tree
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(1f, 0.2f, 0f));

        // Draw trunk collider preview
        if (useTrunkCollider)
        {
            Gizmos.color = Color.yellow;
            Vector3 colliderCenter = transform.position + new Vector3(trunkColliderOffset.x, trunkColliderOffset.y, 0);
            Gizmos.DrawWireCube(colliderCenter, new Vector3(trunkColliderSize.x, trunkColliderSize.y, 0));
        }
    }
}
