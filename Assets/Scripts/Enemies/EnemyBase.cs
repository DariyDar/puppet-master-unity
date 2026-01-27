using UnityEngine;

/// <summary>
/// Base class for all enemies. Handles health, damage, and death.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int damage = 10;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected int xpReward = 10;

    [Header("Detection")]
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float chaseRange = 12f;

    [Header("References")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody2D rb;

    // State
    protected int currentHealth;
    protected Transform target;
    protected float lastAttackTime;
    protected bool isDead;

    // Status effects
    protected bool isStunned;
    protected float stunEndTime;
    protected bool isMindControlled;
    protected UnitBase mindController;
    protected float mindControlEndTime;

    // Animation hashes
    protected static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    protected static readonly int AnimAttack = Animator.StringToHash("Attack");
    protected static readonly int AnimDie = Animator.StringToHash("Die");

    protected virtual void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        FindTarget();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Skip normal behavior if stunned
        if (IsStunned) return;

        // If mind controlled, target other enemies instead of player
        if (IsMindControlled)
        {
            FindEnemyTarget();
        }
        else
        {
            FindTarget();
        }
    }

    /// <summary>
    /// Find other enemies to attack when mind controlled.
    /// </summary>
    protected virtual void FindEnemyTarget()
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var enemy in allEnemies)
        {
            // Skip self and other mind-controlled enemies
            if (enemy == this || enemy.IsMindControlled || enemy.IsDead) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist && dist <= detectionRange)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        target = closest;
    }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;
        // Movement handled by derived classes
    }

    protected virtual void FindTarget()
    {
        // Find player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    protected float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector2.Distance(transform.position, target.position);
    }

    protected Vector2 GetDirectionToTarget()
    {
        if (target == null) return Vector2.zero;
        return (target.position - transform.position).normalized;
    }

    protected void FaceTarget()
    {
        if (target == null || spriteRenderer == null) return;

        Vector2 dir = GetDirectionToTarget();
        spriteRenderer.flipX = dir.x < 0;
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(amount);

        // Visual feedback - flash red
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDamaged(gameObject, Mathf.RoundToInt(amount));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
    {
        isDead = true;

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Spawn death effect (dust + skull)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.SpawnDeathEffect(transform.position);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDied(gameObject);
        }

        // Destroy immediately (death effect handles visuals)
        Destroy(gameObject);
    }

    protected virtual bool CanAttack()
    {
        if (IsStunned) return false;
        return Time.time - lastAttackTime >= attackCooldown;
    }

    protected virtual void PerformAttack()
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Damage is dealt via animation event or override
    }

    // Called by animation event or derived class
    public virtual void DealDamage()
    {
        if (target == null) return;

        float dist = GetDistanceToTarget();
        if (dist <= attackRange)
        {
            // Try to damage player
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public int XpReward => xpReward;
    public bool IsStunned => isStunned && Time.time < stunEndTime;
    public bool IsMindControlled => isMindControlled && Time.time < mindControlEndTime;

    #region Healing

    /// <summary>
    /// Heal the enemy by the specified amount.
    /// Health cannot exceed maxHealth.
    /// </summary>
    public virtual void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        int healAmount = Mathf.RoundToInt(amount);
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        int actualHeal = currentHealth - previousHealth;
        if (actualHeal > 0)
        {
            // Visual feedback - flash green briefly
            if (spriteRenderer != null)
            {
                StartCoroutine(HealFlash());
            }

            Debug.Log($"[EnemyBase] {gameObject.name} healed for {actualHeal}. Health: {currentHealth}/{maxHealth}");
        }
    }

    private System.Collections.IEnumerator HealFlash()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.15f);
        if (spriteRenderer != null && !isDead)
            spriteRenderer.color = originalColor;
    }

    #endregion

    #region Status Effects

    /// <summary>
    /// Apply stun effect. Visual: -10% anim speed, +10% size, swirl effect over head.
    /// </summary>
    public virtual void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;

        // Visual: slow animation by 10%
        if (animator != null)
        {
            animator.speed = 0.9f;
        }

        // Visual: increase size by 10%
        transform.localScale *= 1.1f;

        // TODO: Spawn swirl effect over enemy head

        StartCoroutine(RemoveStunAfterDuration(duration));

        Debug.Log($"[EnemyBase] {gameObject.name} stunned for {duration}s");
    }

    protected virtual System.Collections.IEnumerator RemoveStunAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        isStunned = false;

        // Restore animation speed
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Restore size
        transform.localScale /= 1.1f;
    }

    /// <summary>
    /// Apply mind control effect. Enemy fights for player temporarily.
    /// Visual: purple tint, green particle swarm.
    /// </summary>
    public virtual void ApplyMindControl(UnitBase controller, float duration)
    {
        isMindControlled = true;
        mindController = controller;
        mindControlEndTime = Time.time + duration;

        // Visual: purple/green tint
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.7f, 0.3f, 0.9f); // Purple tint
        }

        // Change target to nearest non-controlled enemy
        // (handled by AI state machine)

        Debug.Log($"[EnemyBase] {gameObject.name} mind controlled for {duration}s");
    }

    /// <summary>
    /// Remove mind control effect.
    /// </summary>
    public virtual void RemoveMindControl()
    {
        isMindControlled = false;
        mindController = null;

        // Restore color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // Reset target to player
        FindTarget();

        Debug.Log($"[EnemyBase] {gameObject.name} mind control ended");
    }

    #endregion
}
