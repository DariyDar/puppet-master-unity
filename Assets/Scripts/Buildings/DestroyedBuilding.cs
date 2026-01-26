using UnityEngine;
using System.Collections;

/// <summary>
/// Visual representation of a destroyed building.
/// Spawned when a building is destroyed, gradually decays and disappears.
/// </summary>
public class DestroyedBuilding : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite destroyedSprite;
    [SerializeField] private Color ruinsColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Decay")]
    [SerializeField] private float decayTime = 30f;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private float fadeStartPercent = 0.7f; // Start fading at 70% of decay time

    [Header("Debris")]
    [SerializeField] private bool spawnDebris = true;
    [SerializeField] private GameObject[] debrisPrefabs;
    [SerializeField] private int debrisCount = 3;
    [SerializeField] private float debrisSpawnRadius = 1f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private ParticleSystem dustParticles;
    [SerializeField] private float smokeDuration = 5f;

    // State
    private float spawnTime;
    private bool isDecaying;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spawnTime = Time.time;
        isDecaying = true;

        // Apply destroyed sprite if available
        if (destroyedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = destroyedSprite;
        }

        // Apply ruins tint
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = ruinsColor;
        }

        // Spawn debris
        if (spawnDebris)
        {
            SpawnDebris();
        }

        // Start smoke effect
        if (smokeParticles != null)
        {
            smokeParticles.Play();
            StartCoroutine(StopSmokeAfterDuration());
        }

        // Start dust effect
        if (dustParticles != null)
        {
            dustParticles.Play();
        }

        // Start decay coroutine
        StartCoroutine(DecayCoroutine());
    }

    /// <summary>
    /// Initialize the destroyed building with specific sprite.
    /// </summary>
    public void Initialize(Sprite sprite, Color? tintColor = null)
    {
        if (spriteRenderer != null)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            if (tintColor.HasValue)
            {
                ruinsColor = tintColor.Value;
                spriteRenderer.color = ruinsColor;
            }
        }
    }

    /// <summary>
    /// Spawn debris objects around the ruins.
    /// </summary>
    private void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;

        for (int i = 0; i < debrisCount; i++)
        {
            // Random position within radius
            Vector2 offset = Random.insideUnitCircle * debrisSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            // Random debris prefab
            GameObject debrisPrefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (debrisPrefab == null) continue;

            GameObject debris = Instantiate(debrisPrefab, spawnPos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            debris.transform.SetParent(transform);

            // Random scale variation
            float scale = Random.Range(0.5f, 1.2f);
            debris.transform.localScale *= scale;

            // Add rigidbody for physics if not present
            Rigidbody2D rb = debris.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = debris.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.5f;
                rb.linearDamping = 2f;
                rb.angularDamping = 1f;
            }

            // Apply small force
            rb.AddForce(offset.normalized * Random.Range(50f, 100f));
            rb.AddTorque(Random.Range(-10f, 10f));

            // Stop physics after short time
            StartCoroutine(FreezeDebrisAfterDelay(rb, 2f));
        }
    }

    private IEnumerator FreezeDebrisAfterDelay(Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private IEnumerator StopSmokeAfterDuration()
    {
        yield return new WaitForSeconds(smokeDuration);

        if (smokeParticles != null)
        {
            smokeParticles.Stop();
        }
    }

    /// <summary>
    /// Coroutine that handles the decay process.
    /// </summary>
    private IEnumerator DecayCoroutine()
    {
        float fadeStartTime = decayTime * fadeStartPercent;
        float fadeDuration = decayTime - fadeStartTime;

        // Wait until fade should start
        yield return new WaitForSeconds(fadeStartTime);

        // Stop any remaining particles
        if (dustParticles != null)
        {
            dustParticles.Stop();
        }

        // Begin fading if enabled
        if (fadeOut && spriteRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                // Also fade child sprites
                foreach (Transform child in transform)
                {
                    SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
                    if (childRenderer != null)
                    {
                        Color childColor = childRenderer.color;
                        childRenderer.color = new Color(childColor.r, childColor.g, childColor.b, alpha);
                    }
                }

                yield return null;
            }
        }
        else
        {
            // Wait remaining time if not fading
            yield return new WaitForSeconds(fadeDuration);
        }

        // Destroy the ruins
        Destroy(gameObject);
    }

    /// <summary>
    /// Force immediate removal of the ruins.
    /// </summary>
    public void ForceRemove()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    /// <summary>
    /// Reset decay timer (e.g., if player interacts with ruins).
    /// </summary>
    public void ResetDecayTimer()
    {
        StopAllCoroutines();
        spawnTime = Time.time;
        StartCoroutine(DecayCoroutine());
    }

    // Properties
    public float RemainingDecayTime => Mathf.Max(0, decayTime - (Time.time - spawnTime));
    public float DecayProgress => (Time.time - spawnTime) / decayTime;
    public bool IsDecaying => isDecaying;

    private void OnDrawGizmosSelected()
    {
        // Debris spawn radius
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, debrisSpawnRadius);
    }
}
