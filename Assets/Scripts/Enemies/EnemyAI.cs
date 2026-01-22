using UnityEngine;

/// <summary>
/// Simple enemy AI with states: Idle, Chase, Attack.
/// Inherits from EnemyBase for stats and health.
/// </summary>
public class EnemyAI : EnemyBase
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("AI Settings")]
    [SerializeField] private float idleWanderRadius = 3f;
    [SerializeField] private float idleWanderInterval = 3f;

    private EnemyState currentState = EnemyState.Idle;
    private Vector2 wanderTarget;
    private float lastWanderTime;
    private Vector2 spawnPosition;

    protected override void Start()
    {
        base.Start();
        spawnPosition = transform.position;
        wanderTarget = spawnPosition;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead)
        {
            currentState = EnemyState.Dead;
            return;
        }

        UpdateState();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isDead) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehavior();
                break;
            case EnemyState.Chase:
                ChaseBehavior();
                break;
            case EnemyState.Attack:
                AttackBehavior();
                break;
        }
    }

    private void UpdateState()
    {
        float distToTarget = GetDistanceToTarget();

        // State transitions
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distToTarget <= detectionRange)
                {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                if (distToTarget <= attackRange)
                {
                    currentState = EnemyState.Attack;
                }
                else if (distToTarget > chaseRange)
                {
                    currentState = EnemyState.Idle;
                }
                break;

            case EnemyState.Attack:
                if (distToTarget > attackRange * 1.2f)
                {
                    currentState = EnemyState.Chase;
                }
                break;
        }
    }

    private void IdleBehavior()
    {
        // Wander around spawn point
        if (Time.time - lastWanderTime > idleWanderInterval)
        {
            lastWanderTime = Time.time;
            Vector2 randomOffset = Random.insideUnitCircle * idleWanderRadius;
            wanderTarget = spawnPosition + randomOffset;
        }

        // Move towards wander target
        Vector2 dir = (wanderTarget - (Vector2)transform.position).normalized;
        float distToWander = Vector2.Distance(transform.position, wanderTarget);

        if (distToWander > 0.5f)
        {
            rb.linearVelocity = dir * moveSpeed * 0.5f;
            UpdateAnimation(true, dir);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(false, Vector2.zero);
        }
    }

    private void ChaseBehavior()
    {
        Vector2 dir = GetDirectionToTarget();
        rb.linearVelocity = dir * moveSpeed;
        FaceTarget();
        UpdateAnimation(true, dir);
    }

    private void AttackBehavior()
    {
        // Stop moving during attack
        rb.linearVelocity = Vector2.zero;
        FaceTarget();
        UpdateAnimation(false, Vector2.zero);

        // Attack if cooldown ready
        if (CanAttack())
        {
            PerformAttack();
        }
    }

    private void UpdateAnimation(bool isMoving, Vector2 direction)
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving, isMoving);

        // Flip sprite based on direction
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Chase range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
