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

    [Header("Wander Behavior")]
    [Tooltip("Radius for wandering from home position (in tiles, 1 tile ≈ 1 unit)")]
    [SerializeField] private float wanderRadius = 2.5f;
    [Tooltip("Minimum time to stay idle before wandering")]
    [SerializeField] private float idleMinTime = 3f;
    [Tooltip("Maximum time to stay idle before wandering")]
    [SerializeField] private float idleMaxTime = 8f;
    [Tooltip("Enable wander behavior when not in combat")]
    [SerializeField] private bool enableWander = true;

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

    // Wander state
    private enum WanderState { Idle, Moving }
    private WanderState currentWanderState = WanderState.Idle;
    private float wanderStateTimer;
    private Vector3 wanderTarget;

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
            // Create a spawn point offset for ranged enemies (bow position = upper body)
            GameObject spawnPointObj = new GameObject("ProjectileSpawnPoint");
            spawnPointObj.transform.SetParent(transform);
            spawnPointObj.transform.localPosition = new Vector3(0f, 0.5f, 0f); // Upper body where bow is
            projectileSpawnPoint = spawnPointObj.transform;
        }
    }

    protected override void Start()
    {
        InitializeFromConfig();
        base.Start();

        // Ensure we have a collider for combat detection
        EnsureCollider();

        // Ensure Y-sorting for proper draw order
        if (GetComponent<YSortingRenderer>() == null)
        {
            gameObject.AddComponent<YSortingRenderer>();
        }

        // Ensure ranged enemies have a projectile prefab
        if (config != null && config.isRanged && config.projectilePrefab == null)
        {
            TryLoadArcherProjectilePrefab();
        }

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

        // Initialize wander state with random idle time
        currentWanderState = WanderState.Idle;
        wanderStateTimer = Random.Range(idleMinTime, idleMaxTime);
        wanderTarget = homePosition;
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
            else if (config != null && config.behavior == EnemyBehavior.Coward)
            {
                // Coward behavior - NEVER attack or chase, only flee
                // The flee is handled above when isFleeing is true
                // If we get here, we're not fleeing - just stand still or wander
                StopMovement();
            }
            else
            {
                // Normal melee behavior (Aggressive, Defensive, Guard)
                if (dist <= attackRange)
                {
                    // In attack range - stop and face target
                    StopMovement();
                    FaceTarget();
                    // Attack if cooldown is ready
                    if (CanAttack())
                    {
                        PerformAttack();
                    }
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
        // Cowards ALWAYS try to flee when they see the player
        FindTarget();

        if (target != null)
        {
            float dist = GetDistanceToTarget();
            // Flee if within detection range (not just fleeRange which is too small)
            float effectiveFleeRange = config?.detectionRange ?? 8f;
            if (dist < effectiveFleeRange && !isFleeing)
            {
                StartFleeing();
            }
            // Keep fleeing as long as player is visible
            else if (isFleeing && dist < effectiveFleeRange)
            {
                fleeTimer = FLEE_DURATION; // Reset timer to keep fleeing
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

        // If wander is enabled, use wander behavior instead of just returning home
        if (enableWander)
        {
            UpdateWanderBehavior();
            return;
        }

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

    /// <summary>
    /// Wander behavior: alternate between idle and moving to random points near home.
    /// </summary>
    private void UpdateWanderBehavior()
    {
        if (rb == null) return;

        wanderStateTimer -= Time.fixedDeltaTime;

        switch (currentWanderState)
        {
            case WanderState.Idle:
                StopMovement();
                if (wanderStateTimer <= 0)
                {
                    // Pick a new wander target within radius of home
                    Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
                    wanderTarget = homePosition + new Vector3(randomOffset.x, randomOffset.y, 0);
                    currentWanderState = WanderState.Moving;
                    // Time to reach target (estimate based on distance and speed)
                    float distance = Vector2.Distance(transform.position, wanderTarget);
                    wanderStateTimer = (distance / (moveSpeed * 0.5f)) + 0.5f; // Add buffer
                }
                break;

            case WanderState.Moving:
                float distToTarget = Vector2.Distance(transform.position, wanderTarget);

                // Check if too far from home - return to home area first
                float distFromHome = Vector2.Distance(transform.position, homePosition);
                if (distFromHome > wanderRadius * 1.5f)
                {
                    // Too far from home, pick new target closer to home
                    Vector2 toHome = ((Vector2)homePosition - (Vector2)transform.position).normalized;
                    wanderTarget = homePosition + new Vector3(toHome.x * wanderRadius * 0.5f, toHome.y * wanderRadius * 0.5f, 0);
                }

                if (distToTarget > 0.3f && wanderStateTimer > 0)
                {
                    // Move towards wander target
                    Vector2 direction = ((Vector2)wanderTarget - (Vector2)transform.position).normalized;
                    rb.linearVelocity = direction * moveSpeed * 0.5f; // Slower wander speed

                    if (spriteRenderer != null && direction.x != 0)
                    {
                        spriteRenderer.flipX = direction.x < 0;
                    }
                }
                else
                {
                    // Reached target or timeout - go idle
                    StopMovement();
                    currentWanderState = WanderState.Idle;
                    wanderStateTimer = Random.Range(idleMinTime, idleMaxTime);
                }
                break;
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
        // Check for valid animator with assigned controller to avoid errors
        if (animator != null && animator.runtimeAnimatorController != null && rb != null)
        {
            animator.SetBool(AnimIsMoving, rb.linearVelocity.magnitude > 0.1f);
        }
    }

    #endregion

    #region Combat

    protected override void PerformAttack()
    {
        if (!CanAttack())
        {
            Debug.Log($"[HumanEnemy] {name} CanAttack=false, cooldown remaining: {attackCooldown - (Time.time - lastAttackTime):F1}s");
            return;
        }

        // RULE: Can only attack when standing still (not moving)
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Debug.Log($"[HumanEnemy] {name} can't attack - still moving (vel={rb.linearVelocity.magnitude:F2})");
            return; // Can't attack while moving
        }

        Debug.Log($"[HumanEnemy] {name} ATTACKING! isRanged={config?.isRanged}, hasPrefab={config?.projectilePrefab != null}");
        lastAttackTime = Time.time;

        // Switch to knife animation for unarmed Pawn
        if (IsUnarmedPawn && !isUsingKnifeAnimation)
        {
            SwitchToKnifeAnimation();
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Play attack sound
        if (config?.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(config.attackSound, transform.position);
        }

        // For ranged enemies, spawn projectile synced with animation end
        if (config != null && config.isRanged)
        {
            // Delay projectile spawn to sync with attack animation completion
            StartCoroutine(SpawnProjectileAfterAnimation());
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

    /// <summary>
    /// Delay projectile spawn to sync with attack animation end.
    /// </summary>
    private IEnumerator SpawnProjectileAfterAnimation()
    {
        // Get attack animation length from animator
        float animDelay = 0.5f; // Default fallback
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("attack"))
                {
                    animDelay = clip.length * 0.85f; // Fire at 85% of animation
                    break;
                }
            }
        }

        yield return new WaitForSeconds(animDelay);

        // Only fire if still alive and target exists
        if (!isDead && target != null)
        {
            SpawnProjectile();
        }
    }

    private void SpawnProjectile()
    {
        if (target == null) return;

        // Check if projectile prefab is missing - try to load it
        if (config != null && config.projectilePrefab == null && config.isRanged)
        {
            TryLoadArcherProjectilePrefab();
        }

        if (config?.projectilePrefab == null)
        {
            Debug.LogWarning($"[HumanEnemy] {name} has no projectilePrefab! Cannot shoot.");
            return;
        }

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        GameObject projectile = Instantiate(config.projectilePrefab, spawnPos, Quaternion.identity);

        // Try ArrowProjectile first (for arc trajectory with ground stick)
        ArrowProjectile arrowScript = projectile.GetComponent<ArrowProjectile>();
        if (arrowScript != null)
        {
            // ArrowProjectile handles parabolic flight, collision, ground stick, and dissolve
            arrowScript.Setup(damage, gameObject, target, true); // true = damages player/units
            Debug.Log($"[HumanEnemy] Archer fired arrow at {target.name}");
            return;
        }

        // Fallback to Projectile component
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Setup(damage, gameObject, true);

            // For Archer without ArrowProjectile - use arc trajectory on Projectile
            if (config.enemyType == EnemyType.Archer)
            {
                float distance = Vector2.Distance(spawnPos, target.position);
                float flightTime = distance / config.projectileSpeed;
                flightTime = Mathf.Clamp(flightTime, 0.5f, 2f);
                float arcHeight = Mathf.Clamp(distance * 0.3f, 1f, 4f);
                projScript.SetupArcTrajectory(target.position, flightTime, arcHeight);
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

        // Play death sound
        if (config?.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(config.deathSound, transform.position);
        }

        // No resource drops from enemies — only skulls
        // (Exception: Peasant carrying resources drops them — handled in Peasant.cs)

        // Spawn death dust + skull simultaneously
        // Dust covers the unit, when it fades the skull is already there underneath
        Vector3 deathPos = transform.position;

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.SpawnDeathEffect(deathPos);
            EffectManager.Instance.SpawnSkullPickup(deathPos);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDied(gameObject);
        }

        Debug.Log($"[HumanEnemy] {config?.displayName ?? "Enemy"} died! Skull will appear after dust.");

        // Destroy immediately — visuals handled by death effect and skull pickup
        Destroy(gameObject);
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

    /// <summary>
    /// Try to load projectile prefab for ranged enemies (Archer).
    /// </summary>
    private void TryLoadArcherProjectilePrefab()
    {
        if (config == null || !config.isRanged) return;
        if (config.projectilePrefab != null) return;

        // Try to load from Resources
        GameObject prefab = Resources.Load<GameObject>("ArcherArrow");
        if (prefab != null)
        {
            config.projectilePrefab = prefab;
            Debug.Log("[HumanEnemy] Loaded ArcherArrow prefab from Resources");
            return;
        }

        prefab = Resources.Load<GameObject>("TowerArrow");
        if (prefab != null)
        {
            config.projectilePrefab = prefab;
            Debug.Log("[HumanEnemy] Loaded TowerArrow prefab from Resources");
            return;
        }

        #if UNITY_EDITOR
        // Try AssetDatabase - multiple paths
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/ArcherArrow.prefab",
            "Assets/Prefabs/TowerArrow.prefab",
            "Assets/Prefabs/Arrow.prefab"
        };

        foreach (string path in prefabPaths)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                config.projectilePrefab = prefab;
                Debug.Log($"[HumanEnemy] Loaded projectile prefab from {path}");
                return;
            }
        }
        #endif

        Debug.LogWarning("[HumanEnemy] Could not find projectile prefab for Archer");
    }

    /// <summary>
    /// Ensure the enemy has a collider for combat detection.
    /// </summary>
    private void EnsureCollider()
    {
        // Check if we already have a collider
        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider != null) return;

        // Add CircleCollider2D for combat detection and physics blocking
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;  // Match LevelEditor ENEMY_COLLIDER_RADIUS
        col.isTrigger = false;

        Debug.Log($"[HumanEnemy] Added missing collider to {name}");
    }

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
