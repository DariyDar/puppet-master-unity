using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls player movement with 8-directional input support.
/// Handles both keyboard (WASD/Arrows) and virtual joystick input.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

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

        // Setup input
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            attackAction = playerInput.actions["Attack"];
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
            // Fallback to direct keyboard input
            moveInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
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
    /// Trigger attack animation and deal damage
    /// </summary>
    public void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger(AnimAttack);
        }

        // Deal damage immediately (or use animation event for precise timing)
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.DealDamage();
        }
    }
}
