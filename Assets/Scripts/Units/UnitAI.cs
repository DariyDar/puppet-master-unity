using UnityEngine;

/// <summary>
/// AI state machine for player army units.
/// Handles Follow, Attack, and Return states.
/// </summary>
[RequireComponent(typeof(UnitBase))]
public class UnitAI : MonoBehaviour
{
    /// <summary>
    /// Possible AI states for units.
    /// </summary>
    public enum UnitState
    {
        Follow,     // Following the player
        Attack,     // Attacking an enemy
        Return,     // Returning to player (too far away)
        Dead        // Unit is dead
    }

    [Header("Current State")]
    [SerializeField] private UnitState currentState = UnitState.Follow;

    [Header("Behavior Settings")]
    [SerializeField] private float followStopDistance = 1.5f;
    [SerializeField] private float formationSpread = 1f;
    [SerializeField] private int formationIndex = 0;

    [Header("Target Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float targetUpdateInterval = 0.25f;

    // References
    private UnitBase unitBase;
    private UnitConfig config;

    // State tracking
    private float lastTargetUpdateTime;
    private Vector2 formationOffset;

    // Properties
    public UnitState CurrentState => currentState;

    private void Awake()
    {
        unitBase = GetComponent<UnitBase>();
    }

    private void Start()
    {
        config = unitBase.Config;

        // Calculate formation offset based on index
        CalculateFormationOffset();
    }

    private void Update()
    {
        if (unitBase.IsDead)
        {
            currentState = UnitState.Dead;
            return;
        }

        // Periodically update target
        if (Time.time - lastTargetUpdateTime > targetUpdateInterval)
        {
            lastTargetUpdateTime = Time.time;
            UpdateTarget();
        }

        // Update state transitions
        UpdateStateMachine();
    }

    private void FixedUpdate()
    {
        if (unitBase.IsDead) return;

        // Execute current state behavior
        switch (currentState)
        {
            case UnitState.Follow:
                FollowBehavior();
                break;
            case UnitState.Attack:
                AttackBehavior();
                break;
            case UnitState.Return:
                ReturnBehavior();
                break;
        }
    }

    #region State Machine

    /// <summary>
    /// Update state transitions based on conditions.
    /// </summary>
    private void UpdateStateMachine()
    {
        float distToPlayer = unitBase.GetDistanceToPlayer();
        float distToTarget = unitBase.GetDistanceToTarget();
        bool hasTarget = unitBase.CurrentTarget != null;
        float returnDist = config != null ? config.returnDistance : 8f;
        float attackRange = unitBase.AttackRange;

        switch (currentState)
        {
            case UnitState.Follow:
                // Check if we should attack
                if (hasTarget && distToTarget <= attackRange)
                {
                    ChangeState(UnitState.Attack);
                }
                // Check if enemy is in range but we need to move closer
                else if (hasTarget && distToPlayer < returnDist)
                {
                    // Move towards target if we have one
                }
                break;

            case UnitState.Attack:
                // Check if target is lost or dead
                if (!hasTarget || IsTargetDead())
                {
                    unitBase.ClearTarget();
                    ChangeState(UnitState.Follow);
                }
                // Check if target moved out of range
                else if (distToTarget > attackRange * 1.5f)
                {
                    // Chase target or return to player?
                    if (distToPlayer > returnDist)
                    {
                        unitBase.ClearTarget();
                        ChangeState(UnitState.Return);
                    }
                    else
                    {
                        // Continue chasing
                        ChangeState(UnitState.Follow);
                    }
                }
                // Check if too far from player
                else if (distToPlayer > returnDist)
                {
                    unitBase.ClearTarget();
                    ChangeState(UnitState.Return);
                }
                break;

            case UnitState.Return:
                // Once close enough to player, resume following
                float followDist = config != null ? config.followDistance : 2f;
                if (distToPlayer <= followDist + 1f)
                {
                    ChangeState(UnitState.Follow);
                }
                break;
        }
    }

    /// <summary>
    /// Change to a new state.
    /// </summary>
    private void ChangeState(UnitState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        OnExitState(currentState);

        currentState = newState;

        // Enter new state
        OnEnterState(newState);
    }

    private void OnEnterState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Follow:
                // Resume normal following
                break;
            case UnitState.Attack:
                unitBase.FaceTarget();
                break;
            case UnitState.Return:
                // Clear target and return to player
                unitBase.ClearTarget();
                break;
        }
    }

