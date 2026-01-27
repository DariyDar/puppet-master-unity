using UnityEngine;

/// <summary>
/// Skull Collector system - automatically collects skull pickups from nearby dead enemies.
/// Spider must stand STILL near a skull for a duration to collect it.
/// During collection, the skull plays a pulsing reverse animation.
/// Only one skull can be collected at a time.
/// If spider moves, collection is canceled.
/// Attach to player GameObject.
/// </summary>
public class SkullCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float collectionRange = 4.5f;
    [SerializeField] private bool autoCollect = true;
    [SerializeField] private float movementThreshold = 0.05f; // Max velocity to count as "standing still"

    [Header("Visual Feedback")]
    [SerializeField] private bool showCollectionRange = true;
    [SerializeField] private Color rangeGizmoColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioClip collectionStartSound;
    [SerializeField] private AudioClip collectionCompleteSound;

    // State
    private bool isCollecting;
    private SkullPickup currentTarget;
    private AudioSource audioSource;
    private Rigidbody2D playerRb;

    // Events
    public event System.Action<SkullPickup> OnCollectionStarted;
    public event System.Action<float> OnCollectionProgress;
    public event System.Action<int> OnCollectionCompleted;
    public event System.Action OnCollectionCanceled;

    // Properties
    public bool IsCollecting => isCollecting;
    public float CollectionProgress => currentTarget != null ? currentTarget.CollectionProgress : 0f;
    public float CollectionRange => collectionRange;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        playerRb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!autoCollect) return;

        // Check if player is moving
        bool isMoving = IsPlayerMoving();

        if (!isCollecting)
        {
            // Only start collection if standing still
            if (!isMoving)
            {
                SkullPickup nearest = FindNearestSkull();
                if (nearest != null)
                {
                    StartCollection(nearest);
                }
            }
        }
        else
        {
            // Cancel if player starts moving
            if (isMoving)
            {
                CancelCollection();
                return;
            }

            // Check if target destroyed or collected
            if (currentTarget == null || currentTarget.IsCollected)
            {
                OnComplete();
                return;
            }

            // Check if player moved too far
            float dist = Vector2.Distance(transform.position, currentTarget.transform.position);
            if (dist > collectionRange * 1.3f)
            {
                CancelCollection();
                return;
            }

            // Fire progress event
            OnCollectionProgress?.Invoke(currentTarget.CollectionProgress);
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnSkullCollectionProgress(currentTarget.CollectionProgress);
            }
        }
    }

    /// <summary>
    /// Check if the player is currently moving.
    /// </summary>
    private bool IsPlayerMoving()
    {
        if (playerRb != null)
        {
            return playerRb.linearVelocity.magnitude > movementThreshold;
        }
        return false;
    }

    /// <summary>
    /// Find the nearest uncollected skull within range.
    /// </summary>
    public SkullPickup FindNearestSkull()
    {
        SkullPickup[] allSkulls = FindObjectsByType<SkullPickup>(FindObjectsSortMode.None);
        SkullPickup nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var skull in allSkulls)
        {
            if (skull.IsBeingCollected || skull.IsCollected) continue;

            float dist = Vector2.Distance(transform.position, skull.transform.position);
            if (dist <= collectionRange && dist < nearestDist)
            {
                nearest = skull;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Start collecting a skull pickup. Only one at a time.
    /// </summary>
    public void StartCollection(SkullPickup skull)
    {
        if (skull == null || isCollecting) return;

        currentTarget = skull;
        isCollecting = true;

        // Tell the skull to start its collection animation
        skull.StartCollection(transform);

        // Play start sound
        if (collectionStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionStartSound);
        }

        OnCollectionStarted?.Invoke(skull);

        Debug.Log("[SkullCollector] Started collecting skull (standing still)");
    }

    private void OnComplete()
    {
        if (collectionCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionCompleteSound);
        }

        OnCollectionCompleted?.Invoke(1);

        Debug.Log("[SkullCollector] Skull collected!");
        ResetState();
    }

    /// <summary>
    /// Cancel the current collection.
    /// </summary>
    public void CancelCollection()
    {
        if (!isCollecting) return;

        if (currentTarget != null && !currentTarget.IsCollected)
        {
            currentTarget.CancelCollection();
        }

        OnCollectionCanceled?.Invoke();
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnSkullCollectionCanceled();
        }

        Debug.Log("[SkullCollector] Collection canceled (player moved)");
        ResetState();
    }

    private void ResetState()
    {
        isCollecting = false;
        currentTarget = null;
    }

    /// <summary>
    /// Check if there's a skull in range (for UI hints).
    /// </summary>
    public bool HasSkullInRange()
    {
        return FindNearestSkull() != null;
    }

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showCollectionRange) return;

        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, collectionRange);

        Gizmos.color = new Color(rangeGizmoColor.r, rangeGizmoColor.g, rangeGizmoColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, collectionRange);
    }

    #endregion
}
