using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Workbench building for crafting units and upgrades.
/// Player can spend resources to create army units.
/// </summary>
public class Workbench : BuildingBase
{
    public enum RecipeType
    {
        Unit,           // Spawn a unit
        Farm,           // Build a farm
        Upgrade         // Purchase upgrade
    }

    [System.Serializable]
    public class Recipe
    {
        public string name;
        [TextArea] public string description;
        public RecipeType recipeType = RecipeType.Unit;
        public string unitType;         // For unit recipes
        public string farmType;         // For farm recipes: "meat", "wood", "gold"
        public int skullsCost;
        public int meatCost;
        public int woodCost;            // Renamed from scrapCost
        public int goldCost;
        public float craftTime;
        public Sprite icon;
        public int unlockLevel = 1;     // Player level required
    }

    [Header("Workbench Settings")]
    [SerializeField] private List<Recipe> recipes = new List<Recipe>();
    [SerializeField] private bool autoOpenMenu = true;

    [Header("State")]
    [SerializeField] private bool isCrafting;
    [SerializeField] private float craftProgress;
    [SerializeField] private Recipe currentRecipe;

    protected override void Awake()
    {
        base.Awake();
        buildingName = "Workbench";

        // Default recipes if none set
        if (recipes.Count == 0)
        {
            SetupDefaultRecipes();
        }
    }

    private void SetupDefaultRecipes()
    {
        // === UNITS - Per GDD v5 ===

        // Skull - basic unit (7 Skulls only)
        recipes.Add(new Recipe
        {
            name = "Skull",
            description = "Basic skeleton warrior. Reliable and cheap.",
            recipeType = RecipeType.Unit,
            unitType = "skull",
            skullsCost = 7,
            meatCost = 0,
            woodCost = 0,
            goldCost = 0,
            craftTime = 3f,
            unlockLevel = 1
        });

        // Gnoll/Bonnie - tank with lifesteal (5 Skulls, 5 Meat, 5 Wood)
        recipes.Add(new Recipe
        {
            name = "Gnoll",
            description = "Tough beast with vampiric attacks. Heals 20% of damage dealt.",
            recipeType = RecipeType.Unit,
            unitType = "gnoll",
            skullsCost = 5,
            meatCost = 5,
            woodCost = 5,
            goldCost = 0,
            craftTime = 5f,
            unlockLevel = 3
        });

        // Gnome/Foxy - DPS with stun (5 Skulls, 3 Meat, 3 Wood, 2 Gold)
        recipes.Add(new Recipe
        {
            name = "Gnome",
            description = "Small but fierce. Every 5th attack stuns the enemy for 1 second.",
            recipeType = RecipeType.Unit,
            unitType = "gnome",
            skullsCost = 5,
            meatCost = 3,
            woodCost = 3,
            goldCost = 2,
            craftTime = 4f,
            unlockLevel = 5
        });

        // TNT/Chica - ranged AoE (5 Skulls, 3 Meat, 5 Wood, 3 Gold)
        recipes.Add(new Recipe
        {
            name = "TNT",
            description = "Demolitions expert. Throws dynamite that explodes in an area.",
            recipeType = RecipeType.Unit,
            unitType = "tntgoblin",
            skullsCost = 5,
            meatCost = 3,
            woodCost = 5,
            goldCost = 3,
            craftTime = 6f,
            unlockLevel = 7
        });

        // Shaman/Marionette - support with mind control (5 Skulls, 3 Meat, 5 Wood, 5 Gold)
        recipes.Add(new Recipe
        {
            name = "Shaman",
            description = "Dark magic wielder. Can take control of enemy minds for 10 seconds.",
            recipeType = RecipeType.Unit,
            unitType = "shaman",
            skullsCost = 5,
            meatCost = 3,
            woodCost = 5,
            goldCost = 5,
            craftTime = 8f,
            unlockLevel = 10
        });

        // === FARMS - Per GDD v5 ===
        // Each farm costs 100 of its resource type

        recipes.Add(new Recipe
        {
            name = "Meat Farm",
            description = "Generates Meat over time. Place on designated farm spot.",
            recipeType = RecipeType.Farm,
            farmType = "meat",
            skullsCost = 0,
            meatCost = 100,
            woodCost = 0,
            goldCost = 0,
            craftTime = 5f,
            unlockLevel = 5
        });

        recipes.Add(new Recipe
        {
            name = "Wood Farm",
            description = "Generates Wood over time. Place on designated farm spot.",
            recipeType = RecipeType.Farm,
            farmType = "wood",
            skullsCost = 0,
            meatCost = 0,
            woodCost = 100,
            goldCost = 0,
            craftTime = 5f,
            unlockLevel = 5
        });

        recipes.Add(new Recipe
        {
            name = "Gold Farm",
            description = "Generates Gold over time. Place on designated farm spot.",
            recipeType = RecipeType.Farm,
            farmType = "gold",
            skullsCost = 0,
            meatCost = 0,
            woodCost = 0,
            goldCost = 100,
            craftTime = 5f,
            unlockLevel = 5
        });
    }

