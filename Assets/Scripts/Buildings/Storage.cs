using UnityEngine;
using System.Collections;

/// <summary>
/// Storage building (chest) where player deposits collected resources.
/// - Opens when player approaches with resources
/// - Closes when player has no resources or leaves
/// </summary>
public class Storage : BuildingBase
{
    [Header("Storage Settings")]
    [SerializeField] private bool autoDeposit = true;
    [SerializeField] private float depositCooldown = 0.5f;

    [Header("Chest Sprites")]
    [SerializeField] private Sprite chestClosed;
    [SerializeField] private Sprite chestOpen;

    [Header("Resource Sprites (for flying effect)")]
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite meatSprite;
    [SerializeField] private Sprite woodSprite;

    [Header("Effects")]
    [SerializeField] private AudioClip depositSound;

    private float lastDepositTime;
    private AudioSource audioSource;
    private bool isChestOpen = false;

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
        // Close chest when player leaves or has no cargo
        else if ((!isPlayerInRange || !playerHasCargo) && isChestOpen)
        {
            CloseChest();
        }

        // Auto-deposit when player is in range
        if (autoDeposit && isPlayerInRange && playerHasCargo && Time.time - lastDepositTime >= depositCooldown)
        {
            TryDeposit();
        }
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
        TryDeposit();
    }

    private void TryDeposit()
    {
        if (GameManager.Instance == null) return;

        int cargoMeat = GameManager.Instance.CargoMeat;
        int cargoWood = GameManager.Instance.CargoWood;
        int cargoGold = GameManager.Instance.CargoGold;
        int cargoAmount = GameManager.Instance.CurrentCargo;

        if (cargoAmount <= 0) return;

        bool success = GameManager.Instance.DepositCargo();

        if (success)
        {
            lastDepositTime = Time.time;
            Debug.Log($"[Storage] Deposited {cargoAmount} resources (Meat={cargoMeat}, Wood={cargoWood}, Gold={cargoGold})");

            // Play sound
            if (depositSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(depositSound);
            }

            // Visual pulse
            StartCoroutine(ScalePulse());

            // Notify event system
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnResourceDeposited(cargoMeat, cargoWood, cargoGold);
            }
        }
    }

    private IEnumerator ScalePulse()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.15f;
        float duration = 0.1f;

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

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = isChestOpen ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}
