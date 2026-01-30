using UnityEngine;
using System.Collections;

/// <summary>
/// Peasant - a resource-collecting enemy that:
/// - Wanders around when idle
/// - Spots any resource (except Skull) and runs to collect it
/// - Carries resource to nearest building while avoiding player
/// - Deposits resource in building
/// - ALWAYS flees from player (fear radius = archer's attack range = 30)
/// - When cornered (can't flee), switches to Combat with knife
/// Per GDD: Peasant collects ANY resource (Meat, Wood, Gold) and carries it to nearest house.
/// Two forces when carrying: attraction to building + fear of player.
/// </summary>
public class Peasant : EnemyBase
{
    public enum PeasantState
    {
        Idle,               // Standing still
        Wandering,          // Walking around home position
        Fleeing,            // Running away from player
        RunningToResource,  // Spotted resource, going to pick it up
        CarryingResource,   // Has resource, heading to building while avoiding player
        Combat              // Cornered - fighting with knife
    }

    [Header("Configuration")]
    [SerializeField] private EnemyConfig config;

    [Header("Peasant Settings")]
    [SerializeField] private float pickupRadius = 0.5f;
    [SerializeField] private float resourceDetectionRadius = 8f;
    [SerializeField] private float buildingDeliveryRadius = 1f;
    [SerializeField] private float playerFearRadius = 16f; // Fear range from config
    [SerializeField] private float playerFearStrength = 0.7f;
    [SerializeField] private float fleeSpeed = 3f;

    [Header("Cornered Combat")]
    [SerializeField] private float corneredCheckInterval = 0.5f;
    [SerializeField] private float corneredDistance = 6f; // If player within this range and pawn can't move, considered cornered
    [SerializeField] private float corneredTime = 1.0f; // Must be cornered this long before fighting
    [SerializeField] private float knifeAttackRange = 2f; // Desperate range from config
    [SerializeField] private int knifeDamage = 5; // From config.damage
    [SerializeField] private float knifeAttackCooldown = 1f; // From config.attackCooldown
    [SerializeField] private float combatExitDistance = 8f; // If player goes this far, exit combat

    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 2.5f;
    [SerializeField] private float idleMinTime = 3f;
    [SerializeField] private float idleMaxTime = 8f;
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float carrySpeed = 2f;

    // Public accessor for config
    public EnemyConfig Config => config;

    [Header("Visual Feedback")]
    [SerializeField] private Color carryingTint = new Color(1f, 1f, 0.8f);
    [SerializeField] private GameObject resourceIndicator;

    // State
    private PeasantState currentState = PeasantState.Idle;
    private Vector3 homePosition;
    private Vector3 wanderTarget;
    private float idleTimer;
    private float currentIdleDuration;

    // Resource carrying
    private ResourcePickup.ResourceType carriedResourceType;
    private int carriedAmount = 0;
    private ResourcePickup targetResource;
    private MonoBehaviour targetBuilding; // EnemyHouse, Watchtower, or Castle

    // Flee & cornered state
    private float corneredTimer = 0f;
    private Vector3 lastFleePosition;
    private float corneredCheckTimer = 0f;
    private bool isCornered = false;

    // Combat state
    private float lastCombatAttackTime;

    // Animation - loaded at runtime from sprite sheets
    private bool isMoving;
    private bool spritesLoaded = false;
    private bool useManualSprites = false; // True when carrying resource (disable Animator)

    // Loaded sprite arrays for carrying (all frames for animation)
    private Sprite[] loadedIdleMeatFrames, loadedRunMeatFrames;
    private Sprite[] loadedIdleWoodFrames, loadedRunWoodFrames;
    private Sprite[] loadedIdleGoldFrames, loadedRunGoldFrames;
    private Sprite[] loadedIdleKnifeFrames, loadedRunKnifeFrames, loadedInteractKnifeFrames;
    // Single references for quick access (first frame)
    private Sprite loadedInteractKnife;
    private Sprite normalIdleSprite;

    // Manual animation state
    private float manualAnimTimer = 0f;
    private int manualAnimFrame = 0;
    private float manualAnimFrameRate = 8f; // FPS for manual sprite animation

    // Animator controllers
    private RuntimeAnimatorController originalAnimatorController;
    private RuntimeAnimatorController knifeAnimatorController;

