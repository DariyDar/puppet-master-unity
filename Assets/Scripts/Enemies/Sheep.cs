using UnityEngine;
using System.Collections;

/// <summary>
/// Sheep - passive creature that runs away from player and drops Meat when killed.
/// Per GDD v5: Sheep is resource source, NOT enemy.
/// </summary>
public class Sheep : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth = 30;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Behavior")]
    [SerializeField] private float fleeDistance = 4f;       // Distance at which sheep starts fleeing
    [SerializeField] private float fleeSpeed = 3.5f;        // Speed when fleeing
    [SerializeField] private float wanderRadius = 3f;       // Radius for random wandering
    [SerializeField] private float wanderInterval = 3f;     // Time between wander direction changes

    [Header("Loot")]
    [SerializeField] private int meatMin = 10;
    [SerializeField] private int meatMax = 20;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioClip bleatSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    // State
    private Transform player;
    private Vector2 homePosition;
    private Vector2 wanderTarget;
    private float lastWanderTime;
    private bool isDead;
    private bool isFleeing;

    // Animation hashes
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        homePosition = transform.position;
        wanderTarget = homePosition;
    }

    private void Start()
    {
        FindPlayer();
        lastWanderTime = Time.time;

        // Random bleat occasionally
        StartCoroutine(RandomBleat());
    }

    private void Update()
    {
        if (isDead) return;

        FindPlayer();

        // Check if should flee
        if (player != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, player.position);
            isFleeing = distToPlayer <= fleeDistance;
        }
        else
        {
            isFleeing = false;
        }

        if (isFleeing)
        {
            Flee();
        }
        else
        {
            Wander();
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

    /// <summary>
    /// Flee away from player.
    /// </summary>
    private void Flee()
    {
        if (player == null) return;

        // Direction away from player
        Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;

        // Move away
        Vector2 newPos = (Vector2)transform.position + fleeDirection * fleeSpeed * Time.deltaTime;
        transform.position = newPos;

        // Face away from player
        UpdateFacing(fleeDirection);

        // Update animation
        if (animator != null)
        {
            animator.SetBool(IsMoving, true);
        }
    }

    /// <summary>
    /// Wander randomly near home position.
    /// </summary>
    private void Wander()
    {
        // Pick new wander target periodically
        if (Time.time - lastWanderTime >= wanderInterval)
        {
            lastWanderTime = Time.time;
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = homePosition + randomOffset;
        }

        // Move towards wander target
        float distToTarget = Vector2.Distance(transform.position, wanderTarget);

        if (distToTarget > 0.2f)
        {
            Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
            Vector2 newPos = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;
            transform.position = newPos;

            UpdateFacing(direction);

            if (animator != null)
            {
                animator.SetBool(IsMoving, true);
            }
        }
        else
        {
            // Reached target, stop
            if (animator != null)
            {
                animator.SetBool(IsMoving, false);
            }
        }
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    /// <summary>
    /// Take damage from player or units.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(damage);

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Play hit sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // Start fleeing immediately when hit
        isFleeing = true;

        Debug.Log($"[Sheep] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = original;
        }
    }

    /// <summary>
    /// Die and drop loot.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Sheep] Died!");

        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger(AnimDie);
        }

        // Drop Meat
        DropLoot();

        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Fade out and destroy
        StartCoroutine(FadeOutAndDestroy());
    }

    private void DropLoot()
    {
        if (ResourceSpawner.Instance == null) return;

        int meatAmount = Random.Range(meatMin, meatMax + 1);
        ResourceSpawner.Instance.SpawnMeat(transform.position, meatAmount);

        Debug.Log($"[Sheep] Dropped {meatAmount} Meat");
    }

    private IEnumerator FadeOutAndDestroy()
    {
        // Wait for death animation if any
        yield return new WaitForSeconds(0.5f);

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

    private IEnumerator RandomBleat()
    {
        while (!isDead)
        {
            // Wait random time
            yield return new WaitForSeconds(Random.Range(5f, 15f));

            if (!isDead && bleatSound != null && Random.value < 0.3f)
            {
                AudioSource.PlayClipAtPoint(bleatSound, transform.position, 0.5f);
            }
        }
    }

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void OnDrawGizmosSelected()
    {
        // Flee distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);

        // Wander radius
        Gizmos.color = Color.green;
        Vector3 home = Application.isPlaying ? (Vector3)homePosition : transform.position;
        Gizmos.DrawWireSphere(home, wanderRadius);
    }
}
