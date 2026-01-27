using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Castle - the main enemy stronghold.
/// High health, spawns elite guards, drops loot when destroyed.
/// Destroying a castle triggers a "zone liberated" event.
/// Per GDD: HP=500, spawn limit 10, spawns all types + Monk 50%.
/// Drops 10-15 Wood, 5-12 Gold on destruction. 10% Wood / 5% Gold on attack.
/// Castle RESPAWNS (unlike other buildings).
/// </summary>
public class Castle : MonoBehaviour
{
    [Header("Castle Settings")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int currentHealth = 500;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private int maxGuards = 10;
    [SerializeField] private float guardSpawnRadius = 3f;
    [SerializeField] private float activationRange = 15f;

    [Header("Guard Configs")]
    [SerializeField] private List<EnemyConfig> guardConfigs = new List<EnemyConfig>(); // All types
    [SerializeField] private EnemyConfig monkConfig; // 50% chance to include Monk

    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject destroyedPrefab;

    [Header("Loot on Destruction")]
    [SerializeField] private int woodMin = 10;
    [SerializeField] private int woodMax = 15;
    [SerializeField] private int goldMin = 5;
    [SerializeField] private int goldMax = 12;

    [Header("Loot on Attack")]
    [SerializeField] private float woodDropChance = 0.1f;   // 10%
    [SerializeField] private float goldDropChance = 0.05f;  // 5%

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite damagedSprite;
    [SerializeField] private Sprite heavilyDamagedSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip liberationFanfare;

    [Header("Respawn Settings")]
    [Tooltip("If true, castle will respawn after being destroyed")]
    [SerializeField] private bool canRespawn = true;
    [Tooltip("Time in seconds before respawn (5 minutes default)")]
    [SerializeField] private float respawnTime = 300f;
    [Tooltip("Castle only respawns if player is farther than this distance")]
    [SerializeField] private float playerSafeDistance = 500f;

    // State
    private Transform player;
    private int spawnCount = 0;
    private float lastSpawnTime;
    private bool isDestroyed;
    private bool isActivated;
    private bool hasLoggedMissingConfig;
    private List<GameObject> spawnedGuards = new List<GameObject>();

    // Respawn state
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool waitingForRespawn = false;
    private float respawnTimer = 0f;
    private Collider2D castleCollider;

    // Resource storage (resources brought by Peasants)
    private Dictionary<ResourcePickup.ResourceType, int> storedResources = new Dictionary<ResourcePickup.ResourceType, int>();

    // XP reward for destroying castle
    private int xpReward = 500;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
    }

