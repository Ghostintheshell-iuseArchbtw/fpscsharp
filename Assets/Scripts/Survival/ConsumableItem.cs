using UnityEngine;
using FPS.Combat;
using FPS.Survival;

namespace FPS.Survival
{
    public class ConsumableItem : MonoBehaviour, IInteractable
    {
        [Header("Consumable Settings")]
        [SerializeField] private string itemName = "Consumable";
        [SerializeField] private Sprite itemIcon;
        [SerializeField] private float weight = 0.5f;
        
        [Header("Effects")]
        [SerializeField] private float healthEffect = 0f;
        [SerializeField] private float hungerEffect = 0f;
        [SerializeField] private float thirstEffect = 0f;
        [SerializeField] private float staminaEffect = 0f;
        [SerializeField] private float radiationEffect = 0f;
        [SerializeField] private float temperatureEffect = 0f;
        
        [Header("Use Settings")]
        [SerializeField] private float useTime = 2f;
        [SerializeField] private AudioClip useSound;
        [SerializeField] private GameObject useEffect;
        
        public string GetInteractionPrompt()
        {
            return $"Pick up {itemName}";
        }
        
        public void Interact(GameObject player)
        {
            // Add to player's inventory instead of using immediately
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                // Create consumable data
                ConsumableItemData consumableData = new ConsumableItemData
                {
                    ItemName = itemName,
                    ItemIcon = itemIcon,
                    Weight = weight,
                    HealthEffect = healthEffect,
                    HungerEffect = hungerEffect,
                    ThirstEffect = thirstEffect,
                    StaminaEffect = staminaEffect,
                    RadiationEffect = radiationEffect,
                    TemperatureEffect = temperatureEffect,
                    UseTime = useTime,
                    UseSound = useSound,
                    UseEffect = useEffect
                };
                
                // Create lootable item
                ConsumableLootItem lootItem = new ConsumableLootItem(consumableData);
                
                // Add to inventory
                if (inventory.AddItem(lootItem))
                {
                    // Success - destroy this object
                    Destroy(gameObject);
                }
            }
        }
        
        // Initialize consumable from data
        public void InitializeFromData(ConsumableItemData data)
        {
            if (data == null) return;
            
            itemName = data.ItemName;
            itemIcon = data.ItemIcon;
            weight = data.Weight;
            
            healthEffect = data.HealthEffect;
            hungerEffect = data.HungerEffect;
            thirstEffect = data.ThirstEffect;
            staminaEffect = data.StaminaEffect;
            radiationEffect = data.RadiationEffect;
            temperatureEffect = data.TemperatureEffect;
            
            useTime = data.UseTime;
            useSound = data.UseSound;
            useEffect = data.UseEffect;
        }
    }
}

// Data structure for consumable items
[System.Serializable]
public class ConsumableItemData
{
    public string ItemName;
    public Sprite ItemIcon;
    public float Weight;
    public float HealthEffect;
    public float HungerEffect;
    public float ThirstEffect;
    public float StaminaEffect;
    public float RadiationEffect;
    public float TemperatureEffect;
    public float UseTime;
    public AudioClip UseSound;
    public GameObject UseEffect;
}

// Lootable item implementation for consumables
public class ConsumableLootItem : LootableItem
{
    private ConsumableItemData itemData;
    
    public ConsumableLootItem(ConsumableItemData data)
    {
        itemData = data;
    }
    
    public override string ItemName => itemData.ItemName;
    public override Sprite ItemIcon => itemData.ItemIcon;
    public override float ItemWeight => itemData.Weight;
    public override ItemType Type => ItemType.Consumable;
    
