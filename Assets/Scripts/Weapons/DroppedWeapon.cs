using UnityEngine;
using FPS.ScriptableObjects;

namespace FPS.Weapons
{
    public class DroppedWeapon : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private int currentAmmo;
        [SerializeField] private int reserveAmmo;
        
        [Header("Highlight Settings")]
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float glowIntensity = 0.5f;
        
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private bool isHighlighted = false;
        
        private void Awake()
        {
            // Cache renderers
            renderers = GetComponentsInChildren<Renderer>();
            
            // Store original materials
            if (renderers.Length > 0)
            {
                originalMaterials = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMaterials[i] = renderers[i].material;
                }
            }
        }
        
        public void Initialize(WeaponData data, int currAmmo, int resAmmo)
        {
            weaponData = data;
            currentAmmo = currAmmo;
            reserveAmmo = resAmmo;
        }
        
        public string GetInteractionPrompt()
        {
            return $"Pick up {weaponData.weaponName}";
        }
        
        public void Interact(GameObject player)
        {
            // Get player's weapon manager
            WeaponManager weaponManager = player.GetComponent<WeaponManager>();
            
            if (weaponManager != null)
            {
                // Add weapon to player's inventory
                weaponManager.LootWeapon(weaponData, currentAmmo, reserveAmmo);
                
                // Destroy this object
                Destroy(gameObject);
            }
        }
        
        public void Highlight(bool highlight)
        {
            if (highlight == isHighlighted || highlightMaterial == null) return;
            
            isHighlighted = highlight;
            
            foreach (Renderer rend in renderers)
            {
                if (highlight)
                {
                    // Apply highlight material
                    Material highlightInstance = new Material(highlightMaterial);
                    highlightInstance.SetFloat("_GlowIntensity", glowIntensity);
                    rend.material = highlightInstance;
                }
                else
                {
                    // Restore original material
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].material = originalMaterials[i];
                    }
                }
            }
        }
        
        // Optional: Add visual indicator above weapon
        private void OnDrawGizmos()
        {
            if (weaponData != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            }
        }
    }
}
