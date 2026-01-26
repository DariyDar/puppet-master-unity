using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A bush that hides units inside it.
/// Units within the bush trigger become invisible to enemies.
/// </summary>
public class Bush : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrder = 5;
    [SerializeField] private float hiddenAlpha = 0.7f; // Alpha when something is hidden inside

    [Header("Hide Settings")]
    [SerializeField] private string[] hideableTags = { "Unit", "Player" };
    [SerializeField] private LayerMask hideableLayer;

    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;

    // Track hidden objects
    private HashSet<GameObject> hiddenObjects = new HashSet<GameObject>();
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        // Ensure collider is trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldHide(other.gameObject))
        {
            HideObject(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (hiddenObjects.Contains(other.gameObject))
        {
            UnhideObject(other.gameObject);
        }
    }

    /// <summary>
    /// Check if an object should be hidden by this bush.
    /// </summary>
    private bool ShouldHide(GameObject obj)
    {
        // Check by tag
        foreach (string tag in hideableTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }

        // Check by layer
        if (hideableLayer != 0)
        {
            if (((1 << obj.layer) & hideableLayer) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Hide an object inside the bush.
    /// </summary>
    private void HideObject(GameObject obj)
    {
        if (hiddenObjects.Contains(obj)) return;

        hiddenObjects.Add(obj);

        // Mark as hidden (add component or set flag)
        HiddenInBush hidden = obj.GetComponent<HiddenInBush>();
        if (hidden == null)
        {
            hidden = obj.AddComponent<HiddenInBush>();
        }
        hidden.AddBush(this);

        // Update bush visual
        UpdateBushVisual();

        Debug.Log($"[Bush] {obj.name} hidden in bush");
    }

    /// <summary>
    /// Unhide an object from the bush.
    /// </summary>
    private void UnhideObject(GameObject obj)
    {
        if (!hiddenObjects.Contains(obj)) return;

        hiddenObjects.Remove(obj);

        // Remove hidden marker if no longer in any bush
        HiddenInBush hidden = obj.GetComponent<HiddenInBush>();
        if (hidden != null)
        {
            hidden.RemoveBush(this);
        }

        // Update bush visual
        UpdateBushVisual();

        Debug.Log($"[Bush] {obj.name} left bush");
    }

    /// <summary>
    /// Update bush visual based on whether something is hidden.
    /// </summary>
    private void UpdateBushVisual()
    {
        if (spriteRenderer == null) return;

        if (hiddenObjects.Count > 0)
        {
            // Something is hidden - slightly transparent
            Color c = originalColor;
            c.a = hiddenAlpha;
            spriteRenderer.color = c;
        }
        else
        {
            // Nothing hidden - restore original
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Get all hidden objects.
    /// </summary>
    public IReadOnlyCollection<GameObject> GetHiddenObjects()
    {
        return hiddenObjects;
    }

    /// <summary>
    /// Check if any objects are hidden in this bush.
    /// </summary>
    public bool HasHiddenObjects()
    {
        return hiddenObjects.Count > 0;
    }

    /// <summary>
    /// Force reveal all hidden objects (e.g., when bush is destroyed).
    /// </summary>
    public void RevealAll()
    {
        foreach (var obj in new List<GameObject>(hiddenObjects))
        {
            if (obj != null)
            {
                UnhideObject(obj);
            }
        }
        hiddenObjects.Clear();
    }

    private void OnDestroy()
    {
        // Reveal all objects when bush is destroyed
        RevealAll();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw trigger area
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(0f, 0.5f, 0f, 0.3f);
            Gizmos.DrawCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
}

/// <summary>
/// Component added to objects hidden in bushes.
/// Tracks which bushes are hiding this object.
/// </summary>
public class HiddenInBush : MonoBehaviour
{
    private HashSet<Bush> hidingBushes = new HashSet<Bush>();

    /// <summary>
    /// Add a bush that is hiding this object.
    /// </summary>
    public void AddBush(Bush bush)
    {
        hidingBushes.Add(bush);
    }

    /// <summary>
    /// Remove a bush that was hiding this object.
    /// </summary>
    public void RemoveBush(Bush bush)
    {
        hidingBushes.Remove(bush);

        // If no longer in any bush, remove this component
        if (hidingBushes.Count == 0)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Check if this object is currently hidden.
    /// </summary>
    public bool IsHidden => hidingBushes.Count > 0;

    /// <summary>
    /// Get the number of bushes hiding this object.
    /// </summary>
    public int BushCount => hidingBushes.Count;
}
