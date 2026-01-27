using UnityEngine;

/// <summary>
/// Spawns resources when enemies die or from resource nodes.
/// </summary>
public class ResourceSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject meatPrefab;     // From sheep
    [SerializeField] private GameObject woodPrefab;     // From destroyed buildings
    [SerializeField] private GameObject goldPrefab;     // From gold mines
    [SerializeField] private GameObject skullPrefab;    // From killed humans (1 per enemy)

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 0.5f;

    public static ResourceSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to enemy death events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.EnemyDied += OnEnemyDied;
        }
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.EnemyDied -= OnEnemyDied;
        }
    }

    private void OnEnemyDied(GameObject enemy)
    {
        // Enemies no longer drop resources on death.
        // Only skulls drop (handled by EffectManager.SpawnSkullPickup in HumanEnemy/Peasant Die()).
        // Exception: Peasant carrying resources drops them far away (handled in Peasant.Die()).
    }

    public void SpawnRandomLoot(Vector3 position)
    {
        // Random chance for different resources
        int lootCount = Random.Range(1, 4);

        for (int i = 0; i < lootCount; i++)
        {
            ResourcePickup.ResourceType type = GetRandomResourceType();
            int amount = GetRandomAmount(type);
            SpawnResource(type, amount, position);
        }
    }

    public void SpawnResource(ResourcePickup.ResourceType type, int amount, Vector3 position)
    {
        // Add random offset
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = position + new Vector3(offset.x, offset.y, 0);

        GameObject prefab = GetPrefabForType(type);

        if (prefab != null)
        {
            GameObject pickup = Instantiate(prefab, spawnPos, Quaternion.identity);
            ResourcePickup resource = pickup.GetComponent<ResourcePickup>();
            if (resource != null)
            {
                resource.Setup(type, amount);
            }
        }
        else
        {
            // Create simple pickup if no prefab
            CreateSimplePickup(type, amount, spawnPos);
        }
    }

    private void CreateSimplePickup(ResourcePickup.ResourceType type, int amount, Vector3 position)
    {
        GameObject pickup = new GameObject($"Pickup_{type}");
        pickup.transform.position = position;

        // Add sprite renderer
        SpriteRenderer sr = pickup.AddComponent<SpriteRenderer>();
        sr.color = GetColorForType(type);
        sr.sortingOrder = 5;

        // Try to load a default sprite
        sr.sprite = CreateDefaultSprite();

        // Add rigidbody
        Rigidbody2D rb = pickup.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 3f;

        // Add collider
        CircleCollider2D col = pickup.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Add pickup script
        ResourcePickup resource = pickup.AddComponent<ResourcePickup>();
        resource.Setup(type, amount);

        // Set layer
        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer != -1)
        {
            pickup.layer = pickupLayer;
        }
    }

    private Sprite CreateDefaultSprite()
    {
        // Create a simple circle sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                colors[y * 32 + x] = dist < 14 ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
    }

    private GameObject GetPrefabForType(ResourcePickup.ResourceType type)
    {
        switch (type)
        {
            case ResourcePickup.ResourceType.Meat: return meatPrefab;
            case ResourcePickup.ResourceType.Wood: return woodPrefab;
            case ResourcePickup.ResourceType.Gold: return goldPrefab;
            case ResourcePickup.ResourceType.Skull: return skullPrefab;
            default: return null;
        }
    }

    private Color GetColorForType(ResourcePickup.ResourceType type)
    {
        switch (type)
        {
            case ResourcePickup.ResourceType.Meat: return new Color(0.8f, 0.2f, 0.2f);     // Red (meat)
            case ResourcePickup.ResourceType.Wood: return new Color(0.6f, 0.4f, 0.2f);     // Brown (wood)
            case ResourcePickup.ResourceType.Gold: return new Color(1f, 0.85f, 0f);        // Gold
            case ResourcePickup.ResourceType.Skull: return new Color(0.9f, 0.9f, 0.8f);    // Bone white
            default: return Color.white;
        }
    }

    private ResourcePickup.ResourceType GetRandomResourceType()
    {
        float rand = Random.value;
        if (rand < 0.4f) return ResourcePickup.ResourceType.Meat;
        if (rand < 0.7f) return ResourcePickup.ResourceType.Wood;
        if (rand < 0.9f) return ResourcePickup.ResourceType.Gold;
        return ResourcePickup.ResourceType.Skull;
    }

    private int GetRandomAmount(ResourcePickup.ResourceType type)
    {
        switch (type)
        {
            case ResourcePickup.ResourceType.Meat: return Random.Range(1, 3);
            case ResourcePickup.ResourceType.Wood: return Random.Range(1, 4);
            case ResourcePickup.ResourceType.Gold: return Random.Range(1, 2);
            case ResourcePickup.ResourceType.Skull: return 1;  // Always 1 skull per enemy
            default: return 1;
        }
    }

    #region Specific Resource Spawning

    /// <summary>
    /// Spawn a skull when a human enemy is killed (always 1).
    /// </summary>
    public void SpawnSkull(Vector3 position)
    {
        SpawnResource(ResourcePickup.ResourceType.Skull, 1, position);
    }

    /// <summary>
    /// Spawn meat when a sheep is killed.
    /// </summary>
    public void SpawnMeat(Vector3 position, int amount = 1)
    {
        SpawnResource(ResourcePickup.ResourceType.Meat, amount, position);
    }

    /// <summary>
    /// Spawn wood when a building is destroyed.
    /// </summary>
    public void SpawnWood(Vector3 position, int amount = 1)
    {
        SpawnResource(ResourcePickup.ResourceType.Wood, amount, position);
    }

    /// <summary>
    /// Spawn gold from gold mines.
    /// </summary>
    public void SpawnGold(Vector3 position, int amount = 1)
    {
        SpawnResource(ResourcePickup.ResourceType.Gold, amount, position);
    }

    #endregion
}
