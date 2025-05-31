using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float healthRegenRate = 10f;
    [SerializeField] private float healthRegenDelay = 5f;
    [SerializeField] private bool canRegenHealth = true;
    
    [Header("Armor Settings")]
    [SerializeField] private float maxArmor = 100f;
    [SerializeField] private float currentArmor;
    [SerializeField] private float armorDamageReduction = 0.5f; // 50% damage reduction
    
    [Header("Damage Effects")]
    [SerializeField] private float damageScreenEffectDuration = 0.3f;
    [SerializeField] private Material damageScreenMaterial;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private GameObject bloodSplatterPrefab;
    
    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamaged;
    public UnityEvent<float> OnHealed;
    
    // Components
    private AudioSource audioSource;
    private Camera playerCamera;
    private PlayerController playerController;
    private CharacterController characterController;
    
    // State variables
    private bool isDead = false;
    private float lastDamageTime;
    private float damageScreenIntensity;
    private bool isInvulnerable = false;
    
    // Public properties
    public bool IsInvulnerable { get { return isInvulnerable; } set { isInvulnerable = value; } }
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerCamera = GetComponentInChildren<Camera>();
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        
        // Initialize health and armor
        currentHealth = maxHealth;
        currentArmor = maxArmor;
    }
    
    private void Start()
    {
        // Create damage screen effect (optional)
        if (damageScreenMaterial != null && playerCamera != null)
        {
            GameObject damageScreen = new GameObject("DamageScreen");
            damageScreen.transform.SetParent(playerCamera.transform);
            
            // Setup as a fullscreen quad
            MeshRenderer renderer = damageScreen.AddComponent<MeshRenderer>();
            MeshFilter filter = damageScreen.AddComponent<MeshFilter>();
            renderer.material = damageScreenMaterial;
            
            // Create a quad mesh
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                new Vector3(-1, -1, 0.1f),
                new Vector3(1, -1, 0.1f),
                new Vector3(-1, 1, 0.1f),
                new Vector3(1, 1, 0.1f)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            
            filter.mesh = mesh;
            
            // Position in front of camera
            damageScreen.transform.localPosition = new Vector3(0, 0, 0.2f);
            damageScreen.transform.localRotation = Quaternion.identity;
            damageScreen.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            
            // Initially transparent
            Color color = damageScreenMaterial.color;
            color.a = 0;
            damageScreenMaterial.color = color;
        }
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Update damage screen effect
        UpdateDamageScreen();
        
        // Health regeneration
        if (canRegenHealth && Time.time > lastDamageTime + healthRegenDelay && currentHealth < maxHealth)
        {
            Heal(healthRegenRate * Time.deltaTime);
        }
    }
    
    public void TakeDamage(float damageAmount, Vector3 hitDirection = default, float knockbackForce = 0)
    {
        if (isDead || isInvulnerable) return;
        
        lastDamageTime = Time.time;
        
        // Apply armor reduction if available
        float damageAfterArmor = damageAmount;
        
        if (currentArmor > 0)
        {
            // Calculate how much damage armor absorbs
            float absorbedDamage = damageAmount * armorDamageReduction;
            
            // Reduce armor by the absorbed amount
            currentArmor = Mathf.Max(0, currentArmor - absorbedDamage);
            
            // Reduce damage by the absorbed amount
            damageAfterArmor = damageAmount - absorbedDamage;
        }
        
        // Apply remaining damage to health
        currentHealth = Mathf.Max(0, currentHealth - damageAfterArmor);
        
        // Trigger damage effects
        PlayHurtEffects(hitDirection, knockbackForce);
        
        // Invoke damage event
        OnDamaged?.Invoke(damageAfterArmor);
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        // Only invoke event if health actually increased
        if (currentHealth > oldHealth)
        {
            OnHealed?.Invoke(currentHealth - oldHealth);
        }
    }
    
    public void AddArmor(float armorAmount)
    {
        currentArmor = Mathf.Min(maxArmor, currentArmor + armorAmount);
    }
    
    private void PlayHurtEffects(Vector3 hitDirection, float knockbackForce)
    {
        // Increase damage screen intensity
        damageScreenIntensity = Mathf.Clamp01(damageScreenIntensity + 0.3f);
        
        // Play random hurt sound
        if (hurtSounds.Length > 0 && audioSource != null)
        {
            int index = Random.Range(0, hurtSounds.Length);
            audioSource.PlayOneShot(hurtSounds[index]);
        }
        
        // Spawn blood effect if there's a prefab
        if (bloodSplatterPrefab != null)
        {
            GameObject blood = Instantiate(bloodSplatterPrefab, playerCamera.transform.position + playerCamera.transform.forward * 0.5f, 
                                         Quaternion.LookRotation(-playerCamera.transform.forward));
            Destroy(blood, 1f);
        }
        
        // Apply knockback force
        if (knockbackForce > 0 && characterController != null)
        {
            // Ensure hit direction is normalized and includes horizontal component only
            if (hitDirection == default)
            {
                hitDirection = -transform.forward; // Default to backward if no direction given
            }
            
            hitDirection.y = 0;
            hitDirection.Normalize();
            
            // Apply knockback as an impulse
            StartCoroutine(ApplyKnockback(hitDirection * knockbackForce));
        }
    }
    
    private IEnumerator ApplyKnockback(Vector3 knockbackVelocity)
    {
        float knockbackDuration = 0.2f;
        float elapsedTime = 0;
        
        while (elapsedTime < knockbackDuration)
        {
            // Apply decreasing knockback force
            float t = elapsedTime / knockbackDuration;
            Vector3 currentKnockback = Vector3.Lerp(knockbackVelocity, Vector3.zero, t);
            
            characterController.Move(currentKnockback * Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private void UpdateDamageScreen()
    {
        if (damageScreenMaterial != null)
        {
            // Gradually reduce damage intensity
            damageScreenIntensity = Mathf.Max(0, damageScreenIntensity - Time.deltaTime / damageScreenEffectDuration);
            
            // Update material alpha
            Color color = damageScreenMaterial.color;
            color.a = damageScreenIntensity;
            damageScreenMaterial.color = color;
        }
    }
    
    private void Die()
    {
        isDead = true;
        
        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Disable player controls
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Drop currently held weapon
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        WeaponController weaponController = GetComponentInChildren<WeaponController>();
        
        if (inventory != null && weaponController != null && weaponController.GetCurrentWeapon() != null)
        {
            // Create a loot item from current weapon
            WeaponLootItem weaponLoot = new WeaponLootItem(weaponController.GetCurrentWeapon(), weaponController.GetCurrentAmmo());
            inventory.DropItem(weaponLoot);
        }
        
        // Invoke death event
        OnDeath?.Invoke();
    }
    
    public void Respawn(Vector3 respawnPosition)
    {
        // Reset health and armor
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        
        // Reset state
        isDead = false;
        damageScreenIntensity = 0f;
        
        // Move player to respawn position
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Disable character controller to teleport
            characterController.enabled = false;
            transform.position = respawnPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }
        
        // Re-enable player controls
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Grant temporary invulnerability
        StartCoroutine(TemporaryInvulnerability(2f));
    }
    
    private IEnumerator TemporaryInvulnerability(float duration)
    {
        isInvulnerable = true;
        
        // Visual feedback for invulnerability
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        
        // Flash effect during invulnerability
        float flashInterval = 0.2f;
        int flashCount = Mathf.FloorToInt(duration / flashInterval);
        
        for (int i = 0; i < flashCount; i++)
        {
            // Toggle visibility for flash effect
            foreach (var renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = color.a > 0.5f ? 0.3f : 1f;
                renderer.material.color = color;
            }
            
            yield return new WaitForSeconds(flashInterval);
        }
        
        // Restore normal visibility
        foreach (var renderer in renderers)
        {
            Color color = renderer.material.color;
            color.a = 1f;
            renderer.material.color = color;
        }
        
        isInvulnerable = false;
    }
    
    // Getters for UI
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    
    public float GetCurrentArmor() => currentArmor;
    public float GetMaxArmor() => maxArmor;
    public float GetArmorPercentage() => currentArmor / maxArmor;
    
    public bool IsDead() => isDead;
}
