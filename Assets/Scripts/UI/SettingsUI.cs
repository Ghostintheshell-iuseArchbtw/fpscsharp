using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FPS.Managers;

namespace FPS.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private TextMeshProUGUI fovValueText;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        [SerializeField] private TextMeshProUGUI uiVolumeText;
        
        [Header("Gameplay Settings")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TextMeshProUGUI mouseSensitivityText;
        [SerializeField] private Toggle invertMouseYToggle;
        [SerializeField] private Slider aimSensitivitySlider;
        [SerializeField] private TextMeshProUGUI aimSensitivityText;
        [SerializeField] private Toggle toggleAimToggle;
        [SerializeField] private Toggle showCrosshairToggle;
        [SerializeField] private Toggle showHitMarkersToggle;
        [SerializeField] private Toggle autoReloadToggle;
        
        [Header("Accessibility Settings")]
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Slider subtitleSizeSlider;
        [SerializeField] private Toggle colorBlindAssistToggle;
        [SerializeField] private Toggle reduceMotionToggle;
        
        [Header("Control Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button defaultsButton;
        
        [Header("Localization Settings")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        
        private SettingsManager settingsManager;
        private GameSettings currentSettings;
        private bool isInitializing = false;
        
        private void Start()
        {
            settingsManager = SettingsManager.Instance;
            if (settingsManager != null)
            {
                currentSettings = settingsManager.GetSettings();
                InitializeUI();
                SetupListeners();
            }
        }
        
        private void InitializeUI()
        {
            isInitializing = true;
            
            InitializeGraphicsUI();
            InitializeAudioUI();
            InitializeGameplayUI();
            InitializeAccessibilityUI();
            InitializeLocalizationUI();
            
            isInitializing = false;
        }
        
        private void InitializeGraphicsUI()
        {
            // Quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.options.Clear();
                string[] qualityNames = settingsManager.GetQualityLevels();
                foreach (string qualityName in qualityNames)
                {
                    qualityDropdown.options.Add(new TMP_Dropdown.OptionData(qualityName));
                }
                qualityDropdown.value = currentSettings.qualityLevel;
                qualityDropdown.RefreshShownValue();
            }
            
            // Resolution dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.options.Clear();
                Resolution[] resolutions = settingsManager.GetAvailableResolutions();
                for (int i = 0; i < resolutions.Length; i++)
                {
                    Resolution res = resolutions[i];
                    resolutionDropdown.options.Add(new TMP_Dropdown.OptionData($"{res.width}x{res.height} {res.refreshRate}Hz"));
                }
                resolutionDropdown.value = currentSettings.resolutionIndex;
                resolutionDropdown.RefreshShownValue();
            }
            
            // Other graphics settings
            if (fullscreenToggle != null) fullscreenToggle.isOn = currentSettings.fullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = currentSettings.vsync;
            if (brightnessSlider != null) brightnessSlider.value = currentSettings.brightness;
            if (fovSlider != null)
            {
                fovSlider.value = currentSettings.fieldOfView;
                UpdateFOVText(currentSettings.fieldOfView);
            }
        }
        
        private void InitializeAudioUI()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = currentSettings.masterVolume;
                UpdateMasterVolumeText(currentSettings.masterVolume);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = currentSettings.musicVolume;
                UpdateMusicVolumeText(currentSettings.musicVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = currentSettings.sfxVolume;
                UpdateSFXVolumeText(currentSettings.sfxVolume);
            }
            
            if (uiVolumeSlider != null)
            {
                uiVolumeSlider.value = currentSettings.uiVolume;
                UpdateUIVolumeText(currentSettings.uiVolume);
            }
        }
        
        private void InitializeGameplayUI()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = currentSettings.mouseSensitivity;
                UpdateMouseSensitivityText(currentSettings.mouseSensitivity);
            }
            
            if (invertMouseYToggle != null) invertMouseYToggle.isOn = currentSettings.invertMouseY;
            
            if (aimSensitivitySlider != null)
            {
                aimSensitivitySlider.value = currentSettings.aimSensitivityMultiplier;
                UpdateAimSensitivityText(currentSettings.aimSensitivityMultiplier);
            }
            
            if (toggleAimToggle != null) toggleAimToggle.isOn = currentSettings.toggleAim;
            if (showCrosshairToggle != null) showCrosshairToggle.isOn = currentSettings.showCrosshair;
            if (showHitMarkersToggle != null) showHitMarkersToggle.isOn = currentSettings.showHitMarkers;
            if (autoReloadToggle != null) autoReloadToggle.isOn = currentSettings.autoReload;
        }
        
        private void InitializeAccessibilityUI()
        {
            if (subtitlesToggle != null) subtitlesToggle.isOn = currentSettings.subtitles;
            if (subtitleSizeSlider != null) subtitleSizeSlider.value = currentSettings.subtitleSize;
            if (colorBlindAssistToggle != null) colorBlindAssistToggle.isOn = currentSettings.colorBlindAssist;
            if (reduceMotionToggle != null) reduceMotionToggle.isOn = currentSettings.reduceMotion;
        }
        
        private void InitializeLocalizationUI()
        {
            if (languageDropdown != null && FPS.Managers.LocalizationManager.Instance != null)
            {
                var languages = FPS.Managers.LocalizationManager.Instance.GetSupportedLanguages();
                languageDropdown.options.Clear();
                int selectedIndex = 0;
                string currentLang = FPS.Managers.LocalizationManager.Instance.GetCurrentLanguage();
                for (int i = 0; i < languages.Count; i++)
                {
                    var lang = languages[i];
                    languageDropdown.options.Add(new TMP_Dropdown.OptionData(lang.displayName));
                    if (lang.languageCode == currentLang)
                        selectedIndex = i;
                }
                languageDropdown.value = selectedIndex;
                languageDropdown.RefreshShownValue();
            }
        }
        
        private void SetupListeners()
        {
            // Graphics listeners
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            
            if (brightnessSlider != null)
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            
            if (fovSlider != null)
                fovSlider.onValueChanged.AddListener(OnFOVChanged);
            
            // Audio listeners
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            if (uiVolumeSlider != null)
                uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
            
            // Gameplay listeners
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            
            if (invertMouseYToggle != null)
                invertMouseYToggle.onValueChanged.AddListener(OnInvertMouseYChanged);
            
            if (aimSensitivitySlider != null)
                aimSensitivitySlider.onValueChanged.AddListener(OnAimSensitivityChanged);
            
            if (toggleAimToggle != null)
                toggleAimToggle.onValueChanged.AddListener(OnToggleAimChanged);
            
            if (showCrosshairToggle != null)
                showCrosshairToggle.onValueChanged.AddListener(OnShowCrosshairChanged);
            
            if (showHitMarkersToggle != null)
                showHitMarkersToggle.onValueChanged.AddListener(OnShowHitMarkersChanged);
            
            if (autoReloadToggle != null)
                autoReloadToggle.onValueChanged.AddListener(OnAutoReloadChanged);
            
            // Accessibility listeners
            if (subtitlesToggle != null)
                subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
            
            if (subtitleSizeSlider != null)
                subtitleSizeSlider.onValueChanged.AddListener(OnSubtitleSizeChanged);
            
            if (colorBlindAssistToggle != null)
                colorBlindAssistToggle.onValueChanged.AddListener(OnColorBlindAssistChanged);
            
            if (reduceMotionToggle != null)
                reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
            
            // Localization listeners
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            
            // Button listeners
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSettings);
            
            if (defaultsButton != null)
                defaultsButton.onClick.AddListener(ResetToDefaults);
        }
        
        #region Setting Change Handlers
        
        private void OnQualityChanged(int value)
        {
            if (isInitializing) return;
            settingsManager.SetQualityLevel(value);
        }
        
        private void OnResolutionChanged(int value)
        {
            if (isInitializing) return;
            settingsManager.SetResolution(value);
        }
        
        private void OnFullscreenChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetFullscreen(value);
        }
        
        private void OnVSyncChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetVSync(value);
        }
        
        private void OnBrightnessChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetBrightness(value);
        }
        
        private void OnFOVChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetFieldOfView(value);
            UpdateFOVText(value);
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetMasterVolume(value);
            UpdateMasterVolumeText(value);
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetMusicVolume(value);
            UpdateMusicVolumeText(value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetSFXVolume(value);
            UpdateSFXVolumeText(value);
        }
        
        private void OnUIVolumeChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetUIVolume(value);
            UpdateUIVolumeText(value);
        }
        
        private void OnMouseSensitivityChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetMouseSensitivity(value);
            UpdateMouseSensitivityText(value);
        }
        
        private void OnInvertMouseYChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetInvertMouseY(value);
        }
        
        private void OnAimSensitivityChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetAimSensitivityMultiplier(value);
            UpdateAimSensitivityText(value);
        }
        
        private void OnToggleAimChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetToggleAim(value);
        }
        
        private void OnShowCrosshairChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetShowCrosshair(value);
        }
        
        private void OnShowHitMarkersChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetShowHitMarkers(value);
        }
        
        private void OnAutoReloadChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetAutoReload(value);
        }
        
        private void OnSubtitlesChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetSubtitles(value);
        }
        
        private void OnSubtitleSizeChanged(float value)
        {
            if (isInitializing) return;
            settingsManager.SetSubtitleSize(value);
        }
        
        private void OnColorBlindAssistChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetColorBlindAssist(value);
        }
        
        private void OnReduceMotionChanged(bool value)
        {
            if (isInitializing) return;
            settingsManager.SetReduceMotion(value);
        }
        
        private void OnLanguageChanged(int index)
        {
            if (isInitializing) return;
            if (FPS.Managers.LocalizationManager.Instance == null) return;
            var languages = FPS.Managers.LocalizationManager.Instance.GetSupportedLanguages();
            if (index >= 0 && index < languages.Count)
            {
                FPS.Managers.LocalizationManager.Instance.SetLanguage(languages[index].languageCode);
            }
        }
        
        #endregion
        
        #region Text Update Methods
        
        private void UpdateFOVText(float value)
        {
            if (fovValueText != null)
                fovValueText.text = $"{value:F0}°";
        }
        
        private void UpdateMasterVolumeText(float value)
        {
            if (masterVolumeText != null)
                masterVolumeText.text = $"{value * 100:F0}%";
        }
        
        private void UpdateMusicVolumeText(float value)
        {
            if (musicVolumeText != null)
                musicVolumeText.text = $"{value * 100:F0}%";
        }
        
        private void UpdateSFXVolumeText(float value)
        {
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{value * 100:F0}%";
        }
        
        private void UpdateUIVolumeText(float value)
        {
            if (uiVolumeText != null)
                uiVolumeText.text = $"{value * 100:F0}%";
        }
        
        private void UpdateMouseSensitivityText(float value)
        {
            if (mouseSensitivityText != null)
                mouseSensitivityText.text = $"{value:F1}";
        }
        
        private void UpdateAimSensitivityText(float value)
        {
            if (aimSensitivityText != null)
                aimSensitivityText.text = $"{value:F1}x";
        }
        
        #endregion
        
        #region Button Actions
        
        private void ApplySettings()
        {
            if (settingsManager != null)
            {
                settingsManager.SaveSettings();
            }
        }
        
        private void ResetSettings()
        {
            if (settingsManager != null)
            {
                currentSettings = settingsManager.GetSettings();
                InitializeUI();
            }
        }
        
        private void ResetToDefaults()
        {
            if (settingsManager != null)
            {
                settingsManager.ResetToDefaults();
                currentSettings = settingsManager.GetSettings();
                InitializeUI();
            }
        }
        
        #endregion
        
        public void RefreshUI()
        {
            if (settingsManager != null)
            {
                currentSettings = settingsManager.GetSettings();
                InitializeUI();
            }
        }
    }
}
