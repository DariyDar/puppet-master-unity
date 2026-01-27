using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Types of enemy houses with different spawn patterns.
/// </summary>
public enum HouseType
{
    Small,      // Peasants only
    Medium,     // Peasants + Guards
    Large       // Mixed strong enemies
}

/// <summary>
/// Enemy house that spawns enemies when player is nearby.
/// Can be destroyed by player army, drops Wood.
/// Per GDD: HP=100, drops 5-8 Wood on destruction.
/// Attack drops: 10% Wood, 5% Gold.
/// </summary>
public class EnemyHouse : MonoBehaviour
{
    [Header("House Settings")]
    [SerializeField] private HouseType houseType = HouseType.Small;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    [Header("Spawning")]
    [SerializeField] private int maxSpawns = 3;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private float spawnRadius = 2f;
    [SerializeField] private float activationRange = 10f;

    [Header("Enemy Configs")]
    [SerializeField] private EnemyConfig peasantUnarmedConfig;
    [SerializeField] private EnemyConfig peasantAxeConfig;
    [SerializeField] private EnemyConfig lancerConfig;
    [SerializeField] private EnemyConfig archerConfig;
    [SerializeField] private EnemyConfig warriorConfig;

    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject destroyedPrefab;

    [Header("Loot on Destruction")]
    [SerializeField] private int woodMin = 5;
    [SerializeField] private int woodMax = 8;

    [Header("Loot on Attack")]
    [SerializeField] private float woodDropChance = 0.1f;   // 10%
    [SerializeField] private float goldDropChance = 0.05f;  // 5%

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite damagedSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;

    // State
    private Transform player;
    private int spawnCount = 0;
    private float lastSpawnTime;
    private bool isDestroyed;
    private bool isActivated;
    private bool hasLoggedMissingConfig;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // Resource storage (resources brought by Peasants)
    private Dictionary<ResourcePickup.ResourceType, int> storedResources = new Dictionary<ResourcePickup.ResourceType, int>();

    // XP reward
    private int xpReward => houseType switch
    {
        HouseType.Small => 40,
        HouseType.Medium => 70,
        HouseType.Large => 120,
        _ => 40
    };

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        SetupHouseType();
    }

    private void Start()
    {
        FindPlayer();
        lastSpawnTime = Time.time - spawnInterval; // Allow immediate first spawn
    }

    private void Update()
    {
        if (isDestroyed) return;

        FindPlayer();
        CleanupDeadEnemies();

        // Check if player is in activation range
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            isActivated = dist <= activationRange;
        }

        // Spawn enemies if activated and not at max
        if (isActivated && spawnCount < maxSpawns && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnEnemy();
        }
    }

    /// <summary>
    /// Setup house based on type.
    /// Per GDD: House HP=100, spawn limit 2-4.
    /// </summary>
    private void SetupHouseType()
    {
        switch (houseType)
        {
            case HouseType.Small:
                maxHealth = 100;
                maxSpawns = 2;
                spawnInterval = 8f;
                woodMin = 5;
                woodMax = 8;
                break;

            case HouseType.Medium:
                maxHealth = 100;
                maxSpawns = 3;
                spawnInterval = 10f;
                woodMin = 5;
                woodMax = 8;
                break;

            case HouseType.Large:
                maxHealth = 100;
                maxSpawns = 4;
                spawnInterval = 12f;
                woodMin = 5;
                woodMax = 8;
                break;
        }

        currentHealth = maxHealth;
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

    private void CleanupDeadEnemies()
    {
        spawnedEnemies.RemoveAll(e => e == null);
    }

    /// <summary>
    /// Spawn an enemy based on house type.
    /// </summary>
    private void SpawnEnemy()
    {
        EnemyConfig config = GetRandomEnemyConfig();
        if (config == null || enemyPrefab == null)
        {
            // Only log warning once to avoid console spam
            if (!hasLoggedMissingConfig)
            {
                Debug.LogWarning($"[EnemyHouse] {gameObject.name}: Missing config or prefab for spawning. Assign in Inspector or disable spawning.");
                hasLoggedMissingConfig = true;
            }
            return;
        }

        // Calculate spawn position
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

        // Spawn enemy
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemyObj.name = $"{config.displayName}_{spawnCount}";

        // Initialize enemy
        HumanEnemy enemy = enemyObj.GetComponent<HumanEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(config);
            enemy.SetHomePosition(transform.position);
        }

        spawnedEnemies.Add(enemyObj);
        spawnCount++;
        lastSpawnTime = Time.time;

        // Play spawn sound
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position);
        }

        Debug.Log($"[EnemyHouse] Spawned {config.displayName} ({spawnCount}/{maxSpawns})");
    }

    /// <summary>
    /// Get random enemy config based on house type.
    /// </summary>
    private EnemyConfig GetRandomEnemyConfig()
    {
        float rand = Random.value;

        switch (houseType)
        {
            case HouseType.Small:
                // 70% unarmed, 30% axe
                return rand < 0.7f ? peasantUnarmedConfig : peasantAxeConfig;

            case HouseType.Medium:
                // 30% unarmed, 40% axe, 30% lancer
                if (rand < 0.3f) return peasantUnarmedConfig;
                if (rand < 0.7f) return peasantAxeConfig;
                return lancerConfig;

            case HouseType.Large:
                // 20% axe, 30% lancer, 30% warrior, 20% archer
                if (rand < 0.2f) return peasantAxeConfig;
                if (rand < 0.5f) return lancerConfig;
                if (rand < 0.8f) return warriorConfig;
                return archerConfig;

            default:
                return peasantUnarmedConfig;
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

        // Update sprite if damaged
        if (damagedSprite != null && currentHealth < maxHealth / 2)
        {
            spriteRenderer.sprite = damagedSprite;
        }

        Debug.Log($"[EnemyHouse] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyHouse();
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
        Debug.Log($"[EnemyHouse] Stored {amount} {type}. Total: {storedResources[type]}");
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
                Vector2 offset = Random.insideUnitCircle * 1.5f;
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
            Debug.Log($"[EnemyHouse] Dropped {kvp.Value} stored {kvp.Key}");
        }
        storedResources.Clear();
    }

    /// <summary>
    /// Destroy the house and drop loot.
    /// </summary>
    private void DestroyHouse()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[EnemyHouse] {houseType} house destroyed!");

        // Play destroy sound
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // Drop stored resources first (brought by peasants)
        DropStoredResources();

        // Drop loot (Wood from buildings)
        DropLoot();

        // Spawn destroyed visual
        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, Quaternion.identity);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBuildingDestroyed(gameObject);
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

        // Drop Wood (per GDD: 5-8 Wood on destruction)
        int woodAmount = Random.Range(woodMin, woodMax + 1);
        ResourceSpawner.Instance.SpawnWood(transform.position, woodAmount);

        Debug.Log($"[EnemyHouse] Dropped {woodAmount} Wood");
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (spriteRenderer != null)
        {
            float fadeTime = 1f;
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

        Destroy(gameObject);
    }

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => isDestroyed;
    public int SpawnCount => spawnCount;

    private void OnDrawGizmosSelected()
    {
        // Activation range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        // Spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
