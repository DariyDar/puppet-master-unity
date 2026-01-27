using UnityEngine;

/// <summary>
/// Projectile fired by Watchtower.
/// Deals damage to player or player units on hit.
/// </summary>
public class TowerProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float speed = 10f;

    private Vector2 direction;
    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        // Destroy after lifetime
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize projectile with damage and direction.
    /// Called by Watchtower after spawning.
    /// </summary>
    public void Initialize(float damageAmount, Vector2 moveDirection, float moveSpeed)
    {
        damage = damageAmount;
        direction = moveDirection.normalized;
        speed = moveSpeed;

        // Set velocity
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Mathf.RoundToInt(damage));
                Debug.Log($"[TowerProjectile] Hit player for {damage} damage");
            }
            Destroy(gameObject);
            return;
        }

        // Check if hit player unit
        UnitBase unit = other.GetComponent<UnitBase>();
        if (unit != null && !unit.IsDead)
        {
            unit.TakeDamage(Mathf.RoundToInt(damage));
            Debug.Log($"[TowerProjectile] Hit unit {unit.name} for {damage} damage");
            Destroy(gameObject);
            return;
        }

        // Ignore collision with enemies and towers
        if (other.CompareTag("Enemy") || other.CompareTag("Building"))
        {
            return;
        }

        // Hit something else (wall, obstacle)
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
