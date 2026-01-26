using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Storage building (chest) where player deposits collected resources.
/// - Opens when player approaches with resources
/// - Resources fly from player to chest with arc trajectory
/// - Closes when player has no resources or leaves
/// </summary>
public class Storage : BuildingBase
{
    [Header("Storage Settings")]
    [SerializeField] private bool autoDeposit = true;
    [SerializeField] private float depositInterval = 0.15f; // Time between each resource flying

    [Header("Chest Sprites")]
    [SerializeField] private Sprite chestClosed;
    [SerializeField] private Sprite chestOpen;

    [Header("Resource Sprites (for flying effect)")]
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite meatSprite;
    [SerializeField] private Sprite woodSprite;

    [Header("Flying Effect Settings")]
    [SerializeField] private float flyDuration = 0.6f;      // How long resource takes to fly to chest
    [SerializeField] private float arcHeight = 1.5f;        // Height of the arc
    [SerializeField] private float resourceScale = 0.5f;    // Scale of flying resource sprites

    [Header("Effects")]
    [SerializeField] private AudioClip depositSound;

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

        // Load resource sprites if not assigned
        LoadResourceSprites();
    }

    private void LoadResourceSprites()
    {
        if (goldSprite == null)
            goldSprite = Resources.Load<Sprite>("Sprites/Resources/Gold");
        if (meatSprite == null)
            meatSprite = Resources.Load<Sprite>("Sprites/Resources/Meat");
        if (woodSprite == null)
            woodSprite = Resources.Load<Sprite>("Sprites/Resources/Wood");
    }

    protected override void Update()
    {
        base.Update();

        bool playerHasCargo = GameManager.Instance != null && GameManager.Instance.CurrentCargo > 0;

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
        if (isDepositing) return;
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
        isDepositing = true;

        while (GameManager.Instance != null && GameManager.Instance.CurrentCargo > 0 && isPlayerInRange)
        {
            // Determine which resource to deposit (prioritize: meat, wood, gold)
            string resourceType = null;
            Sprite resourceSprite = null;

            if (GameManager.Instance.CargoMeat > 0)
            {
                resourceType = "meat";
                resourceSprite = meatSprite;
            }
            else if (GameManager.Instance.CargoWood > 0)
            {
                resourceType = "wood";
                resourceSprite = woodSprite;
            }
            else if (GameManager.Instance.CargoGold > 0)
            {
                resourceType = "gold";
                resourceSprite = goldSprite;
            }

            if (resourceType != null)
            {
                // Spawn flying resource visual
                if (player != null && resourceSprite != null)
                {
                    SpawnFlyingResource(player.position, transform.position, resourceSprite);
                }

                // Actually transfer the resource
                DepositSingleResource(resourceType);

                // Play sound
                if (depositSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(depositSound, 0.5f);
                }
            }

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
    /// </summary>
    private void DepositSingleResource(string resourceType)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.DepositSingleResource(resourceType);
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
        sr.sortingOrder = 100; // Above everything
        flyingObj.transform.localScale = Vector3.one * resourceScale;

        flyingResources.Add(flyingObj);
        StartCoroutine(FlyResourceToChest(flyingObj, startPos, endPos));
    }

    /// <summary>
    /// Animate resource flying in an arc from start to end position.
    /// </summary>
    private IEnumerator FlyResourceToChest(GameObject obj, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        // End position is slightly above chest center
        Vector3 chestTop = end + Vector3.up * 0.5f;

        while (elapsed < flyDuration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            // Ease out for smooth deceleration
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);

            // Calculate arc position
            Vector3 linearPos = Vector3.Lerp(start, chestTop, smoothT);
            float arc = arcHeight * Mathf.Sin(smoothT * Mathf.PI);
            linearPos.y += arc;

            obj.transform.position = linearPos;

            // Scale down as it approaches chest
            float scale = Mathf.Lerp(resourceScale, resourceScale * 0.5f, smoothT);
            obj.transform.localScale = Vector3.one * scale;

            yield return null;
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

        isChestOpen = false;

        if (chestClosed != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = chestClosed;
        }

        Debug.Log("[Storage] Chest closed");
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
