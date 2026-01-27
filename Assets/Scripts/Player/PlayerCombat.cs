using UnityEngine;

/// <summary>
/// Handles player attack detection and damage dealing.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRange = 2f;  // Increased for better hit detection
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask buildingLayer;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private PlayerController playerController;

    // Tier system
    private float damageMultiplier = 1f;
    private float baseDamage;
    private float lastAttackTime;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // Create attack point if not assigned
        if (attackPoint == null)
        {
            GameObject point = new GameObject("AttackPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = new Vector3(1f, 0, 0);
            attackPoint = point.transform;
        }

        // Store base damage
        baseDamage = attackDamage;
    }

    private void Update()
    {
        // Update attack point position based on facing direction
        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null || playerController == null) return;

        Vector2 dir = playerController.GetMoveDirection();
        attackPoint.localPosition = dir * attackRange * 0.5f;
    }

    /// <summary>
    /// Called when player attacks (from animation event or directly)
    /// </summary>
    public void PerformAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Trigger attack animation
        if (playerController != null)
        {
            playerController.Attack();
        }
    }

    /// <summary>
    /// Called by animation event at the moment of impact
    /// </summary>
    public void DealDamage()
    {
        bool hitSomething = false;

        // Find ALL colliders in range (not filtered by layer - layer may not exist)
        Collider2D[] allHits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange
        );

        foreach (Collider2D hit in allHits)
        {
            // Check for enemy
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit {enemy.name} for {attackDamage} damage");
                hitSomething = true;
                continue;
            }
        }

        // Check buildings in the same hits array
        foreach (Collider2D hit in allHits)
        {
            // Skip if already handled as enemy
            if (hit.GetComponent<EnemyBase>() != null) continue;

            // Try EnemyHouse
            EnemyHouse house = hit.GetComponent<EnemyHouse>();
            if (house != null)
            {
                house.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit EnemyHouse for {attackDamage} damage");
                hitSomething = true;
                continue;
            }

            // Try Watchtower
            Watchtower tower = hit.GetComponent<Watchtower>();
            if (tower != null)
            {
                tower.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit Watchtower for {attackDamage} damage");
                hitSomething = true;
                continue;
            }

            // Try Castle
            Castle castle = hit.GetComponent<Castle>();
            if (castle != null)
            {
                castle.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit Castle for {attackDamage} damage");
                hitSomething = true;
                continue;
            }

            // Try DestructibleTree
            DestructibleTree tree = hit.GetComponent<DestructibleTree>();
            if (tree != null && !tree.IsDestroyed)
            {
                tree.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit Tree for {attackDamage} damage");
                hitSomething = true;
            }
        }

        if (!hitSomething)
        {
            Debug.Log("[PlayerCombat] Attack missed - no targets in range");
        }
    }

    /// <summary>
    /// Auto-attack nearest enemy in range
    /// </summary>
    public void AutoAttackNearest()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        // Find all colliders and check for enemies (not filtered by layer)
        Collider2D[] allHits = Physics2D.OverlapCircleAll(
            transform.position,
            attackRange * 2f
        );

        foreach (var col in allHits)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                PerformAttack();
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.right, attackRange);
        }
    }

    // Properties
    public int AttackDamage
    {
        get => attackDamage;
        set => attackDamage = value;
    }

    /// <summary>
    /// Set damage multiplier (from tier system).
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
        attackDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        Debug.Log($"[PlayerCombat] Damage multiplier set to {multiplier}x, damage is now {attackDamage}");
    }

    /// <summary>
    /// Set base damage (from UpgradeSystem).
    /// </summary>
    public void SetDamage(float damage)
    {
        baseDamage = damage;
        attackDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
    }
}
