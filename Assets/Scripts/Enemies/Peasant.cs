using UnityEngine;
using System.Collections;

/// <summary>
/// Peasant enemy that runs to grab gold and flees.
/// Player must catch and kill them to get the gold.
/// When killed, drops the gold they're carrying.
/// </summary>
public class Peasant : EnemyBase
{
    public enum PeasantState
    {
        RunningToGold,
        GrabbingGold,
        Fleeing
    }

    [Header("Peasant Settings")]
    [SerializeField] private float grabRadius = 0.5f;
    [SerializeField] private float grabTime = 0.5f;
    [SerializeField] private float fleeSpeed = 4f;
    [SerializeField] private int goldCarried = 0;
    [SerializeField] private int maxGoldCarry = 3;

    [Header("Visual")]
    [SerializeField] private GameObject goldCarryIndicator;
    [SerializeField] private Color fleeingTint = new Color(0.8f, 0.8f, 1f);

    private PeasantState currentState = PeasantState.RunningToGold;
    private Vector3 goldTargetPosition;
    private bool hasGoldTarget = false;
    private float grabTimer = 0f;
    private Vector3 fleeDirection;

    protected override void Start()
    {
        base.Start();
        currentState = PeasantState.RunningToGold;
    }

    protected override void Update()
    {
        if (isDead) return;
        if (IsStunned) return;

        switch (currentState)
        {
            case PeasantState.RunningToGold:
                UpdateRunningToGold();
                break;

            case PeasantState.GrabbingGold:
                UpdateGrabbingGold();
                break;

            case PeasantState.Fleeing:
                // Fleeing handled in FixedUpdate
                break;
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || IsStunned) return;

        switch (currentState)
        {
            case PeasantState.RunningToGold:
                MoveTowardsGold();
                break;

            case PeasantState.Fleeing:
                Flee();
                break;
        }

        UpdateMoveAnimation();
    }

    private void UpdateRunningToGold()
    {
        if (!hasGoldTarget)
        {
            // No gold target - look for nearby gold
            FindNearestGold();

            if (!hasGoldTarget)
            {
                // No gold found - start fleeing
                StartFleeing();
                return;
            }
        }

        // Check if reached gold
        float dist = Vector2.Distance(transform.position, goldTargetPosition);
        if (dist <= grabRadius)
        {
            StartGrabbingGold();
        }
    }

    private void UpdateGrabbingGold()
    {
        grabTimer += Time.deltaTime;

        if (grabTimer >= grabTime)
        {
            // Try to grab gold
            GrabNearbyGold();

            // Check if carrying max gold or no more gold nearby
            if (goldCarried >= maxGoldCarry || !FindNearestGold())
            {
                StartFleeing();
            }
            else
            {
                // Keep grabbing
                currentState = PeasantState.RunningToGold;
            }
        }
    }