    public override bool Use(GameObject player)
    {
        // Find player systems
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        SurvivalSystem survival = player.GetComponent<SurvivalSystem>();
        
        // Apply effects
        if (health != null && itemData.HealthEffect != 0)
        {
            health.Heal(itemData.HealthEffect);
        }
        
        if (survival != null)
        {
            if (itemData.HungerEffect != 0)
            {
                survival.ModifyHunger(itemData.HungerEffect);
            }
            
            if (itemData.ThirstEffect != 0)
            {
                survival.ModifyThirst(itemData.ThirstEffect);
            }
            
            if (itemData.StaminaEffect != 0)
            {
                survival.ModifyStamina(itemData.StaminaEffect);
            }
            
            if (itemData.RadiationEffect != 0)
            {
                survival.ModifyRadiation(itemData.RadiationEffect);
            }
            
            if (itemData.TemperatureEffect != 0)
            {
                survival.ModifyTemperature(itemData.TemperatureEffect);
            }
        }
        
        // Play sound effect
        if (itemData.UseSound != null)
        {
            AudioSource audioSource = player.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(itemData.UseSound);
            }
        }
        
        // Spawn use effect
        if (itemData.UseEffect != null)
        {
            Instantiate(itemData.UseEffect, player.transform.position, Quaternion.identity);
        }
        
        return true; // Item was used successfully
    }
    
    // Get the item data
    public ConsumableItemData GetItemData()
    {
        return itemData;
    }
}
                    UseEffect = useEffect
                };
                
                // Create a lootable item
                ConsumableLootItem lootItem = new ConsumableLootItem(consumableData);
                
                // Add to inventory
                bool added = inventory.AddItem(lootItem);
                
                if (added)
                {
                    // Destroy world object
                    Destroy(gameObject);
                }
            }
        }
    }
    
    // Data container for consumable properties
    [System.Serializable]
    public class ConsumableItemData
    {
        public string ItemName;
        public Sprite ItemIcon;
        public float Weight;
        
        public float HealthEffect;
        public float HungerEffect;
        public float ThirstEffect;
        public float StaminaEffect;
        public float RadiationEffect;
        public float TemperatureEffect;
        
        public float UseTime;
        public AudioClip UseSound;
        public GameObject UseEffect;
    }
    
    // Lootable item implementation
    [System.Serializable]
    public class ConsumableLootItem : LootableItem
    {
        private ConsumableItemData consumableData;
        
        public ConsumableLootItem(ConsumableItemData data)
        {
            consumableData = data;
        }
        
        public override bool CanBeLooted => true;
        public override float DropChance => 1.0f;
        public override GameObject WorldModelPrefab => null; // Could be set if needed
        public override Vector3 WorldModelRotation => Vector3.zero;
        public override float ItemWeight => consumableData.Weight;
        public override string ItemName => consumableData.ItemName;
        public override Sprite ItemIcon => consumableData.ItemIcon;
        
        public void Use(GameObject player)
        {
            // Apply effects to player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            SurvivalSystem survivalSystem = player.GetComponent<SurvivalSystem>();
            
            if (playerHealth != null && consumableData.HealthEffect != 0)
            {
                playerHealth.Heal(consumableData.HealthEffect);
            }
            
            if (survivalSystem != null)
            {
                if (consumableData.HungerEffect != 0)
                {
                    survivalSystem.AddHunger(consumableData.HungerEffect);
                }
                
                if (consumableData.ThirstEffect != 0)
                {
                    survivalSystem.AddThirst(consumableData.ThirstEffect);
                }
                
                if (consumableData.StaminaEffect != 0)
                {
                    survivalSystem.AddStamina(consumableData.StaminaEffect);
                }
                
                if (consumableData.RadiationEffect != 0)
                {
                    survivalSystem.ReduceRadiation(consumableData.RadiationEffect);
                }
                
                if (consumableData.TemperatureEffect != 0)
                {
                    survivalSystem.ModifyBodyTemperature(consumableData.TemperatureEffect);
                }
            }
            
            // Play use sound
            if (consumableData.UseSound != null)
            {
                AudioSource.PlayClipAtPoint(consumableData.UseSound, player.transform.position);
            }
            
            // Spawn effect
            if (consumableData.UseEffect != null)
            {
                Instantiate(consumableData.UseEffect, player.transform.position, Quaternion.identity);
            }
        }
        
        // Get the item data
        public ConsumableItemData GetItemData()
        {
            return consumableData;
        }
    }
}
