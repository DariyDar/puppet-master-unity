using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gold Mine - resource building that CANNOT be destroyed, only depleted.
/// Mechanics (from GDD):
/// - Mine cannot be destroyed - only depleted
/// - 70% chance per hit to extract 1 gold
/// - When empty -> switches to Inactive sprite
/// - Regeneration: 1 gold per 2 minutes
/// - Maximum: 15 gold
/// - Miners spawn as guards (1 per 3 gold, max 5)
/// </summary>
public class GoldMine : BuildingBase
{
    [Header("Gold Mine Settings")]
    [SerializeField] private int currentGold = 15;
    [SerializeField] private int maxGold = 15;
    [SerializeField] private float extractionChance = 0.7f;  // 70% chance per hit

    [Header("Regeneration")]
    [SerializeField] private float regenerationInterval = 120f;  // 2 minutes
    [SerializeField] private int regenerationAmount = 1;

    [Header("Miner Guards")]
    [SerializeField] private int minersPerGold = 3;  // 1 miner per 3 gold
    [SerializeField] private int maxMiners = 5;
    [SerializeField] private float minerSpawnRadius = 2f;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;    // GoldMine_Active.png
    [SerializeField] private Sprite inactiveSprite;  // GoldMine_Inactive.png

    [Header("Prefabs")]
    [SerializeField] private GameObject minerPrefab;  // PawnMiner prefab

    [Header("Audio")]
    [SerializeField] private AudioClip miningSound;
    [SerializeField] private AudioClip goldDropSound;
    [SerializeField] private AudioClip depletedSound;
    [SerializeField] private AudioClip regenerateSound;

    // State
    private bool isDepleted = false;
    private List<GameObject> activeMiners = new List<GameObject>();
    private AudioSource audioSource;
    private Coroutine regenerationCoroutine;

    // Properties
    public int CurrentGold => currentGold;
    public int MaxGold => maxGold;
    public bool IsDepleted => isDepleted;
    public float GoldPercent => maxGold > 0 ? (float)currentGold / maxGold : 0f;

    protected override void Awake()
    {
        base.Awake();
        buildingName = "Gold Mine";

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Set initial sprite
        UpdateSprite();

        // Spawn initial miners based on gold amount
        SpawnMinersBasedOnGold();

        // Start regeneration timer
        regenerationCoroutine = StartCoroutine(RegenerationCoroutine());
    }

    protected override void Update()
    {
        base.Update();

        // Clean up null references from dead miners
        activeMiners.RemoveAll(m => m == null);
    }

    /// <summary>
    /// Called when player/unit attacks the mine.
    /// Has 70% chance to extract 1 gold per hit.
    /// Mine CANNOT be destroyed!
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Mine cannot be destroyed, but attacks can extract gold
        if (isDepleted)
        {
            Debug.Log("[GoldMine] Mine is depleted. No gold to extract.");
            return;
        }

        // Play mining sound
        if (miningSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(miningSound);
        }

