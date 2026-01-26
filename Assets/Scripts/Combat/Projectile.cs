using UnityEngine;
using System.Collections;

/// <summary>
/// Projectile that damages targets on collision.
/// Used by ranged units and enemies.
/// Supports arc trajectory with gravity and sticking to ground.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool damagesPlayer = true;
    [SerializeField] private bool damagesEnemies = false;
    [SerializeField] private float lifetime = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;

    [Header("Flight Behavior")]
    [SerializeField] private bool rotateTowardsVelocity = true;
    [SerializeField] private float hitRadius = 0.5f; // Radius to check for target when landing

    [Header("Ground Stick")]
    [SerializeField] private float groundStickDuration = 10f; // How long arrow stays in ground
    [SerializeField] private float fadeOutDuration = 1f; // How long to fade out

    private GameObject owner;
    private bool hasHit;
    private bool isStuck;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    // Arc trajectory
    private Vector3 targetPosition;
    private bool useArcTrajectory;
    private float flightTime;
    private float elapsedTime;
    private Vector3 startPosition;
    private float arcHeight = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (!useArcTrajectory)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Update()
    {
        if (isStuck || hasHit) return;

        if (useArcTrajectory)
        {
            UpdateArcTrajectory();
        }
        else
        {
            // Rotate arrow to face direction of travel
            if (rotateTowardsVelocity && rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    /// <summary>
    /// Setup projectile damage and targeting.
    /// </summary>
    public void Setup(float projectileDamage, GameObject projectileOwner, bool hitsPlayer)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        damagesPlayer = hitsPlayer;
        damagesEnemies = !hitsPlayer;
    }

    /// <summary>
    /// Setup with explicit targeting.
    /// </summary>
    public void Setup(float projectileDamage, GameObject projectileOwner, bool hitsPlayer, bool hitsEnemies)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        damagesPlayer = hitsPlayer;
        damagesEnemies = hitsEnemies;
    }

    /// <summary>
    /// Setup arc trajectory to target position.
    /// Arrow will fly in an arc and check for hits at landing.
    /// </summary>
    public void SetupArcTrajectory(Vector3 target, float time, float height = 2f)
    {
        useArcTrajectory = true;
        targetPosition = target;
        flightTime = time;
        arcHeight = height;
        startPosition = transform.position;
        elapsedTime = 0f;

        // Disable physics for arc trajectory (we control position manually)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Disable collider during flight - we'll check manually at landing
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void UpdateArcTrajectory()
    {
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / flightTime;

        if (t >= 1f)
        {
            // Reached target - check for hit and stick to ground
            transform.position = targetPosition;
            CheckLandingHit();
            return;
        }

        // Calculate position along arc
        // Lerp XY position
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);

        // Add arc height (parabola)
        float heightOffset = arcHeight * 4f * t * (1f - t); // Parabola: 0 at start and end, max at middle
        currentPos.y += heightOffset;

        transform.position = currentPos;

        // Rotate to face direction of movement
        if (rotateTowardsVelocity)
        {
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, Mathf.Min(t + 0.01f, 1f));
            float nextHeight = arcHeight * 4f * (t + 0.01f) * (1f - (t + 0.01f));
            nextPos.y += nextHeight;

            Vector3 direction = nextPos - currentPos;
            if (direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    private void CheckLandingHit()
    {
        if (hasHit) return;

        // Check for targets in hit radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        bool hitTarget = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == owner) continue;
            if (hit.gameObject == gameObject) continue;

            // Check for player damage
            if (damagesPlayer)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(Mathf.RoundToInt(damage));
                    hitTarget = true;
                    break;
                }

                UnitBase unit = hit.GetComponent<UnitBase>();
                if (unit != null && !unit.IsDead)
                {
                    unit.TakeDamage(damage);
                    hitTarget = true;
                    break;
                }
            }

            if (damagesEnemies)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && !enemy.IsDead && enemy.gameObject != owner)
                {
                    enemy.TakeDamage(damage);
                    hitTarget = true;
                    break;
                }
            }
        }

        if (hitTarget)
        {
            Hit();
        }
        else
        {
            // Missed - stick to ground
            StickToGround();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || isStuck || useArcTrajectory) return;

        // Don't hit owner
        if (other.gameObject == owner) return;

        bool hitSomething = false;

        // Check for player damage
        if (damagesPlayer)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Mathf.RoundToInt(damage));
                hitSomething = true;
            }

            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null && !unit.IsDead)
            {
                unit.TakeDamage(damage);
                hitSomething = true;
            }
        }

        // Check for enemy damage
        if (damagesEnemies)
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject != owner)
            {
                enemy.TakeDamage(damage);
                hitSomething = true;
            }
        }

        // Hit something solid (walls, etc)
        if (!hitSomething && other.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
        {
            hitSomething = true;
        }

        if (hitSomething)
        {
            Hit();
        }
    }

    private void Hit()
    {
        hasHit = true;

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play hit sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        Destroy(gameObject);
    }

    private void StickToGround()
    {
        isStuck = true;

        // Stop all physics
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }

        // Point arrow downward (stuck in ground)
        transform.rotation = Quaternion.Euler(0, 0, -90f);

        // Start decay coroutine
        StartCoroutine(DecayAndFade());
    }

    private IEnumerator DecayAndFade()
    {
        // Wait for stick duration
        yield return new WaitForSeconds(groundStickDuration);

        // Fade out
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
