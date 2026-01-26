using UnityEngine;

/// <summary>
/// A cloud that slowly moves across the screen.
/// Respawns at the opposite edge when exiting the screen bounds.
/// Supports optional parallax effect.
/// </summary>
public class Cloud : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector2 moveDirection = new Vector2(-1f, 0f);
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float speedVariation = 0.2f;

    [Header("Bounds")]
    [SerializeField] private float leftBound = -20f;
    [SerializeField] private float rightBound = 20f;
    [SerializeField] private float topBound = 10f;
    [SerializeField] private float bottomBound = -10f;
    [SerializeField] private bool useScreenBounds = true;
    [SerializeField] private float boundsPadding = 5f;

    [Header("Parallax")]
    [SerializeField] private bool enableParallax = false;
    [SerializeField] private float parallaxFactor = 0.5f; // 0 = no movement, 1 = full camera movement
    [SerializeField] private Transform cameraTransform;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrder = -10;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 0.9f;

    [Header("Randomization")]
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.5f;

    private float actualSpeed;
    private Vector3 lastCameraPosition;
    private Vector3 startPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        // Calculate actual speed with variation
        actualSpeed = moveSpeed + Random.Range(-speedVariation, speedVariation);

        // Find camera if not assigned
        if (cameraTransform == null && enableParallax)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
                lastCameraPosition = cameraTransform.position;
            }
        }

        // Calculate screen bounds
        if (useScreenBounds)
        {
            CalculateScreenBounds();
        }

        // Apply sorting order
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        // Randomize appearance
        if (randomizeOnStart)
        {
            RandomizeAppearance();
        }

        startPosition = transform.position;
    }

    private void Update()
    {
        // Move cloud
        MoveCloud();

        // Apply parallax
        if (enableParallax)
        {
            ApplyParallax();
        }

        // Check bounds and respawn
        CheckBoundsAndRespawn();
    }

    /// <summary>
    /// Move the cloud in its direction.
    /// </summary>
    private void MoveCloud()
    {
        Vector3 movement = new Vector3(moveDirection.x, moveDirection.y, 0f) * actualSpeed * Time.deltaTime;
        transform.position += movement;
    }

    /// <summary>
    /// Apply parallax effect based on camera movement.
    /// </summary>
    private void ApplyParallax()
    {
        if (cameraTransform == null) return;

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;
        Vector3 parallaxMovement = cameraDelta * parallaxFactor;

        transform.position += parallaxMovement;
        lastCameraPosition = cameraTransform.position;
    }

    /// <summary>
    /// Check if cloud is outside bounds and respawn if needed.
    /// </summary>
    private void CheckBoundsAndRespawn()
    {
        Vector3 pos = transform.position;
        bool needsRespawn = false;
        Vector3 newPos = pos;

        // Check horizontal bounds
        if (moveDirection.x < 0 && pos.x < leftBound)
        {
            needsRespawn = true;
            newPos.x = rightBound;
        }
        else if (moveDirection.x > 0 && pos.x > rightBound)
        {
            needsRespawn = true;
            newPos.x = leftBound;
        }

        // Check vertical bounds
        if (moveDirection.y < 0 && pos.y < bottomBound)
        {
            needsRespawn = true;
            newPos.y = topBound;
        }
        else if (moveDirection.y > 0 && pos.y > topBound)
        {
            needsRespawn = true;
            newPos.y = bottomBound;
        }

        if (needsRespawn)
        {
            Respawn(newPos);
        }
    }

    /// <summary>
    /// Respawn the cloud at a new position.
    /// </summary>
    private void Respawn(Vector3 newPosition)
    {
        // Add some random vertical offset
        newPosition.y += Random.Range(-2f, 2f);
        transform.position = newPosition;

        // Optionally randomize appearance on respawn
        if (randomizeOnStart)
        {
            RandomizeAppearance();
        }

        // Recalculate speed
        actualSpeed = moveSpeed + Random.Range(-speedVariation, speedVariation);

        Debug.Log($"[Cloud] {gameObject.name} respawned at {newPosition}");
    }

    /// <summary>
    /// Randomize the cloud's appearance (scale, alpha).
    /// </summary>
    private void RandomizeAppearance()
    {
        // Random scale
        float scale = Random.Range(minScale, maxScale);
        transform.localScale = new Vector3(scale, scale, 1f);

        // Random alpha
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Random.Range(minAlpha, maxAlpha);
            spriteRenderer.color = c;
        }

        // Random flip
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = Random.value > 0.5f;
        }
    }

    /// <summary>
    /// Calculate bounds based on screen/camera size.
    /// </summary>
    private void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        leftBound = cam.transform.position.x - camWidth - boundsPadding;
        rightBound = cam.transform.position.x + camWidth + boundsPadding;
        topBound = cam.transform.position.y + camHeight + boundsPadding;
        bottomBound = cam.transform.position.y - camHeight - boundsPadding;
    }

    /// <summary>
    /// Set the movement direction and speed.
    /// </summary>
    public void SetMovement(Vector2 direction, float speed)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        actualSpeed = speed + Random.Range(-speedVariation, speedVariation);
    }

    /// <summary>
    /// Set the bounds for respawning.
    /// </summary>
    public void SetBounds(float left, float right, float top, float bottom)
    {
        leftBound = left;
        rightBound = right;
        topBound = top;
        bottomBound = bottom;
        useScreenBounds = false;
    }

    /// <summary>
    /// Enable or disable parallax effect.
    /// </summary>
    public void SetParallax(bool enabled, float factor = 0.5f)
    {
        enableParallax = enabled;
        parallaxFactor = factor;

        if (enabled && cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
                lastCameraPosition = cameraTransform.position;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw bounds
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        Vector3 center = new Vector3((leftBound + rightBound) / 2f, (topBound + bottomBound) / 2f, 0f);
        Vector3 size = new Vector3(rightBound - leftBound, topBound - bottomBound, 0f);
        Gizmos.DrawWireCube(center, size);

        // Draw movement direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, new Vector3(moveDirection.x, moveDirection.y, 0f) * 2f);
    }
}