        // 70% chance to extract gold
        if (Random.value <= extractionChance)
        {
            ExtractGold();
        }
        else
        {
            Debug.Log("[GoldMine] Mining attempt missed. No gold extracted.");
        }
    }

    /// <summary>
    /// Extract 1 gold from the mine and spawn it.
    /// </summary>
    private void ExtractGold()
    {
        if (currentGold <= 0) return;

        currentGold--;

        // Spawn gold pickup
        if (ResourceSpawner.Instance != null)
        {
            Vector2 offset = Random.insideUnitCircle * 1f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            ResourceSpawner.Instance.SpawnGold(spawnPos, 1);
        }

        // Play gold drop sound
        if (goldDropSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(goldDropSound);
        }

        Debug.Log($"[GoldMine] Extracted 1 gold. Remaining: {currentGold}/{maxGold}");

        // Check if depleted
        if (currentGold <= 0)
        {
            BecomeDepleted();
        }

        // Update sprite based on gold level
        UpdateSprite();

        // Check if miners need to be reduced (1 miner per 3 gold)
        UpdateMinerCount();
    }

    /// <summary>
    /// Mine becomes depleted - switch to inactive sprite.
    /// </summary>
    private void BecomeDepleted()
    {
        isDepleted = true;

        // Play depleted sound
        if (depletedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(depletedSound);
        }

        // Update sprite to inactive
        UpdateSprite();

        Debug.Log("[GoldMine] Mine depleted! Will regenerate over time.");
    }

    /// <summary>
    /// Regeneration coroutine - adds 1 gold every 2 minutes.
    /// </summary>
    private IEnumerator RegenerationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenerationInterval);

            if (currentGold < maxGold)
            {
                currentGold += regenerationAmount;
                currentGold = Mathf.Min(currentGold, maxGold);

                // Play regenerate sound
                if (regenerateSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(regenerateSound);
                }

                Debug.Log($"[GoldMine] Regenerated {regenerationAmount} gold. Current: {currentGold}/{maxGold}");

                // No longer depleted if we have gold
                if (isDepleted && currentGold > 0)
                {
                    isDepleted = false;
                }

                // Update sprite
                UpdateSprite();

                // Check if we need more miners
                UpdateMinerCount();
            }
        }
    }

    /// <summary>
    /// Update sprite based on depleted state.
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        if (isDepleted || currentGold <= 0)
        {
            if (inactiveSprite != null)
            {
                spriteRenderer.sprite = inactiveSprite;
            }
            else
            {
                // Fallback: tint the sprite gray
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
        else
        {
            if (activeSprite != null)
            {
                spriteRenderer.sprite = activeSprite;
            }
            spriteRenderer.color = Color.white;
        }
    }

    /// <summary>
    /// Spawn miners based on gold amount (1 miner per 3 gold, max 5).
    /// </summary>
    private void SpawnMinersBasedOnGold()
    {
        int desiredMiners = Mathf.Min(currentGold / minersPerGold, maxMiners);
        int currentMinerCount = activeMiners.Count;

        // Spawn more miners if needed
        while (currentMinerCount < desiredMiners)
        {
            SpawnMiner();
            currentMinerCount++;
        }
    }

    /// <summary>
    /// Update miner count based on current gold.
    /// </summary>
    private void UpdateMinerCount()
    {
        int desiredMiners = Mathf.Min(currentGold / minersPerGold, maxMiners);

        // If we have too many miners and some died, don't spawn more
        // Only spawn when regenerating increases gold enough
        if (activeMiners.Count < desiredMiners)
        {
            int toSpawn = desiredMiners - activeMiners.Count;
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnMiner();
            }
        }
    }

    /// <summary>
    /// Spawn a single miner guard.
    /// </summary>
    private void SpawnMiner()
    {
        if (minerPrefab == null)
        {
            Debug.LogWarning("[GoldMine] No miner prefab assigned");
            return;
        }

        Vector2 offset = Random.insideUnitCircle * minerSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

        GameObject miner = Instantiate(minerPrefab, spawnPos, Quaternion.identity);
        miner.name = $"Miner_{activeMiners.Count}";

        // Set miner to guard this mine
        Miner minerScript = miner.GetComponent<Miner>();
        if (minerScript != null)
        {
            minerScript.SetGuardPosition(transform.position);
            minerScript.SetParentMine(this);
        }

        activeMiners.Add(miner);

        Debug.Log($"[GoldMine] Spawned miner. Total miners: {activeMiners.Count}");
    }

    /// <summary>
    /// Called by miner when it picks up gold from the ground.
    /// </summary>
    public void DepositGold(int amount)
    {
        currentGold += amount;
        currentGold = Mathf.Min(currentGold, maxGold);

        if (isDepleted && currentGold > 0)
        {
            isDepleted = false;
        }

        UpdateSprite();

        Debug.Log($"[GoldMine] Miner deposited {amount} gold. Current: {currentGold}/{maxGold}");
    }

    public override void Interact()
    {
        // Mine can't be interacted with directly - must be attacked
        Debug.Log("[GoldMine] Attack the mine to extract gold");
    }

    public override bool CanInteract()
    {
        return false; // Can't interact, must attack
    }

    private void OnDestroy()
    {
        // Stop regeneration coroutine
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Miner spawn radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minerSpawnRadius);

        // Draw gold level indicator
        Gizmos.color = isDepleted ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
