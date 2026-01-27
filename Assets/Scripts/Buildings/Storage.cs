using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Storage building (chest) where player deposits collected resources.
/// - Opens when player approaches with resources (from ~3 spider lengths away)
/// - Resources fly from player to chest with arc trajectory
/// - Closes when player has no resources or leaves
/// </summary>
public class Storage : BuildingBase
{
    [Header("Storage Settings")]
    [SerializeField] private bool autoDeposit = true;
    [SerializeField] private float depositInterval = 0.3f; // Time between each resource flying

    [Header("Chest Sprites")]
    [SerializeField] private Sprite chestClosed;
    [SerializeField] private Sprite chestOpen;

    [Header("Resource Sprites (for flying effect)")]
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite meatSprite;
    [SerializeField] private Sprite woodSprite;

    [Header("Flying Effect Settings")]
    [SerializeField] private float flyDuration = 0.8f;      // How long resource takes to fly to chest
    [SerializeField] private float arcHeight = 5f;          // Height of the arc (bigger for longer distances)
    private float resourceScale = 1f;                       // Will be set in Awake to match collected resources

    [Header("Effects")]
    [SerializeField] private AudioClip depositSound;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private float lastDepositTime;
    private AudioSource audioSource;
    private bool isChestOpen = false;
    private bool isDepositing = false;
    private Coroutine depositCoroutine;
    private List<GameObject> flyingResources = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        buildingName = "Storage";

        // Set interaction range same as player's ResourcePickup.magnetRadius (9f)
        // BuildingBase multiplies interactionRange by transform.lossyScale.x
        // So we divide by scale to get exactly 9f effective radius
        float scale = transform.lossyScale.x;
        interactionRange = (scale > 0) ? (9f / scale) : 9f;

        Debug.Log($"[Storage] Scale={scale}, interactionRange={interactionRange}, effectiveRadius={interactionRange * scale}");

        // Set initial closed state (spriteRenderer inherited from BuildingBase)
        if (chestClosed != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = chestClosed;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Ensure chest has collision - player cannot walk through or push it
        SetupCollision();

        // Set sorting order so chest renders above player/spider
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 200; // Much higher than player to ensure chest is always on top
        }

        // Flying resources should match the visual size of collected resources
        // ResourcePickup prefabs typically have scale around 3-5
        resourceScale = 5f;

