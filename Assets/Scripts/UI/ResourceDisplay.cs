using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Component for displaying a single resource with icon and animated amount.
/// Tiny Swords style UI with delta animations (+10, -5, etc.)
/// </summary>
public class ResourceDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;

    [Header("Delta Animation")]
    [SerializeField] private TextMeshProUGUI deltaText;
    [SerializeField] private float deltaDisplayDuration = 1f;
    [SerializeField] private float deltaFloatSpeed = 50f;
    [SerializeField] private Color positiveColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color negativeColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Animation")]
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private float bounceDuration = 0.15f;

    [Header("Resource Type")]
    [SerializeField] private ResourcePickup.ResourceType resourceType;

    // State
    private int currentAmount;
    private int displayedAmount;
    private Coroutine deltaCoroutine;
    private Coroutine countCoroutine;
    private Vector3 originalScale;
    private Vector3 deltaOriginalPosition;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (deltaText != null)
        {
            deltaOriginalPosition = deltaText.transform.localPosition;
            deltaText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Update the displayed amount
    /// </summary>
    public void UpdateAmount(int amount)
    {
        int previousAmount = currentAmount;
        currentAmount = amount;

        // Animate count change
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
        }
        countCoroutine = StartCoroutine(AnimateCount(displayedAmount, amount));

        // Bounce effect on change
        if (previousAmount != amount)
        {
            StartCoroutine(BounceEffect());
        }
    }

    /// <summary>
    /// Animate delta display (+10, -5, etc.)
    /// </summary>
    public void AnimateDelta(int delta)
    {
        if (deltaText == null) return;

        // Stop existing delta animation
        if (deltaCoroutine != null)
        {
            StopCoroutine(deltaCoroutine);
        }

        deltaCoroutine = StartCoroutine(ShowDelta(delta));
    }

    /// <summary>
    /// Set the resource icon
    /// </summary>
    public void SetIcon(Sprite sprite)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
        }
    }

    /// <summary>
    /// Set the resource type
    /// </summary>
    public void SetResourceType(ResourcePickup.ResourceType type)
    {
        resourceType = type;
    }

    /// <summary>
    /// Get the resource type
    /// </summary>
    public ResourcePickup.ResourceType ResourceType => resourceType;

    /// <summary>
    /// Get current amount
    /// </summary>
    public int Amount => currentAmount;

    #region Animations

    private IEnumerator AnimateCount(int from, int to)
    {
        if (amountText == null) yield break;

        float elapsed = 0f;
        float duration = 0.3f;
        int difference = Mathf.Abs(to - from);

        // Quick animation for small changes, slower for large
        if (difference > 100)
        {
            duration = 0.5f;
        }
        else if (difference < 10)
        {
            duration = 0.15f;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Ease out
            t = 1f - Mathf.Pow(1f - t, 3f);

            displayedAmount = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            amountText.text = FormatAmount(displayedAmount);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        displayedAmount = to;
        amountText.text = FormatAmount(to);
    }

    private IEnumerator ShowDelta(int delta)
    {
        if (deltaText == null) yield break;

        // Setup delta text
        deltaText.gameObject.SetActive(true);
        deltaText.transform.localPosition = deltaOriginalPosition;

        string prefix = delta >= 0 ? "+" : "";
        deltaText.text = $"{prefix}{delta}";
        deltaText.color = delta >= 0 ? positiveColor : negativeColor;

        // Float up and fade
        float elapsed = 0f;
        Color startColor = deltaText.color;

        while (elapsed < deltaDisplayDuration)
        {
            float t = elapsed / deltaDisplayDuration;

            // Float up
            Vector3 pos = deltaOriginalPosition + Vector3.up * (deltaFloatSpeed * t);
            deltaText.transform.localPosition = pos;

            // Fade out in second half
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) * 2f;
                deltaText.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    Mathf.Lerp(1f, 0f, fadeT)
                );
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        deltaText.gameObject.SetActive(false);
        deltaText.transform.localPosition = deltaOriginalPosition;
    }

    private IEnumerator BounceEffect()
    {
        float elapsed = 0f;
        float halfDuration = bounceDuration * 0.5f;

        // Scale up
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * bounceScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale * bounceScale, originalScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    #endregion

    #region Formatting

    /// <summary>
    /// Format large numbers (1000 -> 1K, 1000000 -> 1M)
    /// </summary>
    private string FormatAmount(int amount)
    {
        if (amount >= 1000000)
        {
            return $"{amount / 1000000f:F1}M";
        }
        else if (amount >= 10000)
        {
            return $"{amount / 1000f:F1}K";
        }
        else if (amount >= 1000)
        {
            return $"{amount / 1000f:F1}K";
        }
        return amount.ToString();
    }

    #endregion

    #region Editor

    private void OnValidate()
    {
        // Update text in editor
        if (amountText != null && !Application.isPlaying)
        {
            amountText.text = FormatAmount(currentAmount);
        }
    }

    #endregion
}
