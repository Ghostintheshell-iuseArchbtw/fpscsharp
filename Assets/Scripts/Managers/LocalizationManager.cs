using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using TMPro;

namespace FPS.Managers
{
    [System.Serializable]
    public class LocalizationData
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
        
        public string GetText(string key)
        {
            var entry = entries.Find(e => e.key == key);
            return entry != null ? entry.value : key;
        }
    }
    
    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }
    
    [System.Serializable]
    public class LanguageInfo
    {
        public string languageCode;
        public string displayName;
        public string nativeName;
        public bool isRightToLeft;
        public string fontAssetName; // Optional custom font for this language
    }
    
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        
        [Header("Language Settings")]
        [SerializeField] private List<LanguageInfo> supportedLanguages = new List<LanguageInfo>
        {
            new LanguageInfo { languageCode = "en", displayName = "English", nativeName = "English", isRightToLeft = false },
            new LanguageInfo { languageCode = "es", displayName = "Spanish", nativeName = "Español", isRightToLeft = false },
            new LanguageInfo { languageCode = "fr", displayName = "French", nativeName = "Français", isRightToLeft = false },
            new LanguageInfo { languageCode = "de", displayName = "German", nativeName = "Deutsch", isRightToLeft = false },
            new LanguageInfo { languageCode = "ja", displayName = "Japanese", nativeName = "日本語", isRightToLeft = false },
            new LanguageInfo { languageCode = "ko", displayName = "Korean", nativeName = "한국어", isRightToLeft = false },
            new LanguageInfo { languageCode = "zh", displayName = "Chinese", nativeName = "中文", isRightToLeft = false },
            new LanguageInfo { languageCode = "ru", displayName = "Russian", nativeName = "Русский", isRightToLeft = false },
            new LanguageInfo { languageCode = "ar", displayName = "Arabic", nativeName = "العربية", isRightToLeft = true },
            new LanguageInfo { languageCode = "pt", displayName = "Portuguese", nativeName = "Português", isRightToLeft = false }
        };
        
        [Header("Default Settings")]
        [SerializeField] private string defaultLanguage = "en";
        [SerializeField] private bool autoDetectSystemLanguage = true;
        [SerializeField] private string localizationPath = "Localization";
        [SerializeField] private bool enablePluralSupport = true;
        
        [Header("Font Assets")]
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private List<LanguageFontPair> languageFonts = new List<LanguageFontPair>();
        
        // Current language state
        private string currentLanguage;
        private LocalizationData currentLocalizationData;
        private Dictionary<string, LocalizationData> loadedLanguages = new Dictionary<string, LocalizationData>();
        private List<LocalizedText> registeredTexts = new List<LocalizedText>();
        
        // Events
        public UnityEvent<string> OnLanguageChanged;
        
        // Caching for performance
        private Dictionary<string, string> textCache = new Dictionary<string, string>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLocalization();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeLocalization()
        {
            // Load saved language preference or auto-detect
            string savedLanguage = PlayerPrefs.GetString("Language", "");
            
            if (string.IsNullOrEmpty(savedLanguage) && autoDetectSystemLanguage)
            {
                savedLanguage = DetectSystemLanguage();
            }
            
            if (string.IsNullOrEmpty(savedLanguage) || !IsLanguageSupported(savedLanguage))
            {
                savedLanguage = defaultLanguage;
            }
            
            SetLanguage(savedLanguage);
        }
        
        private string DetectSystemLanguage()
        {
            SystemLanguage systemLang = Application.systemLanguage;
            
            switch (systemLang)
            {
                case SystemLanguage.English:
                    return "en";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.Korean:
                    return "ko";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh";
                case SystemLanguage.Russian:
                    return "ru";
                case SystemLanguage.Arabic:
                    return "ar";
                case SystemLanguage.Portuguese:
                    return "pt";
                default:
                    return defaultLanguage;
            }
        }
        
        public void SetLanguage(string languageCode)
        {
            if (!IsLanguageSupported(languageCode))
            {
                Debug.LogWarning($"Language '{languageCode}' is not supported. Using default language.");
                languageCode = defaultLanguage;
            }
            
            currentLanguage = languageCode;
            
            // Load language data
            LoadLanguageData(languageCode);
            
            // Save preference
            PlayerPrefs.SetString("Language", languageCode);
            PlayerPrefs.Save();
            
            // Clear cache
            textCache.Clear();
            
            // Update all registered texts
            UpdateAllLocalizedTexts();
            
            // Trigger event
            OnLanguageChanged?.Invoke(languageCode);
            
            Debug.Log($"Language changed to: {GetLanguageDisplayName(languageCode)}");
        }
        
        private void LoadLanguageData(string languageCode)
        {
            // Check if already loaded
            if (loadedLanguages.ContainsKey(languageCode))
            {
                currentLocalizationData = loadedLanguages[languageCode];
                return;
            }
            
            // Try to load from Resources
            TextAsset localizationFile = Resources.Load<TextAsset>($"{localizationPath}/{languageCode}");
            
            if (localizationFile != null)
            {
                try
                {
                    currentLocalizationData = JsonUtility.FromJson<LocalizationData>(localizationFile.text);
                    loadedLanguages[languageCode] = currentLocalizationData;
                    Debug.Log($"Loaded localization data for {languageCode}: {currentLocalizationData.entries.Count} entries");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse localization file for {languageCode}: {e.Message}");
                    CreateDefaultLocalizationData(languageCode);
                }
            }
            else
            {
                Debug.LogWarning($"Localization file not found for {languageCode}. Creating default data.");
                CreateDefaultLocalizationData(languageCode);
            }
        }
        
        private void CreateDefaultLocalizationData(string languageCode)
        {
            currentLocalizationData = new LocalizationData();
            
            // Add some default entries
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.menu.start", value = "Start Game" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.menu.continue", value = "Continue" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.menu.settings", value = "Settings" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.menu.quit", value = "Quit" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.hud.health", value = "Health" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.hud.ammo", value = "Ammo" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.survival.hunger", value = "Hunger" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.survival.thirst", value = "Thirst" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.survival.temperature", value = "Temperature" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.survival.radiation", value = "Radiation" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.survival.stamina", value = "Stamina" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.inventory.title", value = "Inventory" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.crafting.title", value = "Crafting" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.interaction.pickup", value = "Press [E] to pick up" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.notification.checkpoint", value = "Checkpoint reached!" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.notification.autosave", value = "Game auto-saved" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.objective.eliminate_enemies", value = "Eliminate all enemies" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.objective.find_exit", value = "Find the exit" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.objective.collect_item", value = "Collect the item" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.gameover.title", value = "Game Over" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.gameover.restart", value = "Restart" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.levelcomplete.title", value = "Level Complete!" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.levelcomplete.next", value = "Next Level" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.pause.resume", value = "Resume" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "ui.pause.mainmenu", value = "Main Menu" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "tutorial.movement", value = "Use WASD to move" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "tutorial.look", value = "Move mouse to look around" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "tutorial.shoot", value = "Left click to shoot" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "tutorial.reload", value = "Press R to reload" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "loading.tip.cover", value = "Use cover to avoid enemy fire" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "loading.tip.hunger", value = "Check your hunger and thirst levels regularly" });
            currentLocalizationData.entries.Add(new LocalizationEntry { key = "loading.tip.crafting", value = "Craft better weapons to increase your survival chances" });
            
            loadedLanguages[languageCode] = currentLocalizationData;
        }
        
        public string GetText(string key, params object[] args)
        {
            // Check cache first
            string cacheKey = key + (args.Length > 0 ? string.Join("_", args) : "");
            if (textCache.ContainsKey(cacheKey))
            {
                return textCache[cacheKey];
            }
            
            string text = currentLocalizationData?.GetText(key) ?? key;
            
            // Format with arguments if provided
            if (args.Length > 0)
            {
                try
                {
                    text = string.Format(text, args);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to format localized text '{key}': {e.Message}");
                }
            }
            
            // Cache the result
            textCache[cacheKey] = text;
            
            return text;
        }
        
        public string GetPluralText(string key, int count, params object[] args)
        {
            if (!enablePluralSupport)
            {
                return GetText(key, args);
            }
            
            // Look for plural forms
            string pluralKey = count == 1 ? key + ".singular" : key + ".plural";
            string text = currentLocalizationData?.GetText(pluralKey);
            
            if (string.IsNullOrEmpty(text) || text == pluralKey)
            {
                // Fallback to regular key
                text = GetText(key, args);
            }
            else if (args.Length > 0)
            {
                try
                {
                    text = string.Format(text, args);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to format plural text '{pluralKey}': {e.Message}");
                }
            }
            
            return text;
        }
        
        public void RegisterLocalizedText(LocalizedText localizedText)
        {
            if (!registeredTexts.Contains(localizedText))
            {
                registeredTexts.Add(localizedText);
            }
        }
        
        public void UnregisterLocalizedText(LocalizedText localizedText)
        {
            registeredTexts.Remove(localizedText);
        }
        
        private void UpdateAllLocalizedTexts()
        {
            for (int i = registeredTexts.Count - 1; i >= 0; i--)
            {
                if (registeredTexts[i] != null)
                {
                    registeredTexts[i].UpdateText();
                }
                else
                {
                    registeredTexts.RemoveAt(i);
                }
            }
        }
        
        public TMP_FontAsset GetFontForCurrentLanguage()
        {
            var languageInfo = GetCurrentLanguageInfo();
            if (languageInfo != null && !string.IsNullOrEmpty(languageInfo.fontAssetName))
            {
                var fontPair = languageFonts.Find(f => f.languageCode == currentLanguage);
                if (fontPair != null && fontPair.fontAsset != null)
                {
                    return fontPair.fontAsset;
                }
            }
            
            return defaultFont;
        }
        
        public bool IsLanguageSupported(string languageCode)
        {
            return supportedLanguages.Exists(lang => lang.languageCode == languageCode);
        }
        
        public List<LanguageInfo> GetSupportedLanguages()
        {
            return new List<LanguageInfo>(supportedLanguages);
        }
        
        public string GetCurrentLanguage()
        {
            return currentLanguage;
        }
        
        public LanguageInfo GetCurrentLanguageInfo()
        {
            return supportedLanguages.Find(lang => lang.languageCode == currentLanguage);
        }
        
        public string GetLanguageDisplayName(string languageCode)
        {
            var lang = supportedLanguages.Find(l => l.languageCode == languageCode);
            return lang?.displayName ?? languageCode;
        }
        
        public string GetLanguageNativeName(string languageCode)
        {
            var lang = supportedLanguages.Find(l => l.languageCode == languageCode);
            return lang?.nativeName ?? languageCode;
        }
        
        public bool IsCurrentLanguageRightToLeft()
        {
            var lang = GetCurrentLanguageInfo();
            return lang?.isRightToLeft ?? false;
        }
        
        // Development/Editor helper methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ExportLanguageTemplate(string languageCode)
        {
            #if UNITY_EDITOR
            if (currentLocalizationData == null) return;
            
            string json = JsonUtility.ToJson(currentLocalizationData, true);
            string path = Path.Combine(Application.dataPath, "Resources", localizationPath, $"{languageCode}.json");
            
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            
            Debug.Log($"Exported language template to: {path}");
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ImportLanguageFile(string languageCode, string filePath)
        {
            #if UNITY_EDITOR
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                try
                {
                    LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);
                    loadedLanguages[languageCode] = data;
                    
                    if (currentLanguage == languageCode)
                    {
                        currentLocalizationData = data;
                        UpdateAllLocalizedTexts();
                    }
                    
                    Debug.Log($"Imported language file for {languageCode}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to import language file: {e.Message}");
                }
            }
            #endif
        }
        
        private void OnDestroy()
        {
            registeredTexts.Clear();
            loadedLanguages.Clear();
            textCache.Clear();
        }
    }
    
    [System.Serializable]
    public class LanguageFontPair
    {
        public string languageCode;
        public TMP_FontAsset fontAsset;
    }
}
