using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all player army units.
/// Handles health, damage, movement, and visual feedback.
/// </summary>
public class UnitBase : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] protected UnitConfig config;

    [Header("Stats (Runtime)")]
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float damage;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float attackCooldown;

    [Header("References")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Collider2D col;

    // State
    protected Transform playerTransform;
    protected Transform currentTarget;
    protected float lastAttackTime;
    protected bool isDead;
    protected Color originalColor;

    // Animation hashes
    protected static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    protected static readonly int AnimAttack = Animator.StringToHash("Attack");
    protected static readonly int AnimDie = Animator.StringToHash("Die");

    // Ability tracking
    protected int attackCount = 0;
    protected float lastAbilityTime;
    protected bool isStunned;
    protected float stunEndTime;
    protected GameObject mindControlledEnemy;

    // Properties
    public UnitType UnitTypeEnum => config != null ? config.unitType : UnitType.Skull;
    public string UnitTypeName => config != null ? config.displayName : "Unknown";
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float MoveSpeed => moveSpeed;
    public bool IsDead => isDead;
    public bool IsStunned => isStunned && Time.time < stunEndTime;
    public UnitConfig Config => config;
    public Transform PlayerTransform => playerTransform;
    public Transform CurrentTarget => currentTarget;

    protected virtual void Awake()
    {
        // Get components if not assigned
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    protected virtual void Start()
    {
        InitializeFromConfig();
        FindPlayer();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// Initialize stats from UnitConfig ScriptableObject.
    /// </summary>
    public virtual void InitializeFromConfig()
    {
        if (config == null)
        {
            Debug.LogWarning($"[UnitBase] No config assigned to {gameObject.name}");
            return;
        }

        maxHealth = config.maxHealth;
        currentHealth = maxHealth;
        damage = config.damage;
        moveSpeed = config.moveSpeed;
        attackRange = config.attackRange;
        // Convert attacks per second to cooldown between attacks
        attackCooldown = config.attackSpeed > 0 ? 1f / config.attackSpeed : 1f;
    }

    /// <summary>
    /// Initialize this unit with a specific configuration.
    /// Called by UnitFactory when spawning.
    /// </summary>
    public virtual void Initialize(UnitConfig unitConfig)
    {
        config = unitConfig;
        InitializeFromConfig();
    }

    /// <summary>
    /// Find and cache the player transform.
    /// </summary>
    protected virtual void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Ensure we have player reference
        if (playerTransform == null)
        {
            FindPlayer();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;
        // Movement handled by UnitAI
    }

    #region Movement

    /// <summary>
    /// Move the unit in the specified direction.
    /// </summary>
    public virtual void Move(Vector2 direction)
    {
        if (isDead || rb == null) return;

        rb.linearVelocity = direction.normalized * moveSpeed;
        UpdateAnimation(direction.magnitude > 0.1f, direction);
    }

    /// <summary>
    /// Stop all movement.
    /// </summary>
    public virtual void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        UpdateAnimation(false, Vector2.zero);
    }

    /// <summary>
    /// Get distance to player.
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, playerTransform.position);
    }

    /// <summary>
    /// Get direction to player.
    /// </summary>
    public Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector2.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    /// <summary>
    /// Get distance to current target.
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector2.Distance(transform.position, currentTarget.position);
    }

    /// <summary>
    /// Get direction to current target.
    /// </summary>
    public Vector2 GetDirectionToTarget()
    {
        if (currentTarget == null) return Vector2.zero;
        return (currentTarget.position - transform.position).normalized;
    }

    #endregion

    #region Combat

    /// <summary>
    /// Set the current attack target.
    /// </summary>
    public virtual void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    /// <summary>
    /// Clear the current target.
    /// </summary>
    public virtual void ClearTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// Check if unit can attack (cooldown ready).
    /// </summary>
    public virtual bool CanAttack()
    {
        if (IsStunned) return false;
        return Time.time - lastAttackTime >= attackCooldown;
    }

    /// <summary>
    /// Perform an attack on the current target.
    /// </summary>
    public virtual void PerformAttack()
    {
        if (!CanAttack() || isDead) return;

        lastAttackTime = Time.time;
        attackCount++;

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Deal damage immediately (or use animation event)
        DealDamage();
    }

    /// <summary>
    /// Deal damage to the current target.
    /// Called during attack animation or directly.
    /// </summary>
    public virtual void DealDamage()
    {
        if (currentTarget == null) return;

        float dist = GetDistanceToTarget();
        if (dist <= attackRange)
        {
            // Try to damage enemy
            EnemyBase enemy = currentTarget.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                float actualDamage = damage;
                enemy.TakeDamage(actualDamage);

                // Apply ability effects after dealing damage
                ApplyAbilityOnHit(enemy, actualDamage);
            }
        }
    }

    /// <summary>
    /// Apply ability effects when hitting an enemy.
    /// </summary>
    protected virtual void ApplyAbilityOnHit(EnemyBase enemy, float damageDealt)
    {
        if (config == null) return;

        switch (config.abilityType)
        {
            case UnitAbilityType.Lifesteal:
                ApplyLifesteal(damageDealt);
                break;

            case UnitAbilityType.Stun:
                TryApplyStun(enemy);
                break;

            case UnitAbilityType.AoE:
                ApplyAoEDamage(enemy.transform.position, damageDealt);
                break;

            case UnitAbilityType.MindControl:
                TryApplyMindControl(enemy);
                break;
        }
    }

    #region Ability: Lifesteal (Gnoll)

    /// <summary>
    /// Heal based on damage dealt. Visual: HP particles fly from victim to Gnoll.
    /// </summary>
    protected virtual void ApplyLifesteal(float damageDealt)
    {
        if (config == null || config.abilityType != UnitAbilityType.Lifesteal) return;

        float healAmount = damageDealt * config.abilityValue;
        Heal(healAmount);

        // Spawn HP particle effect (from target to self)
        SpawnLifestealEffect();

        Debug.Log($"[UnitBase] {UnitTypeName} lifesteal healed {healAmount:F1} HP");
    }

    protected virtual void SpawnLifestealEffect()
    {
        if (config.abilityEffectPrefab != null && currentTarget != null)
        {
            GameObject effect = Instantiate(config.abilityEffectPrefab, currentTarget.position, Quaternion.identity);
            // Effect should move from target to this unit (handled by effect script)
            Destroy(effect, 1f);
        }
    }

    #endregion

    #region Ability: Stun (Gnome)

    /// <summary>
    /// Every Nth attack stuns enemy. Visual: -10% anim speed, +10% size, swirl effect.
    /// </summary>
    protected virtual void TryApplyStun(EnemyBase enemy)
    {
        if (config == null || config.abilityType != UnitAbilityType.Stun) return;
        if (config.abilityTriggerCount <= 0) return;

        // Check if this is the Nth attack
        if (attackCount % config.abilityTriggerCount == 0)
        {
            float stunDuration = config.abilityValue;
            enemy.ApplyStun(stunDuration);

            // Visual feedback on Gnome: bigger dust effect
            SpawnStunAttackEffect();

            Debug.Log($"[UnitBase] {UnitTypeName} stunned enemy for {stunDuration}s (attack #{attackCount})");
        }
    }

    protected virtual void SpawnStunAttackEffect()
    {
        if (config.abilityEffectPrefab != null)
        {
            // Larger dust effect on attack
            GameObject effect = Instantiate(config.abilityEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale *= 1.5f; // Bigger dust
            Destroy(effect, 0.5f);
        }
    }

    /// <summary>
    /// Apply stun to this unit (called by enemies or other effects).
    /// </summary>
    public virtual void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;

        // Visual: slow animation
        if (animator != null)
        {
            animator.speed = 0.9f; // -10% speed
        }

        // Visual: increase size slightly
        transform.localScale *= 1.1f;

        StartCoroutine(RemoveStunAfterDuration(duration));
    }

    protected virtual IEnumerator RemoveStunAfterDuration(float duration)
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

    #endregion

    #region Ability: AoE (TNT Goblin)

    /// <summary>
    /// Deal splash damage to nearby enemies. Visual: explosion effect.
    /// </summary>
    protected virtual void ApplyAoEDamage(Vector3 hitPosition, float baseDamage)
    {
        if (config == null || config.abilityType != UnitAbilityType.AoE) return;

        float aoeDamage = baseDamage * config.abilityValue;
        float aoeRadius = attackRange * 0.5f; // Half of attack range as splash radius

        // Find nearby enemies
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(hitPosition, aoeRadius);

        foreach (var col in nearbyColliders)
        {
            // Skip the primary target (already damaged)
            if (col.transform == currentTarget) continue;

            EnemyBase nearbyEnemy = col.GetComponent<EnemyBase>();
            if (nearbyEnemy != null && !nearbyEnemy.IsDead)
            {
                nearbyEnemy.TakeDamage(aoeDamage);
                Debug.Log($"[UnitBase] {UnitTypeName} AoE dealt {aoeDamage:F1} splash damage");
            }
        }

        // Spawn explosion effect
        SpawnExplosionEffect(hitPosition);
    }

    protected virtual void SpawnExplosionEffect(Vector3 position)
    {
        if (config.abilityEffectPrefab != null)
        {
            GameObject effect = Instantiate(config.abilityEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    #endregion

    #region Ability: Mind Control (Shaman)

    /// <summary>
    /// Take control of enemy mind. Visual: purple projectile, green particle swarm.
    /// </summary>
    protected virtual void TryApplyMindControl(EnemyBase enemy)
    {
        if (config == null || config.abilityType != UnitAbilityType.MindControl) return;

        // Check cooldown
        if (Time.time - lastAbilityTime < config.abilityCooldown) return;

        // Can only control one enemy at a time
        if (mindControlledEnemy != null) return;

        lastAbilityTime = Time.time;
        float controlDuration = config.abilityValue;

        // Apply mind control to enemy
        enemy.ApplyMindControl(this, controlDuration);
        mindControlledEnemy = enemy.gameObject;

        // Spawn mind control effect (purple projectile -> green swarm)
        SpawnMindControlEffect(enemy.transform);

        StartCoroutine(ClearMindControlAfterDuration(controlDuration));

        Debug.Log($"[UnitBase] {UnitTypeName} mind controlled enemy for {controlDuration}s");
    }

    protected virtual void SpawnMindControlEffect(Transform target)
    {
        if (config.abilityEffectPrefab != null)
        {
            // Projectile effect from shaman to target
            GameObject effect = Instantiate(config.abilityEffectPrefab, transform.position, Quaternion.identity);
            // Effect script should handle movement to target
            Destroy(effect, 2f);
        }
    }

    protected virtual IEnumerator ClearMindControlAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (mindControlledEnemy != null)
        {
            EnemyBase enemy = mindControlledEnemy.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.RemoveMindControl();
            }
            mindControlledEnemy = null;
        }
    }

    #endregion

    /// <summary>
    /// Take damage from an enemy.
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        if (IsStunned) return; // Can't take damage while stunned? Or maybe double damage?

        currentHealth -= amount;

        // Visual feedback
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        Debug.Log($"[UnitBase] {UnitTypeName} took {amount:F1} damage. Health: {currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Flash red when taking damage.
    /// </summary>
    protected virtual IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (spriteRenderer != null && !isDead)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Heal the unit.
    /// </summary>
    public virtual void Heal(float amount)
    {
        if (isDead) return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        // Visual feedback for healing
        if (amount > 0 && spriteRenderer != null)
        {
            StartCoroutine(HealFlash());
        }
    }

    /// <summary>
    /// Flash green when healing.
    /// </summary>
    protected virtual IEnumerator HealFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.1f);

        if (spriteRenderer != null && !isDead)
        {
            spriteRenderer.color = originalColor;
        }
    }

    #endregion

    #region Death

    /// <summary>
    /// Handle unit death.
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log($"[UnitBase] {UnitTypeName} died!");

        // Stop movement
        StopMovement();

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger(AnimDie);
        }

        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitDied(gameObject);
        }

        // Update army count
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateArmyCount(GameManager.Instance.ArmyCount - 1);
        }

        // Destroy after delay (for death animation)
        Destroy(gameObject, 1f);
    }

    #endregion

    #region Animation

    /// <summary>
    /// Update movement animation and sprite flip.
    /// </summary>
    protected virtual void UpdateAnimation(bool isMoving, Vector2 direction)
    {
        if (animator != null)
        {
            animator.SetBool(AnimIsMoving, isMoving);
        }

        // Flip sprite based on direction
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    /// <summary>
    /// Face towards the current target.
    /// </summary>
    public virtual void FaceTarget()
    {
        if (currentTarget == null || spriteRenderer == null) return;

        Vector2 dir = GetDirectionToTarget();
        spriteRenderer.flipX = dir.x < 0;
    }

    /// <summary>
    /// Face towards the player.
    /// </summary>
    public virtual void FacePlayer()
    {
        if (playerTransform == null || spriteRenderer == null) return;

        Vector2 dir = GetDirectionToPlayer();
        spriteRenderer.flipX = dir.x < 0;
    }

    #endregion

    #region Debug

    /// <summary>
    /// Draw debug gizmos in editor.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Detection range (if config exists)
        if (config != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.detectionRange);

            // Follow distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, config.followDistance);
        }
    }

    #endregion
}
