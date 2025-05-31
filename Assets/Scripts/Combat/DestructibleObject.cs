using UnityEngine;

namespace FPS.Combat
{
    /// <summary>
    /// Interface for any object that can be damaged or destroyed by weapons
    /// </summary>
    public interface IDestructible
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
    }
    
    /// <summary>
    /// Basic implementation of a destructible object with configurable health
    /// </summary>
    public class DestructibleObject : MonoBehaviour, IDestructible
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        
        [Header("Destruction Settings")]
        [SerializeField] private GameObject destroyedVersionPrefab;
        [SerializeField] private GameObject destructionEffectPrefab;
        [SerializeField] private AudioClip destructionSound;
        [SerializeField] private AudioClip hitSound;
        
        [Header("Loot Settings")]
        [SerializeField] private GameObject[] possibleLootItems;
        [SerializeField] private float lootDropChance = 0.3f;
        [SerializeField] private int maxLootItems = 2;
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null && (hitSound != null || destructionSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }
        
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            // Apply damage
            currentHealth -= damage;
            
            // Play hit sound
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // Check for destruction
            if (currentHealth <= 0)
            {
                Destroy();
            }
        }
        
        private void Destroy()
        {
            // Spawn destroyed version if available
            if (destroyedVersionPrefab != null)
            {
                Instantiate(destroyedVersionPrefab, transform.position, transform.rotation);
            }
            
            // Spawn destruction effect
            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Play destruction sound
            if (audioSource != null && destructionSound != null)
            {
                // Detach audio source so it can finish playing
                audioSource.transform.SetParent(null);
                audioSource.PlayOneShot(destructionSound);
                Destroy(audioSource.gameObject, destructionSound.length);
            }
            
            // Drop loot
            DropLoot();
            
            // Destroy the original object
            Destroy(gameObject);
        }
        
        private void DropLoot()
        {
            if (possibleLootItems == null || possibleLootItems.Length == 0) return;
            
            // Determine how many items to drop
            int itemsToDrop = Random.value <= lootDropChance ? Random.Range(1, maxLootItems + 1) : 0;
            
            for (int i = 0; i < itemsToDrop; i++)
            {
                // Select random item
                int itemIndex = Random.Range(0, possibleLootItems.Length);
                
                if (possibleLootItems[itemIndex] != null)
                {
                    // Calculate drop position with slight offset
                    Vector3 dropPosition = transform.position + new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0.5f,
                        Random.Range(-0.5f, 0.5f)
                    );
                    
                    // Instantiate loot item
                    GameObject loot = Instantiate(possibleLootItems[itemIndex], dropPosition, Quaternion.identity);
                    
                    // Add force to the item if it has a rigidbody
                    Rigidbody rb = loot.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 force = new Vector3(
                            Random.Range(-1f, 1f),
                            Random.Range(2f, 4f),
                            Random.Range(-1f, 1f)
                        );
                        
                        rb.AddForce(force, ForceMode.Impulse);
                    }
                }
            }
        }
    }
}
