using UnityEngine;
using System.Collections;

/// <summary>
/// Human enemy controlled by EnemyConfig.
/// All enemies are from Blue Units faction.
/// Supports different behaviors: Aggressive, Defensive, Coward, Guard.
/// Drops skulls and resources on death.
/// </summary>
public class HumanEnemy : EnemyBase
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfig config;

    [Header("Ranged Attack")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("State")]
    [SerializeField] private Vector3 homePosition;
    [SerializeField] private bool hasHomePosition;

    [Header("Corpse Settings")]
    [SerializeField] private float corpseLifetime = 10f;
    [SerializeField] private bool canBeDrained = true;

    [Header("Unarmed Pawn Animation")]
    [Tooltip("Sprite to use when unarmed Pawn attacks (knife animation)")]
    [SerializeField] private RuntimeAnimatorController knifeAnimatorController;
    private RuntimeAnimatorController originalAnimatorController;
    private bool isUsingKnifeAnimation = false;

    // Corpse state
    private bool isCorpse = false;
    private bool isBeingDrained = false;
    private Coroutine corpseDecayCoroutine;

    // Behavior state
    private bool isFleeing;
    private float fleeTimer;
    private const float FLEE_DURATION = 3f;

    public EnemyConfig Config => config;
    public EnemyType EnemyTypeEnum => config != null ? config.enemyType : EnemyType.PawnUnarmed;
    public bool IsCorpse => isCorpse;
    public bool IsUnarmedPawn => config != null && config.enemyType == EnemyType.PawnUnarmed;
    public bool IsBeingDrained => isBeingDrained;
    public bool CanBeDrained => canBeDrained && isCorpse && !isBeingDrained;

    protected override void Awake()
    {
        base.Awake();

        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    protected override void Start()
    {
        InitializeFromConfig();
        base.Start();

        if (!hasHomePosition)
        {
            homePosition = transform.position;
            hasHomePosition = true;
        }

        // Store original animator for unarmed Pawn
        if (animator != null)
        {
            originalAnimatorController = animator.runtimeAnimatorController;
        }
    }

    /// <summary>
    /// Initialize stats from EnemyConfig.
    /// </summary>
    public void InitializeFromConfig()
    {
        if (config == null)
        {
            Debug.LogWarning($"[HumanEnemy] No config assigned to {gameObject.name}");
            return;
        }

        maxHealth = config.maxHealth;
        currentHealth = maxHealth;
        damage = config.damage;
        moveSpeed = config.moveSpeed;
        attackRange = config.attackRange;
        attackCooldown = config.AttackCooldown;
        detectionRange = config.detectionRange;
        chaseRange = config.chaseRange;
        xpReward = config.xpReward;
    }

    /// <summary>
    /// Initialize with specific config (called by spawners).
    /// </summary>
    public void Initialize(EnemyConfig enemyConfig)
    {
        config = enemyConfig;
        InitializeFromConfig();
    }

    /// <summary>
    /// Set home position for defensive/guard behavior.
    /// </summary>
    public void SetHomePosition(Vector3 position)
    {
        homePosition = position;
        hasHomePosition = true;
    }

    protected override void Update()
    {
        if (isDead) return;
        if (IsStunned) return;
        if (IsMindControlled)
        {
            FindEnemyTarget();
            return;
        }

        // Update flee timer
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0)
            {
                isFleeing = false;
            }
        }

        // Behavior-specific logic
        switch (config?.behavior ?? EnemyBehavior.Aggressive)
        {
            case EnemyBehavior.Coward:
                UpdateCowardBehavior();
                break;

            case EnemyBehavior.Defensive:
                UpdateDefensiveBehavior();
                break;

            case EnemyBehavior.Guard:
                UpdateGuardBehavior();
                break;

            case EnemyBehavior.Ranged:
                UpdateRangedBehavior();
                break;

            case EnemyBehavior.Aggressive:
            default:
                UpdateAggressiveBehavior();
                break;
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || IsStunned) return;

        if (isFleeing)
        {
            Flee();
        }
        else if (target != null)
        {
            float dist = GetDistanceToTarget();

            // Special handling for Ranged behavior (Archer)
            if (config != null && config.behavior == EnemyBehavior.Ranged)
            {
                float fearRange = config.fearRange;

                // If too close - flee (handled in UpdateRangedBehavior)
                if (dist < fearRange)
                {
                    // Just wait for flee to be triggered in Update
                }
                // In attack range - stop and shoot
                else if (dist <= attackRange && CanAttack())
                {
                    StopMovement();
                    FaceTarget();
                    PerformAttack();
                }
                // Too far - move closer (handled in UpdateRangedBehavior)
                else if (dist > attackRange)
                {
                    // Movement handled in UpdateRangedBehavior
                }
                else
                {
                    // In range but cooling down - just stand
                    StopMovement();
                    FaceTarget();
                }
            }
            else
            {
                // Normal melee behavior
                if (dist <= attackRange && CanAttack())
                {
                    StopMovement();
                    FaceTarget();
                    PerformAttack();
                }
                else if (dist <= chaseRange)
                {
                    MoveTowardsTarget();
                }
                else
                {
                    // Target out of range
                    target = null;
                    ReturnToHome();
                }
            }
        }
        else
        {
            ReturnToHome();
        }

        UpdateMoveAnimation();
    }

    #region Behavior Updates

    private void UpdateAggressiveBehavior()
    {
        // Always look for player/units to attack
        if (target == null || (target.GetComponent<EnemyBase>() == null && IsMindControlled))
        {
            FindTarget();
        }
    }

    private void UpdateDefensiveBehavior()
    {
        // Only attack if target is close to home position
        if (target == null)
        {
            FindTarget();
        }

        if (target != null)
        {
            float distToHome = Vector2.Distance(homePosition, target.position);
            if (distToHome > detectionRange)
            {
                target = null; // Target too far from home
            }
        }
    }

    private void UpdateGuardBehavior()
    {
        // Similar to defensive but with patrol (could add patrol points)
        UpdateDefensiveBehavior();
    }

    private void UpdateRangedBehavior()
    {
        // Ranged behavior: shoot from distance, flee if too close
        if (target == null)
        {
            FindTarget();
        }

        if (target != null)
        {
            float dist = GetDistanceToTarget();
            float fearRange = config?.fearRange ?? 4f;
            float preferredRange = config?.preferredRange ?? 7f;

            // If player is too close - flee!
            if (dist < fearRange && !isFleeing)
            {
                StartFleeing();
            }
            // If player is at good range - stop and prepare to shoot
            else if (dist >= fearRange && dist <= attackRange)
            {
                // Stop moving while in attack range (but not fleeing)
                if (!isFleeing)
                {
                    StopMovement();
                    FaceTarget();
                }
            }
            // If player is too far - move closer to preferred range
            else if (dist > attackRange && !isFleeing)
            {
                // Move towards preferred range
                float targetDist = preferredRange;
                if (dist > targetDist + 1f)
                {
                    MoveTowardsTarget();
                }
            }
        }
    }

    private void UpdateCowardBehavior()
    {
        // Check if player/units are too close - flee!
        FindTarget();

        if (target != null && !isFleeing)
        {
            float dist = GetDistanceToTarget();
            if (dist < (config?.fleeRange ?? 100f))
            {
                StartFleeing();
            }
        }
    }

    #endregion

    #region Movement

    private void MoveTowardsTarget()
    {
        if (target == null || rb == null) return;

        Vector2 direction = GetDirectionToTarget();
        rb.linearVelocity = direction * moveSpeed;
        FaceTarget();
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ReturnToHome()
    {
        if (!hasHomePosition || rb == null) return;

        float distToHome = Vector2.Distance(transform.position, homePosition);
        if (distToHome > 0.5f)
        {
            Vector2 direction = ((Vector2)homePosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed * 0.5f; // Slower return speed

            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
        else
        {
            StopMovement();
        }
    }

    private void StartFleeing()
    {
        isFleeing = true;
        fleeTimer = FLEE_DURATION;
    }

    private void Flee()
    {
        if (target == null || rb == null) return;

        // Run away from target
        Vector2 direction = -GetDirectionToTarget();
        rb.linearVelocity = direction * moveSpeed * 1.2f; // Faster when fleeing

        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    private void UpdateMoveAnimation()
    {
        if (animator != null && rb != null)
        {
            animator.SetBool(AnimIsMoving, rb.linearVelocity.magnitude > 0.1f);
        }
    }

    #endregion

    #region Combat

    protected override void PerformAttack()
    {
        if (!CanAttack()) return;

        // RULE: Can only attack when standing still (not moving)
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            return; // Can't attack while moving
        }

        lastAttackTime = Time.time;

        // Switch to knife animation for unarmed Pawn
        if (IsUnarmedPawn && !isUsingKnifeAnimation)
        {
            SwitchToKnifeAnimation();
        }

        if (animator != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Play attack sound
        if (config?.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(config.attackSound, transform.position);
        }

        // For ranged enemies, spawn projectile
        if (config != null && config.isRanged)
        {
            SpawnProjectile();
        }
        else
        {
            // Melee - deal damage directly
            DealDamage();
        }

        // Schedule return to unarmed animation for Pawn
        if (IsUnarmedPawn && isUsingKnifeAnimation)
        {
            StartCoroutine(ReturnToUnarmedAnimation());
        }
    }

    /// <summary>
    /// Switch unarmed Pawn to knife animation when attacking.
    /// </summary>
    private void SwitchToKnifeAnimation()
    {
        if (animator == null || knifeAnimatorController == null) return;

        animator.runtimeAnimatorController = knifeAnimatorController;
        isUsingKnifeAnimation = true;
        Debug.Log($"[HumanEnemy] Unarmed Pawn switched to knife animation");
    }

    /// <summary>
    /// Return unarmed Pawn to normal animation after attack.
    /// </summary>
    private IEnumerator ReturnToUnarmedAnimation()
    {
        // Wait for attack animation to complete
        yield return new WaitForSeconds(attackCooldown * 0.8f);

        if (animator != null && originalAnimatorController != null && !isDead)
        {
            animator.runtimeAnimatorController = originalAnimatorController;
            isUsingKnifeAnimation = false;
            Debug.Log($"[HumanEnemy] Unarmed Pawn returned to normal animation");
        }
    }

    public override void DealDamage()
    {
        if (target == null) return;

        float dist = GetDistanceToTarget();
        if (dist <= attackRange)
        {
            // Damage player
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                return;
            }

            // Damage army unit
            UnitBase unit = target.GetComponent<UnitBase>();
            if (unit != null && !unit.IsDead)
            {
                unit.TakeDamage(damage);
                return;
            }

            // Damage other enemy (when mind controlled)
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && enemy != this)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void SpawnProjectile()
    {
        if (config?.projectilePrefab == null || target == null) return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        Vector3 targetPos = target.position;

        GameObject projectile = Instantiate(config.projectilePrefab, spawnPos, Quaternion.identity);

        // Setup projectile damage
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Setup(damage, gameObject, true); // true = damages player/units

            // For Archer - use arc trajectory
            if (config.enemyType == EnemyType.Archer)
            {
                // Calculate flight time based on distance
                float distance = Vector2.Distance(spawnPos, targetPos);
                float flightTime = distance / config.projectileSpeed;
                flightTime = Mathf.Clamp(flightTime, 0.5f, 2f); // Min 0.5s, max 2s flight

                // Arc height based on distance
                float arcHeight = Mathf.Clamp(distance * 0.3f, 1f, 4f);

                projScript.SetupArcTrajectory(targetPos, flightTime, arcHeight);
            }
            else
            {
                // Regular straight projectile
                Vector2 direction = GetDirectionToTarget();
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

                Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
                if (projRb != null)
                {
                    projRb.linearVelocity = direction * config.projectileSpeed;
                }

                // Destroy after time for non-arc projectiles
                Destroy(projectile, 5f);
            }
        }
    }

    #endregion

    #region Death & Loot

    protected override void Die()
    {
        isDead = true;

        StopMovement();

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger(AnimDie);
        }

        // Play death sound
        if (config?.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(config.deathSound, transform.position);
        }

        // Drop loot (resources)
        DropLoot();

        // Note: Skulls are no longer dropped automatically - they come from Soul Drain
        // DropSkull(); // Disabled - souls are extracted via Soul Drain

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDied(gameObject);
        }

        // Disable collider for movement but keep trigger for drain detection
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        Debug.Log($"[HumanEnemy] {config?.displayName ?? "Enemy"} died!");

        // Become a corpse instead of destroying immediately
        if (canBeDrained)
        {
            BecomeCorpse();
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }

    /// <summary>
    /// Transform into a corpse that can be drained for souls.
    /// </summary>
    private void BecomeCorpse()
    {
        isCorpse = true;

        // Re-enable a trigger collider for drain detection
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true;
        }

        // Start decay timer
        corpseDecayCoroutine = StartCoroutine(CorpseDecay());

        Debug.Log($"[HumanEnemy] {config?.displayName ?? "Enemy"} is now a corpse. Drain within {corpseLifetime}s!");
    }

    /// <summary>
    /// Corpse decay coroutine - destroys corpse after timeout.
    /// </summary>
    private IEnumerator CorpseDecay()
    {
        yield return new WaitForSeconds(corpseLifetime);

        // Only destroy if still a corpse (wasn't drained)
        if (isCorpse)
        {
            Debug.Log($"[HumanEnemy] Corpse decayed without being drained.");
            Destroy(gameObject);
        }
    }

    #region Soul Drain Interface

    /// <summary>
    /// Called when a player starts draining this corpse.
    /// </summary>
    public void OnDrainStarted()
    {
        if (!isCorpse) return;

        isBeingDrained = true;

        // Stop decay while being drained
        if (corpseDecayCoroutine != null)
        {
            StopCoroutine(corpseDecayCoroutine);
            corpseDecayCoroutine = null;
        }

        Debug.Log($"[HumanEnemy] Soul drain started on {config?.displayName ?? "corpse"}");
    }

    /// <summary>
    /// Called when drain is canceled.
    /// </summary>
    public void OnDrainCanceled()
    {
        if (!isCorpse) return;

        isBeingDrained = false;

        // Resume decay
        corpseDecayCoroutine = StartCoroutine(CorpseDecay());

        Debug.Log($"[HumanEnemy] Soul drain canceled on {config?.displayName ?? "corpse"}");
    }

    /// <summary>
    /// Called when the corpse has been fully drained.
    /// </summary>
    public void OnDrained()
    {
        isCorpse = false;
        isBeingDrained = false;

        // Stop decay coroutine
        if (corpseDecayCoroutine != null)
        {
            StopCoroutine(corpseDecayCoroutine);
            corpseDecayCoroutine = null;
        }

        Debug.Log($"[HumanEnemy] Soul drained from {config?.displayName ?? "corpse"}! Converting to army unit.");

        // Destroy with a small visual delay
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// Get the unit type this enemy converts to when drained.
    /// </summary>
    public string GetDrainUnitType()
    {
        return EnemyToUnitMapping.GetUnitType(EnemyTypeEnum);
    }

    #endregion

    private void DropLoot()
    {
        if (config == null || ResourceSpawner.Instance == null) return;

        // Drop meat
        int meatAmount = Random.Range(config.meatMin, config.meatMax + 1);
        if (meatAmount > 0)
        {
            ResourceSpawner.Instance.SpawnMeat(transform.position, meatAmount);
        }

        // Drop wood (chance based)
        if (Random.value <= config.woodChance)
        {
            int woodAmount = Random.Range(config.woodMin, config.woodMax + 1);
            if (woodAmount > 0)
            {
                ResourceSpawner.Instance.SpawnWood(transform.position, woodAmount);
            }
        }

        // Drop gold (chance based)
        if (Random.value <= config.goldChance)
        {
            int goldAmount = Random.Range(config.goldMin, config.goldMax + 1);
            if (goldAmount > 0)
            {
                ResourceSpawner.Instance.SpawnGold(transform.position, goldAmount);
            }
        }
    }

    private void DropSkull()
    {
        if (ResourceSpawner.Instance == null) return;

        // Always drop exactly 1 skull per enemy
        ResourceSpawner.Instance.SpawnSkull(transform.position);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config?.detectionRange ?? detectionRange);

        // Chase range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config?.chaseRange ?? chaseRange);

        // Home position
        if (hasHomePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, homePosition);
            Gizmos.DrawWireSphere(homePosition, 0.5f);
        }

        // Flee range (for cowards)
        if (config?.behavior == EnemyBehavior.Coward)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, config.fleeRange);
        }
    }
}