        // Load resource sprites if not assigned
        LoadResourceSprites();
    }

    /// <summary>
    /// Setup collision so player cannot walk through or push the chest.
    /// Box collider covers the entire chest body (not the open lid).
    /// </summary>
    private void SetupCollision()
    {
        // Add BoxCollider2D if missing
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Collider covers the visible chest body only (not empty space below sprite pivot)
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            // Full width, 70% height (chest body without empty space below)
            boxCollider.size = new Vector2(spriteBounds.size.x, spriteBounds.size.y * 0.7f);
            // Offset UP to align with visible chest (pivot is at bottom, chest body is above)
            boxCollider.offset = new Vector2(0f, spriteBounds.size.y * 0.35f);
        }
        else
        {
            // Fallback - reasonable chest size
            boxCollider.size = new Vector2(1f, 0.7f);
            boxCollider.offset = new Vector2(0f, 0.35f);
        }
        boxCollider.isTrigger = false; // Solid collision

        // Add Rigidbody2D as static so it's completely immovable
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Static;
    }

    private void LoadResourceSprites()
    {
        // Try to load from Resources folder
        if (goldSprite == null)
            goldSprite = Resources.Load<Sprite>("Sprites/Resources/Gold");
        if (meatSprite == null)
            meatSprite = Resources.Load<Sprite>("Sprites/Resources/Meat");
        if (woodSprite == null)
            woodSprite = Resources.Load<Sprite>("Sprites/Resources/Wood");

        // Log what was loaded
        Debug.Log($"[Storage] Resource sprites loaded - Gold: {(goldSprite != null ? goldSprite.name : "FALLBACK")}, Meat: {(meatSprite != null ? meatSprite.name : "FALLBACK")}, Wood: {(woodSprite != null ? woodSprite.name : "FALLBACK")}");

        // Create fallback colored sprites if none found
        if (goldSprite == null)
        {
            Debug.LogWarning("[Storage] Gold sprite not found in Resources/Sprites/Resources/Gold, using fallback");
            goldSprite = CreateColoredSprite(new Color(1f, 0.85f, 0f)); // Gold
        }
        if (meatSprite == null)
        {
            Debug.LogWarning("[Storage] Meat sprite not found in Resources/Sprites/Resources/Meat, using fallback");
            meatSprite = CreateColoredSprite(new Color(0.9f, 0.3f, 0.3f)); // Red meat
        }
        if (woodSprite == null)
        {
            Debug.LogWarning("[Storage] Wood sprite not found in Resources/Sprites/Resources/Wood, using fallback");
            woodSprite = CreateColoredSprite(new Color(0.6f, 0.4f, 0.2f)); // Brown wood
        }
    }

    /// <summary>
    /// Create a simple colored circle sprite as fallback.
    /// </summary>
    private Sprite CreateColoredSprite(Color color)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        // Draw a filled circle
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Add some shading
                    float shade = 1f - (dist / radius) * 0.3f;
                    pixels[y * size + x] = new Color(color.r * shade, color.g * shade, color.b * shade, 1f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    protected override void Update()
    {
        base.Update();

        bool playerHasCargo = GameManager.Instance != null && GameManager.Instance.CurrentCargo > 0;

        // Debug output every second
        if (debugMode && Time.frameCount % 60 == 0 && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            float scaledRange = interactionRange * transform.lossyScale.x;
            Debug.Log($"[Storage] Distance: {distance:F1}, ScaledRange: {scaledRange:F1}, InRange: {isPlayerInRange}, HasCargo: {playerHasCargo}, CargoCount: {GameManager.Instance?.CurrentCargo ?? 0}");
        }

        // Open chest when player is in range with cargo
        if (isPlayerInRange && playerHasCargo && !isChestOpen)
        {
            OpenChest();
        }
        // Close chest when player leaves or has no cargo (and not currently depositing)
        else if ((!isPlayerInRange || !playerHasCargo) && isChestOpen && !isDepositing)
        {
            CloseChest();
        }

        // Start auto-deposit when player is in range with cargo
        if (autoDeposit && isPlayerInRange && playerHasCargo && !isDepositing)
        {
            StartDepositing();
        }
    }

    private void StartDepositing()
    {
        if (isDepositing)
        {
            if (debugMode) Debug.Log($"[Storage:{GetInstanceID()}] StartDepositing called but already depositing, ignoring");
            return;
        }

        // Set flag IMMEDIATELY to prevent multiple coroutines
        isDepositing = true;

        if (debugMode) Debug.Log($"[Storage:{GetInstanceID()}] Starting deposit coroutine. Cargo: Meat={GameManager.Instance?.CargoMeat}, Wood={GameManager.Instance?.CargoWood}, Gold={GameManager.Instance?.CargoGold}");

        if (depositCoroutine != null) StopCoroutine(depositCoroutine);
        depositCoroutine = StartCoroutine(DepositResourcesGradually());
    }

    private void StopDepositing()
    {
        isDepositing = false;
        if (depositCoroutine != null)
        {
            StopCoroutine(depositCoroutine);
            depositCoroutine = null;
        }
    }

    /// <summary>
    /// Gradually deposit resources one by one with flying visual effect.
    /// </summary>
    private IEnumerator DepositResourcesGradually()
    {
        // isDepositing is already set in StartDepositing()

        while (GameManager.Instance != null && GameManager.Instance.CurrentCargo > 0 && isPlayerInRange)
        {
            // Capture current cargo counts BEFORE transfer
            int meatBefore = GameManager.Instance.CargoMeat;
            int woodBefore = GameManager.Instance.CargoWood;
            int goldBefore = GameManager.Instance.CargoGold;

            // Determine which resource to deposit (prioritize: meat, wood, gold)
            string resourceType = null;
            Sprite resourceSprite = null;

            if (meatBefore > 0)
            {
                resourceType = "meat";
                resourceSprite = meatSprite;
            }
            else if (woodBefore > 0)
            {
                resourceType = "wood";
                resourceSprite = woodSprite;
            }
            else if (goldBefore > 0)
            {
                resourceType = "gold";
                resourceSprite = goldSprite;
            }

            if (resourceType != null)
            {
                // Actually transfer the resource FIRST
                bool transferred = DepositSingleResource(resourceType);

                // Verify transfer actually happened by checking cargo changed
                int meatAfter = GameManager.Instance.CargoMeat;
                int woodAfter = GameManager.Instance.CargoWood;
                int goldAfter = GameManager.Instance.CargoGold;

                bool cargoActuallyChanged = (meatAfter < meatBefore) || (woodAfter < woodBefore) || (goldAfter < goldBefore);

                // Only spawn visual if resource was actually transferred AND cargo changed
                if (transferred && cargoActuallyChanged)
                {
                    if (player != null && resourceSprite != null)
                    {
                        SpawnFlyingResource(player.position, transform.position, resourceSprite);
                    }

                    // Play sound
                    if (depositSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(depositSound, 0.5f);
                    }

                    if (debugMode)
                    {
                        Debug.Log($"[Storage:{GetInstanceID()}] Deposited 1 {resourceType}. Cargo: Meat={meatAfter}, Wood={woodAfter}, Gold={goldAfter}");
                    }
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"[Storage:{GetInstanceID()}] Transfer failed or cargo didn't change! transferred={transferred}, cargoChanged={cargoActuallyChanged}");
                }
            }

            // Wait before next deposit
            yield return new WaitForSeconds(depositInterval);
        }

        isDepositing = false;

        // Close chest if player left or no more cargo
        if (!isPlayerInRange || (GameManager.Instance != null && GameManager.Instance.CurrentCargo <= 0))
        {
            CloseChest();
        }
    }

    /// <summary>
    /// Deposit a single unit of a specific resource type.
    /// Returns true if resource was successfully transferred.
    /// </summary>
    private bool DepositSingleResource(string resourceType)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.DepositSingleResource(resourceType);
    }

    /// <summary>
    /// Spawn a flying resource that arcs from player to chest.
    /// </summary>
    private void SpawnFlyingResource(Vector3 startPos, Vector3 endPos, Sprite sprite)
    {
        GameObject flyingObj = new GameObject("FlyingResource");
        flyingObj.transform.position = startPos;

        SpriteRenderer sr = flyingObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 300; // Above chest (200) so resources are visible flying into it
        flyingObj.transform.localScale = Vector3.one * resourceScale;

        if (debugMode)
        {
            Debug.Log($"[Storage] Spawning flying resource from {startPos} to {endPos}, sprite: {(sprite != null ? sprite.name : "fallback")}, scale: {resourceScale}");
        }

        flyingResources.Add(flyingObj);
        StartCoroutine(FlyResourceToChest(flyingObj, startPos, endPos));
    }

    /// <summary>
    /// Animate resource flying in an arc from start to end position.
    /// Resource lands in the open interior of the chest (upper part) and shrinks before disappearing.
    /// </summary>
    private IEnumerator FlyResourceToChest(GameObject obj, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        // End position is in the open interior of the chest (the dark opening when lid is open)
        // 20 pixels = 20/32 = 0.625 units (at PPU=32)
        Vector3 chestInterior = end + Vector3.up * 3.4f; // Slightly lower than before

        // Phase 1: Fly to chest with arc (70% of duration)
        float flyPhase = flyDuration * 0.7f;

        while (elapsed < flyPhase && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyPhase;

            // Ease out for smooth deceleration
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);

            // Calculate arc position
            Vector3 linearPos = Vector3.Lerp(start, chestInterior, smoothT);
            float arc = arcHeight * Mathf.Sin(smoothT * Mathf.PI);
            linearPos.y += arc;

            obj.transform.position = linearPos;

            // Keep scale constant during flight
            obj.transform.localScale = Vector3.one * resourceScale;

            yield return null;
        }

        // Phase 2: Shrink rapidly inside chest interior (30% of duration)
        if (obj != null)
        {
            obj.transform.position = chestInterior;
            float shrinkDuration = flyDuration * 0.3f;
            float shrinkElapsed = 0f;
            float startScale = resourceScale;

            while (shrinkElapsed < shrinkDuration && obj != null)
            {
                shrinkElapsed += Time.deltaTime;
                float t = shrinkElapsed / shrinkDuration;

                // Shrink from full size to zero (ease in - starts slow, ends fast)
                float scale = startScale * (1f - t * t);
                obj.transform.localScale = Vector3.one * Mathf.Max(0f, scale);

                yield return null;
            }
        }

        // Clean up
        if (obj != null)
        {
            flyingResources.Remove(obj);
            Destroy(obj);

            // Small pulse effect on chest when resource arrives
            StartCoroutine(MicroPulse());
        }
    }

    /// <summary>
    /// Small pulse when resource lands in chest.
    /// </summary>
    private IEnumerator MicroPulse()
    {
        if (spriteRenderer == null) yield break;

        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.05f;
        float duration = 0.05f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void OpenChest()
    {
        if (isChestOpen) return;

        isChestOpen = true;

        if (chestOpen != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = chestOpen;
        }

        Debug.Log("[Storage] Chest opened");
    }

    private void CloseChest()
    {
        if (!isChestOpen) return;

        // Don't close while resources are still flying - wait for them to finish
        if (flyingResources.Count > 0)
        {
            StartCoroutine(CloseChestWhenReady());
            return;
        }

        isChestOpen = false;

        if (chestClosed != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = chestClosed;
        }

        Debug.Log("[Storage] Chest closed");
    }

    /// <summary>
    /// Wait for all flying resources to finish before closing chest.
    /// </summary>
    private IEnumerator CloseChestWhenReady()
    {
        // Wait until all flying resources are gone
        while (flyingResources.Count > 0)
        {
            yield return null;
        }

        // Now close
        if (isChestOpen)
        {
            isChestOpen = false;
            if (chestClosed != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = chestClosed;
            }
            Debug.Log("[Storage] Chest closed (after resources finished)");
        }
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract()) return false;
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.CurrentCargo > 0;
    }

    public override void Interact()
    {
        if (!isDepositing)
        {
            StartDepositing();
        }
    }

    private void OnDestroy()
    {
        // Clean up any flying resources
        foreach (var obj in flyingResources)
        {
            if (obj != null) Destroy(obj);
        }
        flyingResources.Clear();
    }

    protected override void OnPlayerExitRange()
    {
        base.OnPlayerExitRange();
        StopDepositing();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = isChestOpen ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}
