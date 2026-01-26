using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Resource magnet system - attracts nearby Meat, Wood, Gold to player.
/// Per GDD: Resources are collected by "magnetizing" (small attraction radius).
/// Skulls are collected separately via SkullCollector with progress bar.
/// </summary>
public class ResourceMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float attractionRadius = 1.5f;
    [SerializeField] private float attractionSpeed = 8f;
    [SerializeField] private float pickupRadius = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupVolume = 0.5f;

    private AudioSource audioSource;
    private List<ResourcePickup> nearbyResources = new List<ResourcePickup>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        FindNearbyResources();
        AttractResources();
    }

    /// <summary>
    /// Find all resource pickups within detection radius.
    /// </summary>
    private void FindNearbyResources()
    {
        nearbyResources.Clear();

        ResourcePickup[] allPickups = Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);

        foreach (var pickup in allPickups)
        {
            if (pickup == null) continue;

            // Skip skulls - they use SkullCollector
            if (pickup.Type == ResourcePickup.ResourceType.Skull) continue;

            float dist = Vector2.Distance(transform.position, pickup.transform.position);
            if (dist <= detectionRadius)
            {
                nearbyResources.Add(pickup);
            }
        }
    }

    /// <summary>
    /// Attract resources within attraction radius towards player.
    /// </summary>
    private void AttractResources()
    {
        for (int i = nearbyResources.Count - 1; i >= 0; i--)
        {
            ResourcePickup pickup = nearbyResources[i];
            if (pickup == null)
            {
                nearbyResources.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(transform.position, pickup.transform.position);

            // Within pickup radius - collect immediately
            if (dist <= pickupRadius)
            {
                CollectResource(pickup);
                nearbyResources.RemoveAt(i);
                continue;
            }

            // Within attraction radius - move towards player
            if (dist <= attractionRadius)
            {
                Vector3 direction = (transform.position - pickup.transform.position).normalized;
                float speed = attractionSpeed * (1f - dist / attractionRadius); // Faster when closer
                pickup.transform.position += direction * speed * Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// Collect a resource pickup.
    /// </summary>
    private void CollectResource(ResourcePickup pickup)
    {
        if (pickup == null) return;

        int amount = pickup.Amount;
        ResourcePickup.ResourceType type = pickup.Type;

        // Add to cargo or storage
        if (GameManager.Instance != null)
        {
            switch (type)
            {
                case ResourcePickup.ResourceType.Meat:
                    GameManager.Instance.AddCargoMeat(amount);
                    break;
                case ResourcePickup.ResourceType.Wood:
                    GameManager.Instance.AddCargoWood(amount);
                    break;
                case ResourcePickup.ResourceType.Gold:
                    GameManager.Instance.AddCargoGold(amount);
                    break;
            }
        }

        // Fire event
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnResourceCollected(type.ToString(), amount);
        }

        // Play sound
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupVolume);
        }

        Debug.Log($"[ResourceMagnet] Collected {amount} {type}");

        // Destroy pickup
        Destroy(pickup.gameObject);
    }

    /// <summary>
    /// Check if there are resources nearby (for UI hints).
    /// </summary>
    public bool HasResourcesNearby()
    {
        return nearbyResources.Count > 0;
    }

    /// <summary>
    /// Get count of nearby resources by type.
    /// </summary>
    public int GetNearbyResourceCount(ResourcePickup.ResourceType type)
    {
        int count = 0;
        foreach (var pickup in nearbyResources)
        {
            if (pickup != null && pickup.Type == type)
            {
                count++;
            }
        }
        return count;
    }

    // Properties
    public float DetectionRadius
    {
        get => detectionRadius;
        set => detectionRadius = value;
    }

    public float AttractionRadius
    {
        get => attractionRadius;
        set => attractionRadius = value;
    }

    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attraction radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attractionRadius);

        // Pickup radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
