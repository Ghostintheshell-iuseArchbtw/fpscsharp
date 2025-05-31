using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FPS.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Day/Night Cycle")]
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField] private float dayLength = 600f; // 10 minutes per day
        [SerializeField] private Light directionalLight;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        [SerializeField] private Gradient lightColorGradient;
        [SerializeField] private Gradient fogColorGradient;
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private float nightVisionIntensity = 0.3f;
        
        [Header("Weather System")]
        [SerializeField] private bool enableWeatherSystem = true;
        [SerializeField] private WeatherPreset[] weatherPresets;
        [SerializeField] private float minWeatherDuration = 300f; // 5 minutes
        [SerializeField] private float maxWeatherDuration = 900f; // 15 minutes
        [SerializeField] private float weatherTransitionTime = 60f; // 1 minute
        
        [Header("Environmental Effects")]
        [SerializeField] private float windStrength = 1f;
        [SerializeField] private Vector3 windDirection = Vector3.right;
        [SerializeField] private List<WindAffectedObject> windAffectedObjects = new List<WindAffectedObject>();
        [SerializeField] private float maxRaycastDistance = 50f;
        [SerializeField] private LayerMask environmentMask;
        [SerializeField] private float debrisLifetime = 30f;
        
        // Time and weather state
        private float currentTimeOfDay = 0.3f; // 0-1, start at morning
        private float timeScale = 1f;
        private WeatherPreset currentWeatherPreset;
        private WeatherPreset targetWeatherPreset;
        private float weatherTransitionProgress = 1f;
        private float currentWeatherDuration;
        private float currentWeatherTime;
        
        // Cached components
        private Camera mainCamera;
        private AudioSource ambientAudioSource;
        
        // Events
        public delegate void TimeOfDayChangedHandler(float timeOfDay, bool isNight);
        public event TimeOfDayChangedHandler OnTimeOfDayChanged;
        
        public delegate void WeatherChangedHandler(WeatherPreset newWeather);
        public event WeatherChangedHandler OnWeatherChanged;
        
        private void Awake()
        {
            // Cache components
            mainCamera = Camera.main;
            ambientAudioSource = GetComponent<AudioSource>();
            
            // Initialize
            if (weatherPresets != null && weatherPresets.Length > 0)
            {
                currentWeatherPreset = weatherPresets[0]; // Start with first preset (should be clear weather)
                targetWeatherPreset = currentWeatherPreset;
                ApplyWeatherSettings(currentWeatherPreset, 1f);
            }
            
            // Register wind affected objects
            RegisterWindAffectedObjects();
        }
        
        private void Start()
        {
            // Apply initial settings
            if (enableDayNightCycle && directionalLight != null)
            {
                UpdateDayNightCycle();
            }
            
            // Start weather cycle if enabled
            if (enableWeatherSystem)
            {
                currentWeatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
                currentWeatherTime = 0f;
            }
        }
        
        private void Update()
        {
            // Update day/night cycle
            if (enableDayNightCycle)
            {
                // Update time of day
                currentTimeOfDay += (Time.deltaTime / dayLength) * timeScale;
                if (currentTimeOfDay >= 1f)
                {
                    currentTimeOfDay -= 1f;
                }
                
                UpdateDayNightCycle();
            }
            
            // Update weather system
            if (enableWeatherSystem)
            {
                UpdateWeather();
            }
            
            // Update wind effects
            UpdateWindEffects();
        }
        
        private void UpdateDayNightCycle()
        {
            // Calculate sun rotation
            float sunRotation = currentTimeOfDay * 360f;
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(sunRotation - 90f, 170f, 0));
            
            // Update light intensity and color
            float lightIntensity = lightIntensityCurve.Evaluate(currentTimeOfDay);
            directionalLight.intensity = lightIntensity;
            directionalLight.color = lightColorGradient.Evaluate(currentTimeOfDay);
            
            // Update environmental lighting
            RenderSettings.ambientLight = ambientColorGradient.Evaluate(currentTimeOfDay);
            RenderSettings.fogColor = fogColorGradient.Evaluate(currentTimeOfDay);
            
            // Determine if it's night (for gameplay purposes)
            bool isNight = currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;
            
            // Night vision effect when it's dark
            if (isNight && mainCamera != null)
            {
                // Here you would activate a night vision post-processing effect
                // or modify a shader parameter
            }
            
            // Trigger event
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay, isNight);
        }
        
        private void UpdateWeather()
        {
            // Update current weather duration
            currentWeatherTime += Time.deltaTime;
            
            // Check if we should transition to a new weather state
            if (currentWeatherTime >= currentWeatherDuration && weatherTransitionProgress >= 1f)
            {
                // Select a new weather preset (different from current)
                int attempts = 0;
                WeatherPreset newWeather = currentWeatherPreset;
                
                // Try to pick a different weather preset
                while (newWeather == currentWeatherPreset && attempts < 5 && weatherPresets.Length > 1)
                {
                    newWeather = weatherPresets[Random.Range(0, weatherPresets.Length)];
                    attempts++;
                }
                
                // Start transition to new weather
                targetWeatherPreset = newWeather;
                weatherTransitionProgress = 0f;
                
                // Reset timer and set new duration
                currentWeatherTime = 0f;
                currentWeatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
                
                // Trigger event
                OnWeatherChanged?.Invoke(targetWeatherPreset);
            }
            
            // Handle weather transition
            if (weatherTransitionProgress < 1f)
            {
                // Progress the transition
                weatherTransitionProgress += Time.deltaTime / weatherTransitionTime;
                weatherTransitionProgress = Mathf.Clamp01(weatherTransitionProgress);
                
                // Apply interpolated weather settings
                ApplyWeatherSettings(currentWeatherPreset, targetWeatherPreset, weatherTransitionProgress);
                
                // If transition complete, update current weather
                if (weatherTransitionProgress >= 1f)
                {
                    currentWeatherPreset = targetWeatherPreset;
                }
            }
        }
        
        private void ApplyWeatherSettings(WeatherPreset preset, float intensity)
        {
            if (preset == null) return;
            
            // Apply fog settings
            RenderSettings.fog = preset.EnableFog;
            RenderSettings.fogMode = preset.FogMode;
            RenderSettings.fogDensity = preset.FogDensity * intensity;
            
            // Apply particle systems
            foreach (var particleSystem in preset.ParticleSystems)
            {
                if (particleSystem.ParticleSystem != null)
                {
                    var emission = particleSystem.ParticleSystem.emission;
                    emission.rateOverTimeMultiplier = particleSystem.EmissionRate * intensity;
                    
                    if (intensity > 0.01f)
                    {
                        if (!particleSystem.ParticleSystem.isPlaying)
                        {
                            particleSystem.ParticleSystem.Play();
                        }
                    }
                    else
                    {
                        particleSystem.ParticleSystem.Stop();
                    }
                }
            }
            
            // Apply wind settings
            windStrength = preset.WindStrength * intensity;
            windDirection = preset.WindDirection.normalized;
            
            // Apply post-processing settings
            // In a real implementation, you would modify post-processing settings here
            
            // Play ambient sounds
            if (ambientAudioSource != null && preset.AmbientSound != null)
            {
                if (ambientAudioSource.clip != preset.AmbientSound)
                {
                    ambientAudioSource.clip = preset.AmbientSound;
                    ambientAudioSource.Play();
                }
                ambientAudioSource.volume = preset.AmbientSoundVolume * intensity;
            }
        }
        
        private void ApplyWeatherSettings(WeatherPreset fromPreset, WeatherPreset toPreset, float t)
        {
            if (fromPreset == null || toPreset == null) return;
            
            // Interpolate fog settings
            RenderSettings.fog = t > 0.5f ? toPreset.EnableFog : fromPreset.EnableFog;
            RenderSettings.fogMode = t > 0.5f ? toPreset.FogMode : fromPreset.FogMode;
            RenderSettings.fogDensity = Mathf.Lerp(fromPreset.FogDensity, toPreset.FogDensity, t);
            
            // Interpolate wind settings
            windStrength = Mathf.Lerp(fromPreset.WindStrength, toPreset.WindStrength, t);
            windDirection = Vector3.Lerp(fromPreset.WindDirection, toPreset.WindDirection, t).normalized;
            
            // Handle particle systems
            foreach (var fromParticle in fromPreset.ParticleSystems)
            {
                // Find matching particle system in target preset
                WeatherParticleSystem toParticle = null;
                foreach (var p in toPreset.ParticleSystems)
                {
                    if (p.ParticleSystem == fromParticle.ParticleSystem)
                    {
                        toParticle = p;
                        break;
                    }
                }
                
                if (fromParticle.ParticleSystem != null)
                {
                    float emissionRate = toParticle != null 
                        ? Mathf.Lerp(fromParticle.EmissionRate, toParticle.EmissionRate, t) 
                        : fromParticle.EmissionRate * (1 - t);
                    
                    var emission = fromParticle.ParticleSystem.emission;
                    emission.rateOverTimeMultiplier = emissionRate;
                    
                    if (emissionRate > 0.01f)
                    {
                        if (!fromParticle.ParticleSystem.isPlaying)
                        {
                            fromParticle.ParticleSystem.Play();
                        }
                    }
                    else
                    {
                        fromParticle.ParticleSystem.Stop();
                    }
                }
            }
            
            // Start any new particle systems from target preset
            foreach (var toParticle in toPreset.ParticleSystems)
            {
                bool exists = false;
                foreach (var p in fromPreset.ParticleSystems)
                {
                    if (p.ParticleSystem == toParticle.ParticleSystem)
                    {
                        exists = true;
                        break;
                    }
                }
                
                if (!exists && toParticle.ParticleSystem != null && t > 0.5f)
                {
                    var emission = toParticle.ParticleSystem.emission;
                    emission.rateOverTimeMultiplier = toParticle.EmissionRate * (t - 0.5f) * 2f;
                    
                    if (!toParticle.ParticleSystem.isPlaying)
                    {
                        toParticle.ParticleSystem.Play();
                    }
                }
            }
            
            // Cross-fade ambient sounds
            if (ambientAudioSource != null)
            {
                if (t < 0.5f && fromPreset.AmbientSound != null)
                {
                    if (ambientAudioSource.clip != fromPreset.AmbientSound)
                    {
                        ambientAudioSource.clip = fromPreset.AmbientSound;
                        ambientAudioSource.Play();
                    }
                    ambientAudioSource.volume = fromPreset.AmbientSoundVolume * (1 - t * 2f);
                }
                else if (t >= 0.5f && toPreset.AmbientSound != null)
                {
                    if (ambientAudioSource.clip != toPreset.AmbientSound)
                    {
                        ambientAudioSource.clip = toPreset.AmbientSound;
                        ambientAudioSource.Play();
                    }
                    ambientAudioSource.volume = toPreset.AmbientSoundVolume * (t - 0.5f) * 2f;
                }
            }
        }
        
        private void UpdateWindEffects()
        {
            foreach (var obj in windAffectedObjects)
            {
                if (obj == null || obj.Transform == null) continue;
                
                // Calculate wind force based on object properties and current wind
                Vector3 windForce = windDirection * windStrength * obj.WindInfluence;
                
                // Apply wind force to rigidbody if available
                if (obj.Rigidbody != null)
                {
                    obj.Rigidbody.AddForce(windForce, ForceMode.Force);
                }
                
                // Apply wind to cloth if available
                if (obj.Cloth != null)
                {
                    obj.Cloth.externalAcceleration = windForce;
                }
                
                // Apply wind to particle systems
                if (obj.ParticleSystem != null)
                {
                    var velocityOverLifetime = obj.ParticleSystem.velocityOverLifetime;
                    if (velocityOverLifetime.enabled)
                    {
                        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(
                            velocityOverLifetime.x.constant + windForce.x);
                        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(
                            velocityOverLifetime.z.constant + windForce.z);
                    }
                }
                
                // Apply to vegetation (shader parameters, animation, etc.)
                // This would typically be done through a custom shader
            }
        }
        
        private void RegisterWindAffectedObjects()
        {
            // Clear the list
            windAffectedObjects.Clear();
            
            // Find all objects with the relevant components
            Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                // Skip if the object is too heavy or is kinematic
                if (rb.isKinematic || rb.mass > 50f) continue;
                
                // Add to wind affected objects
                windAffectedObjects.Add(new WindAffectedObject
                {
                    Transform = rb.transform,
                    Rigidbody = rb,
                    WindInfluence = 1f / rb.mass // Lighter objects are affected more
                });
            }
            
            // Find all cloth components
            Cloth[] cloths = FindObjectsOfType<Cloth>();
            foreach (var cloth in cloths)
            {
                windAffectedObjects.Add(new WindAffectedObject
                {
                    Transform = cloth.transform,
                    Cloth = cloth,
                    WindInfluence = 1f
                });
            }
            
            // Find particle systems that should be affected by wind
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                // Check if this particle system should be affected by wind
                // This could be based on tags, layers, or component names
                if (ps.gameObject.CompareTag("WindAffected"))
                {
                    windAffectedObjects.Add(new WindAffectedObject
                    {
                        Transform = ps.transform,
                        ParticleSystem = ps,
                        WindInfluence = 1f
                    });
                }
            }
        }
        
        // Getters for external systems
        public float GetTimeOfDay() => currentTimeOfDay;
        public bool IsNight() => currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;
        public WeatherPreset GetCurrentWeather() => currentWeatherPreset;
        public Vector3 GetWindDirection() => windDirection;
        public float GetWindStrength() => windStrength;
        
        // Control methods
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 0f, 100f);
        }
        
        public void SetTimeOfDay(float time)
        {
            currentTimeOfDay = Mathf.Clamp01(time);
            UpdateDayNightCycle();
        }
        
        public void ForceWeatherChange(WeatherPreset preset, float transitionTime = 10f)
        {
            if (preset == null) return;
            
            targetWeatherPreset = preset;
            weatherTransitionProgress = 0f;
            weatherTransitionTime = transitionTime;
            
            // Reset timer and set new duration
            currentWeatherTime = 0f;
            currentWeatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
            
            // Trigger event
            OnWeatherChanged?.Invoke(targetWeatherPreset);
        }
        
        // Create environmental debris at impact point
        public void CreateDebrisAtPoint(Vector3 position, Vector3 normal, Material material = null)
        {
            // Instantiate debris prefab based on material
            // This would be implemented with a lookup system matching materials to debris prefabs
            
            // Play impact sound based on material
            
            // Create particle effect based on material
        }
    }
    
    [System.Serializable]
    public class WindAffectedObject
    {
        public Transform Transform;
        public Rigidbody Rigidbody;
        public Cloth Cloth;
        public ParticleSystem ParticleSystem;
        public float WindInfluence = 1f;
    }
    
    [System.Serializable]
    public class WeatherParticleSystem
    {
        public ParticleSystem ParticleSystem;
        public float EmissionRate = 100f;
    }
    
    [CreateAssetMenu(fileName = "NewWeatherPreset", menuName = "FPS/Weather Preset", order = 3)]
    public class WeatherPreset : ScriptableObject
    {
        [Header("Identification")]
        public string WeatherName = "Clear";
        public Sprite WeatherIcon;
        
        [Header("Visual Settings")]
        public bool EnableFog = false;
        public FogMode FogMode = FogMode.ExponentialSquared;
        public float FogDensity = 0.01f;
        public List<WeatherParticleSystem> ParticleSystems = new List<WeatherParticleSystem>();
        
        [Header("Lighting Settings")]
        public float SunIntensityMultiplier = 1f;
        public float AmbientIntensityMultiplier = 1f;
        
        [Header("Audio Settings")]
        public AudioClip AmbientSound;
        public float AmbientSoundVolume = 1f;
        
        [Header("Physics Settings")]
        public float WindStrength = 1f;
        public Vector3 WindDirection = Vector3.right;
        public float PrecipitationIntensity = 0f;
        
        [Header("Gameplay Impact")]
        public float VisibilityRange = 100f;
        public float MovementSpeedMultiplier = 1f;
        public float WeaponAccuracyMultiplier = 1f;
    }
}
