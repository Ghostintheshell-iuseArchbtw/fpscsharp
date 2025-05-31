using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

namespace FPS.Managers
{
    [System.Serializable]
    public class GameSettings
    {
        [Header("Graphics Settings")]
        public int qualityLevel = 2;
        public int resolutionIndex = 0;
        public bool fullscreen = true;
        public bool vsync = true;
        public float brightness = 1f;
        public float fieldOfView = 90f;
        
        [Header("Audio Settings")]
        public float masterVolume = 0.8f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float uiVolume = 0.6f;
        
        [Header("Gameplay Settings")]
        public float mouseSensitivity = 2f;
        public bool invertMouseY = false;
        public float aimSensitivityMultiplier = 0.5f;
        public bool toggleAim = false;
        public bool showCrosshair = true;
        public bool showHitMarkers = true;
        public bool autoReload = true;
        
        [Header("Accessibility")]
        public bool subtitles = false;
        public float subtitleSize = 1f;
        public bool colorBlindAssist = false;
        public bool reduceMotion = false;
    }
    
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }
        
        [Header("Audio Mixers")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        
        [Header("Graphics References")]
        [SerializeField] private Camera playerCamera;
        
        [Header("Settings File")]
        [SerializeField] private string settingsFileName = "GameSettings.json";
        
        private GameSettings currentSettings;
        private Resolution[] availableResolutions;
        private string settingsPath;
        
        // Events
        public event System.Action<GameSettings> OnSettingsChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                settingsPath = System.IO.Path.Combine(Application.persistentDataPath, settingsFileName);
                InitializeSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Get available resolutions
            availableResolutions = Screen.resolutions;
            
            // Apply loaded settings
            ApplyAllSettings();
        }
        
        private void InitializeSettings()
        {
            // Try to load existing settings
            if (System.IO.File.Exists(settingsPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(settingsPath);
                    currentSettings = JsonUtility.FromJson<GameSettings>(json);
                    Debug.Log("Settings loaded successfully");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load settings: {e.Message}. Using defaults.");
                    currentSettings = new GameSettings();
                }
            }
            else
            {
                // Create default settings
                currentSettings = new GameSettings();
                SaveSettings();
            }
        }
        
        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                System.IO.File.WriteAllText(settingsPath, json);
                Debug.Log("Settings saved successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }
        
        public GameSettings GetSettings()
        {
            return currentSettings;
        }
        
        public void ApplyAllSettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyGameplaySettings();
            
            OnSettingsChanged?.Invoke(currentSettings);
        }
        
        #region Graphics Settings
        
        public void SetQualityLevel(int qualityLevel)
        {
            currentSettings.qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        }
        
        public void SetResolution(int resolutionIndex)
        {
            if (availableResolutions != null && resolutionIndex >= 0 && resolutionIndex < availableResolutions.Length)
            {
                currentSettings.resolutionIndex = resolutionIndex;
                Resolution resolution = availableResolutions[resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, currentSettings.fullscreen);
            }
        }
        
        public void SetFullscreen(bool fullscreen)
        {
            currentSettings.fullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        public void SetVSync(bool vsync)
        {
            currentSettings.vsync = vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }
        
        public void SetBrightness(float brightness)
        {
            currentSettings.brightness = Mathf.Clamp01(brightness);
            // Apply brightness to post-processing or screen overlay
            ApplyBrightness();
        }
        
        public void SetFieldOfView(float fov)
        {
            currentSettings.fieldOfView = Mathf.Clamp(fov, 60f, 120f);
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentSettings.fieldOfView;
            }
        }
        
        private void ApplyGraphicsSettings()
        {
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
            
            if (availableResolutions != null && currentSettings.resolutionIndex < availableResolutions.Length)
            {
                Resolution resolution = availableResolutions[currentSettings.resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, currentSettings.fullscreen);
            }
            
            Screen.fullScreen = currentSettings.fullscreen;
            QualitySettings.vSyncCount = currentSettings.vsync ? 1 : 0;
            
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentSettings.fieldOfView;
            }
            
            ApplyBrightness();
        }
        
        private void ApplyBrightness()
        {
            // This would typically be handled by post-processing
            // For now, we'll adjust the ambient light
            RenderSettings.ambientIntensity = currentSettings.brightness;
        }
        
        #endregion
        
        #region Audio Settings
        
        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            ApplyMasterVolume();
        }
        
        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();
        }
        
        public void SetSFXVolume(float volume)
        {
            currentSettings.sfxVolume = Mathf.Clamp01(volume);
            ApplySFXVolume();
        }
        
        public void SetUIVolume(float volume)
        {
            currentSettings.uiVolume = Mathf.Clamp01(volume);
            ApplyUIVolume();
        }
        
        private void ApplyAudioSettings()
        {
            ApplyMasterVolume();
            ApplyMusicVolume();
            ApplySFXVolume();
            ApplyUIVolume();
        }
        
        private void ApplyMasterVolume()
        {
            if (masterMixer != null)
            {
                float db = currentSettings.masterVolume > 0 ? Mathf.Log10(currentSettings.masterVolume) * 20 : -80f;
                masterMixer.SetFloat("MasterVolume", db);
            }
        }
        
        private void ApplyMusicVolume()
        {
            if (masterMixer != null)
            {
                float db = currentSettings.musicVolume > 0 ? Mathf.Log10(currentSettings.musicVolume) * 20 : -80f;
                masterMixer.SetFloat("MusicVolume", db);
            }
        }
        
        private void ApplySFXVolume()
        {
            if (masterMixer != null)
            {
                float db = currentSettings.sfxVolume > 0 ? Mathf.Log10(currentSettings.sfxVolume) * 20 : -80f;
                masterMixer.SetFloat("SFXVolume", db);
            }
        }
        
        private void ApplyUIVolume()
        {
            if (masterMixer != null)
            {
                float db = currentSettings.uiVolume > 0 ? Mathf.Log10(currentSettings.uiVolume) * 20 : -80f;
                masterMixer.SetFloat("UIVolume", db);
            }
        }
        
        #endregion
        
        #region Gameplay Settings
        
        public void SetMouseSensitivity(float sensitivity)
        {
            currentSettings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            ApplyMouseSensitivity();
        }
        
        public void SetInvertMouseY(bool invert)
        {
            currentSettings.invertMouseY = invert;
            ApplyMouseSettings();
        }
        
        public void SetAimSensitivityMultiplier(float multiplier)
        {
            currentSettings.aimSensitivityMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
            ApplyMouseSensitivity();
        }
        
        public void SetToggleAim(bool toggle)
        {
            currentSettings.toggleAim = toggle;
        }
        
        public void SetShowCrosshair(bool show)
        {
            currentSettings.showCrosshair = show;
            ApplyCrosshairSettings();
        }
        
        public void SetShowHitMarkers(bool show)
        {
            currentSettings.showHitMarkers = show;
        }
        
        public void SetAutoReload(bool autoReload)
        {
            currentSettings.autoReload = autoReload;
        }
        
        private void ApplyGameplaySettings()
        {
            ApplyMouseSensitivity();
            ApplyMouseSettings();
            ApplyCrosshairSettings();
        }
        
        private void ApplyMouseSensitivity()
        {
            // Apply to player controller
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMouseSensitivity(currentSettings.mouseSensitivity);
                playerController.SetAimSensitivityMultiplier(currentSettings.aimSensitivityMultiplier);
            }
        }
        
        private void ApplyMouseSettings()
        {
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetInvertMouseY(currentSettings.invertMouseY);
            }
        }
        
        private void ApplyCrosshairSettings()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.SetCrosshairVisible(currentSettings.showCrosshair);
            }
        }
        
        #endregion
        
        #region Accessibility Settings
        
        public void SetSubtitles(bool enabled)
        {
            currentSettings.subtitles = enabled;
        }
        
        public void SetSubtitleSize(float size)
        {
            currentSettings.subtitleSize = Mathf.Clamp(size, 0.5f, 2f);
        }
        
        public void SetColorBlindAssist(bool enabled)
        {
            currentSettings.colorBlindAssist = enabled;
            // Apply colorblind-friendly color schemes
        }
        
        public void SetReduceMotion(bool enabled)
        {
            currentSettings.reduceMotion = enabled;
            // Reduce camera shake, screen effects, etc.
        }
        
        #endregion
        
        #region Utility Methods
        
        public Resolution[] GetAvailableResolutions()
        {
            return availableResolutions;
        }
        
        public string[] GetQualityLevels()
        {
            return QualitySettings.names;
        }
        
        public void ResetToDefaults()
        {
            currentSettings = new GameSettings();
            ApplyAllSettings();
            SaveSettings();
        }
        
        #endregion
    }
}
