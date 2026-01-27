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
    [SerializeField] private float detectionRadius = 9f;        // Doubled from 4.5f
    [SerializeField] private float attractionRadius = 4.5f;     // Doubled from 2.25f
    [SerializeField] private float attractionSpeed = 40f;       // 5x faster (was 8f)
    // NOTE: pickupRadius removed - ResourcePickup handles actual pickup

    private List<ResourcePickup> nearbyResources = new List<ResourcePickup>();

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
    /// ResourceMagnet ONLY moves resources closer - ResourcePickup.Pickup() handles the actual collection.
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

            // Within attraction radius - move towards player
            // ResourcePickup.Update() will handle the actual pickup when close enough
            if (dist <= attractionRadius)
            {
                Vector3 direction = (transform.position - pickup.transform.position).normalized;
                float speed = attractionSpeed * (1f - dist / attractionRadius); // Faster when closer
                pickup.transform.position += direction * speed * Time.deltaTime;
            }
        }
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
    }
}
