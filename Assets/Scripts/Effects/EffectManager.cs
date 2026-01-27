using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for spawning and pooling visual effects.
/// Handles dust, explosions, fire, water splashes, and other effects.
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("Effect Prefabs")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private GameObject waterSplashPrefab;
    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private GameObject healPrefab;

    [Header("Death Effect Sprites")]
    [Tooltip("Sprite frames from Dust_01.png spritesheet (assign all sliced frames)")]
    [SerializeField] private Sprite[] deathDustFrames;
    [Tooltip("Sprite frames from Explosion_01.png spritesheet (for TNT death)")]
    [SerializeField] private Sprite[] tntExplosionFrames;

    [Header("Skull Pickup Sprites")]
    [Tooltip("First 10 frames from Dead.png (Dead_0 through Dead_9). Used for skull on ground + collection animation.")]
    [SerializeField] private Sprite[] skullPickupFrames;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private bool expandPool = true;

    // Object pools
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
    private Transform poolParent;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create pool parent
        poolParent = new GameObject("EffectPools").transform;
        poolParent.SetParent(transform);

        // Initialize pools
        InitializePools();
    }

    /// <summary>
    /// Initialize object pools for all effect types.
    /// </summary>
    private void InitializePools()
    {
        RegisterPrefab("Dust", dustPrefab);
        RegisterPrefab("Explosion", explosionPrefab);
        RegisterPrefab("Fire", firePrefab);
        RegisterPrefab("WaterSplash", waterSplashPrefab);
        RegisterPrefab("Hit", hitPrefab);
        RegisterPrefab("Heal", healPrefab);

        // Pre-populate pools
        foreach (var kvp in prefabMap)
        {
            if (kvp.Value != null)
            {
                PrePopulatePool(kvp.Key, initialPoolSize);
            }
        }
    }

    /// <summary>
    /// Register a prefab for pooling.
    /// </summary>
    private void RegisterPrefab(string effectName, GameObject prefab)
    {
        if (prefab == null) return;

        prefabMap[effectName] = prefab;
        pools[effectName] = new Queue<GameObject>();
    }

    /// <summary>
    /// Pre-populate a pool with inactive objects.
    /// </summary>
    private void PrePopulatePool(string effectName, int count)
    {
        if (!prefabMap.ContainsKey(effectName) || prefabMap[effectName] == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = CreateNewEffect(effectName);
            if (obj != null)
            {
                ReturnToPool(effectName, obj);
            }
        }
    }

    /// <summary>
    /// Create a new effect instance.
    /// </summary>
    private GameObject CreateNewEffect(string effectName)
    {
        if (!prefabMap.ContainsKey(effectName) || prefabMap[effectName] == null) return null;

        GameObject obj = Instantiate(prefabMap[effectName], poolParent);
        obj.name = $"{effectName}_Pooled";
        obj.SetActive(false);

        return obj;
    }

    /// <summary>
    /// Get an effect from the pool or create a new one.
    /// </summary>
    private GameObject GetFromPool(string effectName)
    {
        if (!pools.ContainsKey(effectName))
        {
            Debug.LogWarning($"[EffectManager] No pool for effect: {effectName}");
            return null;
        }

        Queue<GameObject> pool = pools[effectName];

        // Try to get from pool
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        // Pool empty - create new if allowed
        if (expandPool)
        {
            GameObject newObj = CreateNewEffect(effectName);
            if (newObj != null)
            {
                newObj.SetActive(true);
                return newObj;
            }
        }

        Debug.LogWarning($"[EffectManager] Pool exhausted for effect: {effectName}");
        return null;
    }

    /// <summary>
    /// Return an effect to its pool.
    /// </summary>
    public void ReturnToPool(string effectName, GameObject obj)
    {
        if (obj == null) return;

        if (!pools.ContainsKey(effectName))
        {
            Destroy(obj);
            return;
        }

        // Check pool size limit
        if (pools[effectName].Count >= maxPoolSize)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        pools[effectName].Enqueue(obj);
    }

    #region Effect Spawning Methods

    /// <summary>
    /// Spawn a dust effect at the specified position.
    /// </summary>
    public GameObject SpawnDust(Vector3 position)
    {
        GameObject effect = GetFromPool("Dust");

        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.identity;

            // Setup auto-return
            DustEffect dust = effect.GetComponent<DustEffect>();
            if (dust != null)
            {
                dust.Initialize();
            }
        }
        else
        {
            // Fallback: create simple dust effect
            effect = CreateSimpleDustEffect(position);
        }

        return effect;
    }

    /// <summary>
    /// Spawn an explosion effect at the specified position.
    /// </summary>
    public GameObject SpawnExplosion(Vector3 position, float radius = 1f)
    {
        return SpawnExplosion(position, radius, 0f);
    }

    /// <summary>
    /// Spawn an explosion effect with damage.
    /// </summary>
    public GameObject SpawnExplosion(Vector3 position, float radius, float damage)
    {
        GameObject effect = GetFromPool("Explosion");

        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.identity;

            // Configure explosion
            ExplosionEffect explosion = effect.GetComponent<ExplosionEffect>();
            if (explosion != null)
            {
                explosion.Initialize(radius, damage);
            }
        }
        else
        {
            // Fallback: create simple explosion effect
            effect = CreateSimpleExplosionEffect(position, radius);
        }

        return effect;
    }

    /// <summary>
    /// Spawn a fire effect at the specified position.
    /// </summary>
    public GameObject SpawnFire(Vector3 position, float duration = 3f)
    {
        return SpawnFire(position, duration, 0f);
    }

    /// <summary>
    /// Spawn a fire effect with damage over time.
    /// </summary>
    public GameObject SpawnFire(Vector3 position, float duration, float damagePerSecond)
    {
        GameObject effect = GetFromPool("Fire");

        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.identity;

            // Configure fire
            FireEffect fire = effect.GetComponent<FireEffect>();
            if (fire != null)
            {
                fire.Initialize(duration, damagePerSecond);
            }
        }
        else
        {
            // Fallback: create simple fire effect
            effect = CreateSimpleFireEffect(position, duration);
        }

        return effect;
    }

    /// <summary>
    /// Spawn a water splash effect at the specified position.
    /// </summary>
    public GameObject SpawnWaterSplash(Vector3 position)
    {
        GameObject effect = GetFromPool("WaterSplash");

        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.identity;

            // Auto-return after animation
            PooledEffect pooled = effect.GetComponent<PooledEffect>();
            if (pooled != null)
            {
                pooled.Initialize("WaterSplash");
            }
        }
        else
        {
            // Fallback: create simple splash effect
            effect = CreateSimpleSplashEffect(position);
        }

        return effect;
    }

    /// <summary>
    /// Spawn a hit effect (damage indicator).
    /// </summary>
    public GameObject SpawnHit(Vector3 position)
    {
        GameObject effect = GetFromPool("Hit");

        if (effect != null)
        {
            effect.transform.position = position;

            PooledEffect pooled = effect.GetComponent<PooledEffect>();
            if (pooled != null)
            {
                pooled.Initialize("Hit");
            }
        }

        return effect;
    }

    /// <summary>
    /// Spawn a heal effect.
    /// </summary>
    public GameObject SpawnHeal(Vector3 position)
    {
        GameObject effect = GetFromPool("Heal");

        if (effect != null)
        {
            effect.transform.position = position;

            PooledEffect pooled = effect.GetComponent<PooledEffect>();
            if (pooled != null)
            {
                pooled.Initialize("Heal");
            }
        }

        return effect;
    }

    /// <summary>
    /// Spawn death effect: dust cloud animation at death position.
    /// Returns the DeathEffectPlayer so callers can check dust duration.
    /// </summary>
    public DeathEffectPlayer SpawnDeathEffect(Vector3 position, bool useTntExplosion = false)
    {
        Sprite[] dustSprites = useTntExplosion ? tntExplosionFrames : deathDustFrames;

        if (dustSprites == null || dustSprites.Length == 0)
        {
            Debug.LogWarning($"[EffectManager] No death dust sprites assigned! dust={deathDustFrames?.Length ?? 0}, tnt={tntExplosionFrames?.Length ?? 0}. Run 'Puppet Master > Setup Death Effects'.");
            return null;
        }

        GameObject go = new GameObject("DeathEffect");
        go.transform.position = position;
        DeathEffectPlayer player = go.AddComponent<DeathEffectPlayer>();

        // Dust only — skull is now a separate pickup
        player.Setup(dustSprites, null, useTntExplosion ? 6f : 8f);
        player.Play(position);

        Debug.Log($"[EffectManager] Spawned death dust at {position} with {dustSprites.Length} frames");
        return player;
    }

    /// <summary>
    /// Spawn a skull pickup at position (dropped from dead enemy).
    /// Shows Dead_9 frame on ground, collected by standing nearby.
    /// </summary>
    public GameObject SpawnSkullPickup(Vector3 position)
    {
        if (skullPickupFrames == null || skullPickupFrames.Length == 0)
        {
            Debug.LogWarning("[EffectManager] No skull pickup sprites assigned. Assign Dead_0 through Dead_9 frames in Inspector.");
            return null;
        }

        GameObject go = new GameObject("SkullPickup");
        go.transform.position = position;
        SkullPickup pickup = go.AddComponent<SkullPickup>();
        pickup.Initialize(skullPickupFrames);

        return go;
    }

    /// <summary>
    /// Spawn a skull pickup after a delay (waits for dust to dissipate).
    /// </summary>
    public void SpawnSkullPickupDelayed(Vector3 position, float delay)
    {
        if (delay <= 0f)
        {
            SpawnSkullPickup(position);
        }
        else
        {
            StartCoroutine(SpawnSkullPickupAfterDelay(position, delay));
        }
    }

    private IEnumerator SpawnSkullPickupAfterDelay(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnSkullPickup(position);
    }

    #endregion

    #region Fallback Effect Creation

    /// <summary>
    /// Create a simple dust effect when prefab is not available.
    /// </summary>
    private GameObject CreateSimpleDustEffect(Vector3 position)
    {
        GameObject dust = new GameObject("SimpleDust");
        dust.transform.position = position;

        SpriteRenderer sr = dust.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.6f, 0.5f, 0.4f, 0.7f);
        sr.sortingOrder = 100;

        // Create simple circle sprite
        sr.sprite = CreateCircleSprite();

        // Add auto-destroy
        DustEffect dustEffect = dust.AddComponent<DustEffect>();
        dustEffect.Initialize();

        return dust;
    }

    /// <summary>
    /// Create a simple explosion effect when prefab is not available.
    /// </summary>
    private GameObject CreateSimpleExplosionEffect(Vector3 position, float radius)
    {
        GameObject explosion = new GameObject("SimpleExplosion");
        explosion.transform.position = position;
        explosion.transform.localScale = Vector3.one * radius;

        SpriteRenderer sr = explosion.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.5f, 0f, 0.8f);
        sr.sortingOrder = 100;
        sr.sprite = CreateCircleSprite();

        ExplosionEffect explosionEffect = explosion.AddComponent<ExplosionEffect>();
        explosionEffect.Initialize(radius, 0f);

        return explosion;
    }

    /// <summary>
    /// Create a simple fire effect when prefab is not available.
    /// </summary>
    private GameObject CreateSimpleFireEffect(Vector3 position, float duration)
    {
        GameObject fire = new GameObject("SimpleFire");
        fire.transform.position = position;

        SpriteRenderer sr = fire.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.3f, 0f, 0.8f);
        sr.sortingOrder = 100;
        sr.sprite = CreateCircleSprite();

        FireEffect fireEffect = fire.AddComponent<FireEffect>();
        fireEffect.Initialize(duration, 0f);

        return fire;
    }

    /// <summary>
    /// Create a simple splash effect when prefab is not available.
    /// </summary>
    private GameObject CreateSimpleSplashEffect(Vector3 position)
    {
        GameObject splash = new GameObject("SimpleSplash");
        splash.transform.position = position;

        SpriteRenderer sr = splash.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.5f, 1f, 0.7f);
        sr.sortingOrder = 100;
        sr.sprite = CreateCircleSprite();

        PooledEffect pooled = splash.AddComponent<PooledEffect>();
        pooled.SetAutoDestroy(0.5f);

        return splash;
    }

    /// <summary>
    /// Create a simple circle sprite for fallback effects.
    /// </summary>
    private Sprite CreateCircleSprite()
    {
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

    #endregion

    /// <summary>
    /// Clear all pools.
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// Base component for pooled effects to handle auto-return.
/// </summary>
public class PooledEffect : MonoBehaviour
{
    [SerializeField] protected float lifetime = 1f;
    protected string poolName;
    protected float spawnTime;
    protected bool autoDestroy = true;

    public virtual void Initialize(string pool)
    {
        poolName = pool;
        spawnTime = Time.time;
    }

    public void SetAutoDestroy(float time)
    {
        lifetime = time;
        autoDestroy = true;
        Invoke(nameof(ReturnOrDestroy), lifetime);
    }

    protected virtual void Update()
    {
        if (autoDestroy && Time.time - spawnTime >= lifetime)
        {
            ReturnOrDestroy();
        }
    }

    protected void ReturnOrDestroy()
    {
        CancelInvoke();

        if (EffectManager.Instance != null && !string.IsNullOrEmpty(poolName))
        {
            EffectManager.Instance.ReturnToPool(poolName, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
