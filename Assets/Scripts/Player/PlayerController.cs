using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls player movement with 8-directional input support.
/// Handles both keyboard (WASD/Arrows) and virtual joystick input.
/// Spider player with auto-attack when not moving.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Auto-Attack")]
    [SerializeField] private bool autoAttackEnabled = true;
    [SerializeField] private float autoAttackRange = 2f;
    [SerializeField] private float autoAttackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat combat;

    [Header("Tier Visuals - Per GDD")]
    [Tooltip("Tier 1: Normal front legs, Tier 2: Claws, Tier 3: Saw blade")]
    [SerializeField] private int currentTier = 1;
    [SerializeField] private Sprite[] tierSprites;              // Different sprites per tier
    [SerializeField] private RuntimeAnimatorController[] tierAnimators;  // Different animations per tier
    [SerializeField] private GameObject[] tierVisualPrefabs;    // Optional overlay effects

    // Auto-attack state
    private float lastAutoAttackTime;
    private bool wasMovingLastFrame;

    // Input
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction attackAction;

    // State
    private bool isMoving;
    private Vector2 lastMoveDirection = Vector2.down;

    // Animation parameter hashes for performance
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    private void Awake()
    {
        // Get components if not assigned
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (combat == null) combat = GetComponent<PlayerCombat>();

        // Setup input
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            attackAction = playerInput.actions["Attack"];
        }

        // Setup enemy layer if not set
        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
        if (attackAction != null)
        {
            attackAction.Enable();
            attackAction.performed += OnAttackCallback;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackCallback;
            attackAction.Disable();
        }
    }

    private void OnAttackCallback(InputAction.CallbackContext context)
    {
        Attack();
    }

    // Called by PlayerInput component via SendMessage
    public void OnAttack()
    {
        Attack();
    }

    private void Update()
    {
        // Read input
        ReadInput();

        // Update animation
        UpdateAnimation();

        // Auto-attack when not moving and enemies in range
        if (autoAttackEnabled)
        {
            HandleAutoAttack();
        }
    }

    /// <summary>
    /// Handle automatic attack when player is not moving and enemies are in range.
    /// </summary>
    private void HandleAutoAttack()
    {
        // Only auto-attack when player STOPS moving (transition from moving to idle)
        bool justStoppedMoving = wasMovingLastFrame && !isMoving;
        wasMovingLastFrame = isMoving;

        // Don't attack while moving
        if (isMoving) return;

        // Check cooldown
        if (Time.time - lastAutoAttackTime < autoAttackCooldown) return;

        // Find enemies in range
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            autoAttackRange,
            enemyLayer
        );

        // Filter for valid targets (not dead)
        Transform nearestEnemy = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestEnemy = col.transform;
                }
            }
        }

        // Attack if enemy found
        if (nearestEnemy != null)
        {
            // Face the enemy
            Vector2 dirToEnemy = (nearestEnemy.position - transform.position).normalized;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = dirToEnemy.x < 0;
            }
            lastMoveDirection = dirToEnemy;

            // Perform attack
            lastAutoAttackTime = Time.time;
            Attack();

            Debug.Log($"[PlayerController] Auto-attacking {nearestEnemy.name}");
        }
    }

    private void FixedUpdate()
    {
        // Apply movement in FixedUpdate for physics
        Move();
    }

    private void ReadInput()
    {
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }
        else
        {
            // Fallback using new Input System Keyboard
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    input.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    input.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    input.x -= 1f;
            }
            moveInput = input;
        }

        // Normalize diagonal movement
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        isMoving = moveInput.magnitude > 0.1f;

        // Track last direction for idle animation
        if (isMoving)
        {
            lastMoveDirection = moveInput.normalized;
        }
    }

    private void Move()
    {
        Vector2 vel = moveInput * moveSpeed;
        rb.linearVelocity = vel;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving, isMoving);

        // Flip sprite based on horizontal direction
        UpdateSpriteFlip();
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null) return;

        // Get current direction (use last direction if not moving)
        Vector2 dir = isMoving ? moveInput : lastMoveDirection;

        // Flip sprite when moving left
        if (dir.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
        else if (dir.x > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
        // If moving only vertically, keep current flip state
    }

    /// <summary>
    /// Set movement input from external source (e.g., virtual joystick)
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    /// <summary>
    /// Get current movement direction (normalized)
    /// </summary>
    public Vector2 GetMoveDirection()
    {
        return isMoving ? moveInput.normalized : lastMoveDirection;
    }

    /// <summary>
    /// Check if player is currently moving
    /// </summary>
    public bool IsMoving => isMoving;

    /// <summary>
    /// Get/Set movement speed
    /// </summary>
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    /// <summary>
    /// Get/Set auto-attack range
    /// </summary>
    public float AutoAttackRange
    {
        get => autoAttackRange;
        set => autoAttackRange = value;
    }

    /// <summary>
    /// Enable/disable auto-attack
    /// </summary>
    public bool AutoAttackEnabled
    {
        get => autoAttackEnabled;
        set => autoAttackEnabled = value;
    }

    /// <summary>
    /// Trigger attack animation and deal damage
    /// </summary>
    public void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Deal damage immediately (or use animation event for precise timing)
        if (combat != null)
        {
            combat.DealDamage();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw auto-attack range
        if (autoAttackEnabled)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
            Gizmos.DrawWireSphere(transform.position, autoAttackRange);
        }
    }

    #region Tier System - Per GDD

    /// <summary>
    /// Current player tier (1-3).
    /// Tier 1: Normal front legs, attacks with jabs
    /// Tier 2: Front legs become claws
    /// Tier 3: One claw becomes a saw blade
    /// </summary>
    public int CurrentTier
    {
        get => currentTier;
        set
        {
            int newTier = Mathf.Clamp(value, 1, 3);
            if (newTier != currentTier)
            {
                currentTier = newTier;
                ApplyTierVisuals();
                Debug.Log($"[PlayerController] Upgraded to Tier {currentTier}!");
            }
        }
    }

    /// <summary>
    /// Upgrade player to next tier.
    /// </summary>
    public void UpgradeTier()
    {
        if (currentTier < 3)
        {
            CurrentTier = currentTier + 1;
        }
    }

    /// <summary>
    /// Apply visuals for current tier.
    /// </summary>
    private void ApplyTierVisuals()
    {
        // Apply sprite if available
        if (tierSprites != null && tierSprites.Length >= currentTier && spriteRenderer != null)
        {
            if (tierSprites[currentTier - 1] != null)
            {
                spriteRenderer.sprite = tierSprites[currentTier - 1];
            }
        }

        // Apply animator if available
        if (tierAnimators != null && tierAnimators.Length >= currentTier && animator != null)
        {
            if (tierAnimators[currentTier - 1] != null)
            {
                animator.runtimeAnimatorController = tierAnimators[currentTier - 1];
            }
        }

        // Spawn visual effect for tier
        if (tierVisualPrefabs != null && tierVisualPrefabs.Length >= currentTier)
        {
            if (tierVisualPrefabs[currentTier - 1] != null)
            {
                GameObject effect = Instantiate(tierVisualPrefabs[currentTier - 1], transform);
                effect.transform.localPosition = Vector3.zero;
            }
        }

        // Update attack properties based on tier
        UpdateTierCombatStats();
    }

    /// <summary>
    /// Update combat stats based on tier.
    /// </summary>
    private void UpdateTierCombatStats()
    {
        if (combat == null) return;

        // Tier bonuses
        switch (currentTier)
        {
            case 1:
                // Base stats
                break;
            case 2:
                // Claws - increased damage
                combat.SetDamageMultiplier(1.25f);
                break;
            case 3:
                // Saw blade - further increased damage and attack speed
                combat.SetDamageMultiplier(1.5f);
                autoAttackCooldown *= 0.8f; // 20% faster
                break;
        }
    }

    /// <summary>
    /// Check if player can upgrade to next tier.
    /// Called by UpgradeSystem when conditions are met.
    /// </summary>
    public bool CanUpgradeTier()
    {
        return currentTier < 3;
    }

    #endregion
}
