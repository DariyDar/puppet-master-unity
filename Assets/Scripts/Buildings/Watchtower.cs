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
    [SerializeField] private float attackRange = 180f;
    [SerializeField] private Transform projectileSpawnPoint;

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
    }

    private void Start()
    {
        SpawnGuards();
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
                damage = 5f;
                attackSpeed = 0.8f;
                attackRange = 180f;
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
                attackRange = 220f;
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
    /// Attack current target.
    /// </summary>
    private void Attack()
    {
        if (currentTarget == null || projectilePrefab == null) return;

        lastAttackTime = Time.time;

        // Spawn projectile
        Vector2 direction = ((Vector2)currentTarget.position - (Vector2)projectileSpawnPoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

        // Rotate projectile
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Set velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * 300f;
        }

        // Setup damage
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Setup(damage, gameObject, true); // Damages player and units
        }

        Destroy(projectile, 3f);

        // Play sound
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
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
