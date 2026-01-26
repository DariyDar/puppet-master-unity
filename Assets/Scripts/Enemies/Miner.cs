using UnityEngine;

/// <summary>
/// Miner (Pawn) enemy that guards gold mines.
/// Attacks player and army units that come near the mine.
/// NEW: When sees gold on ground, picks it up and returns to mine.
/// Same stats as Pawn with Axe (HP: 30, Damage: 10).
/// </summary>
public class Miner : EnemyBase
{
    [Header("Miner Settings")]
    [SerializeField] private float guardRadius = 4f;
    [SerializeField] private float returnToGuardSpeed = 2f;
    [SerializeField] private float goldDetectionRadius = 6f;

    [Header("Gold Collection")]
    [SerializeField] private float goldPickupRange = 0.5f;
    [SerializeField] private int carryingGold = 0;
    [SerializeField] private bool isCollectingGold = false;

    private Vector3 guardPosition;
    private bool hasGuardPosition = false;
    private bool isReturningToGuard = false;
    private GoldMine parentMine;
    private ResourcePickup targetGold;

    protected override void Start()
    {
        base.Start();

        // Set default guard position to spawn position
        if (!hasGuardPosition)
        {
            guardPosition = transform.position;
            hasGuardPosition = true;
        }
    }

    protected override void Update()
    {
        if (isDead) return;
        if (IsStunned) return;

        // Priority 1: If carrying gold, return to mine
        if (carryingGold > 0 && parentMine != null)
        {
            isCollectingGold = false;
            ReturnGoldToMine();
            return;
        }

        // Priority 2: Check for gold on the ground
        ResourcePickup nearbyGold = FindNearbyGold();
        if (nearbyGold != null)
        {
            targetGold = nearbyGold;
            isCollectingGold = true;
            target = null;
            return;
        }

        isCollectingGold = false;
        targetGold = null;

        // Priority 3: Check for threats in guard radius
        Transform threat = FindThreatInGuardRadius();

        if (threat != null)
        {
            target = threat;
            isReturningToGuard = false;
        }
        else if (hasGuardPosition)
        {
            // No threat - return to guard position
            float distToGuard = Vector2.Distance(transform.position, guardPosition);
            if (distToGuard > 0.5f)
            {
                isReturningToGuard = true;
                ReturnToGuardPosition();
            }
            else
            {
                isReturningToGuard = false;
                target = null;
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || IsStunned) return;

        // If collecting gold, move towards gold
        if (isCollectingGold && targetGold != null)
        {
            MoveTowardsGold();
            UpdateMoveAnimation();
            return;
        }

        // If carrying gold, move handled in Update
        if (carryingGold > 0)
        {
            UpdateMoveAnimation();
            return;
        }

        if (target != null)
        {
            // Chase and attack threat
            float dist = GetDistanceToTarget();

            if (dist <= attackRange && CanAttack())
            {
                // Stop and attack
                if (rb != null) rb.linearVelocity = Vector2.zero;
                FaceTarget();
                PerformAttack();
            }
            else if (dist <= detectionRange)
            {
                // Move towards target
                MoveTowardsTarget();
            }
        }

        // Update animation
        UpdateMoveAnimation();
    }

    /// <summary>
    /// Find player or army units within guard radius.
    /// </summary>
    private Transform FindThreatInGuardRadius()
    {
        float closestDist = float.MaxValue;
        Transform closestThreat = null;

        // Check for player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distToPlayer = Vector2.Distance(guardPosition, player.transform.position);
            if (distToPlayer <= guardRadius)
            {
                float distFromSelf = Vector2.Distance(transform.position, player.transform.position);
                if (distFromSelf < closestDist)
                {
                    closestDist = distFromSelf;
                    closestThreat = player.transform;
                }
            }
        }

        // Check for army units
        UnitBase[] units = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            if (unit.IsDead) continue;

            float distToUnit = Vector2.Distance(guardPosition, unit.transform.position);
            if (distToUnit <= guardRadius)
            {
                float distFromSelf = Vector2.Distance(transform.position, unit.transform.position);
                if (distFromSelf < closestDist)
                {
                    closestDist = distFromSelf;
                    closestThreat = unit.transform;
                }
            }
        }

