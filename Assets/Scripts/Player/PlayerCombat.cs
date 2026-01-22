using UnityEngine;

/// <summary>
/// Handles player attack detection and damage dealing.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private PlayerController playerController;

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
        // Find enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(attackDamage);
                Debug.Log($"[PlayerCombat] Hit {enemy.name} for {attackDamage} damage");
            }
        }
    }

    /// <summary>
    /// Auto-attack nearest enemy in range
    /// </summary>
    public void AutoAttackNearest()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        // Find nearest enemy
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            attackRange * 2f,
            enemyLayer
        );

        if (enemies.Length > 0)
        {
            PerformAttack();
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
}
