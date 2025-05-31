using UnityEngine;
using System.Collections;

namespace FPS.Effects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("FPS/Effects/WeaponImpactEffect")]
    public class WeaponImpactEffect : MonoBehaviour
    {
        [Header("Screen Shake")]
        [SerializeField] private float shakeIntensity = 0.2f;
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeDecay = 0.8f;
        
        [Header("Impact Vignette")]
        [SerializeField] private Material postProcessMaterial;
        [SerializeField] private float vignetteIntensity = 0.5f;
        [SerializeField] private float vignetteSpeed = 5f;
        [SerializeField] private Color vignetteColor = Color.red;
        
        [Header("Chromatic Aberration")]
        [SerializeField] private float chromaticAberrationIntensity = 0.05f;
        [SerializeField] private float chromaticAberrationSpeed = 5f;
        
        [Header("Damage Effects")]
        [SerializeField] private float damageVignetteIntensity = 0.8f;
        [SerializeField] private Color damageVignetteColor = new Color(0.8f, 0, 0, 1);
        [SerializeField] private float lowHealthPulseSpeed = 1f;
        [SerializeField] private float lowHealthThreshold = 0.3f;
        
        // Private variables
        private Camera mainCamera;
        private Vector3 originalPosition;
        private float currentShakeIntensity = 0f;
        private float currentVignetteIntensity = 0f;
        private float currentChromaticAberration = 0f;
        private float currentDamageVignette = 0f;
        private float playerHealthPercent = 1f;
        
        // Property IDs for shader parameters
        private int vignetteIntensityID;
        private int vignetteColorID;
        private int chromaticAberrationID;
        
        private void OnEnable()
        {
            mainCamera = GetComponent<Camera>();
            originalPosition = transform.localPosition;
            
            // Get shader property IDs for efficient setting
            vignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
            vignetteColorID = Shader.PropertyToID("_VignetteColor");
            chromaticAberrationID = Shader.PropertyToID("_ChromaticAberration");
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (postProcessMaterial != null)
            {
                // Calculate total vignette effect (impact + damage)
                float totalVignette = Mathf.Clamp01(currentVignetteIntensity + currentDamageVignette);
                
                // Set shader parameters
                postProcessMaterial.SetFloat(vignetteIntensityID, totalVignette);
                
                // Determine vignette color based on which effect is stronger
                Color finalVignetteColor = Color.Lerp(vignetteColor, damageVignetteColor, 
                                                    currentDamageVignette / (totalVignette + 0.0001f));
                postProcessMaterial.SetColor(vignetteColorID, finalVignetteColor);
                
                // Set chromatic aberration
                postProcessMaterial.SetFloat(chromaticAberrationID, currentChromaticAberration);
                
                // Apply post-processing
                Graphics.Blit(source, destination, postProcessMaterial);
            }
            else
            {
                // If no material, just copy the source to destination
                Graphics.Blit(source, destination);
            }
        }
        
        private void Update()
        {
            // Update shake effect
            if (currentShakeIntensity > 0)
            {
                // Apply camera shake
                transform.localPosition = originalPosition + Random.insideUnitSphere * currentShakeIntensity;
                
                // Decay the shake intensity
                currentShakeIntensity *= shakeDecay;
                
                // Reset when below threshold
                if (currentShakeIntensity < 0.01f)
                {
                    currentShakeIntensity = 0f;
                    transform.localPosition = originalPosition;
                }
            }
            
            // Update vignette effect
            if (currentVignetteIntensity > 0)
            {
                // Decay vignette
                currentVignetteIntensity = Mathf.MoveTowards(
                    currentVignetteIntensity, 0, Time.deltaTime * vignetteSpeed);
            }
            
            // Update chromatic aberration
            if (currentChromaticAberration > 0)
            {
                // Decay chromatic aberration
                currentChromaticAberration = Mathf.MoveTowards(
                    currentChromaticAberration, 0, Time.deltaTime * chromaticAberrationSpeed);
            }
            
            // Update damage vignette
            UpdateDamageVignette();
        }
        
        private void UpdateDamageVignette()
        {
            // Base damage vignette on health percentage
            float targetDamageVignette = (1f - playerHealthPercent) * damageVignetteIntensity;
            
            // Add pulsing effect when health is low
            if (playerHealthPercent <= lowHealthThreshold)
            {
                targetDamageVignette += 
                    (Mathf.Sin(Time.time * lowHealthPulseSpeed * Mathf.PI * 2) * 0.5f + 0.5f) * 
                    (lowHealthThreshold - playerHealthPercent) * damageVignetteIntensity;
            }
            
            // Smooth transition to target value
            currentDamageVignette = Mathf.Lerp(currentDamageVignette, targetDamageVignette, Time.deltaTime * 3f);
        }
        
        // Public methods to trigger effects
        
        public void TriggerScreenShake(float intensity = 1f)
        {
            currentShakeIntensity = shakeIntensity * Mathf.Clamp01(intensity);
            StartCoroutine(ResetCameraPosition(shakeDuration));
        }
        
        public void TriggerImpactVignette(float intensity = 1f)
        {
            currentVignetteIntensity = Mathf.Clamp01(vignetteIntensity * intensity);
        }
        
        public void TriggerChromaticAberration(float intensity = 1f)
        {
            currentChromaticAberration = Mathf.Clamp01(chromaticAberrationIntensity * intensity);
        }
        
        public void SetPlayerHealth(float healthPercent)
        {
            playerHealthPercent = Mathf.Clamp01(healthPercent);
        }
        
        // Helper coroutines
        private IEnumerator ResetCameraPosition(float delay)
        {
            yield return new WaitForSeconds(delay);
            transform.localPosition = originalPosition;
        }
    }
}