    // Sprite paths (Blue Units Pawn)
    private const string PAWN_SPRITES = "Assets/Sprites/Tiny Swords (Free Pack)/Tiny Swords (Free Pack)/Units/Blue Units/Pawn";

    protected override void Start()
    {
        // Initialize from config BEFORE base.Start()
        InitializeFromConfig();

        base.Start();
        homePosition = transform.position;
        currentState = PeasantState.Idle;
        StartNewIdlePeriod();

        // Store original sprite and animator
        if (spriteRenderer != null)
        {
            normalIdleSprite = spriteRenderer.sprite;
        }
        if (animator != null)
        {
            originalAnimatorController = animator.runtimeAnimatorController;
        }

        // Load carrying & knife sprites from sprite sheets
        LoadCarryingSprites();
        LoadKnifeAnimatorController();
    }

    /// <summary>
    /// Initialize stats from EnemyConfig (like HumanEnemy).
    /// </summary>
    public void InitializeFromConfig()
    {
        if (config == null)
        {
            Debug.LogWarning($"[Peasant] No config assigned to {gameObject.name}, using defaults");
            return;
        }

        // Base stats
        maxHealth = config.maxHealth;
        currentHealth = maxHealth;
        damage = config.damage;
        moveSpeed = config.moveSpeed;
        xpReward = config.xpReward;

        // Peasant-specific stats from config
        playerFearRadius = config.fearRange;
        knifeAttackRange = config.desperateRange;
        resourceDetectionRadius = config.resourceDetectionRange;
        wanderRadius = config.wanderRadius;
        knifeDamage = config.damage;
        knifeAttackCooldown = config.AttackCooldown;

        Debug.Log($"[Peasant] Initialized from config: fear={playerFearRadius}, desperate={knifeAttackRange}, resource={resourceDetectionRadius}");
    }

    /// <summary>
    /// Initialize with specific config (called by spawners/LevelEditor).
    /// </summary>
    public void Initialize(EnemyConfig enemyConfig)
    {
        config = enemyConfig;
        InitializeFromConfig();
    }

    /// <summary>
    /// Load all frames from each carrying sprite sheet for animated sprite swap.
    /// </summary>
    private void LoadCarryingSprites()
    {
        loadedIdleMeatFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Idle Meat.png");
        loadedRunMeatFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Run Meat.png");
        loadedIdleWoodFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Idle Wood.png");
        loadedRunWoodFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Run Wood.png");
        loadedIdleGoldFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Idle Gold.png");
        loadedRunGoldFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Run Gold.png");
        loadedIdleKnifeFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Idle Knife.png");
        loadedRunKnifeFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Run Knife.png");
        loadedInteractKnifeFrames = LoadAllSprites($"{PAWN_SPRITES}/Pawn_Interact Knife.png");

        // Keep single reference for knife attack
        if (loadedInteractKnifeFrames != null && loadedInteractKnifeFrames.Length > 0)
            loadedInteractKnife = loadedInteractKnifeFrames[0];

        spritesLoaded = (loadedIdleMeatFrames != null || loadedIdleWoodFrames != null || loadedIdleGoldFrames != null);
        if (spritesLoaded)
        {
            Debug.Log("[Peasant] Carrying sprites loaded successfully");
        }
        else
        {
            Debug.LogWarning("[Peasant] Could not load carrying sprites from sprite sheets");
        }
    }

    /// <summary>
    /// Load all sprites from a sliced sprite sheet asset.
    /// </summary>
    private Sprite[] LoadAllSprites(string assetPath)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets != null)
        {
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }
            if (sprites.Count > 0) return sprites.ToArray();
        }
