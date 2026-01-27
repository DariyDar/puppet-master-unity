using UnityEngine;
using System.Collections;

/// <summary>
/// Peasant - a resource-collecting enemy that:
/// - Wanders around when idle
/// - Spots any resource (except Skull) and runs to collect it
/// - Carries resource to nearest building while avoiding player
/// - Deposits resource in building
/// Per GDD: Peasant collects ANY resource (Meat, Wood, Gold) and carries it to nearest house.
/// Two forces when carrying: attraction to building + fear of player.
/// </summary>
public class Peasant : EnemyBase
{
    public enum PeasantState
    {
        Idle,               // Standing still
        Wandering,          // Walking around home position
        RunningToResource,  // Spotted resource, going to pick it up
        CarryingResource,   // Has resource, heading to building while avoiding player
        Combat              // In combat (doesn't collect resources)
    }

    [Header("Peasant Settings")]
    [SerializeField] private float pickupRadius = 0.5f;
    [SerializeField] private float resourceDetectionRadius = 8f;
    [SerializeField] private float buildingDeliveryRadius = 1f;
    [SerializeField] private float playerFearRadius = 5f;
    [SerializeField] private float playerFearStrength = 0.7f;

    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 2.5f;
    [SerializeField] private float idleMinTime = 3f;
    [SerializeField] private float idleMaxTime = 8f;
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float carrySpeed = 2f;

    [Header("Carrying Animation Sprites")]
    [Tooltip("Sprites for carrying meat (idle, run)")]
    [SerializeField] private Sprite idleMeatSprite;
    [SerializeField] private Sprite runMeatSprite;
    [Tooltip("Sprites for carrying wood (idle, run)")]
    [SerializeField] private Sprite idleWoodSprite;
    [SerializeField] private Sprite runWoodSprite;
    [Tooltip("Sprites for carrying gold (idle, run)")]
    [SerializeField] private Sprite idleGoldSprite;
    [SerializeField] private Sprite runGoldSprite;
    [Tooltip("Normal sprites (no resource)")]
    [SerializeField] private Sprite normalIdleSprite;
    [SerializeField] private Sprite normalRunSprite;

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

    // Animation
    private bool isMoving;
    private Sprite currentIdleSprite;
    private Sprite currentRunSprite;

    protected override void Start()
    {
        base.Start();
        homePosition = transform.position;
        currentState = PeasantState.Idle;
        StartNewIdlePeriod();

        // Store original sprites
        if (spriteRenderer != null && normalIdleSprite == null)
        {
            normalIdleSprite = spriteRenderer.sprite;
        }
    }

    protected override void Update()
    {
        if (isDead) return;
        if (IsStunned) return;

        switch (currentState)
        {
            case PeasantState.Idle:
                UpdateIdle();
                break;
            case PeasantState.Wandering:
                // Movement in FixedUpdate
                break;
            case PeasantState.RunningToResource:
                UpdateRunningToResource();
                break;
            case PeasantState.CarryingResource:
                UpdateCarryingResource();
                break;
            case PeasantState.Combat:
                // Combat handled by base class
                break;
        }

        // Always check for nearby resources when idle/wandering
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
            case PeasantState.RunningToResource:
                if (targetResource != null)
                {
                    MoveTowardsTarget(targetResource.transform.position, moveSpeed);
                }
                break;
            case PeasantState.CarryingResource:
                MoveTowardsBuildingWithFear();
                break;
        }
    }

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
        if (isCarrying)
        {
            // Set sprites based on resource type
            switch (carriedResourceType)
            {
                case ResourcePickup.ResourceType.Meat:
                    currentIdleSprite = idleMeatSprite;
                    currentRunSprite = runMeatSprite;
                    break;
                case ResourcePickup.ResourceType.Wood:
                    currentIdleSprite = idleWoodSprite;
                    currentRunSprite = runWoodSprite;
                    break;
                case ResourcePickup.ResourceType.Gold:
                    currentIdleSprite = idleGoldSprite;
                    currentRunSprite = runGoldSprite;
                    break;
            }

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
            currentIdleSprite = normalIdleSprite;
            currentRunSprite = normalRunSprite;

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

        // Update animator if present
        if (animator != null)
        {
            animator.SetBool(AnimIsMoving, isMoving);
        }

        // Update sprite based on movement (if using sprite swap instead of animator)
        if (spriteRenderer != null && currentIdleSprite != null && currentRunSprite != null)
        {
            spriteRenderer.sprite = isMoving ? currentRunSprite : currentIdleSprite;
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

    #region Death & Combat

    protected override void Die()
    {
        isDead = true;

        // If carrying a resource, fling it far away (beyond magnet radius of 9)
        if (carriedAmount > 0)
        {
            DropCarriedResourceFar();
        }

        // Stop movement
        StopMovement();

        // Spawn death dust effect — skull appears simultaneously
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

    // Peasants don't attack - they run away
    protected override void PerformAttack() { }
    public override void DealDamage() { }

    #endregion

    #region Properties

    public PeasantState State => currentState;
    public int CarriedAmount => carriedAmount;
    public ResourcePickup.ResourceType CarriedType => carriedResourceType;
    public bool IsCarrying => carriedAmount > 0;

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        // Home position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, wanderRadius);

        // Resource detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, resourceDetectionRadius);

        // Pickup radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Player fear radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerFearRadius);

        // Target resource
        if (targetResource != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetResource.transform.position);
        }

        // Target building
        if (targetBuilding != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetBuilding.transform.position);
        }
    }

    #endregion
}
