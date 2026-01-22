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

    // State
    private bool isMoving;
    private Vector2 lastMoveDirection = Vector2.down;

    // Animation parameter hashes for performance
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

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
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
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
        Vector2 velocity = moveInput * moveSpeed;
        rb.linearVelocity = velocity;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetBool(IsMoving, isMoving);

        // Set direction for blend tree
        if (isMoving)
        {
            animator.SetFloat(MoveX, moveInput.x);
            animator.SetFloat(MoveY, moveInput.y);
        }
        else
        {
            // Keep last direction for idle
            animator.SetFloat(MoveX, lastMoveDirection.x);
            animator.SetFloat(MoveY, lastMoveDirection.y);
        }
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
}
