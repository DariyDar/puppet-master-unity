using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Types of watchtowers with different stats.
/// </summary>
public enum TowerType
{
    Basic,      // Low damage, 1 guard
    Advanced    // Higher damage, multiple guards
}

/// <summary>
/// Tower (renamed from Watchtower) that shoots at player/units and spawns guards.
/// Can be destroyed by player army.
/// Per GDD: HP=150, spawns Archer 40%, Lancer 30%, Knight 30%.
/// Drops 4-6 Wood, 1-5 Gold on destruction. 10% Wood / 5% Gold on attack.
/// </summary>
public class Watchtower : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private TowerType towerType = TowerType.Basic;
    [SerializeField] private int maxHealth = 150;
    [SerializeField] private int currentHealth = 150;

    [Header("Attack")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private float attackSpeed = 0.8f;          // attacks per second
    [SerializeField] private float attackRange = 8f;            // Detection/attack range in units
    [SerializeField] private float projectileSpeed = 10f;       // Speed of projectile
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Collider Settings")]
    [Tooltip("Setup collider at tower base so player can walk behind upper part")]
    [SerializeField] private bool setupBaseCollider = true;
    [SerializeField] private Vector2 baseColliderSize = new Vector2(5.5f, 5.5f);
    [SerializeField] private Vector2 baseColliderOffset = new Vector2(0f, -3f);
    [SerializeField] private float baseColliderEdgeRadius = 0.5f;

    [Header("Guards")]
    [SerializeField] private int guardCount = 5;
    [SerializeField] private float guardSpawnRadius = 2f;

    [Header("Prefabs")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyConfig lancerConfig;
    [SerializeField] private EnemyConfig archerConfig;
    [SerializeField] private EnemyConfig warriorConfig;

    [Header("Loot on Destruction")]
    [SerializeField] private int woodMin = 4;
    [SerializeField] private int woodMax = 6;
    [SerializeField] private int goldMin = 1;
    [SerializeField] private int goldMax = 5;

    [Header("Loot on Attack")]
    [SerializeField] private float woodDropChance = 0.1f;   // 10%
    [SerializeField] private float goldDropChance = 0.05f;  // 5%

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;

    // State
    private Transform currentTarget;
    private float lastAttackTime;
    private bool isDestroyed;
    private List<GameObject> guards = new List<GameObject>();

    // Resource storage (resources brought by Peasants)
    private Dictionary<ResourcePickup.ResourceType, int> storedResources = new Dictionary<ResourcePickup.ResourceType, int>();

    // XP reward
    private int xpReward => towerType == TowerType.Basic ? 50 : 100;

    private float attackCooldown => attackSpeed > 0 ? 1f / attackSpeed : 1f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;

        SetupTowerType();

        // Setup collider at tower base
        if (setupBaseCollider)
        {
            SetupTowerCollider();
        }

        // Ensure Y-sorting for proper draw order
        EnsureYSorting();
    }

    private void EnsureYSorting()
    {
        if (GetComponent<YSortingRenderer>() != null) return;
        var ySorting = gameObject.AddComponent<YSortingRenderer>();
        // Tower sorting point at the base (offset down)
        ySorting.SetYOffset(-3f);
    }

    /// <summary>
    /// Setup collider at the tower base so player can walk behind the upper part.
    /// </summary>
    private void SetupTowerCollider()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        boxCollider.size = baseColliderSize;
        boxCollider.offset = baseColliderOffset;
        boxCollider.edgeRadius = baseColliderEdgeRadius;

        Debug.Log($"[Watchtower] Collider set at base - Size: {baseColliderSize}, Offset: {baseColliderOffset}, EdgeRadius: {baseColliderEdgeRadius}");
    }

    private void Start()
    {
        // Ensure we have a projectile prefab
        EnsureProjectilePrefab();

        // Ensure we have a projectile spawn point
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }

        SpawnGuards();
    }

    /// <summary>
    /// Ensure tower has a projectile prefab for shooting.
    /// </summary>
    private void EnsureProjectilePrefab()
    {
        if (projectilePrefab != null) return;

        // Try to load existing prefab
        projectilePrefab = Resources.Load<GameObject>("TowerArrow");
        if (projectilePrefab != null) return;

        // Try AssetDatabase in editor
        #if UNITY_EDITOR
        projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TowerArrow.prefab");
        if (projectilePrefab != null)
        {
            Debug.Log("[Watchtower] Loaded TowerArrow prefab from Assets");
            return;
        }

        // Try ArcherArrow as fallback (same arrow sprite)
        projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArcherArrow.prefab");
        if (projectilePrefab != null)
        {
            Debug.Log("[Watchtower] Loaded ArcherArrow prefab as fallback");
            return;
        }
        #endif

        Debug.LogWarning("[Watchtower] No projectile prefab found! Tower will use instant damage.");
    }

    private void Update()
    {
        if (isDestroyed) return;

        FindTarget();
        CleanupDeadGuards();

        // Attack if target in range
        if (currentTarget != null && Time.time - lastAttackTime >= attackCooldown)
        {
            float dist = Vector2.Distance(transform.position, currentTarget.position);
            if (dist <= attackRange)
            {
                Attack();
            }
            else
            {
                currentTarget = null;
            }
        }
    }

    /// <summary>
    /// Setup tower based on type.
    /// Per GDD: Tower HP=150, spawn limit 5.
    /// </summary>
    private void SetupTowerType()
    {
        switch (towerType)
        {
            case TowerType.Basic:
                maxHealth = 150;
                damage = 12f;           // Archer(10) × 1.2
                attackSpeed = 0.5f;     // Same as Archer before 20% nerf
                attackRange = 36f;      // Archer(30) × 1.2
                projectileSpeed = 10f;
                guardCount = 5;
                woodMin = 4;
                woodMax = 6;
                goldMin = 1;
                goldMax = 5;
                break;

            case TowerType.Advanced:
                maxHealth = 150;
                damage = 10f;
                attackSpeed = 1f;
                attackRange = 10f;  // 10 units detection range
                projectileSpeed = 12f;
                guardCount = 5;
                woodMin = 4;
                woodMax = 6;
                goldMin = 1;
                goldMax = 5;
                break;
        }

        currentHealth = maxHealth;
    }

    /// <summary>
    /// Spawn guards near the tower.
    /// </summary>
    private void SpawnGuards()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[Watchtower] No enemy prefab for guards");
            return;
        }

        for (int i = 0; i < guardCount; i++)
        {
            EnemyConfig config = GetGuardConfig(i);
            if (config == null) continue;

            Vector2 offset = Random.insideUnitCircle * guardSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            GameObject guard = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            guard.name = $"Guard_{config.displayName}_{i}";

            HumanEnemy enemy = guard.GetComponent<HumanEnemy>();
            if (enemy != null)
            {
                enemy.Initialize(config);
                enemy.SetHomePosition(transform.position);
            }

            guards.Add(guard);
        }

        Debug.Log($"[Watchtower] Spawned {guardCount} guards");
    }

    /// <summary>
    /// Get guard config based on GDD: Archer 40%, Lancer 30%, Knight 30%.
    /// </summary>
    private EnemyConfig GetGuardConfig(int index)
    {
        float rand = Random.value;

        // Per GDD: Archer 40%, Lancer 30%, Knight(Warrior) 30%
        if (rand < 0.4f && archerConfig != null)
            return archerConfig;
        if (rand < 0.7f && lancerConfig != null)
            return lancerConfig;
        if (warriorConfig != null)
            return warriorConfig;

        // Fallback
        return lancerConfig ?? archerConfig;
    }

    private void CleanupDeadGuards()
    {
        guards.RemoveAll(g => g == null);
    }

    /// <summary>
    /// Find nearest valid target (player or player units).
    /// </summary>
    private void FindTarget()
    {
        float closestDist = attackRange;
        Transform closest = null;

        // Check player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = player.transform;
            }
        }

        // Check player units
        UnitBase[] units = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            if (unit.IsDead) continue;

            float dist = Vector2.Distance(transform.position, unit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = unit.transform;
            }
        }

        currentTarget = closest;
    }

    /// <summary>
    /// Attack current target with arrow projectile (parabolic arc trajectory).
    /// </summary>
    private void Attack()
    {
        if (currentTarget == null) return;

        lastAttackTime = Time.time;

        // If we have a prefab, use it
        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

            // Try ArrowProjectile first (parabolic arc with ground stick and dissolve)
            ArrowProjectile arrowProj = projectile.GetComponent<ArrowProjectile>();
            if (arrowProj != null)
            {
                // ArrowProjectile handles parabolic flight, collision, ground stick, and dissolve
                arrowProj.Setup(damage, gameObject, currentTarget, true); // true = damages player/units
                Debug.Log($"[Watchtower] Fired arrow at {currentTarget.name}");
            }
            else
            {
                // Fallback to TowerProjectile (straight line)
                Vector2 direction = ((Vector2)currentTarget.position - (Vector2)projectileSpawnPoint.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

                TowerProjectile towerProj = projectile.GetComponent<TowerProjectile>();
                if (towerProj != null)
                {
                    towerProj.Initialize(damage, direction, projectileSpeed);
                }
                else
                {
                    // Generic Projectile fallback
                    Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = direction * projectileSpeed;
                    }

                    Projectile proj = projectile.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Setup(damage, gameObject, true);
                    }
                }

                Destroy(projectile, 5f);
            }
        }
        else
        {
            // No prefab - do instant damage (fallback)
            Debug.LogWarning($"[Watchtower] No projectilePrefab assigned! Using instant damage fallback.");
            DealInstantDamage();
        }

        // Play sound
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }
    }

    /// <summary>
    /// Deal instant damage when no projectile prefab is available.
    /// </summary>
    private void DealInstantDamage()
    {
        if (currentTarget == null) return;

        // Check if player
        PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(Mathf.RoundToInt(damage));
            Debug.Log($"[Watchtower] Instant hit player for {damage} damage");
            return;
        }

        // Check if unit
        UnitBase unit = currentTarget.GetComponent<UnitBase>();
        if (unit != null)
        {
            unit.TakeDamage(Mathf.RoundToInt(damage));
            Debug.Log($"[Watchtower] Instant hit unit for {damage} damage");
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

        StartCoroutine(DamageFlash());

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // Drop resources on attack (per GDD)
        DropOnAttack();

        Debug.Log($"[Tower] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyTower();
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
        Debug.Log($"[Watchtower] Stored {amount} {type}. Total: {storedResources[type]}");
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
            Debug.Log($"[Watchtower] Dropped {kvp.Value} stored {kvp.Key}");
        }
        storedResources.Clear();
    }

    /// <summary>
    /// Destroy the tower.
    /// </summary>
    private void DestroyTower()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[Watchtower] {towerType} tower destroyed!");

        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // Drop stored resources first (brought by peasants)
        DropStoredResources();

        DropLoot();

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBuildingDestroyed(gameObject);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    private void DropLoot()
    {
        if (ResourceSpawner.Instance == null) return;

        // Per GDD: 4-6 Wood, 1-5 Gold on destruction
        int woodAmount = Random.Range(woodMin, woodMax + 1);
        ResourceSpawner.Instance.SpawnWood(transform.position, woodAmount);

        int goldAmount = Random.Range(goldMin, goldMax + 1);
        ResourceSpawner.Instance.SpawnGold(transform.position, goldAmount);

        Debug.Log($"[Tower] Dropped {woodAmount} Wood, {goldAmount} Gold");
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

    private void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Guard spawn radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, guardSpawnRadius);
    }
}
