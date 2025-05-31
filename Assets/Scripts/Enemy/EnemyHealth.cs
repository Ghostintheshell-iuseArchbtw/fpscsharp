using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Damage Effects")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private GameObject[] gibPrefabs;
    [SerializeField] private float gibsThreshold = 0.3f; // Spawn gibs when health below this percentage
    [SerializeField] private bool shouldExplodeOnDeath = false;
    
    [Header("Hitbox Multipliers")]
    [SerializeField] private float headMultiplier = 2.0f;
    [SerializeField] private float limbMultiplier = 0.7f;
    
    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamaged;
    
    // State variables
    private bool isDead = false;
    private AudioSource audioSource;
    private EnemyAI enemyAI;
    
    // Properties
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        enemyAI = GetComponent<EnemyAI>();
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage, Vector3 hitPosition = default, Vector3 hitDirection = default, EnemyHitArea hitArea = EnemyHitArea.Body)
    {
        if (isDead) return;
        
        // Apply hitbox multiplier
        float modifiedDamage = damage;
        switch (hitArea)
        {
            case EnemyHitArea.Head:
                modifiedDamage *= headMultiplier;
                break;
            case EnemyHitArea.Limb:
                modifiedDamage *= limbMultiplier;
                break;
            default:
                break;
        }
        
        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - modifiedDamage);
        
        // Spawn hit effect
        SpawnHitEffect(hitPosition, hitDirection);
        
        // Notify enemy AI about damage
        if (enemyAI != null)
        {
            enemyAI.PlayHurtSound();
        }
        
        // Invoke event
        OnDamaged?.Invoke(modifiedDamage);
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die(hitDirection);
        }
    }
    
    private void SpawnHitEffect(Vector3 hitPosition, Vector3 hitDirection)
    {
        // Play hit sound
        if (hitSounds.Length > 0 && audioSource != null)
        {
            int index = Random.Range(0, hitSounds.Length);
            audioSource.PlayOneShot(hitSounds[index]);
        }
        
        // Spawn blood effect
        if (bloodEffectPrefab != null)
        {
            // If no hit position specified, use a random point on the body
            if (hitPosition == default)
            {
                hitPosition = transform.position + Vector3.up * Random.Range(0.5f, 1.5f);
            }
            
            // If no direction specified, use random direction
            if (hitDirection == default)
            {
                hitDirection = Random.onUnitSphere;
            }
            
            GameObject blood = Instantiate(bloodEffectPrefab, hitPosition, Quaternion.LookRotation(hitDirection));
            Destroy(blood, 2f);
        }
    }
    
    private void Die(Vector3 hitDirection)
    {
        isDead = true;
        
        // Invoke death event
        OnDeath?.Invoke();
        
        // Check if we should explode into gibs
        if (shouldExplodeOnDeath || (gibPrefabs.Length > 0 && currentHealth / maxHealth <= -gibsThreshold))
        {
            SpawnGibs(hitDirection);
        }
        
        // Note: The actual destruction of the enemy is handled in the EnemyAI component
        // This allows it to play death animations and handle other cleanup
    }
    
    private void SpawnGibs(Vector3 hitDirection)
    {
        if (gibPrefabs.Length == 0) return;
        
        // Spawn multiple gib pieces
        for (int i = 0; i < Random.Range(3, 6); i++)
        {
            int index = Random.Range(0, gibPrefabs.Length);
            
            if (gibPrefabs[index] != null)
            {
                // Spawn at random offset from enemy center
                Vector3 offset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0f, 1f),
                    Random.Range(-0.5f, 0.5f)
                );
                
                GameObject gib = Instantiate(gibPrefabs[index], transform.position + offset, Random.rotation);
                
                // Add force to gib
                Rigidbody gibRigidbody = gib.GetComponent<Rigidbody>();
                if (gibRigidbody != null)
                {
                    // Apply force in hit direction plus random variation
                    Vector3 forceDir = hitDirection + Random.insideUnitSphere;
                    gibRigidbody.AddForce(forceDir.normalized * Random.Range(3f, 8f), ForceMode.Impulse);
                    
                    // Add random torque
                    gibRigidbody.AddTorque(Random.insideUnitSphere * Random.Range(1f, 5f), ForceMode.Impulse);
                }
                
                // Destroy gib after delay
                Destroy(gib, Random.Range(5f, 10f));
            }
        }
    }
}

// Enum for different hit areas with different damage multipliers
public enum EnemyHitArea
{
    Body,
    Head,
    Limb
}

// Component to identify specific hit areas on an enemy
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyHitArea hitArea = EnemyHitArea.Body;
    [SerializeField] private EnemyHealth parentHealth;
    
    private void Awake()
    {
        // If no health component specified, try to find it in parent
        if (parentHealth == null)
        {
            parentHealth = GetComponentInParent<EnemyHealth>();
        }
    }
    
    public void TakeDamage(float damage, Vector3 hitPosition, Vector3 hitDirection)
    {
        if (parentHealth != null)
        {
            parentHealth.TakeDamage(damage, hitPosition, hitDirection, hitArea);
        }
    }
}
