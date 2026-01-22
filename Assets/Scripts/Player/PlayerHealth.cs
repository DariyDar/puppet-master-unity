using UnityEngine;

/// <summary>
/// Handles player health, damage, and death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;

    private int currentHealth;
    private float lastDamageTime;
    private bool isDead;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Notify UI
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // Check invincibility
        if (Time.time - lastDamageTime < invincibilityDuration)
            return;

        lastDamageTime = Time.time;
        currentHealth -= amount;

        // Visual feedback
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        // Notify UI
        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        NotifyHealthChanged();
    }

    private void Die()
    {
        isDead = true;

        // Notify event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerDied();
        }

        // Disable player controls
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Debug.Log("[PlayerHealth] Player died!");
    }

    private void NotifyHealthChanged()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerHealthChanged(currentHealth, maxHealth);
        }

        // Also update GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetHealth(currentHealth, maxHealth);
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
    }

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => (float)currentHealth / maxHealth;
}
