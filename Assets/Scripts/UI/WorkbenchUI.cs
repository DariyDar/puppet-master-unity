using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI panel for workbench crafting system.
/// Displays recipes, costs, and crafting progress.
/// Tiny Swords style UI.
/// </summary>
public class WorkbenchUI : MonoBehaviour
{
    [Header("Recipe List")]
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private GameObject recipeButtonPrefab;
    [SerializeField] private ScrollRect recipeScrollRect;

    [Header("Selected Recipe Info")]
    [SerializeField] private Image selectedRecipeIcon;
    [SerializeField] private TextMeshProUGUI selectedRecipeName;
    [SerializeField] private TextMeshProUGUI selectedRecipeDescription;

    [Header("Cost Display - Per GDD v5")]
    [SerializeField] private TextMeshProUGUI skullsCostText;
    [SerializeField] private TextMeshProUGUI meatCostText;
    [SerializeField] private TextMeshProUGUI woodCostText;
    [SerializeField] private TextMeshProUGUI goldCostText;

    [Header("Recipe Info")]
    [SerializeField] private TextMeshProUGUI recipeDescriptionText;
    [SerializeField] private TextMeshProUGUI unlockLevelText;

    [Header("Crafting")]
    [SerializeField] private Image craftProgressBar;
    [SerializeField] private TextMeshProUGUI craftingText;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftButtonText;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.4f);

    // State
    private Workbench currentWorkbench;
    private int selectedRecipeIndex = -1;
    private List<RecipeButton> recipeButtons = new List<RecipeButton>();
    private bool isCrafting;
    private Coroutine craftingCoroutine;

    // Input System
    private InputAction escapeAction;

    private void Awake()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseUI);
        }

        // Setup craft button
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        // Setup Input System
        escapeAction = new InputAction("Escape", InputActionType.Button);
        escapeAction.AddBinding("<Keyboard>/escape");
    }

    private void OnDestroy()
    {
        if (escapeAction != null)
        {
            escapeAction.Disable();
            escapeAction.Dispose();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StorageUpdated += OnStorageUpdated;
        }

        // Enable input
        if (escapeAction != null)
        {
            escapeAction.Enable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StorageUpdated -= OnStorageUpdated;
        }

        // Stop crafting coroutine
        if (craftingCoroutine != null)
        {
            StopCoroutine(craftingCoroutine);
            craftingCoroutine = null;
        }

        // Disable input
        if (escapeAction != null)
        {
            escapeAction.Disable();
        }
    }

    private void Update()
    {
        // Update crafting progress
        if (currentWorkbench != null && currentWorkbench.IsCrafting)
        {
            UpdateCraftProgress(currentWorkbench.CraftProgress / currentWorkbench.CurrentRecipe.craftTime);
        }

        // Close on Escape (using new Input System)
        if (escapeAction != null && escapeAction.WasPressedThisFrame())
        {
            CloseUI();
        }
    }

    #region Recipe Display

    /// <summary>
    /// Show recipes from a workbench
    /// </summary>
    public void ShowRecipes(Workbench workbench)
    {
        if (workbench == null) return;

        currentWorkbench = workbench;
        selectedRecipeIndex = -1;
        isCrafting = workbench.IsCrafting;

        // Clear existing buttons
        ClearRecipeButtons();

        // Create buttons for each recipe
        List<Workbench.Recipe> recipes = workbench.Recipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            CreateRecipeButton(recipes[i], i);
        }

        // Select first recipe by default
        if (recipes.Count > 0)
        {
            OnRecipeSelected(0);
        }

        // Update crafting state
        UpdateCraftingState();

        gameObject.SetActive(true);
    }

    private void CreateRecipeButton(Workbench.Recipe recipe, int index)
    {
        if (recipeContainer == null) return;

        GameObject buttonObj;

        if (recipeButtonPrefab != null)
        {
            buttonObj = Instantiate(recipeButtonPrefab, recipeContainer);
        }
        else
        {
            // Create basic button if no prefab
            buttonObj = new GameObject($"RecipeButton_{index}");
            buttonObj.transform.SetParent(recipeContainer, false);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button btn = buttonObj.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = recipe.name;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 50);
        }

        // Setup button component
        RecipeButton recipeBtn = buttonObj.GetComponent<RecipeButton>();
        if (recipeBtn == null)
        {
            recipeBtn = buttonObj.AddComponent<RecipeButton>();
        }

        recipeBtn.Setup(recipe, index, OnRecipeSelected);
        recipeButtons.Add(recipeBtn);

        // Update affordability
        bool canAfford = currentWorkbench != null && currentWorkbench.CanAfford(index);
        recipeBtn.SetAffordable(canAfford, affordableColor, unaffordableColor);
    }

    private void ClearRecipeButtons()
    {
        foreach (RecipeButton btn in recipeButtons)
        {
            if (btn != null && btn.gameObject != null)
            {
                Destroy(btn.gameObject);
            }
        }
        recipeButtons.Clear();
    }

    /// <summary>
    /// Handle recipe selection
    /// </summary>
    public void OnRecipeSelected(int index)
    {
        if (currentWorkbench == null) return;

        List<Workbench.Recipe> recipes = currentWorkbench.Recipes;
        if (index < 0 || index >= recipes.Count) return;

        selectedRecipeIndex = index;
        Workbench.Recipe recipe = recipes[index];

        // Update selection visual
        for (int i = 0; i < recipeButtons.Count; i++)
        {
            recipeButtons[i].SetSelected(i == index, selectedColor);
        }

        // Update info display
        if (selectedRecipeIcon != null && recipe.icon != null)
        {
            selectedRecipeIcon.sprite = recipe.icon;
            selectedRecipeIcon.gameObject.SetActive(true);
        }
        else if (selectedRecipeIcon != null)
        {
            selectedRecipeIcon.gameObject.SetActive(false);
        }

        if (selectedRecipeName != null)
        {
            selectedRecipeName.text = recipe.name;
        }

        // Update cost display
        UpdateCostDisplay(recipe);

        // Update craft button
        UpdateCraftButton();
    }

    private void UpdateCostDisplay(Workbench.Recipe recipe)
    {
        bool canAfford = currentWorkbench != null && currentWorkbench.CanAfford(recipe);
        bool isUnlocked = currentWorkbench != null && currentWorkbench.IsUnlocked(recipe);

        if (skullsCostText != null)
        {
            skullsCostText.text = recipe.skullsCost.ToString();
            skullsCostText.color = GameManager.Instance != null &&
                GameManager.Instance.Skulls >= recipe.skullsCost ? affordableColor : unaffordableColor;
            skullsCostText.transform.parent.gameObject.SetActive(recipe.skullsCost > 0);
        }

        if (meatCostText != null)
        {
            meatCostText.text = recipe.meatCost.ToString();
            meatCostText.color = GameManager.Instance != null &&
                GameManager.Instance.Meat >= recipe.meatCost ? affordableColor : unaffordableColor;
            meatCostText.transform.parent.gameObject.SetActive(recipe.meatCost > 0);
        }

        if (woodCostText != null)
        {
            woodCostText.text = recipe.woodCost.ToString();
            woodCostText.color = GameManager.Instance != null &&
                GameManager.Instance.Wood >= recipe.woodCost ? affordableColor : unaffordableColor;
            woodCostText.transform.parent.gameObject.SetActive(recipe.woodCost > 0);
        }

        if (goldCostText != null)
        {
            goldCostText.text = recipe.goldCost.ToString();
            goldCostText.color = GameManager.Instance != null &&
                GameManager.Instance.Gold >= recipe.goldCost ? affordableColor : unaffordableColor;
            goldCostText.transform.parent.gameObject.SetActive(recipe.goldCost > 0);
        }

        // Update description
        if (recipeDescriptionText != null)
        {
            recipeDescriptionText.text = recipe.description;
        }

        // Update unlock level requirement
        if (unlockLevelText != null)
        {
            if (!isUnlocked)
            {
                unlockLevelText.text = $"Requires Level {recipe.unlockLevel}";
                unlockLevelText.color = unaffordableColor;
                unlockLevelText.gameObject.SetActive(true);
            }
            else
            {
                unlockLevelText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region Crafting

    private void OnCraftButtonClicked()
    {
        if (selectedRecipeIndex < 0 || currentWorkbench == null) return;

        if (currentWorkbench.IsCrafting)
        {
            Debug.Log("[WorkbenchUI] Already crafting!");
            return;
        }

        // Start crafting
        bool success = currentWorkbench.StartCrafting(selectedRecipeIndex);

        if (success)
        {
            isCrafting = true;
            UpdateCraftingState();
            craftingCoroutine = StartCoroutine(CraftingAnimation());
        }
    }

    private IEnumerator CraftingAnimation()
    {
        while (currentWorkbench != null && currentWorkbench.IsCrafting)
        {
            float progress = currentWorkbench.CraftProgress / currentWorkbench.CurrentRecipe.craftTime;
            UpdateCraftProgress(progress);

            yield return null;
        }

        // Crafting complete
        isCrafting = false;
        UpdateCraftProgress(0f);
        UpdateCraftingState();

        // Refresh affordability
        RefreshAffordability();
    }

    /// <summary>
    /// Update crafting progress bar
    /// </summary>
    public void UpdateCraftProgress(float progress)
    {
        if (craftProgressBar != null)
        {
            craftProgressBar.fillAmount = Mathf.Clamp01(progress);
        }

        if (craftingText != null)
        {
            if (progress > 0 && progress < 1f)
            {
                craftingText.text = $"Crafting... {Mathf.RoundToInt(progress * 100)}%";
            }
            else if (progress >= 1f)
            {
                craftingText.text = "Complete!";
            }
            else
            {
                craftingText.text = "";
            }
        }
    }

    private void UpdateCraftingState()
    {
        Workbench.Recipe selectedRecipe = null;
        if (selectedRecipeIndex >= 0 && currentWorkbench != null && selectedRecipeIndex < currentWorkbench.Recipes.Count)
        {
            selectedRecipe = currentWorkbench.Recipes[selectedRecipeIndex];
        }

        // For unit recipes, check army limit
        bool armyFull = selectedRecipe != null &&
            selectedRecipe.recipeType == Workbench.RecipeType.Unit &&
            GameManager.Instance != null &&
            !GameManager.Instance.CanAddUnit();

        // Check if unlocked
        bool isUnlocked = currentWorkbench != null && currentWorkbench.IsUnlocked(selectedRecipeIndex);

        bool canCraft = selectedRecipeIndex >= 0 &&
            currentWorkbench != null &&
            !currentWorkbench.IsCrafting &&
            currentWorkbench.CanAfford(selectedRecipeIndex) &&
            isUnlocked &&
            !armyFull;

        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
        }

        if (craftButtonText != null)
        {
            if (currentWorkbench != null && currentWorkbench.IsCrafting)
            {
                craftButtonText.text = "Crafting...";
            }
            else if (!isUnlocked)
            {
                craftButtonText.text = "Locked";
            }
            else if (armyFull)
            {
                craftButtonText.text = "Army Full";
            }
            else if (!canCraft)
            {
                craftButtonText.text = "Not Enough";
            }
            else
            {
                // Different text based on recipe type
                if (selectedRecipe != null)
                {
                    craftButtonText.text = selectedRecipe.recipeType switch
                    {
                        Workbench.RecipeType.Unit => "Craft",
                        Workbench.RecipeType.Farm => "Build",
                        Workbench.RecipeType.Upgrade => "Purchase",
                        _ => "Craft"
                    };
                }
                else
                {
                    craftButtonText.text = "Craft";
                }
            }
        }
    }

    private void UpdateCraftButton()
    {
        UpdateCraftingState();
    }

    #endregion

    #region Event Handlers

    private void OnStorageUpdated(int skulls, int meat, int wood, int gold)
    {
        // Refresh affordability of all recipes
        RefreshAffordability();

        // Update selected recipe cost display
        if (selectedRecipeIndex >= 0 && currentWorkbench != null)
        {
            UpdateCostDisplay(currentWorkbench.Recipes[selectedRecipeIndex]);
            UpdateCraftButton();
        }
    }

    private void RefreshAffordability()
    {
        for (int i = 0; i < recipeButtons.Count; i++)
        {
            bool canAfford = currentWorkbench != null && currentWorkbench.CanAfford(i);
            recipeButtons[i].SetAffordable(canAfford, affordableColor, unaffordableColor);
        }
    }

    #endregion

    #region UI Control

    /// <summary>
    /// Close the workbench UI
    /// </summary>
    public void CloseUI()
    {
        gameObject.SetActive(false);
        currentWorkbench = null;
        selectedRecipeIndex = -1;

        EventManager.Instance?.OnCloseModal();
    }

    /// <summary>
    /// Get current workbench
    /// </summary>
    public Workbench CurrentWorkbench => currentWorkbench;

    /// <summary>
    /// Get selected recipe index
    /// </summary>
    public int SelectedRecipeIndex => selectedRecipeIndex;

    /// <summary>
    /// Check if crafting
    /// </summary>
    public bool IsCrafting => isCrafting;

    #endregion
}

/// <summary>
/// Button component for a recipe in the crafting list.
/// </summary>
public class RecipeButton : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    // State
    private int recipeIndex;
    private Workbench.Recipe recipe;
    private System.Action<int> onClickCallback;
    private Color normalColor;
    private bool isSelected;

    private void Awake()
    {
        // Get components
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }

        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }
    }

    /// <summary>
    /// Setup the recipe button
    /// </summary>
    public void Setup(Workbench.Recipe recipeData, int index, System.Action<int> onClick)
    {
        recipe = recipeData;
        recipeIndex = index;
        onClickCallback = onClick;

        // Update display
        if (nameText != null)
        {
            nameText.text = recipe.name;
        }

        if (iconImage != null && recipe.icon != null)
        {
            iconImage.sprite = recipe.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Set affordability visual
    /// </summary>
    public void SetAffordable(bool canAfford, Color affordableColor, Color unaffordableColor)
    {
        if (nameText != null)
        {
            nameText.color = canAfford ? affordableColor : unaffordableColor;
        }

        if (button != null)
        {
            button.interactable = canAfford;
        }
    }

    /// <summary>
    /// Set selected visual
    /// </summary>
    public void SetSelected(bool selected, Color selectedColor)
    {
        isSelected = selected;

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
    }

    private void OnClick()
    {
        onClickCallback?.Invoke(recipeIndex);
    }

    /// <summary>
    /// Get recipe data
    /// </summary>
    public Workbench.Recipe Recipe => recipe;

    /// <summary>
    /// Get recipe index
    /// </summary>
    public int RecipeIndex => recipeIndex;
}
