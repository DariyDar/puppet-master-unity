using UnityEngine;

/// <summary>
/// Base class for all enemies. Handles health, damage, and death.
/// Attack rule: damage is dealt ONLY when attack animation completes.
/// If animation is interrupted (movement, stun, death), the attack is canceled.
/// Animation speed scales with attack speed so animation duration always matches cooldown.
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

    // Attack animation state
    protected bool isPlayingAttackAnimation;
    protected Coroutine attackAnimCoroutine;

    // Status effects
    protected bool isStunned;
    protected float stunEndTime;
    protected bool isMindControlled;
    protected UnitBase mindController;
    protected float mindControlEndTime;

    // Visual state
    protected Color originalSpriteColor = Color.white;
    protected bool hasStoredOriginalColor = false;

    // World health bar
    protected WorldHealthBar worldHealthBar;
    [Header("Health Bar")]
    [SerializeField] protected Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);

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

        // Store original sprite color for damage/heal flashes
        if (spriteRenderer != null && !hasStoredOriginalColor)
        {
            originalSpriteColor = spriteRenderer.color;
            hasStoredOriginalColor = true;
        }

        // Create world health bar
        CreateWorldHealthBar();
    }

    /// <summary>
    /// Create world-space health bar above this enemy.
    /// Uses simple colored bar with proper fill animation.
    /// </summary>
    protected virtual void CreateWorldHealthBar()
    {
        worldHealthBar = WorldHealthBar.Create(transform, transform, healthBarOffset);
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

        int damageAmount = Mathf.RoundToInt(amount);
        currentHealth -= damageAmount;

        // Visual feedback - flash red
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        // Update world health bar
        if (worldHealthBar != null)
        {
            worldHealthBar.UpdateHealth(currentHealth, maxHealth);
        }

        // Spawn floating damage number
        Vector3 numberPos = transform.position + new Vector3(0, 1f, 0);
        FloatingDamageNumber.SpawnDamage(numberPos, damageAmount);

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDamaged(gameObject, damageAmount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        // Use stored original color to avoid getting stuck on red
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null && !isDead)
            spriteRenderer.color = originalSpriteColor;
    }

    protected virtual void Die()
    {
        isDead = true;

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Destroy health bar
        if (worldHealthBar != null)
        {
            Destroy(worldHealthBar.gameObject);
            worldHealthBar = null;
        }

        // Award XP to player
        if (GameManager.Instance != null && xpReward > 0)
        {
            GameManager.Instance.AddXP(xpReward);
            Debug.Log($"[EnemyBase] {gameObject.name} died, awarded {xpReward} XP");
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
        if (isPlayingAttackAnimation) return false;
        return Time.time - lastAttackTime >= attackCooldown;
    }

    /// <summary>
    /// Start attack: plays animation, then deals damage ONLY when animation completes.
    /// If interrupted by movement, stun, or death — attack is canceled, no damage dealt.
    /// </summary>
    protected virtual void PerformAttack()
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;
        isPlayingAttackAnimation = true;

        // Get attack animation duration (plays at natural speed)
        float animDuration = GetAttackAnimationDuration();

        bool hasAnimator = animator != null;
        bool hasController = hasAnimator && animator.runtimeAnimatorController != null;
        string controllerName = hasController ? animator.runtimeAnimatorController.name : "NONE";

        Debug.Log($"[EnemyBase.PerformAttack] {gameObject.name} | animator={hasAnimator} | controller={controllerName} | animDuration={animDuration:F3}s | attackCooldown={attackCooldown:F2}s");

        if (hasAnimator)
        {
            // Log all clip names to verify "attack" clip exists
            if (hasController)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                string clipNames = "";
                foreach (var c in clips) clipNames += c.name + $"({c.length:F2}s), ";
                Debug.Log($"[EnemyBase.PerformAttack] {gameObject.name} clips: [{clipNames}]");

                // Log animator state before trigger
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[EnemyBase.PerformAttack] {gameObject.name} PRE-TRIGGER state: hash={stateInfo.shortNameHash} normalizedTime={stateInfo.normalizedTime:F2} speed={animator.speed:F2}");
            }

            animator.SetTrigger(AnimAttack);

            Debug.Log($"[EnemyBase.PerformAttack] {gameObject.name} SetTrigger(Attack) called, trigger hash={AnimAttack}");
        }

        // Start coroutine that waits for animation to complete before dealing damage
        if (attackAnimCoroutine != null)
        {
            StopCoroutine(attackAnimCoroutine);
        }
        attackAnimCoroutine = StartCoroutine(AttackAnimationCoroutine(animDuration));
    }

    /// <summary>
    /// Wait for attack animation to complete, then deal damage.
    /// Checks each frame if attack should be canceled.
    /// </summary>
    protected virtual System.Collections.IEnumerator AttackAnimationCoroutine(float duration)
    {
        float elapsed = 0f;
        float waitDuration = duration;
        string unitName = gameObject.name;

        Debug.Log($"[EnemyBase.AttackCoroutine] {unitName} START waiting {waitDuration:F3}s");

        // Log animator state on first frame after trigger
        if (animator != null)
        {
            yield return null; // wait 1 frame for trigger to take effect
            elapsed += Time.deltaTime;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInTransition = animator.IsInTransition(0);
            Debug.Log($"[EnemyBase.AttackCoroutine] {unitName} FRAME1: stateHash={stateInfo.shortNameHash} isTransition={isInTransition} normalizedTime={stateInfo.normalizedTime:F2} vel={rb?.linearVelocity.magnitude:F3}");
        }

        while (elapsed < waitDuration)
        {
            elapsed += Time.deltaTime;

            // Cancel if dead, stunned, or started moving
            if (isDead || IsStunned)
            {
                Debug.Log($"[EnemyBase.AttackCoroutine] {unitName} CANCELED: dead={isDead} stunned={IsStunned} at {elapsed:F3}s/{waitDuration:F3}s");
                CancelAttackAnimation();
                yield break;
            }

            // Cancel if moved during attack (velocity check)
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Debug.Log($"[EnemyBase.AttackCoroutine] {unitName} CANCELED: velocity={rb.linearVelocity.magnitude:F3} at {elapsed:F3}s/{waitDuration:F3}s");
                CancelAttackAnimation();
                yield break;
            }

            yield return null;
        }

        // Animation completed — deal damage
        Debug.Log($"[EnemyBase.AttackCoroutine] {unitName} COMPLETED after {elapsed:F3}s — dealing damage!");
        isPlayingAttackAnimation = false;
        attackAnimCoroutine = null;
        DealDamage();
    }

    /// <summary>
    /// Cancel an in-progress attack animation. No damage is dealt.
    /// </summary>
    public virtual void CancelAttackAnimation()
    {
        if (!isPlayingAttackAnimation) return;

        Debug.Log($"[EnemyBase.CancelAttack] {gameObject.name} attack animation CANCELED");
        isPlayingAttackAnimation = false;
        attackAnimCoroutine = null;
    }

    /// <summary>
    /// Get the duration of the attack animation clip.
    /// Returns attackCooldown * 0.85 as fallback if no clip found.
    /// For Monk, looks for "heal" clip instead of "attack".
    /// </summary>
    protected float GetAttackAnimationDuration()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

            // First try to find 'attack' clip
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("attack"))
                {
                    Debug.Log($"[EnemyBase.GetAttackDuration] {gameObject.name} found clip '{clip.name}' length={clip.length:F3}s");
                    return clip.length;
                }
            }

            // For Monk - try 'heal' clip
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("heal"))
                {
                    Debug.Log($"[EnemyBase.GetAttackDuration] {gameObject.name} found heal clip '{clip.name}' length={clip.length:F3}s");
                    return clip.length;
                }
            }

            Debug.LogWarning($"[EnemyBase.GetAttackDuration] {gameObject.name} NO 'attack' or 'heal' clip found! Using fallback={attackCooldown * 0.85f:F3}s");
        }
        else
        {
            Debug.LogWarning($"[EnemyBase.GetAttackDuration] {gameObject.name} animator={animator != null} controller={animator?.runtimeAnimatorController != null} — using fallback");
        }
        return attackCooldown * 0.85f; // Fallback
    }

    // Called by animation event or derived class — only after animation completes
    public virtual void DealDamage()
    {
        if (target == null) return;

        float dist = GetDistanceToTarget();
        // 50% buffer so player can't dodge by walking away during attack animation
        // If enemy started attack while player was in range, the attack should connect
        float effectiveRange = attackRange * 1.5f;

        if (dist <= effectiveRange)
        {
            // Try to damage player
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"[EnemyBase.DealDamage] {gameObject.name} hit {target.name} for {damage} damage! dist={dist:F1} effectiveRange={effectiveRange:F1}");
            }
        }
        else
        {
            Debug.Log($"[EnemyBase.DealDamage] {gameObject.name} MISSED - target moved away! dist={dist:F1} > effectiveRange={effectiveRange:F1}");
        }
    }

    /// <summary>
    /// Property: is this enemy currently in an attack animation?
    /// </summary>
    public bool IsAttacking => isPlayingAttackAnimation;

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
    /// Visual feedback is handled by EffectManager.SpawnHealEffect() - no sprite tint.
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
            // Update world health bar
            if (worldHealthBar != null)
            {
                worldHealthBar.UpdateHealth(currentHealth, maxHealth);
            }

            // Spawn floating heal number
            Vector3 numberPos = transform.position + new Vector3(0, 1f, 0);
            FloatingDamageNumber.SpawnHeal(numberPos, actualHeal);

            Debug.Log($"[EnemyBase] {gameObject.name} healed for {actualHeal}. Health: {currentHealth}/{maxHealth}");
        }
    }

    #endregion

    #region Status Effects

    /// <summary>
    /// Apply stun effect. Visual: -10% anim speed, +10% size, swirl effect over head.
    /// </summary>
    public virtual void ApplyStun(float duration)
    {
        // Cancel any in-progress attack
        CancelAttackAnimation();

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

    #region Debug Gizmos

    protected virtual void OnDrawGizmosSelected()
    {
        // Collider — black
        Gizmos.color = GizmosHelper.ColliderColor;
        var col = GetComponent<Collider2D>();
        if (col is CircleCollider2D cc)
            Gizmos.DrawWireSphere(transform.TransformPoint(cc.offset), cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        else if (col is BoxCollider2D bc)
            Gizmos.DrawWireCube(transform.TransformPoint(bc.offset), new Vector3(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y, 0));

        // Attack range — red solid
        Gizmos.color = GizmosHelper.MeleeAttackColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Detection range — yellow solid
        Gizmos.color = GizmosHelper.DetectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Chase range — yellow dashed
        Gizmos.color = GizmosHelper.ChaseColor;
        GizmosHelper.DrawDashedWireCircle(transform.position, chaseRange);
    }

    #endregion
}