    protected override void Update()
    {
        base.Update();

        // Update crafting progress
        if (isCrafting && currentRecipe != null)
        {
            craftProgress += Time.deltaTime;

            if (craftProgress >= currentRecipe.craftTime)
            {
                CompleteCrafting();
            }
        }
    }

    protected override void OnPlayerEnterRange()
    {
        base.OnPlayerEnterRange();

        if (autoOpenMenu && !isCrafting)
        {
            // Open crafting UI
            EventManager.Instance?.OnOpenModal("workbench", this);
        }
    }

    protected override void OnPlayerExitRange()
    {
        base.OnPlayerExitRange();

        // Close crafting UI
        EventManager.Instance?.OnCloseModal();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        return !isCrafting;
    }

    public override void Interact()
    {
        if (isCrafting)
        {
            Debug.Log($"[Workbench] Currently crafting: {craftProgress:F1}/{currentRecipe?.craftTime:F1}");
            return;
        }

        // Open crafting menu
        EventManager.Instance?.OnOpenModal("workbench", this);
    }

    /// <summary>
    /// Start crafting a recipe
    /// </summary>
    public bool StartCrafting(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count)
            return false;

        Recipe recipe = recipes[recipeIndex];

        // Check if player can afford
        if (!CanAfford(recipe))
        {
            Debug.Log($"[Workbench] Cannot afford {recipe.name}");
            return false;
        }

        // Check army limit
        if (GameManager.Instance != null && !GameManager.Instance.CanAddUnit())
        {
            Debug.Log("[Workbench] Army is full!");
            return false;
        }

        // Spend resources
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpendResources(recipe.skullsCost, recipe.meatCost, recipe.woodCost, recipe.goldCost);
        }

        // Start crafting
        currentRecipe = recipe;
        craftProgress = 0f;
        isCrafting = true;

        Debug.Log($"[Workbench] Started crafting {recipe.name}");
        return true;
    }

    /// <summary>
    /// Start crafting by recipe name
    /// </summary>
    public bool StartCrafting(string recipeName)
    {
        int index = recipes.FindIndex(r => r.name.ToLower() == recipeName.ToLower());
        if (index >= 0)
        {
            return StartCrafting(index);
        }
        return false;
    }

    private void CompleteCrafting()
    {
        if (currentRecipe == null) return;

        Debug.Log($"[Workbench] Completed crafting {currentRecipe.name}!");

        // Handle different recipe types
        switch (currentRecipe.recipeType)
        {
            case RecipeType.Unit:
                // Request unit spawn
                EventManager.Instance?.OnUnitSpawnRequested(currentRecipe.unitType);
                break;

            case RecipeType.Farm:
                // Request farm placement
                EventManager.Instance?.OnFarmPurchased(currentRecipe.farmType);
                Debug.Log($"[Workbench] Farm purchased: {currentRecipe.farmType}");
                break;

            case RecipeType.Upgrade:
                // Handle upgrade purchase
                Debug.Log($"[Workbench] Upgrade purchased: {currentRecipe.name}");
                break;
        }

        // Reset state
        isCrafting = false;
        craftProgress = 0f;
        currentRecipe = null;

        // Play completion effect
        StartCoroutine(CompletionEffect());
    }

    private System.Collections.IEnumerator CompletionEffect()
    {
        // Flash green
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = original;
        }
    }

    public bool CanAfford(Recipe recipe)
    {
        if (GameManager.Instance == null) return false;

        // Check player level requirement
        if (GameManager.Instance.PlayerLevel < recipe.unlockLevel) return false;

        return GameManager.Instance.Skulls >= recipe.skullsCost &&
               GameManager.Instance.Meat >= recipe.meatCost &&
               GameManager.Instance.Wood >= recipe.woodCost &&
               GameManager.Instance.Gold >= recipe.goldCost;
    }

    /// <summary>
    /// Check if recipe is unlocked by player level.
    /// </summary>
    public bool IsUnlocked(Recipe recipe)
    {
        if (GameManager.Instance == null) return true;
        return GameManager.Instance.PlayerLevel >= recipe.unlockLevel;
    }

    public bool IsUnlocked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count) return false;
        return IsUnlocked(recipes[recipeIndex]);
    }

    public bool CanAfford(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count)
            return false;
        return CanAfford(recipes[recipeIndex]);
    }

    // Properties
    public List<Recipe> Recipes => recipes;
    public bool IsCrafting => isCrafting;
    public float CraftProgress => craftProgress;
    public Recipe CurrentRecipe => currentRecipe;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Show crafting progress
        if (isCrafting && currentRecipe != null)
        {
            Gizmos.color = Color.cyan;
            float progress = craftProgress / currentRecipe.craftTime;
            Gizmos.DrawCube(transform.position + Vector3.up * 1.5f, new Vector3(progress * 2f, 0.2f, 0.1f));
        }
    }
}
