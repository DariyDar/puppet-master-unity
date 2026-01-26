using UnityEngine;

/// <summary>
/// Base class for all buildings. Handles interaction detection and visual feedback.
/// </summary>
public abstract class BuildingBase : MonoBehaviour
{
    [Header("Building Info")]
    [SerializeField] protected string buildingName = "Building";
    [SerializeField] protected Sprite buildingIcon;

    [Header("Interaction")]
    [SerializeField] protected float interactionRange = 2f;
    [SerializeField] protected bool requiresPlayerInRange = true;

    [Header("Visual")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected SpriteRenderer highlightRenderer;
    [SerializeField] protected Color highlightColor = new Color(1f, 1f, 0.5f, 0.3f);

    protected Transform player;
    protected bool isPlayerInRange;
    protected bool isHighlighted;

    protected virtual void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        FindPlayer();
        CreateHighlight();
    }

    protected virtual void Update()
    {
        FindPlayer();
        CheckPlayerDistance();
        UpdateHighlight();
    }

    protected void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    protected void CheckPlayerDistance()
    {
        if (player == null)
        {
            isPlayerInRange = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= interactionRange;

        // Trigger events on enter/exit
        if (isPlayerInRange && !wasInRange)
        {
            OnPlayerEnterRange();
        }
        else if (!isPlayerInRange && wasInRange)
        {
            OnPlayerExitRange();
        }
    }

    protected void CreateHighlight()
    {
        if (highlightRenderer != null) return;

        // Create highlight child object
        GameObject highlightObj = new GameObject("Highlight");
        highlightObj.transform.SetParent(transform);
        highlightObj.transform.localPosition = Vector3.zero;
        highlightObj.transform.localScale = Vector3.one * 1.1f;

        highlightRenderer = highlightObj.AddComponent<SpriteRenderer>();
        highlightRenderer.sprite = spriteRenderer?.sprite;
        highlightRenderer.color = highlightColor;
        highlightRenderer.sortingOrder = (spriteRenderer?.sortingOrder ?? 0) - 1;
        highlightRenderer.enabled = false;
    }

    protected void UpdateHighlight()
    {
        if (highlightRenderer == null) return;

        bool shouldHighlight = isPlayerInRange && CanInteract();

        if (shouldHighlight != isHighlighted)
        {
            isHighlighted = shouldHighlight;
            highlightRenderer.enabled = isHighlighted;

            // Pulse animation could be added here
            if (isHighlighted)
            {
                highlightRenderer.sprite = spriteRenderer?.sprite;
            }
        }
    }

    /// <summary>
    /// Called when player enters interaction range
    /// </summary>
    protected virtual void OnPlayerEnterRange()
    {
        Debug.Log($"[{buildingName}] Player entered range");
    }

    /// <summary>
    /// Called when player exits interaction range
    /// </summary>
    protected virtual void OnPlayerExitRange()
    {
        Debug.Log($"[{buildingName}] Player left range");
    }

    /// <summary>
    /// Check if building can be interacted with
    /// </summary>
    public virtual bool CanInteract()
    {
        if (requiresPlayerInRange && !isPlayerInRange)
            return false;

        return true;
    }

    /// <summary>
    /// Interact with the building (called when player clicks/touches)
    /// </summary>
    public abstract void Interact();

    /// <summary>
    /// Auto-interact when player is in range (for buildings like Storage)
    /// </summary>
    public virtual void AutoInteract()
    {
        if (CanInteract())
        {
            Interact();
        }
    }

    // Properties
    public string BuildingName => buildingName;
    public bool IsPlayerInRange => isPlayerInRange;
    public float InteractionRange => interactionRange;

    protected virtual void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