        return closestThreat;
    }

    private void MoveTowardsTarget()
    {
        if (target == null || rb == null) return;

        Vector2 direction = GetDirectionToTarget();
        rb.linearVelocity = direction * moveSpeed;
        FaceTarget();
    }

    private void ReturnToGuardPosition()
    {
        if (rb == null) return;

        Vector2 direction = ((Vector2)guardPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * returnToGuardSpeed;

        // Face movement direction
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

    /// <summary>
    /// Set the position this miner guards.
    /// </summary>
    public void SetGuardPosition(Vector3 position)
    {
        guardPosition = position;
        hasGuardPosition = true;
    }

    /// <summary>
    /// Set the parent mine for gold deposit.
    /// </summary>
    public void SetParentMine(GoldMine mine)
    {
        parentMine = mine;
    }

    /// <summary>
    /// Find gold pickups on the ground nearby.
    /// </summary>
    private ResourcePickup FindNearbyGold()
    {
        ResourcePickup[] allPickups = Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);
        ResourcePickup closest = null;
        float closestDist = float.MaxValue;

        foreach (var pickup in allPickups)
        {
            if (pickup.Type != ResourcePickup.ResourceType.Gold) continue;

            float dist = Vector2.Distance(transform.position, pickup.transform.position);
            if (dist <= goldDetectionRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }

        return closest;
    }

    /// <summary>
    /// Move towards gold pickup and collect it.
    /// </summary>
    private void MoveTowardsGold()
    {
        if (targetGold == null || rb == null)
        {
            isCollectingGold = false;
            return;
        }

        float dist = Vector2.Distance(transform.position, targetGold.transform.position);

        if (dist <= goldPickupRange)
        {
            // Pick up gold
            carryingGold += targetGold.Amount;
            Debug.Log($"[Miner] Picked up {targetGold.Amount} gold. Carrying: {carryingGold}");

            // Destroy the pickup
            Destroy(targetGold.gameObject);
            targetGold = null;
            isCollectingGold = false;

            // Stop moving
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // Move towards gold
            Vector2 direction = ((Vector2)targetGold.transform.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Face movement direction
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }

    /// <summary>
    /// Return carrying gold to the parent mine.
    /// </summary>
    private void ReturnGoldToMine()
    {
        if (parentMine == null || rb == null)
        {
            // No parent mine, drop gold here (shouldn't happen)
            carryingGold = 0;
            return;
        }

        float dist = Vector2.Distance(transform.position, parentMine.transform.position);

        if (dist <= 1f)
        {
            // Deposit gold
            parentMine.DepositGold(carryingGold);
            Debug.Log($"[Miner] Deposited {carryingGold} gold to mine");
            carryingGold = 0;

            // Stop moving
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // Move towards mine
            Vector2 direction = ((Vector2)parentMine.transform.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Face movement direction
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
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
            }
        }
    }

    protected override void Die()
    {
        // Drop any carried gold before dying
        if (carryingGold > 0 && ResourceSpawner.Instance != null)
        {
            ResourceSpawner.Instance.SpawnGold(transform.position, carryingGold);
            Debug.Log($"[Miner] Dropped {carryingGold} gold on death");
            carryingGold = 0;
        }

        base.Die();

        // Miners can drop gold on death (50% chance per GDD)
        if (Random.value <= 0.5f && ResourceSpawner.Instance != null)
        {
            int goldDrop = Random.Range(1, 4);
            ResourceSpawner.Instance.SpawnGold(transform.position, goldDrop);
            Debug.Log($"[Miner] Dropped {goldDrop} gold!");
        }

        Debug.Log("[Miner] Miner defeated!");
    }

    private void OnDrawGizmosSelected()
    {
        // Guard radius
        Gizmos.color = Color.yellow;
        Vector3 guardPos = hasGuardPosition ? guardPosition : transform.position;
        Gizmos.DrawWireSphere(guardPos, guardRadius);

        // Gold detection radius
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, goldDetectionRadius);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    // Properties
    public int CarryingGold => carryingGold;
    public bool IsCollectingGold => isCollectingGold;
}