    private void MoveTowardsGold()
    {
        if (!hasGoldTarget || rb == null) return;

        Vector2 direction = ((Vector2)goldTargetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Face movement direction
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    private void StartGrabbingGold()
    {
        currentState = PeasantState.GrabbingGold;
        grabTimer = 0f;

        // Stop moving
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Play grab animation
        if (animator != null)
        {
            animator.SetBool(AnimIsMoving, false);
        }

        Debug.Log("[Peasant] Grabbing gold...");
    }

    private void GrabNearbyGold()
    {
        // Find gold pickups in grab radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, grabRadius);

        foreach (var col in colliders)
        {
            ResourcePickup pickup = col.GetComponent<ResourcePickup>();
            if (pickup != null && pickup.Type == ResourcePickup.ResourceType.Gold)
            {
                goldCarried += pickup.Amount;
                Destroy(pickup.gameObject);

                Debug.Log($"[Peasant] Grabbed gold! Now carrying: {goldCarried}");

                // Update visual indicator
                UpdateGoldIndicator();

                if (goldCarried >= maxGoldCarry)
                {
                    break;
                }
            }
        }
    }

    private bool FindNearestGold()
    {
        ResourcePickup[] allGold = Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        Vector3? closestPos = null;

        foreach (var gold in allGold)
        {
            if (gold.Type != ResourcePickup.ResourceType.Gold) continue;

            float dist = Vector2.Distance(transform.position, gold.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPos = gold.transform.position;
            }
        }

        if (closestPos.HasValue)
        {
            goldTargetPosition = closestPos.Value;
            hasGoldTarget = true;
            return true;
        }

        hasGoldTarget = false;
        return false;
    }

    private void StartFleeing()
    {
        currentState = PeasantState.Fleeing;

        // Calculate flee direction (away from player)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            fleeDirection = (transform.position - player.transform.position).normalized;
        }
        else
        {
            // Random flee direction
            fleeDirection = Random.insideUnitCircle.normalized;
        }

        // Visual feedback
        if (spriteRenderer != null)
        {
            spriteRenderer.color = fleeingTint;
        }

        Debug.Log($"[Peasant] Fleeing with {goldCarried} gold!");

        // Destroy after some time if not caught
        StartCoroutine(DestroyAfterFlee());
    }

    private void Flee()
    {
        if (rb == null) return;

        rb.linearVelocity = (Vector2)fleeDirection * fleeSpeed;

        // Face movement direction
        if (spriteRenderer != null && fleeDirection.x != 0)
        {
            spriteRenderer.flipX = fleeDirection.x < 0;
        }
    }

    private IEnumerator DestroyAfterFlee()
    {
        yield return new WaitForSeconds(10f);

        if (!isDead)
        {
            // Escaped with the gold!
            Debug.Log($"[Peasant] Escaped with {goldCarried} gold!");
            Destroy(gameObject);
        }
    }

    private void UpdateGoldIndicator()
    {
        if (goldCarryIndicator != null)
        {
            goldCarryIndicator.SetActive(goldCarried > 0);
            // Could scale based on amount carried
            goldCarryIndicator.transform.localScale = Vector3.one * (0.5f + goldCarried * 0.2f);
        }
    }

    private void UpdateMoveAnimation()
    {
        if (animator != null && rb != null)
        {
            animator.SetBool(AnimIsMoving, rb.linearVelocity.magnitude > 0.1f);
        }
    }

    /// <summary>
    /// Set the position where gold is located.
    /// </summary>
    public void SetGoldTarget(Vector3 position)
    {
        goldTargetPosition = position;
        hasGoldTarget = true;
    }

    protected override void Die()
    {
        isDead = true;

        // Drop all carried gold
        if (goldCarried > 0)
        {
            DropCarriedGold();
        }

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger(AnimDie);
        }

        // Spawn skull (peasant is a human - 1 skull per enemy)
        if (ResourceSpawner.Instance != null)
        {
            ResourceSpawner.Instance.SpawnSkull(transform.position);
        }

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEnemyDied(gameObject);
        }

        Debug.Log($"[Peasant] Killed! Dropped {goldCarried} gold");

        Destroy(gameObject, 1f);
    }

    private void DropCarriedGold()
    {
        if (ResourceSpawner.Instance != null)
        {
            for (int i = 0; i < goldCarried; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
                ResourceSpawner.Instance.SpawnGold(spawnPos, 1);
            }
        }
    }

    // Peasants don't attack
    protected override void PerformAttack() { }
    public override void DealDamage() { }

    // Properties
    public int GoldCarried => goldCarried;
    public PeasantState State => currentState;

    private void OnDrawGizmosSelected()
    {
        // Grab radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRadius);

        // Gold target
        if (hasGoldTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, goldTargetPosition);
            Gizmos.DrawWireSphere(goldTargetPosition, 0.3f);
        }

        // Flee direction
        if (currentState == PeasantState.Fleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 2f);
        }
    }
}
