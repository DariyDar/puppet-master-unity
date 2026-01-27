using UnityEngine;

/// <summary>
/// Collectible resource that can be picked up by the player.
/// Supports magnet attraction when player is nearby.
/// </summary>
public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType
    {
        Meat,       // From sheep
        Wood,       // From destroyed buildings
        Gold,       // From gold mines
        Skull       // From killed humans (1 per enemy)
    }

    [Header("Resource Settings")]
    [SerializeField] private ResourceType type = ResourceType.Meat;
    [SerializeField] private int amount = 1;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 1.5f;     // Doubled from 0.75f
    [SerializeField] private float magnetRadius = 9f;       // Doubled from 4.5f
    [SerializeField] private float magnetSpeed = 40f;       // 5x faster (was 8f)
    [SerializeField] private float spawnBounceForce = 3f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.4f;  // Radius for resource-to-resource collision

    // Layer name for resources - will be created if doesn't exist
    private const string RESOURCE_LAYER = "Resources";

    private Transform player;
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();

        // Setup collision between resources only
        SetupResourceCollision();
    }

    /// <summary>
    /// Setup collision so resources push each other apart but don't block magnet movement.
    /// Uses trigger collider + manual overlap resolution instead of physics collision.
    /// </summary>
    private void SetupResourceCollision()
    {
        // Try to use "Resources" layer, fallback to layer 8 if not defined
        int resourceLayer = LayerMask.NameToLayer(RESOURCE_LAYER);
        if (resourceLayer == -1)
        {
            resourceLayer = 8;
        }
        gameObject.layer = resourceLayer;

        // Add CircleCollider2D as TRIGGER for overlap detection
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        circleCollider.radius = collisionRadius;
        circleCollider.isTrigger = true; // Trigger instead of solid - won't block movement

        // Setup Rigidbody2D for physics queries only
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.linearDamping = 5f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic; // Kinematic - we control movement directly
    }

    private void Start()
    {
        startPosition = transform.position;
        FindPlayer();

        Debug.Log($"[ResourcePickup] Created {type} with amount={amount} at {transform.position}");

        // Initial bounce when spawned
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.AddForce(randomDir * spawnBounceForce, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        FindPlayer();

        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // Check pickup
        if (distToPlayer <= pickupRadius)
        {
            Pickup();
            return;
        }

        // Check magnet
        if (distToPlayer <= magnetRadius)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Bob animation when idle
            if (rb == null || rb.linearVelocity.magnitude < 0.1f)
            {
                float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
                transform.position = new Vector3(
                    transform.position.x,
                    startPosition.y + bob,
                    transform.position.z
                );
            }
        }
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * magnetSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Push resources apart when they overlap (soft collision via triggers).
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        // Only push apart other resources
        ResourcePickup otherResource = other.GetComponent<ResourcePickup>();
        if (otherResource == null) return;

        // Calculate push direction (away from other resource)
        Vector2 pushDir = (transform.position - other.transform.position);
        float distance = pushDir.magnitude;

        if (distance < 0.01f)
        {
            // If exactly overlapping, push in random direction
            pushDir = Random.insideUnitCircle.normalized;
        }
        else
        {
            pushDir = pushDir.normalized;
        }

        // Push force decreases with distance
        float pushStrength = 2f * (1f - distance / (collisionRadius * 2f));
        pushStrength = Mathf.Max(0f, pushStrength);

        transform.position += (Vector3)(pushDir * pushStrength * Time.deltaTime);
    }

    private void Pickup()
    {
        bool success = false;

        if (GameManager.Instance != null)
        {
            switch (type)
            {
                case ResourceType.Meat:
                    success = GameManager.Instance.AddToCargo("meat", amount);
                    break;
                case ResourceType.Wood:
                    success = GameManager.Instance.AddToCargo("wood", amount);
                    break;
                case ResourceType.Gold:
                    success = GameManager.Instance.AddToCargo("gold", amount);
                    break;
                case ResourceType.Skull:
                    GameManager.Instance.AddSkull(amount);
                    success = true;
                    break;
            }
        }
        else
        {
            success = true; // Allow pickup even without GameManager
        }

        if (success)
        {
            // Play pickup effect/sound here
            Debug.Log($"[ResourcePickup] Picked up {amount} {type}");
            Destroy(gameObject);
        }
    }

    // Properties
    public ResourceType Type => type;
    public int Amount => amount;

    // Setup methods for spawning
    public void Setup(ResourceType resourceType, int resourceAmount)
    {
        type = resourceType;
        amount = resourceAmount;
    }

    private void OnDrawGizmosSelected()
    {
        // Pickup radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Magnet radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
