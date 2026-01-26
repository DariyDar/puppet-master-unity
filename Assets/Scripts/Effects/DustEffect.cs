using UnityEngine;

/// <summary>
/// A simple dust particle effect that fades out over time.
/// Can use either a SpriteRenderer with animation or a ParticleSystem.
/// </summary>
public class DustEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float expandSpeed = 1f;
    [SerializeField] private float riseSpeed = 0.5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Color dustColor = new Color(0.6f, 0.5f, 0.4f, 0.7f);

    [Header("Randomization")]
    [SerializeField] private bool randomizeScale = true;
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 0.7f;
    [SerializeField] private bool randomizeRotation = true;

    // Runtime
    private float spawnTime;
    private float currentAlpha;
    private Vector3 initialScale;
    private bool isInitialized;
    private string poolName = "Dust";

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
    }

    /// <summary>
    /// Initialize the dust effect.
    /// </summary>
    public void Initialize()
    {
        spawnTime = Time.time;
        isInitialized = true;

        // Randomize appearance
        if (randomizeScale)
        {
            float scale = Random.Range(minScale, maxScale);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (randomizeRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        initialScale = transform.localScale;

        // Setup sprite
        if (spriteRenderer != null)
        {
            currentAlpha = dustColor.a;
            spriteRenderer.color = dustColor;
        }

        // Play particle system if available
        if (particles != null)
        {
            particles.Play();
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        float elapsed = Time.time - spawnTime;
        float progress = elapsed / lifetime;

        // Update sprite animation
        if (spriteRenderer != null)
        {
            // Fade out
            currentAlpha = Mathf.Lerp(dustColor.a, 0f, progress * fadeSpeed);
            Color c = spriteRenderer.color;
            c.a = currentAlpha;
            spriteRenderer.color = c;

            // Expand
            float scaleMultiplier = 1f + (expandSpeed * progress);
            transform.localScale = initialScale * scaleMultiplier;
        }

        // Rise upward
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Check lifetime
        if (elapsed >= lifetime)
        {
            OnEffectComplete();
        }
    }

    /// <summary>
    /// Called when the effect completes.
    /// </summary>
    private void OnEffectComplete()
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
    /// Set the dust color.
    /// </summary>
    public void SetColor(Color color)
    {
        dustColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            currentAlpha = color.a;
        }
    }

    /// <summary>
    /// Set the lifetime of the effect.
    /// </summary>
    public void SetLifetime(float time)
    {
        lifetime = time;
    }

    private void OnDisable()
    {
        isInitialized = false;
    }
}