#endif
        return null;
    }

    /// <summary>
    /// Try to find the knife animator controller in the project.
    /// </summary>
    private void LoadKnifeAnimatorController()
    {
#if UNITY_EDITOR
        // Try to find the PawnKnife or Pawn_Knife controller
        string[] guids = UnityEditor.AssetDatabase.FindAssets("Pawn_Controller t:RuntimeAnimatorController",
            new[] { "Assets/Animations/Enemies" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            knifeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        }
#endif
    }

    private float debugLogTimer = 0f;

    protected override void Update()
    {
        if (isDead) return;
        if (IsStunned) return;

        // Debug: log state periodically
        debugLogTimer += Time.deltaTime;
        if (debugLogTimer >= 1f)
        {
            debugLogTimer = 0f;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            float pd = p != null ? Vector2.Distance(transform.position, p.transform.position) : -1f;
            Debug.Log($"[Peasant] State={currentState} playerDist={pd:F1} hp={currentHealth}/{maxHealth} useManual={useManualSprites}");
        }

        // Check player distance for fear in ALL states (except Combat)
        if (currentState != PeasantState.Combat)
        {
            CheckPlayerFear();
        }

        switch (currentState)
        {
            case PeasantState.Idle:
                UpdateIdle();
                break;
            case PeasantState.Wandering:
                // Movement in FixedUpdate
                break;
            case PeasantState.Fleeing:
                UpdateFleeing();
                break;
            case PeasantState.RunningToResource:
                UpdateRunningToResource();
                break;
            case PeasantState.CarryingResource:
                UpdateCarryingResource();
                break;
            case PeasantState.Combat:
                UpdateCombat();
                break;
        }

        // Knife attack when player is super close (works in ANY state)
        if (currentState != PeasantState.Combat)
        {
            CheckProximityKnifeAttack();
        }

        // Check for nearby resources when idle/wandering (not fleeing)
        if (currentState == PeasantState.Idle || currentState == PeasantState.Wandering)
        {
            CheckForNearbyResources();
        }

        UpdateAnimation();
    }

    protected override void FixedUpdate()
    {
        if (isDead || IsStunned) return;

        switch (currentState)
        {
            case PeasantState.Wandering:
                MoveTowardsTarget(wanderTarget, wanderSpeed);
                CheckWanderArrival();
                break;
            case PeasantState.Fleeing:
                FleeFromPlayer();
                break;
            case PeasantState.RunningToResource:
                if (targetResource != null)
                {
                    MoveTowardsTarget(targetResource.transform.position, moveSpeed);
                }
                break;
            case PeasantState.CarryingResource:
                MoveTowardsBuildingWithFear();
                break;
            case PeasantState.Combat:
                UpdateCombatMovement();
                break;
        }
    }

    #region Fear & Flee

    /// <summary>
    /// Check if player is within fear radius. If so, start fleeing.
    /// </summary>
    private void CheckPlayerFear()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);

        if (playerDist < playerFearRadius && currentState != PeasantState.Fleeing)
        {
            // Drop resource pickup target if we were running to one
            if (currentState == PeasantState.RunningToResource)
            {
                targetResource = null;
            }

            StartFleeing();
        }
    }

    private void StartFleeing()
    {
        currentState = PeasantState.Fleeing;
        corneredTimer = 0f;
        isCornered = false;
        lastFleePosition = transform.position;
        corneredCheckTimer = 0f;
        Debug.Log("[Peasant] Fleeing from player!");
    }

    private void UpdateFleeing()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            StartNewIdlePeriod();
            return;
        }

        float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);

        // Player left fear range - return to idle
        if (playerDist >= playerFearRadius)
        {
            corneredTimer = 0f;
            isCornered = false;
            StartNewIdlePeriod();
            return;
        }

        // DESPERATE MODE: If player catches up and is within desperate range - enter combat immediately!
        // This is the main trigger - Pawn switches to knife when player gets too close
        if (playerDist <= knifeAttackRange)
        {
            Debug.Log($"[Peasant] Player caught up! dist={playerDist:F1} <= desperateRange={knifeAttackRange:F1} — entering Combat!");
            EnterCombat();
            return;
        }

        // Check if cornered (not making progress fleeing) - backup trigger
        corneredCheckTimer += Time.deltaTime;
        if (corneredCheckTimer >= corneredCheckInterval)
        {
            corneredCheckTimer = 0f;
            float movedDistance = Vector2.Distance(transform.position, lastFleePosition);

            if (movedDistance < 0.5f && playerDist < corneredDistance)
            {
                // Not moving much and player is close - might be cornered
                corneredTimer += corneredCheckInterval;

                if (corneredTimer >= corneredTime)
                {
                    // Cornered! Switch to combat
                    EnterCombat();
                    return;
                }
            }
            else
            {
                corneredTimer = 0f;
            }

            lastFleePosition = transform.position;
        }
    }

    private void FleeFromPlayer()
    {
        if (rb == null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Vector2 fromPlayer = ((Vector2)transform.position - (Vector2)playerObj.transform.position).normalized;
        rb.linearVelocity = fromPlayer * fleeSpeed;

        // Face movement direction
        if (spriteRenderer != null && fromPlayer.x != 0)
        {
            spriteRenderer.flipX = fromPlayer.x < 0;
        }

        isMoving = true;
    }

    #endregion

    #region Combat (Cornered with Knife)

    // Peasant knife attack animation state
    private bool isPlayingKnifeAttack;
    private Coroutine knifeAttackCoroutine;

    /// <summary>
    /// Even outside Combat state, stab with knife if player is right next to pawn.
    /// </summary>
    private void CheckProximityKnifeAttack()
    {
        if (isPlayingKnifeAttack) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);
        float effectiveRange = knifeAttackRange;
        if (playerDist <= effectiveRange && Time.time - lastCombatAttackTime >= knifeAttackCooldown)
        {
            lastCombatAttackTime = Time.time;
            Debug.Log($"[Peasant] Proximity knife stab! dist={playerDist:F1}");
            StartKnifeAttack(playerObj);
        }
    }

    private void EnterCombat()
    {
        currentState = PeasantState.Combat;
        isCornered = true;
        lastCombatAttackTime = -999f; // Ensure first attack fires immediately

        // Switch to knife visuals
        SwitchToKnifeVisuals(true);

        // Start attack animation on entering combat — damage on completion
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float dist = Vector2.Distance(transform.position, playerObj.transform.position);
            Debug.Log($"[Peasant] Cornered! Drawing knife! Player dist={dist:F1}");
            lastCombatAttackTime = Time.time;
            StartKnifeAttack(playerObj);
        }
        else
        {
            Debug.Log("[Peasant] Cornered! Drawing knife and fighting!");
        }
    }

    private void ExitCombat()
    {
        isCornered = false;
        corneredTimer = 0f;

        // Switch back to normal visuals
        SwitchToKnifeVisuals(false);

        // Return to fleeing (will transition to idle if player far enough)
        StartFleeing();

        Debug.Log("[Peasant] Exiting combat, back to normal.");
    }

    private void SwitchToKnifeVisuals(bool useKnife)
    {
        if (useKnife)
        {
            // Disable animator, use manual knife sprites
            if (animator != null)
            {
                animator.enabled = false;
            }
            useManualSprites = true;
        }
        else
        {
            // Re-enable animator (unless carrying)
            useManualSprites = (carriedAmount > 0);
            if (!useManualSprites && animator != null)
            {
                animator.enabled = true;
                if (originalAnimatorController != null)
                {
                    animator.runtimeAnimatorController = originalAnimatorController;
                }
            }
        }
    }

    private void UpdateCombat()
    {
        // Don't interrupt ongoing knife attack
        if (isPlayingKnifeAttack) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            ExitCombat();
            return;
        }

        float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);

        // If player moves far enough, exit combat and return to normal
        if (playerDist > combatExitDistance)
        {
            ExitCombat();
            return;
        }

        // Face the player
        if (spriteRenderer != null)
        {
            float dx = playerObj.transform.position.x - transform.position.x;
            if (dx != 0) spriteRenderer.flipX = dx < 0;
        }

        // Attack if cooldown elapsed
        float timeSinceLastAttack = Time.time - lastCombatAttackTime;
        if (timeSinceLastAttack >= knifeAttackCooldown)
        {
            lastCombatAttackTime = Time.time;
            StartKnifeAttack(playerObj);
        }
    }

    /// <summary>
    /// In combat, pawn moves toward player to get within knife range, then stops.
    /// Don't move during attack animation.
    /// </summary>
    private void UpdateCombatMovement()
    {
        if (rb == null) return;

        // Don't move during attack animation
        if (isPlayingKnifeAttack)
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            return;
        }

        float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);

        if (playerDist > knifeAttackRange * 0.9f)
        {
            Vector2 toPlayer = ((Vector2)playerObj.transform.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = toPlayer * moveSpeed;

            if (spriteRenderer != null && toPlayer.x != 0)
            {
                spriteRenderer.flipX = toPlayer.x < 0;
            }
            isMoving = true;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
        }
    }

    /// <summary>
    /// Start knife attack: play animation, deal damage ONLY when animation completes.
    /// If pawn moves or is stunned during animation, attack is canceled.
    /// Animation speed scales with knifeAttackCooldown.
    /// </summary>
    private void StartKnifeAttack(GameObject playerObj)
    {
        if (isPlayingKnifeAttack) return;

        isPlayingKnifeAttack = true;
        Debug.Log($"[Peasant] Starting knife attack animation on {playerObj.name}");

        if (knifeAttackCoroutine != null) StopCoroutine(knifeAttackCoroutine);
        knifeAttackCoroutine = StartCoroutine(KnifeAttackAnimCoroutine(playerObj));
    }

    /// <summary>
    /// Play knife attack animation frames, then deal damage on completion.
    /// </summary>
    private IEnumerator KnifeAttackAnimCoroutine(GameObject playerObj)
    {
        // Calculate animation duration scaled to match attack cooldown
        int frameCount = (loadedInteractKnifeFrames != null) ? loadedInteractKnifeFrames.Length : 0;
        float baseAnimDuration = frameCount > 0 ? frameCount / manualAnimFrameRate : 0.5f;
        float targetDuration = knifeAttackCooldown * 0.85f;
        float speedMultiplier = baseAnimDuration > 0 ? baseAnimDuration / targetDuration : 1f;
        float scaledFrameRate = manualAnimFrameRate * speedMultiplier;

        // Play animation frames
        if (loadedInteractKnifeFrames != null && loadedInteractKnifeFrames.Length > 0 && spriteRenderer != null)
        {
            float frameDuration = 1f / scaledFrameRate;
            foreach (var frame in loadedInteractKnifeFrames)
            {
                // Cancel if dead, stunned, or started moving
                if (isDead || IsStunned)
                {
                    CancelKnifeAttack();
                    yield break;
                }
                if (rb != null && rb.linearVelocity.magnitude > 0.1f)
                {
                    CancelKnifeAttack();
                    yield break;
                }

                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(frameDuration);
            }
        }
        else
        {
            // No frames — wait for cooldown duration
            float elapsed = 0f;
            while (elapsed < targetDuration)
            {
                elapsed += Time.deltaTime;
                if (isDead || IsStunned || (rb != null && rb.linearVelocity.magnitude > 0.1f))
                {
                    CancelKnifeAttack();
                    yield break;
                }
                yield return null;
            }
        }

        // Animation completed — deal damage
        isPlayingKnifeAttack = false;
        knifeAttackCoroutine = null;

        if (playerObj == null)
        {
            Debug.Log("[Peasant] Knife attack completed but player gone!");
            yield break;
        }

        // Deal damage
        PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = playerObj.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(knifeDamage);
            Debug.Log($"[Peasant] Knife hit! {knifeDamage} damage dealt to {playerObj.name}");
        }
        else
        {
            Debug.LogError($"[Peasant] NO PlayerHealth found on {playerObj.name}!");
        }
    }

    private void CancelKnifeAttack()
    {
        isPlayingKnifeAttack = false;
        knifeAttackCoroutine = null;
        Debug.Log("[Peasant] Knife attack canceled (interrupted)");
    }

    #endregion

    #region Idle & Wander

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= currentIdleDuration)
        {
            StartWandering();
        }
    }

    private void StartNewIdlePeriod()
    {
        currentState = PeasantState.Idle;
        idleTimer = 0f;
        currentIdleDuration = Random.Range(idleMinTime, idleMaxTime);
        StopMovement();

        // Restore animator if we were using manual sprites without carrying
        if (carriedAmount <= 0 && currentState != PeasantState.Combat)
        {
            useManualSprites = false;
            if (animator != null)
            {
                animator.enabled = true;
            }
        }
    }

    private void StartWandering()
    {
        // Pick random point within wander radius from home
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = homePosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        currentState = PeasantState.Wandering;
    }

    private void CheckWanderArrival()
    {
        float dist = Vector2.Distance(transform.position, wanderTarget);
        if (dist < 0.3f)
        {
            StartNewIdlePeriod();
        }
    }

    #endregion

    #region Resource Detection & Collection

    private void CheckForNearbyResources()
    {
        // Find all resource pickups in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, resourceDetectionRadius);

        ResourcePickup closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in colliders)
        {
            ResourcePickup pickup = col.GetComponent<ResourcePickup>();
            if (pickup == null) continue;

            // Skip skulls - peasants don't collect souls
            if (pickup.Type == ResourcePickup.ResourceType.Skull) continue;

            float dist = Vector2.Distance(transform.position, pickup.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }

        if (closest != null)
        {
            targetResource = closest;
            currentState = PeasantState.RunningToResource;
            Debug.Log($"[Peasant] Spotted {closest.Type} at {closest.transform.position}");
        }
    }

    private void UpdateRunningToResource()
    {
        // Check if resource still exists
        if (targetResource == null)
        {
            StartNewIdlePeriod();
            return;
        }

        // Check if close enough to pick up
        float dist = Vector2.Distance(transform.position, targetResource.transform.position);
        if (dist <= pickupRadius)
        {
            PickupResource();
        }
    }

    private void PickupResource()
    {
        if (targetResource == null) return;

        // Store resource info
        carriedResourceType = targetResource.Type;
        carriedAmount = targetResource.Amount;

        Debug.Log($"[Peasant] Picked up {carriedAmount} {carriedResourceType}");

        // Destroy the pickup
        Destroy(targetResource.gameObject);
        targetResource = null;

        // Update visuals for carrying
        UpdateCarryingVisuals(true);

        // Find nearest building to deliver to
        FindTargetBuilding();

        if (targetBuilding != null)
        {
            currentState = PeasantState.CarryingResource;
        }
        else
        {
            // No building found - drop resource and return to idle
            DropCarriedResource();
            StartNewIdlePeriod();
        }
    }

    #endregion

    #region Carrying & Delivery

    private void FindTargetBuilding()
    {
        targetBuilding = null;
        float closestDist = float.MaxValue;

        // Check EnemyHouse
        EnemyHouse[] houses = Object.FindObjectsByType<EnemyHouse>(FindObjectsSortMode.None);
        foreach (var house in houses)
        {
            if (house.IsDestroyed) continue;
            float dist = Vector2.Distance(transform.position, house.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetBuilding = house;
            }
        }

        // Check Watchtower
        Watchtower[] towers = Object.FindObjectsByType<Watchtower>(FindObjectsSortMode.None);
        foreach (var tower in towers)
        {
            if (tower.IsDestroyed) continue;
            float dist = Vector2.Distance(transform.position, tower.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetBuilding = tower;
            }
        }

        // Check Castle
        Castle[] castles = Object.FindObjectsByType<Castle>(FindObjectsSortMode.None);
        foreach (var castle in castles)
        {
            if (castle.IsDestroyed) continue;
            float dist = Vector2.Distance(transform.position, castle.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetBuilding = castle;
            }
        }

        if (targetBuilding != null)
        {
            Debug.Log($"[Peasant] Target building: {targetBuilding.name}");
        }
    }

    private void UpdateCarryingResource()
    {
        if (targetBuilding == null)
        {
            // Building was destroyed - find new one or drop resource
            FindTargetBuilding();
            if (targetBuilding == null)
            {
                DropCarriedResource();
                StartNewIdlePeriod();
                return;
            }
        }

        // Check if reached building
        float dist = Vector2.Distance(transform.position, targetBuilding.transform.position);
        if (dist <= buildingDeliveryRadius)
        {
            DeliverResource();
        }
    }

    /// <summary>
    /// Movement with two forces: attraction to building + repulsion from player.
    /// </summary>
    private void MoveTowardsBuildingWithFear()
    {
        if (targetBuilding == null || rb == null) return;

        Vector2 targetPos = targetBuilding.transform.position;
        Vector2 toBuilding = ((Vector2)targetPos - (Vector2)transform.position).normalized;

        // Fear force from player
        Vector2 fearForce = Vector2.zero;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float playerDist = Vector2.Distance(transform.position, playerObj.transform.position);
            if (playerDist < playerFearRadius)
            {
                Vector2 fromPlayer = ((Vector2)transform.position - (Vector2)playerObj.transform.position).normalized;
                // Stronger fear when closer
                float fearIntensity = 1f - (playerDist / playerFearRadius);
                fearForce = fromPlayer * fearIntensity * playerFearStrength;
            }
        }

        // Combine forces
        Vector2 finalDirection = (toBuilding + fearForce).normalized;
        rb.linearVelocity = finalDirection * carrySpeed;

        // Face movement direction
        if (spriteRenderer != null && finalDirection.x != 0)
        {
            spriteRenderer.flipX = finalDirection.x < 0;
        }

        isMoving = rb.linearVelocity.magnitude > 0.1f;
    }

    private void DeliverResource()
    {
        Debug.Log($"[Peasant] Delivered {carriedAmount} {carriedResourceType} to {targetBuilding.name}");

        // Store resource in building
        if (targetBuilding is EnemyHouse house)
        {
            house.StoreResource(carriedResourceType, carriedAmount);
        }
        else if (targetBuilding is Watchtower tower)
        {
            tower.StoreResource(carriedResourceType, carriedAmount);
        }
        else if (targetBuilding is Castle castle)
        {
            castle.StoreResource(carriedResourceType, carriedAmount);
        }

        // Clear carrying state
        carriedAmount = 0;
        targetBuilding = null;

        // Update visuals
        UpdateCarryingVisuals(false);

        // Return to idle
        StartNewIdlePeriod();
    }

    private void DropCarriedResource()
    {
        if (carriedAmount <= 0) return;

        if (ResourceSpawner.Instance != null)
        {
            Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.3f;

            switch (carriedResourceType)
            {
                case ResourcePickup.ResourceType.Meat:
                    ResourceSpawner.Instance.SpawnMeat(dropPos, carriedAmount);
                    break;
                case ResourcePickup.ResourceType.Wood:
                    ResourceSpawner.Instance.SpawnWood(dropPos, carriedAmount);
                    break;
                case ResourcePickup.ResourceType.Gold:
                    ResourceSpawner.Instance.SpawnGold(dropPos, carriedAmount);
                    break;
            }

            Debug.Log($"[Peasant] Dropped {carriedAmount} {carriedResourceType}");
        }

        carriedAmount = 0;
        UpdateCarryingVisuals(false);
    }

    #endregion

    #region Visuals & Animation

    private void UpdateCarryingVisuals(bool isCarrying)
    {
        if (isCarrying && spritesLoaded)
        {
            // Disable Animator — use manual sprite swap for carrying
            if (animator != null)
            {
                animator.enabled = false;
            }
            useManualSprites = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = carryingTint;
            }

            if (resourceIndicator != null)
            {
                resourceIndicator.SetActive(true);
            }
        }
        else
        {
            // Re-enable Animator when not carrying (unless in combat)
            if (currentState != PeasantState.Combat)
            {
                useManualSprites = false;
                if (animator != null)
                {
                    animator.enabled = true;
                    if (originalAnimatorController != null)
                    {
                        animator.runtimeAnimatorController = originalAnimatorController;
                    }
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            if (resourceIndicator != null)
            {
                resourceIndicator.SetActive(false);
            }
        }
    }

    private void UpdateAnimation()
    {
        if (rb != null)
        {
            isMoving = rb.linearVelocity.magnitude > 0.1f;
        }

        // Animator handles animation when not using manual sprites
        if (!useManualSprites)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(AnimIsMoving, isMoving);
            }
            return;
        }

        // Manual sprite animation for carrying / combat
        if (spriteRenderer == null) return;

        // Advance frame timer
        manualAnimTimer += Time.deltaTime;
        float frameDuration = 1f / manualAnimFrameRate;
        if (manualAnimTimer >= frameDuration)
        {
            manualAnimTimer -= frameDuration;
            manualAnimFrame++;
        }

        Sprite[] frames = null;

        if (currentState == PeasantState.Combat)
        {
            frames = isMoving ? loadedRunKnifeFrames : loadedIdleKnifeFrames;
        }
        else if (carriedAmount > 0)
        {
            switch (carriedResourceType)
            {
                case ResourcePickup.ResourceType.Meat:
                    frames = isMoving ? loadedRunMeatFrames : loadedIdleMeatFrames;
                    break;
                case ResourcePickup.ResourceType.Wood:
                    frames = isMoving ? loadedRunWoodFrames : loadedIdleWoodFrames;
                    break;
                case ResourcePickup.ResourceType.Gold:
                    frames = isMoving ? loadedRunGoldFrames : loadedIdleGoldFrames;
                    break;
            }
        }

        if (frames != null && frames.Length > 0)
        {
            int idx = manualAnimFrame % frames.Length;
            spriteRenderer.sprite = frames[idx];
        }
    }

    private void MoveTowardsTarget(Vector3 target, float speed)
    {
        if (rb == null) return;

        Vector2 direction = ((Vector2)target - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * speed;

        // Face movement direction
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        isMoving = true;
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        isMoving = false;
    }

    #endregion

    #region Death & Combat Overrides

    protected override void Die()
    {
        isDead = true;

        // Award XP to player
        if (GameManager.Instance != null && xpReward > 0)
        {
            GameManager.Instance.AddXP(xpReward);
            Debug.Log($"[Peasant] Awarded {xpReward} XP");
        }

        // If carrying a resource, fling it far away (beyond magnet radius of 9)
        if (carriedAmount > 0)
        {
            DropCarriedResourceFar();
        }

        // Stop movement
        StopMovement();

        // Spawn death dust effect -- skull appears simultaneously
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

        Debug.Log("[Peasant] Killed! Skull dropped with dust effect.");
        Destroy(gameObject);
    }

    /// <summary>
    /// Drop carried resource far away (beyond magnet radius) so player sees it before auto-collecting.
    /// </summary>
    private void DropCarriedResourceFar()
    {
        if (carriedAmount <= 0 || ResourceSpawner.Instance == null) return;

        // Drop at random position 10-12 units away (beyond magnetRadius of 9)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float dropDistance = 10f + Random.Range(0f, 2f);
        Vector3 dropPos = transform.position + new Vector3(randomDir.x, randomDir.y, 0) * dropDistance;

        switch (carriedResourceType)
        {
            case ResourcePickup.ResourceType.Meat:
                ResourceSpawner.Instance.SpawnMeat(dropPos, carriedAmount);
                break;
            case ResourcePickup.ResourceType.Wood:
                ResourceSpawner.Instance.SpawnWood(dropPos, carriedAmount);
                break;
            case ResourcePickup.ResourceType.Gold:
                ResourceSpawner.Instance.SpawnGold(dropPos, carriedAmount);
                break;
        }

        Debug.Log($"[Peasant] Flung {carriedAmount} {carriedResourceType} far away on death");
        carriedAmount = 0;
        UpdateCarryingVisuals(false);
    }

    // Base class overrides - Peasant uses its own combat system
    protected override void PerformAttack() { }
    public override void DealDamage() { }

    #endregion

    #region Properties

    public PeasantState State => currentState;
    public int CarriedAmount => carriedAmount;
    public ResourcePickup.ResourceType CarriedType => carriedResourceType;
    public bool IsCarrying => carriedAmount > 0;
    public bool IsCornered => isCornered;

    #endregion

    #region Debug

    protected override void OnDrawGizmosSelected()
    {
        // Read values from config if available (for live editing in Balancer)
        float fearRange = config?.fearRange ?? playerFearRadius;
        float desperateRange = config?.desperateRange ?? knifeAttackRange;
        float resourceRange = config?.resourceDetectionRange ?? resourceDetectionRadius;
        float wanderRange = config?.wanderRadius ?? wanderRadius;

        // Collider — black
        Gizmos.color = GizmosHelper.ColliderColor;
        var col = GetComponent<Collider2D>();
        if (col is CircleCollider2D cc)
            Gizmos.DrawWireSphere(transform.TransformPoint(cc.offset), cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        else if (col is BoxCollider2D bc)
            Gizmos.DrawWireCube(transform.TransformPoint(bc.offset), new Vector3(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y, 0));

        // Desperate attack (knife) — pink
        Gizmos.color = GizmosHelper.DesperateAttackColor;
        Gizmos.DrawWireSphere(transform.position, desperateRange);

        // Fear range — cyan (NOT yellow - Pawn doesn't have Detection/Chase, only Fear)
        Gizmos.color = GizmosHelper.FearColor;
        Gizmos.DrawWireSphere(transform.position, fearRange);

        // Resource detection — brown
        Gizmos.color = GizmosHelper.ResourceDetectionColor;
        Gizmos.DrawWireSphere(transform.position, resourceRange);

        // Wander radius — gray
        Gizmos.color = GizmosHelper.WanderColor;
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, wanderRange);

        // Target resource line
        if (targetResource != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetResource.transform.position);
        }

        // Target building line
        if (targetBuilding != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetBuilding.transform.position);
        }
    }

    #endregion
}
