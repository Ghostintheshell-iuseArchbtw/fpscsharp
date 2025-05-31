using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace FPS.Survival
{
    public class SurvivalSystem : MonoBehaviour
    {
        [Header("Hunger Settings")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger;
        [SerializeField] private float hungerRate = 0.05f;          // Rate per second
        [SerializeField] private float hungerDamageThreshold = 0f;  // Hunger level when damage starts
        [SerializeField] private float hungerDamageRate = 1f;       // Damage per second when hungry
        
        [Header("Thirst Settings")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float currentThirst;
        [SerializeField] private float thirstRate = 0.1f;           // Rate per second
        [SerializeField] private float thirstDamageThreshold = 0f;  // Thirst level when damage starts
        [SerializeField] private float thirstDamageRate = 2f;       // Damage per second when thirsty
        
        [Header("Temperature Settings")]
        [SerializeField] private float bodyTemperature = 37f;       // Normal body temperature in Celsius
        [SerializeField] private float minSafeTemperature = 35f;    // Below this takes cold damage
        [SerializeField] private float maxSafeTemperature = 39f;    // Above this takes heat damage
        [SerializeField] private float environmentTemperature = 22f; // Current environment temperature
        [SerializeField] private float temperatureChangeRate = 0.02f; // How fast body temp changes to match environment
        [SerializeField] private float temperatureDamageRate = 2f;  // Damage per second when temperature is extreme
        
        [Header("Rest Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegenRate = 5f;       // Rate per second when resting
        [SerializeField] private float staminaDepletionMultiplier = 1f; // Affects how fast actions deplete stamina
        
        [Header("Radiation Settings")]
        [SerializeField] private float radiationLevel = 0f;         // Current radiation exposure
        [SerializeField] private float maxRadiation = 100f;         // Max radiation before death
        [SerializeField] private float radiationDecayRate = 0.01f;  // How fast radiation naturally decays
        [SerializeField] private float radiationDamageRate = 3f;    // Damage per second from radiation
        
        [Header("Effects")]
        [SerializeField] private float lowStatMovementPenalty = 0.5f;   // Movement speed multiplier when stats are low
        [SerializeField] private float lowStatAimPenalty = 2f;          // Aim sway multiplier when stats are low
        
        // References
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private UIManager uiManager;
        
        // Events
        public UnityEvent<float> OnHungerChanged;
        public UnityEvent<float> OnThirstChanged;
        public UnityEvent<float> OnTemperatureChanged;
        public UnityEvent<float> OnRadiationChanged;
        public UnityEvent<float> OnStaminaChanged;
        
        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerController = GetComponent<PlayerController>();
            uiManager = FindObjectOfType<UIManager>();
            
            // Initialize values
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            currentStamina = maxStamina;
        }
        
        private void Update()
        {
            // Decrease hunger and thirst over time
            UpdateHunger();
            UpdateThirst();
            
            // Update temperature based on environment
            UpdateTemperature();
            
            // Update radiation
            UpdateRadiation();
            
            // Update movement and aim multipliers
            UpdatePlayerMultipliers();
        }
        
        private void UpdateHunger()
        {
            // Decrease hunger over time
            currentHunger = Mathf.Max(0, currentHunger - hungerRate * Time.deltaTime);
            
            // Apply damage if hunger is too low
            if (currentHunger <= hungerDamageThreshold && playerHealth != null)
            {
                playerHealth.TakeDamage(hungerDamageRate * Time.deltaTime);
            }
            
            // Notify UI
            OnHungerChanged?.Invoke(currentHunger / maxHunger);
        }
        
        private void UpdateThirst()
        {
            // Decrease thirst over time
            currentThirst = Mathf.Max(0, currentThirst - thirstRate * Time.deltaTime);
            
            // Apply damage if thirst is too low
            if (currentThirst <= thirstDamageThreshold && playerHealth != null)
            {
                playerHealth.TakeDamage(thirstDamageRate * Time.deltaTime);
            }
            
            // Notify UI
            OnThirstChanged?.Invoke(currentThirst / maxThirst);
        }
        
        private void UpdateTemperature()
        {
            // Body temperature moves toward environment temperature
            float temperatureDifference = environmentTemperature - bodyTemperature;
            bodyTemperature += temperatureDifference * temperatureChangeRate * Time.deltaTime;
            
            // Apply damage for extreme temperatures
            if (bodyTemperature < minSafeTemperature || bodyTemperature > maxSafeTemperature)
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(temperatureDamageRate * Time.deltaTime);
                }
            }
            
            // Notify UI
            OnTemperatureChanged?.Invoke(bodyTemperature);
        }
        
        private void UpdateRadiation()
        {
            // Radiation naturally decays over time
            radiationLevel = Mathf.Max(0, radiationLevel - radiationDecayRate * Time.deltaTime);
            
            // Apply radiation damage
            if (radiationLevel > 0 && playerHealth != null)
            {
                float radiationDamage = (radiationLevel / maxRadiation) * radiationDamageRate * Time.deltaTime;
                playerHealth.TakeDamage(radiationDamage);
            }
            
            // Notify UI
            OnRadiationChanged?.Invoke(radiationLevel / maxRadiation);
        }
        
        private void UpdatePlayerMultipliers()
        {
            if (playerController == null) return;
            
            // Calculate multipliers based on survival stats
            float hungerMultiplier = Mathf.Lerp(lowStatMovementPenalty, 1f, currentHunger / maxHunger);
            float thirstMultiplier = Mathf.Lerp(lowStatMovementPenalty, 1f, currentThirst / maxThirst);
            float temperatureMultiplier = (bodyTemperature >= minSafeTemperature && bodyTemperature <= maxSafeTemperature) ? 
                1f : lowStatMovementPenalty;
            float radiationMultiplier = Mathf.Lerp(1f, lowStatMovementPenalty, radiationLevel / maxRadiation);
            
            // Calculate combined multiplier
            float movementMultiplier = hungerMultiplier * thirstMultiplier * temperatureMultiplier * radiationMultiplier;
            float aimMultiplier = Mathf.Lerp(lowStatAimPenalty, 1f, movementMultiplier);
            
            // Apply to player controller
            playerController.SetMovementMultiplier(movementMultiplier);
            playerController.SetAimMultiplier(aimMultiplier);
        }
        
        // Public methods for other systems to modify survival stats
        
        public void ModifyHunger(float amount)
        {
            currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
            OnHungerChanged?.Invoke(currentHunger / maxHunger);
        }
        
        public void ModifyThirst(float amount)
        {
            currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
            OnThirstChanged?.Invoke(currentThirst / maxThirst);
        }
        
        public void ModifyTemperature(float amount)
        {
            bodyTemperature += amount;
            OnTemperatureChanged?.Invoke(bodyTemperature);
        }
        
        public void ModifyRadiation(float amount)
        {
            radiationLevel = Mathf.Clamp(radiationLevel + amount, 0, maxRadiation);
            OnRadiationChanged?.Invoke(radiationLevel / maxRadiation);
        }
        
        public void ModifyStamina(float amount)
        {
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }
        
        // Method for sprinting and other stamina-using actions
        public bool UseStamina(float amount)
        {
            // Check if we have enough stamina
            if (currentStamina < amount)
            {
                return false;
            }
            
            // Use stamina
            currentStamina -= amount;
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            return true;
        }
        
        // Regenerate stamina (called when resting)
        public void RegenerateStamina()
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }
        
        // Set the current environment temperature (called by EnvironmentManager)
        public void SetEnvironmentTemperature(float temperature)
        {
            environmentTemperature = temperature;
        }
        
        // Getters for UI and other systems
        public float GetHungerPercentage() => currentHunger / maxHunger;
        public float GetThirstPercentage() => currentThirst / maxThirst;
        public float GetTemperature() => bodyTemperature;
        public float GetRadiationPercentage() => radiationLevel / maxRadiation;
        public float GetStaminaPercentage() => currentStamina / maxStamina;
    }
}
        
        // Events
        public UnityEvent<float> OnHungerChanged;
        public UnityEvent<float> OnThirstChanged;
        public UnityEvent<float> OnTemperatureChanged;
        public UnityEvent<float> OnStaminaChanged;
        public UnityEvent<float> OnRadiationChanged;
        
        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerController = GetComponent<PlayerController>();
            uiManager = FindObjectOfType<UIManager>();
            
            // Initialize stats
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            currentStamina = maxStamina;
        }
        
        private void Update()
        {
            // Don't update if player is dead
            if (playerHealth != null && playerHealth.IsDead()) return;
            
            float deltaTime = Time.deltaTime;
            
            // Update hunger
            UpdateHunger(deltaTime);
            
            // Update thirst
            UpdateThirst(deltaTime);
            
            // Update temperature
            UpdateTemperature(deltaTime);
            
            // Update stamina
            UpdateStamina(deltaTime);
            
            // Update radiation
            UpdateRadiation(deltaTime);
            
            // Apply effects of low stats
            ApplyStatEffects();
        }
        
        private void UpdateHunger(float deltaTime)
        {
            // Decrease hunger over time
            currentHunger = Mathf.Max(0, currentHunger - hungerRate * deltaTime);
            
            // Apply damage if hunger is critically low
            if (currentHunger <= hungerDamageThreshold && playerHealth != null)
            {
                playerHealth.TakeDamage(hungerDamageRate * deltaTime);
            }
            
            // Notify listeners
            OnHungerChanged?.Invoke(currentHunger / maxHunger);
        }
        
        private void UpdateThirst(float deltaTime)
        {
            // Decrease thirst over time
            currentThirst = Mathf.Max(0, currentThirst - thirstRate * deltaTime);
            
            // Apply damage if thirst is critically low
            if (currentThirst <= thirstDamageThreshold && playerHealth != null)
            {
                playerHealth.TakeDamage(thirstDamageRate * deltaTime);
            }
            
            // Notify listeners
            OnThirstChanged?.Invoke(currentThirst / maxThirst);
        }
        
        private void UpdateTemperature(float deltaTime)
        {
            // Gradually adjust body temperature based on environment
            float temperatureDifference = environmentTemperature - bodyTemperature;
            bodyTemperature += temperatureDifference * temperatureChangeRate * deltaTime;
            
            // Apply damage if temperature is extreme
            if (bodyTemperature < minSafeTemperature || bodyTemperature > maxSafeTemperature)
            {
                if (playerHealth != null)
                {
                    float tempDamage = temperatureDamageRate * deltaTime;
                    playerHealth.TakeDamage(tempDamage);
                }
            }
            
            // Notify listeners
            // Convert to a 0-1 range for UI
            float normalizedTemp = Mathf.InverseLerp(minSafeTemperature - 5, maxSafeTemperature + 5, bodyTemperature);
            OnTemperatureChanged?.Invoke(normalizedTemp);
        }
        
        private void UpdateStamina(float deltaTime)
        {
            // Regenerate stamina when not sprinting
            if (playerController != null && !playerController.IsSprinting())
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * deltaTime);
            }
            
            // Notify listeners
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }
        
        private void UpdateRadiation(float deltaTime)
        {
            // Radiation naturally decays over time
            radiationLevel = Mathf.Max(0, radiationLevel - radiationDecayRate * deltaTime);
            
            // Apply radiation damage
            if (radiationLevel > 0 && playerHealth != null)
            {
                float radiationDamage = (radiationLevel / maxRadiation) * radiationDamageRate * deltaTime;
                playerHealth.TakeDamage(radiationDamage);
            }
            
            // Notify listeners
            OnRadiationChanged?.Invoke(radiationLevel / maxRadiation);
        }
        
        private void ApplyStatEffects()
        {
            if (playerController == null) return;
            
            // Calculate overall stat effect (average of non-stamina stats)
            float hungerEffect = currentHunger / maxHunger;
            float thirstEffect = currentThirst / maxThirst;
            float tempEffect = bodyTemperature >= minSafeTemperature && bodyTemperature <= maxSafeTemperature ? 1f : 0.5f;
            float radiationEffect = 1f - (radiationLevel / maxRadiation);
            
            float overallEffect = (hungerEffect + thirstEffect + tempEffect + radiationEffect) / 4f;
            
            // Apply movement speed penalty
            float speedMultiplier = Mathf.Lerp(lowStatMovementPenalty, 1f, overallEffect);
            playerController.SetMovementMultiplier(speedMultiplier);
            
            // Apply aim penalty
            float aimMultiplier = Mathf.Lerp(lowStatAimPenalty, 1f, overallEffect);
            playerController.SetAimMultiplier(aimMultiplier);
        }
        
        // Public methods for consumables and items
        public void AddHunger(float amount)
        {
            currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
            OnHungerChanged?.Invoke(currentHunger / maxHunger);
        }
        
        public void AddThirst(float amount)
        {
            currentThirst = Mathf.Min(maxThirst, currentThirst + amount);
            OnThirstChanged?.Invoke(currentThirst / maxThirst);
        }
        
        public void AddStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }
        
        public void ReduceRadiation(float amount)
        {
            radiationLevel = Mathf.Max(0, radiationLevel - amount);
            OnRadiationChanged?.Invoke(radiationLevel / maxRadiation);
        }
        
        public void ModifyBodyTemperature(float amount)
        {
            bodyTemperature += amount;
            float normalizedTemp = Mathf.InverseLerp(minSafeTemperature - 5, maxSafeTemperature + 5, bodyTemperature);
            OnTemperatureChanged?.Invoke(normalizedTemp);
        }
        
        // For external systems to consume stamina
        public bool UseStamina(float amount)
        {
            // Check if we have enough stamina
            if (currentStamina < amount)
            {
                return false;
            }
            
            // Use stamina
            currentStamina -= amount * staminaDepletionMultiplier;
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            
            return true;
        }
        
        // For environment systems to set temperature
        public void SetEnvironmentTemperature(float temperature)
        {
            environmentTemperature = temperature;
        }
        
        // For environment systems to add radiation
        public void AddRadiation(float amount)
        {
            radiationLevel = Mathf.Min(maxRadiation, radiationLevel + amount);
            OnRadiationChanged?.Invoke(radiationLevel / maxRadiation);
        }
        
        // Getters for UI
        public float GetHungerPercentage() => currentHunger / maxHunger;
        public float GetThirstPercentage() => currentThirst / maxThirst;
        public float GetStaminaPercentage() => currentStamina / maxStamina;
        public float GetRadiationPercentage() => radiationLevel / maxRadiation;
        public float GetBodyTemperature() => bodyTemperature;
    }
}
