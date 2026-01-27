using UnityEngine;
using System.Collections;

/// <summary>
/// Plays death effects: dust cloud covering the dying unit, then animated skull appears.
/// Spawned at unit's position on death. Uses sprite sheet frame animation.
/// </summary>
public class DeathEffectPlayer : MonoBehaviour
{
    [Header("Dust Settings")]
    [SerializeField] private Sprite[] dustFrames;
    [SerializeField] private float dustFrameRate = 15f;
    [SerializeField] private float dustScale = 8f;

    [Header("Skull Settings")]
    [SerializeField] private Sprite[] skullFrames;
    [SerializeField] private float skullFrameRate = 10f;
    [SerializeField] private float skullScale = 1f;

    [Header("Timing")]
    [SerializeField] private float skullDelay = 0.1f; // Start skull slightly before dust ends
    [SerializeField] private float totalLifetime = 3f;

    private SpriteRenderer dustRenderer;
    private SpriteRenderer skullRenderer;

    /// <summary>
    /// Duration of the dust animation in seconds.
    /// </summary>
    public float DustDuration => (dustFrames != null && dustFrames.Length > 0) ? dustFrames.Length / dustFrameRate : 0f;

    /// <summary>
    /// Play the full death sequence: dust poof then skull.
    /// </summary>
    public void Play(Vector3 position)
    {
        transform.position = position;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Create dust sprite renderer
        GameObject dustObj = new GameObject("DeathDust");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = Vector3.zero;
        dustObj.transform.localScale = Vector3.one * dustScale;
        dustRenderer = dustObj.AddComponent<SpriteRenderer>();
        dustRenderer.sortingOrder = 1000;

        // Create skull sprite renderer (hidden initially)
        GameObject skullObj = new GameObject("DeathSkull");
        skullObj.transform.SetParent(transform);
        skullObj.transform.localPosition = Vector3.zero;
        skullObj.transform.localScale = Vector3.one * skullScale;
        skullRenderer = skullObj.AddComponent<SpriteRenderer>();
        skullRenderer.sortingOrder = 999;
        skullRenderer.enabled = false;

        // Play dust animation
        if (dustFrames != null && dustFrames.Length > 0)
        {
            float dustDuration = dustFrames.Length / dustFrameRate;
            float skullStartTime = Mathf.Max(0f, dustDuration - skullDelay);
            bool skullStarted = false;

            float elapsed = 0f;
            int frameIndex = 0;
            float frameTimer = 0f;
            float frameDuration = 1f / dustFrameRate;

            while (frameIndex < dustFrames.Length)
            {
                dustRenderer.sprite = dustFrames[frameIndex];

                // Start skull partway through dust
                if (!skullStarted && elapsed >= skullStartTime)
                {
                    skullStarted = true;
                    StartCoroutine(PlaySkullAnimation());
                }

                yield return null;
                elapsed += Time.deltaTime;
                frameTimer += Time.deltaTime;
                if (frameTimer >= frameDuration)
                {
                    frameTimer -= frameDuration;
                    frameIndex++;
                }
            }

            // Ensure skull started
            if (!skullStarted)
            {
                StartCoroutine(PlaySkullAnimation());
            }

            // Hide dust
            dustRenderer.enabled = false;
        }
        else
        {
            // No dust frames, just play skull
            StartCoroutine(PlaySkullAnimation());
        }

        // Wait for total lifetime then destroy
        yield return new WaitForSeconds(totalLifetime);
        Destroy(gameObject);
    }

    private IEnumerator PlaySkullAnimation()
    {
        if (skullFrames == null || skullFrames.Length == 0) yield break;

        skullRenderer.enabled = true;
        float frameDuration = 1f / skullFrameRate;

        // Play skull animation once (it includes appear + disappear)
        for (int i = 0; i < skullFrames.Length; i++)
        {
            skullRenderer.sprite = skullFrames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        skullRenderer.enabled = false;
    }

    /// <summary>
    /// Setup sprites and scale at runtime (called by EffectManager).
    /// </summary>
    public void Setup(Sprite[] dust, Sprite[] skull, float scale = 2f)
    {
        dustFrames = dust;
        skullFrames = skull;
        dustScale = scale;
    }

    /// <summary>
    /// Static helper to spawn a death effect at position.
    /// </summary>
    public static DeathEffectPlayer SpawnAt(Vector3 position, Sprite[] dust, Sprite[] skull)
    {
        GameObject go = new GameObject("DeathEffect");
        go.transform.position = position;
        DeathEffectPlayer player = go.AddComponent<DeathEffectPlayer>();
        player.dustFrames = dust;
        player.skullFrames = skull;
        player.Play(position);
        return player;
    }
}
