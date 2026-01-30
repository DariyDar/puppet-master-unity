using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Upgrades popup and button interaction.
/// Attach to the UpgradesButton to link it with the popup.
/// </summary>
public class UpgradesUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradesPopup;
    [SerializeField] private Button upgradesButton;
    [SerializeField] private Button closeButton;

    private bool isPopupOpen = false;

    private void Start()
    {
        // Auto-find popup if not set - search in Canvas hierarchy (works for inactive objects too)
        if (upgradesPopup == null)
        {
            // First try active objects
            upgradesPopup = GameObject.Find("UpgradesPopup");

            // If not found, search in Canvas (includes inactive)
            if (upgradesPopup == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform popup = canvas.transform.Find("UpgradesPopup");
                    if (popup != null)
                        upgradesPopup = popup.gameObject;
                }
            }
        }

        // Auto-find button on this object
        if (upgradesButton == null)
        {
            upgradesButton = GetComponent<Button>();
        }

        // Setup button click
        if (upgradesButton != null)
        {
            upgradesButton.onClick.AddListener(TogglePopup);
        }

        // Find close button in popup
        if (upgradesPopup != null && closeButton == null)
        {
            Transform closeBtnTransform = upgradesPopup.transform.Find("CloseButton");
            if (closeBtnTransform != null)
            {
                closeButton = closeBtnTransform.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(ClosePopup);
                }
            }
        }

        // Hide popup at start
        if (upgradesPopup != null)
        {
            upgradesPopup.SetActive(false);
        }
    }

    public void TogglePopup()
    {
        if (isPopupOpen)
        {
            ClosePopup();
        }
        else
        {
            OpenPopup();
        }
    }

    public void OpenPopup()
    {
        if (upgradesPopup != null)
        {
            upgradesPopup.SetActive(true);
            isPopupOpen = true;
            Debug.Log("[UpgradesUIController] Popup opened");
        }
        else
        {
            Debug.LogWarning("[UpgradesUIController] UpgradesPopup not found!");
        }
    }

    public void ClosePopup()
    {
        if (upgradesPopup != null)
        {
            upgradesPopup.SetActive(false);
            isPopupOpen = false;
            Debug.Log("[UpgradesUIController] Popup closed");
        }
    }

    /// <summary>
    /// Set the popup reference (for editor scripts).
    /// </summary>
    public void SetPopupReference(GameObject popup)
    {
        upgradesPopup = popup;
    }

    public bool IsPopupOpen => isPopupOpen;
}