    private void OnExitState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Attack:
                unitBase.StopMovement();
                break;
        }
    }

    #endregion

    #region Behaviors

    /// <summary>
    /// Follow the player at a comfortable distance.
    /// </summary>
    private void FollowBehavior()
    {
        if (unitBase.PlayerTransform == null) return;

        // Calculate target position with formation offset
        Vector2 targetPos = (Vector2)unitBase.PlayerTransform.position + formationOffset;
        float distToTarget = Vector2.Distance(transform.position, targetPos);
        float followDist = config != null ? config.followDistance : 2f;

        // If we have an enemy target, move towards it
        if (unitBase.CurrentTarget != null && !IsTargetDead())
        {
            float distToEnemy = unitBase.GetDistanceToTarget();
            if (distToEnemy > unitBase.AttackRange)
            {
                // Move towards enemy
                Vector2 dirToEnemy = unitBase.GetDirectionToTarget();
                unitBase.Move(dirToEnemy);
                unitBase.FaceTarget();
            }
            else
            {
                // In range, switch to attack
                ChangeState(UnitState.Attack);
            }
        }
        // Otherwise, follow player
        else if (distToTarget > followStopDistance)
        {
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
            unitBase.Move(direction);
        }
        else
        {
            unitBase.StopMovement();
            unitBase.FacePlayer();
        }
    }

    /// <summary>
    /// Attack the current target.
    /// </summary>
    private void AttackBehavior()
    {
        if (unitBase.CurrentTarget == null || IsTargetDead())
        {
            unitBase.ClearTarget();
            ChangeState(UnitState.Follow);
            return;
        }

        float distToTarget = unitBase.GetDistanceToTarget();

        // Stop and attack if in range
        if (distToTarget <= unitBase.AttackRange)
        {
            unitBase.StopMovement();
            unitBase.FaceTarget();

            if (unitBase.CanAttack())
            {
                unitBase.PerformAttack();
            }
        }
        else
        {
            // Move closer to target
            Vector2 direction = unitBase.GetDirectionToTarget();
            unitBase.Move(direction);
        }
    }

    /// <summary>
    /// Return to the player when too far away.
    /// </summary>
    private void ReturnBehavior()
    {
        if (unitBase.PlayerTransform == null) return;

        // Move directly towards player
        Vector2 direction = unitBase.GetDirectionToPlayer();
        unitBase.Move(direction);
    }

    #endregion

    #region Target Selection

    /// <summary>
    /// Update the current target - find nearest enemy in range.
    /// </summary>
    private void UpdateTarget()
    {
        // Skip if we already have a valid target
        if (unitBase.CurrentTarget != null && !IsTargetDead())
        {
            float distToTarget = unitBase.GetDistanceToTarget();
            float detectionRange = config != null ? config.detectionRange : 5f;

            // Keep current target if still in range
            if (distToTarget <= detectionRange)
            {
                return;
            }
        }

        // Find new target
        Transform nearestEnemy = FindNearestEnemy();
        unitBase.SetTarget(nearestEnemy);
    }

    /// <summary>
    /// Find the nearest enemy within detection range.
    /// </summary>
    private Transform FindNearestEnemy()
    {
        float detectionRange = config != null ? config.detectionRange : 5f;

        // Use OverlapCircle to find enemies
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayer);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // Check if it's an enemy
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.transform;
                }
            }
        }

        // Fallback: Find enemies by tag if layer-based detection failed
        if (nearest == null)
        {
            nearest = FindNearestEnemyByTag();
        }

        return nearest;
    }

    /// <summary>
    /// Fallback method to find enemies by tag.
    /// </summary>
    private Transform FindNearestEnemyByTag()
    {
        float detectionRange = config != null ? config.detectionRange : 5f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var enemyObj in enemies)
        {
            EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                float dist = Vector2.Distance(transform.position, enemyObj.transform.position);
                if (dist < nearestDist && dist <= detectionRange)
                {
                    nearestDist = dist;
                    nearest = enemyObj.transform;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Check if current target is dead.
    /// </summary>
    private bool IsTargetDead()
    {
        if (unitBase.CurrentTarget == null) return true;

        EnemyBase enemy = unitBase.CurrentTarget.GetComponent<EnemyBase>();
        return enemy == null || enemy.IsDead;
    }

    #endregion

    #region Formation

    /// <summary>
    /// Calculate formation offset for this unit.
    /// </summary>
    private void CalculateFormationOffset()
    {
        // Simple circular formation around player
        float angle = formationIndex * (360f / 8f) * Mathf.Deg2Rad;
        formationOffset = new Vector2(
            Mathf.Cos(angle) * formationSpread,
            Mathf.Sin(angle) * formationSpread
        );
    }

    /// <summary>
    /// Set the formation index for this unit.
    /// </summary>
    public void SetFormationIndex(int index)
    {
        formationIndex = index;
        CalculateFormationOffset();
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.detectionRange);

        // Return distance
        if (unitBase != null && unitBase.PlayerTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(unitBase.PlayerTransform.position, config.returnDistance);
        }

        // Formation position
        if (Application.isPlaying && unitBase != null && unitBase.PlayerTransform != null)
        {
            Gizmos.color = Color.green;
            Vector3 targetPos = (Vector2)unitBase.PlayerTransform.position + formationOffset;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }
    }

    #endregion
}
