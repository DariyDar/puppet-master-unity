using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fire effect that deals damage over time (DoT) to objects in its area.
/// Duration and damage are configurable.
/// </summary>
public class FireEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private float defaultDamagePerSecond = 5f;
    [SerializeField] private float damageTickRate = 0.5f; // How often to deal damage
    [SerializeField] private float radius = 1f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0f, 0.8f);
    [SerializeField] private float flickerSpeed = 10f;
    [SerializeField] private float flickerAmount = 0.2f;
    [SerializeField] private float scaleFlicker = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip igniteSound;
    [SerializeField] private float volume = 0.5f;

    [Header("Damage")]
    [SerializeField] private bool dealDamage = true;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private string[] damageableTags = { "Enemy", "Unit", "Player" };

    // Runtime
    private float duration;
    private float damagePerSecond;
    private float startTime;
    private float lastDamageTime;
    private bool isInitialized;
    private bool isFadingOut;
    private string poolName = "Fire";
    private AudioSource audioSource;
    private Vector3 originalScale;
    private HashSet<GameObject> objectsInFire = new HashSet<GameObject>();
    private Collider2D triggerCollider;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (particles == null)
        {
            particles = GetComponent<ParticleSystem>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
        }

        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Initialize the fire effect.
    /// </summary>
    public void Initialize(float fireDuration, float fireDamagePerSecond)
    {
        duration = fireDuration > 0 ? fireDuration : defaultDuration;
        damagePerSecond = fireDamagePerSecond > 0 ? fireDamagePerSecond : defaultDamagePerSecond;
        startTime = Time.time;
        lastDamageTime = startTime;
        isInitialized = true;
        isFadingOut = false;
        originalScale = transform.localScale;
        objectsInFire.Clear();

        // Setup sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.color = fireColor;
        }

        // Play particles
        if (particles != null)
        {
            particles.Play();
        }

        // Play sounds
        PlayIgniteSound();
        StartFireLoopSound();

        Debug.Log($"[FireEffect] Initialized: duration={duration}s, damage={damagePerSecond}/s");
    }

    private void Update()
    {
        if (!isInitialized) return;

        float elapsed = Time.time - startTime;
        float progress = elapsed / duration;

        // Animate flicker
        AnimateFlicker();

        // Deal periodic damage
        if (dealDamage && Time.time - lastDamageTime >= damageTickRate)
        {
            DealDamageToObjectsInFire();
            lastDamageTime = Time.time;
        }

        // Check duration
        if (elapsed >= duration && !isFadingOut)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    /// <summary>
    /// Animate fire flickering.
    /// </summary>
    private void AnimateFlicker()
    {
        // Color flicker
        if (spriteRenderer != null)
        {
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            Color c = fireColor;
            c.a = fireColor.a * (1f - flickerAmount + flicker * flickerAmount);
            spriteRenderer.color = c;
        }

        // Scale flicker
        float scaleNoise = Mathf.PerlinNoise(Time.time * flickerSpeed * 0.7f, 100f);
        float scaleMod = 1f + (scaleNoise - 0.5f) * scaleFlicker * 2f;
        transform.localScale = originalScale * scaleMod;
    }

    /// <summary>
    /// Deal damage to all objects currently in the fire.
    /// </summary>
    private void DealDamageToObjectsInFire()
    {
        float tickDamage = damagePerSecond * damageTickRate;

        // Clean up null references
        objectsInFire.RemoveWhere(obj => obj == null);

        foreach (var obj in objectsInFire)
        {
            TryDealDamage(obj, tickDamage);
        }
    }

    /// <summary>
    /// Try to deal damage to a game object.
    /// </summary>
    private void TryDealDamage(GameObject target, float dmg)
    {
        if (target == null) return;

        // Try EnemyBase
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeDamage(dmg);
            return;
        }

        // Try UnitBase
        UnitBase unit = target.GetComponent<UnitBase>();
        if (unit != null && !unit.IsDead)
        {
            unit.TakeDamage(dmg);
            return;
        }

        // Try PlayerHealth
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(Mathf.RoundToInt(dmg));
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        if (ShouldDamage(other.gameObject))
        {
            objectsInFire.Add(other.gameObject);
            Debug.Log($"[FireEffect] {other.name} entered fire");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        objectsInFire.Remove(other.gameObject);
        Debug.Log($"[FireEffect] {other.name} left fire");
    }

    /// <summary>
    /// Check if an object should take fire damage.
    /// </summary>
    private bool ShouldDamage(GameObject obj)
    {
        // Check tags
        foreach (string tag in damageableTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }

        // Check layer
        if (damageableLayers != 0)
        {
            if (((1 << obj.layer) & damageableLayers) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Fade out and destroy/return to pool.
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        isFadingOut = true;
        float fadeTime = 0.5f;
        float elapsed = 0f;

        // Stop fire loop sound
        StopFireLoopSound();

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeTime;

            // Fade sprite
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(fireColor.a, 0f, progress);
                spriteRenderer.color = c;
            }

            // Scale down
            transform.localScale = originalScale * Mathf.Lerp(1f, 0.5f, progress);

            yield return null;
        }

        OnFireComplete();
    }

    /// <summary>
    /// Called when the fire effect ends.
    /// </summary>
    private void OnFireComplete()
    {
        isInitialized = false;
        objectsInFire.Clear();

        // Stop particles
        if (particles != null)
        {
            particles.Stop();
        }

        // Return to pool or destroy
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.ReturnToPool(poolName, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play ignite sound effect.
    /// </summary>
    private void PlayIgniteSound()
    {
        if (igniteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(igniteSound, volume);
        }
    }

    /// <summary>
    /// Start looping fire sound.
    /// </summary>
    private void StartFireLoopSound()
    {
        if (fireSound != null && audioSource != null)
        {
            audioSource.clip = fireSound;
            audioSource.volume = volume;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Stop looping fire sound.
    /// </summary>
    private void StopFireLoopSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Set whether this fire deals damage.
    /// </summary>
    public void SetDealDamage(bool deal)
    {
        dealDamage = deal;
    }

    /// <summary>
    /// Set the fire color.
    /// </summary>
    public void SetColor(Color color)
    {
        fireColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// Extinguish the fire immediately.
    /// </summary>
    public void Extinguish()
    {
        if (isInitialized && !isFadingOut)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    /// <summary>
    /// Get remaining duration.
    /// </summary>
    public float RemainingDuration => Mathf.Max(0f, duration - (Time.time - startTime));

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }

    private void OnDisable()
    {
        isInitialized = false;
        StopAllCoroutines();
        StopFireLoopSound();
    }
}
