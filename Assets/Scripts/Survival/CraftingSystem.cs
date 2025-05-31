using UnityEngine;
using System.Collections.Generic;
using FPS.Combat;

namespace FPS.Survival
{
    public class CraftingSystem : MonoBehaviour
    {
        [Header("Crafting Settings")]
        [SerializeField] private float craftingTime = 2f;
        [SerializeField] private AudioClip craftingSound;
        [SerializeField] private GameObject craftingEffect;
        
        [Header("Recipes")]
        [SerializeField] private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        
        // References
        private PlayerInventory inventory;
        private UIManager uiManager;
        
        // State
        private bool isCrafting = false;
        private float craftingProgress = 0f;
        private CraftingRecipe currentRecipe;
        
        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            uiManager = FindObjectOfType<UIManager>();
        }
        
        public List<CraftingRecipe> GetAvailableRecipes()
        {
            if (inventory == null) return new List<CraftingRecipe>();
            
            List<CraftingRecipe> craftableRecipes = new List<CraftingRecipe>();
            
            // Check which recipes can be crafted with current inventory
            foreach (CraftingRecipe recipe in availableRecipes)
            {
                if (CanCraftRecipe(recipe))
                {
                    craftableRecipes.Add(recipe);
                }
            }
            
            return craftableRecipes;
        }
        
        private bool CanCraftRecipe(CraftingRecipe recipe)
        {
            if (inventory == null) return false;
            
            // Check each required ingredient
            foreach (CraftingIngredient ingredient in recipe.requiredIngredients)
            {
                // Count how many of this ingredient the player has
                int count = inventory.CountItemsOfType(ingredient.itemType);
                
                // If not enough, recipe can't be crafted
                if (count < ingredient.amount)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public void StartCrafting(CraftingRecipe recipe)
        {
            if (isCrafting || !CanCraftRecipe(recipe)) return;
            
            isCrafting = true;
            craftingProgress = 0f;
            currentRecipe = recipe;
            
            // Play crafting sound
            if (craftingSound != null)
            {
                AudioSource.PlayClipAtPoint(craftingSound, transform.position);
            }
            
            // Spawn crafting effect
            if (craftingEffect != null)
            {
                Instantiate(craftingEffect, transform.position, Quaternion.identity);
            }
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.ShowCraftingProgress(0f);
            }
        }
        
        private void Update()
        {
            if (!isCrafting || currentRecipe == null) return;
            
            // Update crafting progress
            craftingProgress += Time.deltaTime;
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.ShowCraftingProgress(craftingProgress / craftingTime);
            }
            
            // Check if crafting is complete
            if (craftingProgress >= craftingTime)
            {
                CompleteCrafting();
            }
        }
        
        private void CompleteCrafting()
        {
            if (currentRecipe == null) return;
            
            // Consume required ingredients
            foreach (CraftingIngredient ingredient in currentRecipe.requiredIngredients)
            {
                // Remove required amount from inventory
                inventory.RemoveItemsOfType(ingredient.itemType, ingredient.amount);
            }
            
            // Add crafted items to inventory
            foreach (CraftingResult result in currentRecipe.results)
            {
                for (int i = 0; i < result.amount; i++)
                {
                    // Create the item
                    LootableItem craftedItem = CreateCraftedItem(result.itemType);
                    
                    if (craftedItem != null)
                    {
                        // Add to inventory
                        inventory.AddItem(craftedItem);
                    }
                }
            }
            
            // Reset crafting state
            isCrafting = false;
            craftingProgress = 0f;
            currentRecipe = null;
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.HideCraftingProgress();
                uiManager.ShowNotification("Crafting complete!");
            }
        }
        
        private LootableItem CreateCraftedItem(ItemType itemType)
        {
            // Create appropriate item based on type
            switch (itemType)
            {
                case ItemType.Bandage:
                    return CreateConsumableItem("Bandage", 15f, 0f, 0f, 0f, 0f, 0f, 0.1f);
                
                case ItemType.MedKit:
                    return CreateConsumableItem("Med Kit", 50f, 0f, 0f, 0f, 0f, 0f, 0.5f);
                
                case ItemType.Water:
                    return CreateConsumableItem("Water Bottle", 0f, 0f, 30f, 0f, 0f, 0f, 0.3f);
                
                case ItemType.Food:
                    return CreateConsumableItem("Canned Food", 0f, 25f, 10f, 0f, 0f, 0f, 0.4f);
                
                case ItemType.Ammunition:
                    // Would need to specify ammo type
                    return null;
                
                default:
                    Debug.LogError($"Unhandled item type in crafting: {itemType}");
                    return null;
            }
        }
        
        private ConsumableLootItem CreateConsumableItem(string name, float health, float hunger, float thirst, 
                                                     float stamina, float radiation, float temperature, float weight)
        {
            ConsumableItemData data = new ConsumableItemData
            {
                ItemName = name,
                HealthEffect = health,
                HungerEffect = hunger,
                ThirstEffect = thirst,
                StaminaEffect = stamina,
                RadiationEffect = radiation,
                TemperatureEffect = temperature,
                Weight = weight,
                UseTime = 2f
            };
            
            return new ConsumableLootItem(data);
        }
        
        public void CancelCrafting()
        {
            isCrafting = false;
            craftingProgress = 0f;
            currentRecipe = null;
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.HideCraftingProgress();
            }
        }
        
        // Method to craft a specific item
        public void CraftItem(string recipeId)
        {
            // Find the recipe
            CraftingRecipe recipe = availableRecipes.Find(r => r.recipeName == recipeId);
            
            if (recipe == null)
            {
                Debug.LogError($"Recipe not found: {recipeId}");
                return;
            }
            
            if (!CanCraftRecipe(recipe))
            {
                // Not enough resources
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Not enough resources to craft " + recipe.recipeName);
                }
                return;
            }
            
            // Start crafting
            StartCrafting(recipe);
        }
        
        // Method to get the current crafting progress
        public float GetCraftingProgress()
        {
            if (!isCrafting || craftingTime <= 0)
            {
                return 0f;
            }
            
            return craftingProgress / craftingTime;
        }
        
        // Method to check if currently crafting
        public bool IsCrafting()
        {
            return isCrafting;
        }
        
        // Method to get current recipe
        public CraftingRecipe GetCurrentRecipe()
        {
            return currentRecipe;
        }
    }
    
    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName;
        public Sprite recipeIcon;
        public string recipeDescription;
        public float craftingTime = 2f;
        public List<CraftingIngredient> requiredIngredients = new List<CraftingIngredient>();
        public List<CraftingResult> results = new List<CraftingResult>();
        public bool requiresCraftingStation = false;
        public CraftingStationType requiredStationType = CraftingStationType.None;
    }
    
    [System.Serializable]
    public class CraftingIngredient
    {
        public ItemType itemType;
        public int amount = 1;
    }
    
    [System.Serializable]
    public class CraftingResult
    {
        public ItemType itemType;
        public int amount = 1;
    }
    
    public enum ItemType
    {
        Bandage,
        MedKit,
        Water,
        Food,
        Wood,
        Stone,
        Metal,
        Cloth,
        Ammunition,
        Weapon,
        Tool
    }
    
    public enum CraftingStationType
    {
        None,
        Workbench,
        Furnace,
        Campfire,
        ChemistryTable
    }
}
