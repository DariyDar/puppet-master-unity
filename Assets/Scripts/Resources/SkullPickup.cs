using UnityEngine;
using System.Collections;

/// <summary>
/// Skull resource that drops from dead enemies.
/// Displays as a skull on the ground (Dead_9 sprite frame).
/// When spider stands nearby, plays reverse animation (Dead_9 → Dead_0) over collection duration.
/// After animation completes, skull is collected and +1 skull added to resources.
/// </summary>
public class SkullPickup : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float collectionRange = 4.5f;
    [SerializeField] private float collectionDuration = 3f;

    [Header("Sprites")]
    [Tooltip("All frames from Dead.png spritesheet (Dead_0 through Dead_9). Index 0 = Dead_0, Index 9 = Dead_9.")]
    [SerializeField] private Sprite[] deadFrames;

    [Header("Visual")]
    [SerializeField] private float skullScale = 0.70f;
    [SerializeField] private int sortingOrder = -100; // Behind player and units

    [Header("Timing")]
    [SerializeField] private float decayTime = 30f; // Skull disappears if not collected

    // State
    private SpriteRenderer spriteRenderer;
    private bool isBeingCollected;
    private bool isCollected;
    private float collectionProgress;
    private Transform collector; // The player collecting this skull
    private Coroutine collectionCoroutine;

    // The idle frame index (frame 10 = index 9 in 0-based)
    private const int IDLE_FRAME_INDEX = 9;

    // Properties
    public bool IsBeingCollected => isBeingCollected;
    public bool IsCollected => isCollected;
    public float CollectionProgress => collectionProgress;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Initialize the skull pickup at a position with the sprite frames.
    /// </summary>
    public void Initialize(Sprite[] frames)
    {
        deadFrames = frames;

        // Scale to match other resources (Dead.png sprites are large at PPU 16)
        transform.localScale = new Vector3(skullScale, skullScale, 1f);

        // Use Y-sorting so skull renders behind entities at same Y position
        YSortingRenderer ySorter = gameObject.AddComponent<YSortingRenderer>();
        ySorter.SetSortingOffset(sortingOrder); // Negative offset = behind other objects

        ShowIdleFrame();

        // Auto-destroy after decay time if not collected
        Destroy(gameObject, decayTime);
    }

    private void ShowIdleFrame()
    {
        if (deadFrames != null && deadFrames.Length > IDLE_FRAME_INDEX && spriteRenderer != null)
        {
            spriteRenderer.sprite = deadFrames[IDLE_FRAME_INDEX];
        }
    }

    private void Update()
    {
        if (isCollected) return;

        if (isBeingCollected && collector != null)
        {
            // Check if player moved out of range
            float dist = Vector2.Distance(transform.position, collector.position);
            if (dist > collectionRange * 1.3f)
            {
                CancelCollection();
            }
        }
    }

    /// <summary>
    /// Start collecting this skull (called by SkullCollector).
    /// </summary>
    public void StartCollection(Transform playerTransform)
    {
        if (isBeingCollected || isCollected) return;

        isBeingCollected = true;
        collector = playerTransform;
        collectionProgress = 0f;

        collectionCoroutine = StartCoroutine(CollectionAnimation());

        Debug.Log("[SkullPickup] Collection started");
    }

    /// <summary>
    /// Cancel collection (player moved away).
    /// </summary>
    public void CancelCollection()
    {
        if (!isBeingCollected) return;

        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
            collectionCoroutine = null;
        }

        isBeingCollected = false;
        collector = null;
        collectionProgress = 0f;

        // Reset to idle frame and scale
        transform.localScale = new Vector3(skullScale, skullScale, 1f);
        ShowIdleFrame();

        Debug.Log("[SkullPickup] Collection canceled");
    }

    /// <summary>
    /// Play pulsing collection animation over 3 seconds:
    /// 10→3→10→2→10→1→disappear (1-based frame numbers).
    /// Each "pulse" goes from idle frame backwards to a target, then back to idle.
    /// </summary>
    private IEnumerator CollectionAnimation()
    {
        if (deadFrames == null || deadFrames.Length == 0)
        {
            CompleteCollection();
            yield break;
        }

        // Animation segments (using 0-based indices):
        // Segment 1: 9→2 (frame10→frame3)
        // Segment 2: 2→9 (frame3→frame10)
        // Segment 3: 9→1 (frame10→frame2)
        // Segment 4: 1→9 (frame2→frame10)
        // Segment 5: 9→0 (frame10→frame1)
        // Then disappear
        int[][] segments = new int[][]
        {
            new int[] { IDLE_FRAME_INDEX, 2 },  // 9→2
            new int[] { 2, IDLE_FRAME_INDEX },   // 2→9
            new int[] { IDLE_FRAME_INDEX, 1 },   // 9→1
            new int[] { 1, IDLE_FRAME_INDEX },    // 1→9
            new int[] { IDLE_FRAME_INDEX, 0 },   // 9→0
        };

        float segmentDuration = collectionDuration / segments.Length;
        float totalElapsed = 0f;

        float baseScale = skullScale;

        for (int seg = 0; seg < segments.Length; seg++)
        {
            int fromFrame = segments[seg][0];
            int toFrame = segments[seg][1];
            float segElapsed = 0f;
            bool isLastSegment = (seg == segments.Length - 1);

            while (segElapsed < segmentDuration)
            {
                segElapsed += Time.deltaTime;
                totalElapsed += Time.deltaTime;
                collectionProgress = Mathf.Clamp01(totalElapsed / collectionDuration);

                float t = Mathf.Clamp01(segElapsed / segmentDuration);
                int frameIndex = Mathf.RoundToInt(Mathf.Lerp(fromFrame, toFrame, t));
                frameIndex = Mathf.Clamp(frameIndex, 0, deadFrames.Length - 1);

                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = deadFrames[frameIndex];
                }

                // Last segment (9→0): when reaching frame 0, scale up to 2x
                if (isLastSegment && frameIndex == 0)
                {
                    float scaleT = Mathf.Clamp01((t - 0.8f) / 0.2f); // Scale up in last 20% of segment
                    float s = Mathf.Lerp(baseScale, baseScale * 2f, scaleT);
                    transform.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }
        }

        CompleteCollection();
    }

    /// <summary>
    /// Collection complete — add skull to resources.
    /// </summary>
    private void CompleteCollection()
    {
        if (isCollected) return;

        isCollected = true;
        isBeingCollected = false;

        // Add skull to player resources
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddSkull(1);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnSkullCollectionCompleted(1);
        }

        Debug.Log("[SkullPickup] Skull collected! +1 skull");

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}
