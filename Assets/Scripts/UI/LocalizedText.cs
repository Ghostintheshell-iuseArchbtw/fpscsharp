using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace FPS.UI
{
    /// <summary>
    /// Component that automatically localizes text content based on the current language
    /// Supports both Legacy Text and TextMeshPro components
    /// </summary>
    [RequireComponent(typeof(Text), typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization Settings")]
        [SerializeField] private string localizationKey;
        [SerializeField] private bool useFormattedText = false;
        [SerializeField] private string[] formatArguments;
        [SerializeField] private bool updateFontOnLanguageChange = true;
        
        [Header("Text Components")]
        [SerializeField] private Text legacyText;
        [SerializeField] private TextMeshProUGUI textMeshPro;
        
        [Header("Dynamic Values")]
        [SerializeField] private bool useDynamicValues = false;
        [SerializeField] private MonoBehaviour[] dynamicValueProviders;
        
        // Events
        public event Action<string> OnTextUpdated;
        
        // Private fields
        private string originalKey;
        private bool isInitialized = false;
        
        private void Awake()
        {
            InitializeComponents();
            originalKey = localizationKey;
        }
        
        private void Start()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
                UpdateText();
                isInitialized = true;
            }
            else
            {
                Debug.LogWarning($"LocalizationManager not found for {gameObject.name}");
            }
        }
        
        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }
        
        private void InitializeComponents()
        {
            if (legacyText == null)
                legacyText = GetComponent<Text>();
            
            if (textMeshPro == null)
                textMeshPro = GetComponent<TextMeshProUGUI>();
            
            if (legacyText == null && textMeshPro == null)
            {
                Debug.LogError($"No Text or TextMeshProUGUI component found on {gameObject.name}");
            }
        }
        
        private void OnLanguageChanged(string newLanguage)
        {
            if (isInitialized)
            {
                UpdateText();
                
                if (updateFontOnLanguageChange)
                {
                    UpdateFont();
                }
            }
        }
        
        /// <summary>
        /// Updates the displayed text with localized content
        /// </summary>
        public void UpdateText()
        {
            if (string.IsNullOrEmpty(localizationKey))
            {
                Debug.LogWarning($"Localization key is empty for {gameObject.name}");
                return;
            }
            
            string localizedText = GetLocalizedText();
            SetText(localizedText);
            OnTextUpdated?.Invoke(localizedText);
        }
        
        /// <summary>
        /// Sets a new localization key and updates the text
        /// </summary>
        /// <param name="newKey">The new localization key</param>
        public void SetLocalizationKey(string newKey)
        {
            localizationKey = newKey;
            UpdateText();
        }
        
        /// <summary>
        /// Sets format arguments for formatted text and updates
        /// </summary>
        /// <param name="args">Format arguments</param>
        public void SetFormatArguments(params string[] args)
        {
            formatArguments = args;
            useFormattedText = args != null && args.Length > 0;
            UpdateText();
        }
        
        /// <summary>
        /// Resets to the original localization key
        /// </summary>
        public void ResetToOriginalKey()
        {
            localizationKey = originalKey;
            UpdateText();
        }
        
        /// <summary>
        /// Enables or disables dynamic value updates
        /// </summary>
        /// <param name="enabled">Whether to enable dynamic updates</param>
        public void SetDynamicUpdates(bool enabled)
        {
            useDynamicValues = enabled;
            
            if (enabled)
            {
                StartDynamicUpdates();
            }
            else
            {
                StopDynamicUpdates();
            }
        }
        
        private string GetLocalizedText()
        {
            if (LocalizationManager.Instance == null)
                return localizationKey;
            
            string baseText = LocalizationManager.Instance.GetLocalizedString(localizationKey);
            
            if (useFormattedText && formatArguments != null && formatArguments.Length > 0)
            {
                try
                {
                    string[] processedArgs = ProcessFormatArguments();
                    return string.Format(baseText, processedArgs);
                }
                catch (FormatException e)
                {
                    Debug.LogError($"Format error in LocalizedText {gameObject.name}: {e.Message}");
                    return baseText;
                }
            }
            
            return baseText;
        }
        
        private string[] ProcessFormatArguments()
        {
            string[] processedArgs = new string[formatArguments.Length];
            
            for (int i = 0; i < formatArguments.Length; i++)
            {
                string arg = formatArguments[i];
                
                // Check if argument is a localization key (starts with @)
                if (arg.StartsWith("@"))
                {
                    string keyToLocalize = arg.Substring(1);
                    processedArgs[i] = LocalizationManager.Instance.GetLocalizedString(keyToLocalize);
                }
                // Check if argument is a dynamic value (starts with $)
                else if (arg.StartsWith("$") && useDynamicValues)
                {
                    processedArgs[i] = GetDynamicValue(arg.Substring(1));
                }
                else
                {
                    processedArgs[i] = arg;
                }
            }
            
            return processedArgs;
        }
        
        private string GetDynamicValue(string valueName)
        {
            if (dynamicValueProviders == null)
                return valueName;
            
            foreach (var provider in dynamicValueProviders)
            {
                if (provider == null) continue;
                
                // Try to get value through reflection or predefined methods
                var valueProperty = provider.GetType().GetProperty(valueName);
                if (valueProperty != null)
                {
                    var value = valueProperty.GetValue(provider);
                    return value?.ToString() ?? "N/A";
                }
                
                var valueField = provider.GetType().GetField(valueName);
                if (valueField != null)
                {
                    var value = valueField.GetValue(provider);
                    return value?.ToString() ?? "N/A";
                }
            }
            
            return valueName;
        }
        
        private void SetText(string text)
        {
            if (textMeshPro != null)
            {
                textMeshPro.text = text;
            }
            else if (legacyText != null)
            {
                legacyText.text = text;
            }
        }
        
        private void UpdateFont()
        {
            if (LocalizationManager.Instance == null)
                return;
            
            var currentLanguage = LocalizationManager.Instance.GetCurrentLanguage();
            var fontAsset = LocalizationManager.Instance.GetFontForLanguage(currentLanguage);
            
            if (fontAsset != null && textMeshPro != null)
            {
                textMeshPro.font = fontAsset;
            }
        }
        
        private void StartDynamicUpdates()
        {
            if (useDynamicValues)
            {
                InvokeRepeating(nameof(UpdateDynamicText), 0.1f, 0.1f);
            }
        }
        
        private void StopDynamicUpdates()
        {
            CancelInvoke(nameof(UpdateDynamicText));
        }
        
        private void UpdateDynamicText()
        {
            if (useFormattedText && useDynamicValues)
            {
                UpdateText();
            }
        }
        
        /// <summary>
        /// Get the current localization key
        /// </summary>
        public string GetLocalizationKey()
        {
            return localizationKey;
        }
        
        /// <summary>
        /// Get the current displayed text
        /// </summary>
        public string GetCurrentText()
        {
            if (textMeshPro != null)
                return textMeshPro.text;
            else if (legacyText != null)
                return legacyText.text;
            
            return string.Empty;
        }
        
        /// <summary>
        /// Check if the component is using formatted text
        /// </summary>
        public bool IsUsingFormattedText()
        {
            return useFormattedText;
        }
        
        /// <summary>
        /// Manually trigger a text update (useful for editor scripts)
        /// </summary>
        [ContextMenu("Update Text")]
        public void ManualUpdateText()
        {
            UpdateText();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                UpdateText();
            }
        }
#endif
    }
}
