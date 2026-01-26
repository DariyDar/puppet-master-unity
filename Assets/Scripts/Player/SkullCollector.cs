using UnityEngine;
using System.Collections;

/// <summary>
/// Skull Collector system - automatically collects skulls from nearby corpses.
/// Shows progress bar during collection. Attach to player GameObject.
/// </summary>
public class SkullCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float collectionRange = 2f;
    [SerializeField] private float collectionDuration = 2f;
    [SerializeField] private bool autoCollect = true;

    [Header("Visual Feedback")]
    [SerializeField] private bool showCollectionRange = true;
    [SerializeField] private Color rangeGizmoColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioClip collectionStartSound;
    [SerializeField] private AudioClip collectionLoopSound;
    [SerializeField] private AudioClip collectionCompleteSound;

    // State
    private bool isCollecting;
    private float collectionProgress;
    private HumanEnemy currentTarget;
    private Coroutine collectionCoroutine;
    private AudioSource audioSource;

    // Events
    public event System.Action<HumanEnemy> OnCollectionStarted;
    public event System.Action<float> OnCollectionProgress;
    public event System.Action<int> OnCollectionCompleted;
    public event System.Action OnCollectionCanceled;

    // Properties
    public bool IsCollecting => isCollecting;
    public float CollectionProgress => collectionProgress;
    public float CollectionRange => collectionRange;
    public HumanEnemy CurrentTarget => currentTarget;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!autoCollect) return;

        // If not collecting, try to find a corpse
        if (!isCollecting)
        {
            HumanEnemy nearestCorpse = FindNearestCorpse();
            if (nearestCorpse != null)
            {
                StartCollection(nearestCorpse);
            }
        }
        else
        {
            // Check if target is still valid
            if (!IsTargetValid())
            {
                CancelCollection();
            }
            // Check if player moved too far from target
            else if (currentTarget != null && GetDistanceToTarget(currentTarget) > collectionRange * 1.2f)
            {
                CancelCollection();
            }
        }
    }

    /// <summary>
    /// Find the nearest corpse within collection range.
    /// </summary>
    public HumanEnemy FindNearestCorpse()
    {
        HumanEnemy[] allEnemies = FindObjectsByType<HumanEnemy>(FindObjectsSortMode.None);
        HumanEnemy nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var enemy in allEnemies)
        {
            // Check if it's a corpse that can be collected
            if (!enemy.IsCorpse) continue;

            float dist = GetDistanceToTarget(enemy);
            if (dist <= collectionRange && dist < nearestDist)
            {
                nearest = enemy;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Start the collection process on a target corpse.
    /// </summary>
    public void StartCollection(HumanEnemy target)
    {
        if (target == null || !target.IsCorpse) return;
        if (isCollecting) return;

        currentTarget = target;
        isCollecting = true;
        collectionProgress = 0f;

        // Notify target that collection started
        target.OnDrainStarted();

        // Play start sound
        if (collectionStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionStartSound);
        }

        // Start loop sound
        if (collectionLoopSound != null && audioSource != null)
        {
            audioSource.clip = collectionLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Fire events
        OnCollectionStarted?.Invoke(target);
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnSkullCollectionStarted(target);
        }

        // Start coroutine
        collectionCoroutine = StartCoroutine(CollectionCoroutine());

        Debug.Log($"[SkullCollector] Started collecting from {target.Config?.displayName ?? "enemy"}");
    }

    /// <summary>
    /// Main collection coroutine - handles progress over time.
    /// </summary>
    private IEnumerator CollectionCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < collectionDuration)
        {
            elapsedTime += Time.deltaTime;
            collectionProgress = Mathf.Clamp01(elapsedTime / collectionDuration);

            // Fire progress events
            OnCollectionProgress?.Invoke(collectionProgress);
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnSkullCollectionProgress(collectionProgress);
            }

            yield return null;
        }

        // Collection complete
        CompleteCollection();
    }

    /// <summary>
    /// Complete the collection process - add skull and destroy corpse.
    /// </summary>
    private void CompleteCollection()
    {
        if (currentTarget == null) return;

        // Stop loop sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // Play complete sound
        if (collectionCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionCompleteSound);
        }

        // Add skull to player's resources
        int skullsCollected = 1; // Always 1 skull per enemy
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddSkull(skullsCollected);
        }

        // Notify target that it was collected
        currentTarget.OnDrained();

        // Fire events
        OnCollectionCompleted?.Invoke(skullsCollected);
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnSkullCollectionCompleted(skullsCollected);
        }

        Debug.Log($"[SkullCollector] Completed! Collected {skullsCollected} skull(s)");

        // Reset state
        ResetCollectionState();
    }

    /// <summary>
    /// Cancel the current collection process.
    /// </summary>
    public void CancelCollection()
    {
        if (!isCollecting) return;

        // Stop coroutine
        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
            collectionCoroutine = null;
        }

        // Stop loop sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // Notify target that collection was canceled
        if (currentTarget != null)
        {
            currentTarget.OnDrainCanceled();
        }

        // Fire events
        OnCollectionCanceled?.Invoke();
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnSkullCollectionCanceled();
        }

        Debug.Log("[SkullCollector] Collection canceled");

        // Reset state
        ResetCollectionState();
    }

    /// <summary>
    /// Reset the collection state.
    /// </summary>
    private void ResetCollectionState()
    {
        isCollecting = false;
        collectionProgress = 0f;
        currentTarget = null;
        collectionCoroutine = null;
    }

    /// <summary>
    /// Check if the current target is still valid.
    /// </summary>
    private bool IsTargetValid()
    {
        return currentTarget != null && currentTarget.IsCorpse;
    }

    /// <summary>
    /// Get distance to a target enemy.
    /// </summary>
    private float GetDistanceToTarget(HumanEnemy target)
    {
        return Vector2.Distance(transform.position, target.transform.position);
    }

    /// <summary>
    /// Check if there's a corpse in range (for UI hints).
    /// </summary>
    public bool HasCorpseInRange()
    {
        return FindNearestCorpse() != null;
    }

    /// <summary>
    /// Get info about nearest corpse for UI.
    /// </summary>
    public (HumanEnemy corpse, float distance) GetNearestCorpseInfo()
    {
        HumanEnemy nearest = FindNearestCorpse();
        if (nearest == null)
        {
            return (null, float.MaxValue);
        }

        float dist = GetDistanceToTarget(nearest);
        return (nearest, dist);
    }

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showCollectionRange) return;

        // Draw collection range
        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, collectionRange);

        // Draw filled sphere for visibility
        Gizmos.color = new Color(rangeGizmoColor.r, rangeGizmoColor.g, rangeGizmoColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, collectionRange);
    }

    #endregion
}
