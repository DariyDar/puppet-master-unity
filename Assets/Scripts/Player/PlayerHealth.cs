using UnityEngine;

/// <summary>
/// Handles player health, damage, and death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Defense (from UpgradeSystem)")]
    [SerializeField] private float armor = 0f;           // Damage reduction %
    [SerializeField] private float regenRate = 0f;       // HP per second

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;

    private int currentHealth;
    private float lastDamageTime;
    private bool isDead;
    private Color originalColor;
    private float regenAccumulator = 0f;

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

        // Apply armor reduction
        float damageReduction = armor / 100f;
        int finalDamage = Mathf.RoundToInt(amount * (1f - damageReduction));
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage

        currentHealth -= finalDamage;

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

    private void Update()
    {
        // HP Regeneration
        if (!isDead && regenRate > 0 && currentHealth < maxHealth)
        {
            regenAccumulator += regenRate * Time.deltaTime;
            if (regenAccumulator >= 1f)
            {
                int healAmount = Mathf.FloorToInt(regenAccumulator);
                regenAccumulator -= healAmount;
                Heal(healAmount);
            }
        }
    }

    #region UpgradeSystem Methods

    /// <summary>
    /// Set max health (from UpgradeSystem).
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        int oldMax = maxHealth;
        maxHealth = newMaxHealth;

        // Scale current health proportionally
        if (oldMax > 0)
        {
            float ratio = (float)currentHealth / oldMax;
            currentHealth = Mathf.RoundToInt(ratio * maxHealth);
        }
        else
        {
            currentHealth = maxHealth;
        }

        NotifyHealthChanged();
        Debug.Log($"[PlayerHealth] Max health set to {maxHealth}");
    }

    /// <summary>
    /// Set armor value (damage reduction %).
    /// </summary>
    public void SetArmor(float armorValue)
    {
        armor = Mathf.Clamp(armorValue, 0f, 90f); // Cap at 90%
        Debug.Log($"[PlayerHealth] Armor set to {armor}%");
    }

    /// <summary>
    /// Set HP regeneration rate (HP per second).
    /// </summary>
    public void SetRegenRate(float hpPerSecond)
    {
        regenRate = Mathf.Max(0f, hpPerSecond);
        Debug.Log($"[PlayerHealth] Regen rate set to {regenRate} HP/sec");
    }

    #endregion

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => (float)currentHealth / maxHealth;
    public float Armor => armor;
    public float RegenRate => regenRate;
}