    private void Start()
    {
        FindPlayer();
        lastSpawnTime = Time.time - spawnInterval; // Allow immediate first spawn

        // Store original position for respawn
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        castleCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // Handle respawn logic
        if (waitingForRespawn)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnTime)
            {
                TryRespawn();
            }
            return;
        }

        if (isDestroyed) return;

        FindPlayer();
        CleanupDeadGuards();

        // Check if player is in activation range
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            isActivated = dist <= activationRange;
        }

        // Spawn guards if activated and not at max
        if (isActivated && spawnCount < maxGuards && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnGuard();
        }

        // Update visual based on damage
        UpdateDamageVisual();
    }

    /// <summary>
    /// Try to respawn the castle. Only succeeds if player is far enough away.
    /// </summary>
    private void TryRespawn()
    {
        FindPlayer();

        if (player != null)
        {
            float distance = Vector2.Distance(originalPosition, player.position);
            if (distance < playerSafeDistance)
            {
                // Player is too close - wait until they leave
                return;
            }
        }

        // Player is far enough - respawn!
        Respawn();
    }

    /// <summary>
    /// Respawn the castle at its original position with full health.
    /// </summary>
    private void Respawn()
    {
        Debug.Log("[Castle] Respawning castle!");

        waitingForRespawn = false;
        respawnTimer = 0f;

        // Reset position
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // Reset health
        currentHealth = maxHealth;

        // Reset spawn count
        spawnCount = 0;
        spawnedGuards.Clear();

        // Reset state
        isDestroyed = false;
        isActivated = false;

        // Reset visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            if (normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }
            spriteRenderer.color = Color.white;
        }

        // Re-enable collider
        if (castleCollider != null)
        {
            castleCollider.enabled = true;
        }

        // Re-enable game object (in case it was hidden)
        gameObject.SetActive(true);

        // Reset spawn timer
        lastSpawnTime = Time.time - spawnInterval;
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void CleanupDeadGuards()
    {
        int before = spawnedGuards.Count;
        spawnedGuards.RemoveAll(g => g == null);

        // If guards died, allow respawning
        int died = before - spawnedGuards.Count;
        if (died > 0)
        {
            spawnCount -= died;
            spawnCount = Mathf.Max(0, spawnCount);
        }
    }

    /// <summary>
    /// Spawn an elite guard based on guard configs.
    /// Per GDD: All types + Monk (50% chance).
    /// </summary>
    private void SpawnGuard()
    {
        if (guardConfigs == null || guardConfigs.Count == 0 || enemyPrefab == null)
        {
            // Only log warning once to avoid console spam
            if (!hasLoggedMissingConfig)
            {
                Debug.LogWarning($"[Castle] {gameObject.name}: Missing guard configs or enemy prefab. Assign in Inspector.");
                hasLoggedMissingConfig = true;
            }
            return;
        }

        // Get random guard config
        // Per GDD: 50% chance to spawn Monk if available
        EnemyConfig config;
        if (monkConfig != null && Random.value < 0.5f)
        {
            config = monkConfig;
        }
        else
        {
            config = guardConfigs[Random.Range(0, guardConfigs.Count)];
        }
        if (config == null) return;

        // Calculate spawn position
        Vector2 offset = Random.insideUnitCircle * guardSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

        // Spawn guard
        GameObject guardObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        guardObj.name = $"CastleGuard_{config.displayName}_{spawnCount}";

        // Initialize enemy
        HumanEnemy enemy = guardObj.GetComponent<HumanEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(config);
            enemy.SetHomePosition(transform.position);
        }

        spawnedGuards.Add(guardObj);
        spawnCount++;
        lastSpawnTime = Time.time;

        // Play spawn sound
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position);
        }

        Debug.Log($"[Castle] Spawned guard {config.displayName} ({spawnCount}/{maxGuards})");
    }

    private void UpdateDamageVisual()
    {
        if (spriteRenderer == null) return;

        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent <= 0.25f && heavilyDamagedSprite != null)
        {
            spriteRenderer.sprite = heavilyDamagedSprite;
        }
        else if (healthPercent <= 0.5f && damagedSprite != null)
        {
            spriteRenderer.sprite = damagedSprite;
        }
        else if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    /// <summary>
    /// Take damage from player army.
    /// Per GDD: 10% chance to drop Wood, 5% chance to drop Gold on each attack.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= Mathf.RoundToInt(damage);

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Play hit sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // Drop resources on attack (per GDD)
        DropOnAttack();

        Debug.Log($"[Castle] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }

    /// <summary>
    /// Chance to drop resources when attacked.
    /// </summary>
    private void DropOnAttack()
    {
        if (ResourceSpawner.Instance == null) return;

        // 10% chance to drop 1 Wood
        if (Random.value <= woodDropChance)
        {
            Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
            ResourceSpawner.Instance.SpawnWood(dropPos, 1);
        }

        // 5% chance to drop 1 Gold
        if (Random.value <= goldDropChance)
        {
            Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
            ResourceSpawner.Instance.SpawnGold(dropPos, 1);
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (!isDestroyed && spriteRenderer != null)
        {
            spriteRenderer.color = original;
        }
    }

    /// <summary>
    /// Store a resource brought by a Peasant.
    /// </summary>
    public void StoreResource(ResourcePickup.ResourceType type, int amount)
    {
        if (!storedResources.ContainsKey(type))
            storedResources[type] = 0;
        storedResources[type] += amount;
        Debug.Log($"[Castle] Stored {amount} {type}. Total: {storedResources[type]}");
    }

    /// <summary>
    /// Drop all stored resources when building is destroyed.
    /// </summary>
    private void DropStoredResources()
    {
        if (ResourceSpawner.Instance == null) return;

        foreach (var kvp in storedResources)
        {
            if (kvp.Value <= 0) continue;

            for (int i = 0; i < kvp.Value; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                Vector3 pos = transform.position + new Vector3(offset.x, offset.y, 0);

                switch (kvp.Key)
                {
                    case ResourcePickup.ResourceType.Meat:
                        ResourceSpawner.Instance.SpawnMeat(pos, 1);
                        break;
                    case ResourcePickup.ResourceType.Wood:
                        ResourceSpawner.Instance.SpawnWood(pos, 1);
                        break;
                    case ResourcePickup.ResourceType.Gold:
                        ResourceSpawner.Instance.SpawnGold(pos, 1);
                        break;
                }
            }
            Debug.Log($"[Castle] Dropped {kvp.Value} stored {kvp.Key}");
        }
        storedResources.Clear();
    }

    /// <summary>
    /// Called when castle is destroyed. Triggers zone liberation event.
    /// </summary>
    private void OnDestroyed()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("[Castle] Castle destroyed! Zone liberated!");

        // Play destroy sound
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // Play liberation fanfare
        if (liberationFanfare != null)
        {
            AudioSource.PlayClipAtPoint(liberationFanfare, transform.position);
        }

        // Drop stored resources first (brought by peasants)
        DropStoredResources();

        // Drop massive loot
        DropLoot();

        // Spawn destroyed visual
        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, Quaternion.identity);
        }

        // Notify event system - building destroyed
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBuildingDestroyed(gameObject);

            // Special castle destroyed event (zone liberation)
            EventManager.Instance.OnCastleDestroyed(gameObject);
        }

        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Fade out and destroy
        StartCoroutine(FadeOutAndDestroy());
    }

    private void DropLoot()
    {
        if (ResourceSpawner.Instance == null) return;

        // Per GDD: 10-15 Wood, 5-12 Gold on destruction
        int woodAmount = Random.Range(woodMin, woodMax + 1);
        ResourceSpawner.Instance.SpawnWood(transform.position, woodAmount);

        int goldAmount = Random.Range(goldMin, goldMax + 1);
        ResourceSpawner.Instance.SpawnGold(transform.position, goldAmount);

        Debug.Log($"[Castle] Dropped loot: Wood={woodAmount}, Gold={goldAmount}");
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (spriteRenderer != null)
        {
            float fadeTime = 2f;
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        // If respawn is enabled, start waiting for respawn instead of destroying
        if (canRespawn)
        {
            waitingForRespawn = true;
            respawnTimer = 0f;

            // Hide the castle but don't destroy it
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            Debug.Log($"[Castle] Castle hidden. Will respawn in {respawnTime} seconds (if player is {playerSafeDistance}+ units away)");
        }
        else
        {
            // No respawn - actually destroy
            Destroy(gameObject);
        }
    }

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => isDestroyed;
    public int SpawnCount => spawnCount;
    public int XpReward => xpReward;

    private void OnDrawGizmosSelected()
    {
        // Activation range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        // Guard spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, guardSpawnRadius);
    }
}
