using UnityEngine;

/// <summary>
/// Collectible resource that can be picked up by the player.
/// Supports magnet attraction when player is nearby.
/// </summary>
public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType
    {
        Meat,
        Wood,
        Gold,
        Soul
    }

    [Header("Resource Settings")]
    [SerializeField] private ResourceType type = ResourceType.Meat;
    [SerializeField] private int amount = 1;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 0.5f;
    [SerializeField] private float magnetRadius = 3f;
    [SerializeField] private float magnetSpeed = 8f;
    [SerializeField] private float spawnBounceForce = 3f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;

    private Transform player;
    private Vector3 startPosition;
    private bool isBeingMagneted;
    private Rigidbody2D rb;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        startPosition = transform.position;
        FindPlayer();

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
            isBeingMagneted = true;
            MoveTowardsPlayer();
        }
        else
        {
            isBeingMagneted = false;
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

        // Disable physics when magneting
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
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
                case ResourceType.Soul:
                    GameManager.Instance.AddSoul(amount);
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
