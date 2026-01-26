using UnityEngine;
using System.Collections;

/// <summary>
/// Explosion effect with visual feedback and optional AoE damage.
/// Used for TNT Goblin and other explosive attacks.
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float defaultRadius = 2f;
    [SerializeField] private float defaultDamage = 20f;
    [SerializeField] private float duration = 0.5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f, 0.8f);
    [SerializeField] private Color coreColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float volume = 1f;

    [Header("Damage")]
    [SerializeField] private bool dealDamage = true;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private string[] damageableTags = { "Enemy", "Unit" };

    // Runtime
    private float radius;
    private float damage;
    private float startTime;
    private bool isInitialized;
    private string poolName = "Explosion";
    private AudioSource audioSource;

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
        }
    }

    /// <summary>
    /// Initialize the explosion effect.
    /// </summary>
    public void Initialize(float explosionRadius, float explosionDamage)
    {
        radius = explosionRadius > 0 ? explosionRadius : defaultRadius;
        damage = explosionDamage > 0 ? explosionDamage : defaultDamage;
        startTime = Time.time;
        isInitialized = true;

        // Reset scale
        transform.localScale = Vector3.zero;

        // Setup sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.color = explosionColor;
        }

        // Play particles
        if (particles != null)
        {
            var main = particles.main;
            main.startSize = radius * 2f;
            particles.Play();
        }

        // Play sound
        PlayExplosionSound();

        // Deal damage immediately
        if (dealDamage && damage > 0)
        {
            DealAoEDamage();
        }

        // Start animation
        StartCoroutine(AnimateExplosion());
    }

    /// <summary>
    /// Play explosion sound effect.
    /// </summary>
    private void PlayExplosionSound()
    {
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound, volume);
        }
    }

    /// <summary>
    /// Deal AoE damage to all damageable objects in radius.
    /// </summary>
    private void DealAoEDamage()
    {
        // Find all colliders in radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius, damageableLayers);

        foreach (var col in colliders)
        {
            // Check tags
            bool isDamageable = false;
            foreach (string tag in damageableTags)
            {
                if (col.CompareTag(tag))
                {
                    isDamageable = true;
                    break;
                }
            }

            if (!isDamageable) continue;

            // Calculate damage falloff based on distance
            float distance = Vector2.Distance(transform.position, col.transform.position);
            float damageMultiplier = 1f - (distance / radius);
            float actualDamage = damage * Mathf.Max(0.25f, damageMultiplier); // Minimum 25% damage

            // Try to deal damage to different entity types
            TryDealDamage(col.gameObject, actualDamage);
        }
    }

    /// <summary>
    /// Try to deal damage to a game object.
    /// </summary>
    private void TryDealDamage(GameObject target, float dmg)
    {
        // Try EnemyBase
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(dmg);
            Debug.Log($"[ExplosionEffect] Dealt {dmg:F1} damage to enemy {target.name}");
            return;
        }

        // Try UnitBase
        UnitBase unit = target.GetComponent<UnitBase>();
        if (unit != null)
        {
            unit.TakeDamage(dmg);
            Debug.Log($"[ExplosionEffect] Dealt {dmg:F1} damage to unit {target.name}");
            return;
        }

        // Try IDamageable interface if implemented
        // (Add interface check here if you have one)
    }

    /// <summary>
    /// Animate the explosion visual.
    /// </summary>
    private IEnumerator AnimateExplosion()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Animate scale using curve
            float scaleValue = scaleCurve.Evaluate(progress) * radius * 2f;
            transform.localScale = new Vector3(scaleValue, scaleValue, 1f);

            // Animate alpha using curve
            if (spriteRenderer != null)
            {
                float alpha = alphaCurve.Evaluate(progress);
                Color c = spriteRenderer.color;
                c.a = alpha * explosionColor.a;
                spriteRenderer.color = c;
            }

            yield return null;
        }

        OnExplosionComplete();
    }

    /// <summary>
    /// Called when the explosion animation completes.
    /// </summary>
    private void OnExplosionComplete()
    {
        isInitialized = false;

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
    /// Set whether this explosion deals damage.
    /// </summary>
    public void SetDealDamage(bool deal)
    {
        dealDamage = deal;
    }

    /// <summary>
    /// Set the explosion color.
    /// </summary>
    public void SetColor(Color color)
    {
        explosionColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius > 0 ? radius : defaultRadius);
    }

    private void OnDisable()
    {
        isInitialized = false;
        StopAllCoroutines();
    }
}
