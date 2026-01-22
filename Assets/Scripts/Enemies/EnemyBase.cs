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

        FindTarget();
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

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDamaged(gameObject, amount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Play death animation if available
        if (animator != null)
        {
            animator.SetTrigger(AnimDie);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDied(gameObject);
        }

        // Destroy after delay (for death animation)
        Destroy(gameObject, 1f);
    }

    protected virtual bool CanAttack()
    {
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
}
