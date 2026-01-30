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
    public EnemyType EnemyTypeEnum => config != null ? config.enemyType : EnemyType.Pawn;
    public bool IsCorpse => isCorpse;
    public bool IsUnarmedPawn => config != null && config.enemyType == EnemyType.Pawn;
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

            case EnemyBehavior.Support:
                UpdateSupportBehavior();
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

        // Don't move while playing attack animation — movement would cancel it
        if (isPlayingAttackAnimation)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            UpdateMoveAnimation();
            return;
        }

        if (isFleeing)
        {
            Flee();
        }
        else if (target != null)
        {
            float dist = GetDistanceToTarget();
            // Use 20% range buffer so unit doesn't need to move and break attacks
            float effectiveRange = attackRange * 1.2f;

            string behavior = config != null ? config.behavior.ToString() : "NoConfig";

            // Log every 60 frames to avoid spam
            if (Time.frameCount % 60 == 0)
            {
                bool canAtk = CanAttack();
                Debug.Log($"[HumanEnemy.FixedUpdate] {gameObject.name} behavior={behavior} dist={dist:F2} effectiveRange={effectiveRange:F2} attackRange={attackRange:F2} chaseRange={chaseRange:F2} canAttack={canAtk} target={target.name}");
            }

            // Special handling for Ranged behavior (Archer)
            if (config != null && config.behavior == EnemyBehavior.Ranged)
            {
                float fearRange = config.fearRange;

                if (dist < fearRange)
                {
                    // Too close - flee (triggered in Update)
                }
                else if (dist <= effectiveRange && CanAttack())
                {
                    StopMovement();
                    FaceTarget();
                    PerformAttack();
                }
                else if (dist > attackRange)
                {
                    // Movement handled in UpdateRangedBehavior
                }
                else
                {
                    StopMovement();
                    FaceTarget();
                }
            }
            else if (config != null && config.behavior == EnemyBehavior.Coward)
            {
                // Pawn (Coward) - only attacks in desperate mode (knife)
                float desperateRange = config?.desperateRange ?? 2f;
                if (isUsingKnifeAnimation && dist <= desperateRange * 1.2f)
                {
                    // In desperate mode - attack!
                    if (dist <= attackRange * 1.2f)
                    {
                        StopMovement();
                        FaceTarget();
                        if (CanAttack())
                        {
                            PerformAttack();
                        }
                    }
                    else
                    {
                        // Move towards target to attack
                        MoveTowardsTarget();
                    }
                }
                else
                {
                    // Not in desperate mode - flee is handled in UpdateCowardBehavior
                    StopMovement();
                }
            }
            else if (config != null && config.behavior == EnemyBehavior.Support)
            {
                // Monk (Support) - doesn't attack player, only heals allies
                // Fleeing is handled in isFleeing check above
                // Here we move towards wounded ally if target is set (it's an ally, not player)
                if (target != null && target.GetComponent<EnemyBase>() != null)
                {
                    // Target is a wounded ally - move towards them
                    float distToAlly = Vector2.Distance(transform.position, target.position);
                    float healRange = config?.healRange ?? 8f;

                    if (distToAlly > healRange * 0.8f) // Move until 80% of heal range
                    {
                        MoveTowardsTarget();
                    }
                    else
                    {
                        StopMovement();
                    }
                }
                else
                {
                    StopMovement();
                }
            }
            else
            {
                // Normal melee behavior (Aggressive, Defensive, Guard)
                if (dist <= effectiveRange)
                {
                    StopMovement();
                    FaceTarget();
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
                    target = null;
                    ReturnToHome();
                }
            }
        }
        else
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[HumanEnemy.FixedUpdate] {gameObject.name} NO TARGET — returning home");
            }
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
        // Pawn (Coward) behavior:
        // 1. When player is within Fear range - flee
        // 2. When player is within Desperate range - switch to knife and attack
        // 3. When player exits Desperate range - put knife away and flee again
        FindTarget();

        if (target != null)
        {
            float dist = GetDistanceToTarget();
            float fearRange = config?.fearRange ?? 16f;
            float desperateRange = config?.desperateRange ?? 2f;

            // If player is too close (within desperate range) - switch to knife attack mode
            if (dist <= desperateRange)
            {
                // Stop fleeing, switch to desperate attack mode
                isFleeing = false;

                // Switch to knife animation if not already
                if (!isUsingKnifeAnimation)
                {
                    SwitchToKnifeAnimation();
                }

                // Attack the player (handled in FixedUpdate)
            }
            // If player is within fear range but outside desperate range - flee
            else if (dist <= fearRange)
            {
                // Return to unarmed animation if was in knife mode
                if (isUsingKnifeAnimation)
                {
                    StartCoroutine(ReturnToUnarmedAnimation());
                }

                // Flee from player
                if (!isFleeing)
                {
                    StartFleeing();
                }
                else
                {
                    fleeTimer = FLEE_DURATION; // Keep fleeing
                }
            }
            // Player is outside fear range - calm down
            else
            {
                isFleeing = false;
                target = null;
            }
        }
        else
        {
            // No target - return to normal and wander
            isFleeing = false;
            if (isUsingKnifeAnimation)
            {
                StartCoroutine(ReturnToUnarmedAnimation());
            }
        }
    }

    /// <summary>
    /// Support (Monk) behavior:
    /// 1. Does NOT attack player - no target seeking for attack
    /// 2. If player is within Fear range - flee from player (stops healing)
    /// 3. If wounded ally is within Heal range - heal them continuously
    /// 4. Otherwise wander around home position
    /// </summary>
    private void UpdateSupportBehavior()
    {
        // Check for player to flee from (NOT to attack!)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float fearRange = config?.fearRange ?? 8f;
        float healRange = config?.healRange ?? 8f;

        if (player != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, player.transform.position);

            // If player is too close - flee!
            if (distToPlayer <= fearRange)
            {
                // Stop healing if we were healing
                if (isHealingContinuously)
                {
                    StopMonkHealing();
                }

                target = player.transform; // Set target for fleeing direction
                if (!isFleeing)
                {
                    StartFleeing();
                    Debug.Log($"[Monk] Player too close ({distToPlayer:F1} <= {fearRange:F1}) - fleeing!");
                }
                else
                {
                    fleeTimer = FLEE_DURATION; // Keep fleeing
                }
                return;
            }
        }

        // Not fleeing - stop and look for allies to heal
        isFleeing = false;
        target = null; // Monk does NOT target player for attack

        // Find wounded ally to heal
        EnemyBase woundedAlly = FindNearestWoundedAlly();
        if (woundedAlly != null)
        {
            float distToAlly = Vector2.Distance(transform.position, woundedAlly.transform.position);

            // If ally is in heal range - start continuous healing (if not already)
            if (distToAlly <= healRange)
            {
                if (!isHealingContinuously)
                {
                    PerformMonkHeal(); // Starts continuous healing loop
                }
                // else: already healing - loop handles finding new targets
            }
            // If ally is outside heal range - move towards them
            else
            {
                // Stop healing while moving
                if (isHealingContinuously)
                {
                    StopMonkHealing();
                }
                // Set temporary target for movement (not for attack)
                target = woundedAlly.transform;
            }
        }
        else
        {
            // No wounded allies - stop healing
            if (isHealingContinuously)
            {
                StopMonkHealing();
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
            Debug.Log($"[HumanEnemy.PerformAttack] {gameObject.name} CanAttack=false (stunned={IsStunned} playing={isPlayingAttackAnimation} cooldown={(Time.time - lastAttackTime):F2}/{attackCooldown:F2})");
            return;
        }

        // RULE: Can only attack when standing still (not moving)
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Debug.Log($"[HumanEnemy.PerformAttack] {gameObject.name} BLOCKED: still moving vel={rb.linearVelocity.magnitude:F3}");
            return;
        }

        string enemyType = config != null ? config.enemyType.ToString() : "NoConfig";
        string behavior = config != null ? config.behavior.ToString() : "NoConfig";
        bool isRanged = config != null && config.isRanged;
        Debug.Log($"[HumanEnemy.PerformAttack] {gameObject.name} type={enemyType} behavior={behavior} isRanged={isRanged} isUnarmedPawn={IsUnarmedPawn}");

        // Monk heals allies instead of attacking
        if (config != null && config.enemyType == EnemyType.Monk)
        {
            PerformMonkHeal();
            return;
        }

        // Switch to knife animation for unarmed Pawn
        if (IsUnarmedPawn && !isUsingKnifeAnimation)
        {
            Debug.Log($"[HumanEnemy.PerformAttack] {gameObject.name} switching to knife animation");
            SwitchToKnifeAnimation();
        }

        // Play attack sound
        if (config?.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(config.attackSound, transform.position);
        }

        // For ranged enemies, use ranged attack coroutine (projectile after animation)
        if (config != null && config.isRanged)
        {
            lastAttackTime = Time.time;
            isPlayingAttackAnimation = true;

            float animDuration = GetAttackAnimationDuration();
            if (animator != null)
            {
                animator.SetTrigger(AnimAttack);
            }

            if (attackAnimCoroutine != null) StopCoroutine(attackAnimCoroutine);
            attackAnimCoroutine = StartCoroutine(RangedAttackAnimCoroutine(animDuration));
        }
        else
        {
            Debug.Log($"[HumanEnemy.PerformAttack] {gameObject.name} MELEE path → calling base.PerformAttack()");
            // Melee - use base class animation-bound attack (damage on animation end)
            base.PerformAttack();

            // Schedule return to unarmed animation for Pawn
            if (IsUnarmedPawn && isUsingKnifeAnimation)
            {
                StartCoroutine(ReturnToUnarmedAnimation());
            }
        }
    }

    /// <summary>
    /// Ranged attack: spawn projectile on the LAST FRAME of animation (not after).
    /// This makes the arrow appear to leave the bow at the right moment.
    /// Canceled if interrupted by stun or death.
    /// </summary>
    private IEnumerator RangedAttackAnimCoroutine(float animDuration)
    {
        // Spawn projectile at ~75% of animation (second-to-last frame)
        // For 8-frame animation at 12fps (0.667s), this is around frame 6 (0.5s)
        float projectileSpawnTime = animDuration * 0.75f;
        bool projectileSpawned = false;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;

            if (isDead || IsStunned)
            {
                CancelAttackAnimation();
                yield break;
            }

            // Spawn projectile on the last frame of animation
            if (!projectileSpawned && elapsed >= projectileSpawnTime)
            {
                projectileSpawned = true;
                if (!isDead && target != null)
                {
                    SpawnProjectile();
                }
            }

            yield return null;
        }

        isPlayingAttackAnimation = false;
        attackAnimCoroutine = null;
    }

    #region Monk Healing

    private Coroutine monkHealCoroutine;
    private EnemyBase currentHealTarget;
    private bool isHealingContinuously = false;

    /// <summary>
    /// Get heal amount from config (with fallback).
    /// </summary>
    private float MonkHealAmount => config?.healAmount ?? 15f;

    /// <summary>
    /// Get heal range from config (with fallback).
    /// </summary>
    private float MonkHealRange => config?.healRange ?? 8f;

    /// <summary>
    /// Monk heals the nearest wounded ally.
    /// Starts continuous healing loop that repeats while there are wounded allies.
    /// </summary>
    private void PerformMonkHeal()
    {
        // Don't start if already healing
        if (isHealingContinuously) return;

        // Find nearest wounded ally in range
        EnemyBase healTarget = FindNearestWoundedAlly();
        if (healTarget == null) return;

        // Set flag BEFORE starting coroutine to prevent multiple starts
        isHealingContinuously = true;

        // Start continuous healing loop
        if (monkHealCoroutine != null) StopCoroutine(monkHealCoroutine);
        monkHealCoroutine = StartCoroutine(ContinuousHealingLoop());

        Debug.Log($"[Monk] Started continuous healing loop for {healTarget.name}");
    }

    /// <summary>
    /// Stop monk healing (called when fleeing or dying).
    /// </summary>
    private void StopMonkHealing()
    {
        if (monkHealCoroutine != null)
        {
            StopCoroutine(monkHealCoroutine);
            monkHealCoroutine = null;
        }
        isHealingContinuously = false;
        isPlayingAttackAnimation = false;
        currentHealTarget = null;
    }

    /// <summary>
    /// Continuous healing loop - keeps healing while there are wounded allies.
    /// Animation loops automatically.
    /// </summary>
    private IEnumerator ContinuousHealingLoop()
    {
        // isHealingContinuously is set in PerformMonkHeal() before starting this coroutine
        float animDuration = GetAttackAnimationDuration();
        Debug.Log($"[Monk] ContinuousHealingLoop started, animDuration={animDuration:F2}s");

        while (true)
        {
            // Check for interruption conditions
            if (isDead || IsStunned || isFleeing)
            {
                StopMonkHealing();
                yield break;
            }

            // Find wounded ally
            EnemyBase healTarget = FindNearestWoundedAlly();
            if (healTarget == null)
            {
                // No more wounded allies - stop healing
                StopMonkHealing();
                yield break;
            }

            currentHealTarget = healTarget;

            // Face the ally being healed
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = healTarget.transform.position.x < transform.position.x;
            }

            // Start heal animation
            lastAttackTime = Time.time;
            isPlayingAttackAnimation = true;

            if (animator != null)
            {
                // Monk uses "Heal" trigger, not "Attack"
                animator.SetTrigger("Heal");
            }

            // Wait for animation to complete
            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;

                // Check for interruption during animation
                if (isDead || IsStunned || isFleeing)
                {
                    StopMonkHealing();
                    yield break;
                }

                // Cancel if moved during heal
                if (rb != null && rb.linearVelocity.magnitude > 0.1f)
                {
                    StopMonkHealing();
                    yield break;
                }

                yield return null;
            }

            isPlayingAttackAnimation = false;

            // Apply heal at end of animation
            if (!isDead && healTarget != null && !healTarget.IsDead)
            {
                float dist = Vector2.Distance(transform.position, healTarget.transform.position);
                if (dist <= MonkHealRange * 1.2f)
                {
                    healTarget.Heal(MonkHealAmount);
                    Debug.Log($"[Monk] Healed {healTarget.name} for {MonkHealAmount} HP (now {healTarget.CurrentHealth}/{healTarget.MaxHealth})");

                    // Spawn heal effect on target
                    if (EffectManager.Instance != null)
                    {
                        GameObject fx = EffectManager.Instance.SpawnHealEffect(healTarget.transform.position);
                        Debug.Log($"[Monk] SpawnHealEffect result: {(fx != null ? fx.name : "NULL")}");
                    }
                    else
                    {
                        Debug.LogWarning("[Monk] EffectManager.Instance is NULL - cannot spawn heal effect!");
                    }
                }
            }

            // Small pause before next heal cycle (attack cooldown)
            float cooldownRemaining = attackCooldown - animDuration;
            if (cooldownRemaining > 0)
            {
                yield return new WaitForSeconds(cooldownRemaining);
            }

            // Loop continues - will check for wounded allies again at top
        }
    }

    private EnemyBase FindNearestWoundedAlly()
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var enemy in allEnemies)
        {
            if (enemy == this || enemy.IsDead) continue;
            if (enemy.CurrentHealth >= enemy.MaxHealth) continue; // Not wounded

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < nearestDist && dist <= MonkHealRange)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    #endregion

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
        if (target == null)
        {
            Debug.Log($"[HumanEnemy.DealDamage] {gameObject.name} target is NULL!");
            return;
        }

        float dist = GetDistanceToTarget();
        Debug.Log($"[HumanEnemy.DealDamage] {gameObject.name} → {target.name} dist={dist:F2} range={attackRange:F2} buffer={attackRange * 1.2f:F2} inRange={dist <= attackRange * 1.2f}");

        if (dist <= attackRange * 1.2f) // 20% buffer to avoid missing due to slight drift
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

    // SpawnProjectileAfterAnimation replaced by RangedAttackAnimCoroutine above

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

        // Calculate spawn position based on archer's facing direction
        // Arrow should come from the upper corner of the sprite (where bow/hand is)
        Vector3 spawnPos = GetProjectileSpawnPosition();

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

    /// <summary>
    /// Calculate the position where projectile should spawn based on archer's facing direction.
    /// Arrow spawns from the upper corner of the sprite (where the bow/hand is).
    /// </summary>
    private Vector3 GetProjectileSpawnPosition()
    {
        // Base offset: upper body height
        float yOffset = 0.4f;
        // Horizontal offset: bow is on the side the archer is facing
        float xOffset = 0.5f;

        // If sprite is flipped, arrow comes from left side; otherwise from right
        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        if (facingLeft)
        {
            xOffset = -xOffset;
        }

        return transform.position + new Vector3(xOffset, yOffset, 0f);
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

        // Award XP to player
        if (GameManager.Instance != null && xpReward > 0)
        {
            GameManager.Instance.AddXP(xpReward);
            Debug.Log($"[HumanEnemy] {config?.displayName ?? gameObject.name} awarded {xpReward} XP");
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

    protected override void OnDrawGizmosSelected()
    {
        // Collider — black
        Gizmos.color = GizmosHelper.ColliderColor;
        var col = GetComponent<Collider2D>();
        if (col is CircleCollider2D cc)
            Gizmos.DrawWireSphere(transform.TransformPoint(cc.offset), cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        else if (col is BoxCollider2D bc)
            Gizmos.DrawWireCube(transform.TransformPoint(bc.offset), new Vector3(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y, 0));

        // Check if this is a Pawn (Coward) or Monk (Support) - they don't have Attack/Chase
        bool isPawn = config != null && config.enemyType == EnemyType.Pawn;
        bool isMonk = config != null && config.behavior == EnemyBehavior.Support;
        bool hideAttackAndChase = isPawn || isMonk;

        // Melee / Ranged attack range — NOT for Pawn (he only has Desperate) or Monk (no attack)
        if (!hideAttackAndChase)
        {
            bool isRanged = config != null && config.isRanged;
            float atkRange = config?.attackRange ?? attackRange;
            if (atkRange > 0)
            {
                Gizmos.color = isRanged ? GizmosHelper.RangedAttackColor : GizmosHelper.MeleeAttackColor;
                if (isRanged)
                    GizmosHelper.DrawDashedWireCircle(transform.position, atkRange);
                else
                    Gizmos.DrawWireSphere(transform.position, atkRange);
            }
        }

        // Chase range — yellow — NOT for Pawn (he only flees) or Monk (no chase)
        if (!hideAttackAndChase)
        {
            float chaseRangeValue = config?.chaseRange ?? chaseRange;
            if (chaseRangeValue > 0)
            {
                Gizmos.color = GizmosHelper.ChaseColor;
                Gizmos.DrawWireSphere(transform.position, chaseRangeValue);
            }
        }

        // Fear range — white (only for enemies that have it: Pawn, Archer, Monk)
        float fearRangeValue = config?.fearRange ?? 0f;
        if (fearRangeValue > 0)
        {
            Gizmos.color = GizmosHelper.FearColor;
            Gizmos.DrawWireSphere(transform.position, fearRangeValue);
        }

        // Heal range — green (Monk only)
        float healRangeValue = config?.healRange ?? 0f;
        if (healRangeValue > 0)
        {
            Gizmos.color = GizmosHelper.HealRangeColor;
            Gizmos.DrawWireSphere(transform.position, healRangeValue);
        }

        // Desperate attack range — pink (Pawn only, knife)
        float desperateRangeValue = config?.desperateRange ?? 0f;
        if (desperateRangeValue > 0)
        {
            Gizmos.color = GizmosHelper.DesperateAttackColor;
            Gizmos.DrawWireSphere(transform.position, desperateRangeValue);
        }

        // Resource detection range — brown (Pawn, Miner)
        float resourceRangeValue = config?.resourceDetectionRange ?? 0f;
        if (resourceRangeValue > 0)
        {
            Gizmos.color = GizmosHelper.ResourceDetectionColor;
            Gizmos.DrawWireSphere(transform.position, resourceRangeValue);
        }

        // Wander radius — gray (read from config)
        float wanderRadiusValue = config?.wanderRadius ?? wanderRadius;
        Gizmos.color = GizmosHelper.WanderColor;
        Vector3 home = hasHomePosition ? homePosition : transform.position;
        Gizmos.DrawWireSphere(home, wanderRadiusValue);

        // Home position line
        if (hasHomePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, homePosition);
        }
    }
}
