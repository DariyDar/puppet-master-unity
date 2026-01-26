using UnityEngine;

/// <summary>
/// Farm building that generates resources over time.
/// Player can collect generated resources when in range.
/// </summary>
public class Farm : BuildingBase
{
    [Header("Farm Settings")]
    [SerializeField] private ResourcePickup.ResourceType resourceType = ResourcePickup.ResourceType.Meat;
    [SerializeField] private int generateAmount = 1;
    [SerializeField] private float generateInterval = 10f;
    [SerializeField] private int maxStorage = 10;

    [Header("State")]
    [SerializeField] private int currentStorage = 0;

    private float lastGenerateTime;

    protected override void Awake()
    {
        base.Awake();
        buildingName = "Farm";
    }

    protected override void Start()
    {
        base.Start();
        lastGenerateTime = Time.time;
    }

    protected override void Update()
    {
        base.Update();

        // Generate resources over time
        if (Time.time - lastGenerateTime >= generateInterval)
        {
            GenerateResource();
            lastGenerateTime = Time.time;
        }
    }

    protected override void OnPlayerEnterRange()
    {
        base.OnPlayerEnterRange();

        if (currentStorage > 0)
        {
            Debug.Log($"[Farm] Has {currentStorage} {resourceType} ready for collection");
        }
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        return currentStorage > 0;
    }

    public override void Interact()
    {
        CollectResources();
    }

    private void GenerateResource()
    {
        if (currentStorage >= maxStorage)
            return;

        currentStorage = Mathf.Min(currentStorage + generateAmount, maxStorage);
        Debug.Log($"[Farm] Generated {generateAmount} {resourceType}. Storage: {currentStorage}/{maxStorage}");
    }

    private void CollectResources()
    {
        if (currentStorage <= 0 || GameManager.Instance == null)
            return;

        string typeStr = resourceType.ToString().ToLower();
        int collected = 0;

        // Try to add to cargo
        for (int i = 0; i < currentStorage; i++)
        {
            if (GameManager.Instance.AddToCargo(typeStr, 1))
            {
                collected++;
            }
            else
            {
                // Cargo full
                break;
            }
        }

        currentStorage -= collected;

        if (collected > 0)
        {
            Debug.Log($"[Farm] Player collected {collected} {resourceType}");
            // Play collect effect
            StartCoroutine(ScalePulse());
        }
    }

    private System.Collections.IEnumerator ScalePulse()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.05f;
        float duration = 0.1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // Properties
    public int CurrentStorage => currentStorage;
    public int MaxStorage => maxStorage;
    public ResourcePickup.ResourceType ResourceType => resourceType;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Show storage status
        Gizmos.color = currentStorage > 0 ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.5f, 0.2f, 0.1f));
    }
}
