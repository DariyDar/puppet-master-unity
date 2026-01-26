using UnityEngine;

/// <summary>
/// Arrow projectile with parabolic trajectory.
/// Per GDD:
/// - Flies in an arc (parabolic trajectory)
/// - Can miss if target moves
/// - When hits ground, shows without tip and dissolves after 5 seconds
/// Used by Archers and Towers.
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool damagesPlayer = true;
    [SerializeField] private bool damagesEnemies = false;
    [SerializeField] private float lifetime = 5f;

    [Header("Trajectory")]
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float groundedLifetime = 5f;

    [Header("Sprites")]
    [SerializeField] private Sprite arrowWithTip;
    [SerializeField] private Sprite arrowWithoutTip;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip groundHitSound;

    // State
    private GameObject owner;
    private Transform target;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightTime;
    private float currentTime;
    private bool hasHit;
    private bool isGrounded;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Setup arrow with target tracking.
    /// </summary>
    public void Setup(float projectileDamage, GameObject projectileOwner, Transform projectileTarget, bool hitsPlayer)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        target = projectileTarget;
        damagesPlayer = hitsPlayer;
        damagesEnemies = !hitsPlayer;

        startPosition = transform.position;

        // Calculate target position - where target will be
        if (target != null)
        {
            targetPosition = target.position;
        }
        else
        {
            // No target, shoot forward
            targetPosition = transform.position + transform.right * 10f;
        }

        // Calculate flight time based on distance
        float distance = Vector2.Distance(startPosition, targetPosition);
        flightTime = distance / speed;

        // Start the flight
        currentTime = 0f;

        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Setup arrow with fixed target position (no tracking).
    /// </summary>
    public void SetupFixed(float projectileDamage, GameObject projectileOwner, Vector3 targetPos, bool hitsPlayer)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        target = null;
        damagesPlayer = hitsPlayer;
        damagesEnemies = !hitsPlayer;

        startPosition = transform.position;
        targetPosition = targetPos;

        float distance = Vector2.Distance(startPosition, targetPosition);
        flightTime = distance / speed;
        currentTime = 0f;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasHit || isGrounded) return;

        currentTime += Time.deltaTime;
        float t = currentTime / flightTime;

        if (t >= 1f)
        {
            // Reached target position - land on ground
            LandOnGround();
            return;
        }

        // Calculate parabolic position
        Vector3 currentPos = CalculateParabolicPosition(t);

        // Calculate next position for rotation
        float nextT = Mathf.Min(t + 0.01f, 1f);
        Vector3 nextPos = CalculateParabolicPosition(nextT);

        // Update position
        transform.position = currentPos;

        // Rotate arrow to face direction of travel
        Vector2 direction = (nextPos - currentPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Calculate position along parabolic arc.
    /// </summary>
    private Vector3 CalculateParabolicPosition(float t)
    {
        // Linear interpolation for X/Y
        Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, t);

        // Parabolic arc for height
        // y = 4h * t * (1-t) where h is max height at t=0.5
        float arc = 4f * arcHeight * t * (1f - t);

        return new Vector3(linearPos.x, linearPos.y + arc, linearPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || isGrounded) return;

        // Don't hit owner
        if (other.gameObject == owner) return;

        bool hitTarget = false;

        // Check for player damage
        if (damagesPlayer)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Mathf.RoundToInt(damage));
                hitTarget = true;
            }

            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null && !unit.IsDead)
            {
                unit.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // Check for enemy damage
        if (damagesEnemies)
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject != owner)
            {
                enemy.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // Hit obstacle
        if (!hitTarget && other.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
        {
            LandOnGround();
            return;
        }

        if (hitTarget)
        {
            Hit();
        }
    }

    /// <summary>
    /// Arrow hit a target.
    /// </summary>
    private void Hit()
    {
        hasHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Arrow missed and landed on the ground.
    /// Per GDD: Shows without tip and dissolves after 5 seconds.
    /// </summary>
    private void LandOnGround()
    {
        isGrounded = true;

        // Switch to arrow without tip sprite
        if (spriteRenderer != null && arrowWithoutTip != null)
        {
            spriteRenderer.sprite = arrowWithoutTip;
        }

        // Play ground hit sound
        if (groundHitSound != null)
        {
            AudioSource.PlayClipAtPoint(groundHitSound, transform.position, 0.5f);
        }

        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Set final position
        transform.position = targetPosition;

        // Rotate to point downward (stuck in ground)
        transform.rotation = Quaternion.Euler(0, 0, -45f);

        Debug.Log("[ArrowProjectile] Arrow landed on ground, dissolving in 5 seconds");

        // Start dissolve and destroy
        StartCoroutine(DissolveAndDestroy());
    }

    /// <summary>
    /// Fade out and destroy the grounded arrow.
    /// </summary>
    private System.Collections.IEnumerator DissolveAndDestroy()
    {
        // Wait before starting fade
        yield return new WaitForSeconds(groundedLifetime - 1f);

        // Fade out over 1 second
        if (spriteRenderer != null)
        {
            float fadeTime = 1f;
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Predict where a moving target will be.
    /// Returns adjusted target position accounting for movement.
    /// </summary>
    public static Vector3 PredictTargetPosition(Vector3 arrowStart, Transform target, float arrowSpeed)
    {
        if (target == null) return arrowStart;

        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null) return target.position;

        Vector2 targetVelocity = targetRb.linearVelocity;
        if (targetVelocity.magnitude < 0.1f) return target.position;

        // Calculate time to reach current target position
        float distance = Vector2.Distance(arrowStart, target.position);
        float timeToReach = distance / arrowSpeed;

        // Predict where target will be
        Vector3 predictedPos = target.position + (Vector3)(targetVelocity * timeToReach);

        return predictedPos;
    }

    // Properties
    public bool HasHit => hasHit;
    public bool IsGrounded => isGrounded;
}
