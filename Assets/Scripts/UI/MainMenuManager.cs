using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using FPS.Audio;

namespace FPS.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject newGamePanel;
        [SerializeField] private GameObject loadGamePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitConfirmPanel;
        
        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        
        [Header("New Game Panel")]
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private Button startNewGameButton;
        [SerializeField] private Button backFromNewGameButton;
        
        [Header("Load Game Panel")]
        [SerializeField] private Transform saveSlotContainer;
        [SerializeField] private GameObject saveSlotPrefab;
        [SerializeField] private Button backFromLoadButton;
        
        [Header("Settings Panel")]
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private Button backFromSettingsButton;
        
        [Header("Credits Panel")]
        [SerializeField] private Button backFromCreditsButton;
        [SerializeField] private ScrollRect creditsScrollRect;
        
        [Header("Quit Confirmation")]
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;
        
        [Header("Background")]
        [SerializeField] private RawImage backgroundVideo;
        [SerializeField] private Image backgroundImage;
        
        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip menuMusicClip;
        
        [Header("Scene References")]
        [SerializeField] private string firstLevelSceneName = "Level1";
        [SerializeField] private string tutorialSceneName = "Tutorial";
        
        private AudioManager audioManager;
        private SaveSystem saveSystem;
        
        private void Start()
        {
            // Get references
            audioManager = AudioManager.Instance;
            saveSystem = SaveSystem.Instance;
            
            // Initialize UI
            InitializeMainMenu();
            
            // Setup button listeners
            SetupButtonListeners();
            
            // Play menu music
            if (audioManager != null && menuMusicClip != null)
            {
                audioManager.PlayMusic(menuMusicClip, true);
            }
            
            // Check for existing save
            UpdateContinueButton();
        }
        
        private void InitializeMainMenu()
        {
            // Show main menu panel, hide others
            ShowPanel(mainMenuPanel);
            
            // Initialize difficulty dropdown
            if (difficultyDropdown != null)
            {
                difficultyDropdown.options.Clear();
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Easy"));
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Normal"));
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Hard"));
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Hardcore"));
                difficultyDropdown.value = 1; // Default to Normal
            }
        }
        
        private void SetupButtonListeners()
        {
            // Main menu buttons
            if (newGameButton != null)
                newGameButton.onClick.AddListener(() => OnButtonClick(ShowNewGamePanel));
            
            if (continueButton != null)
                continueButton.onClick.AddListener(() => OnButtonClick(ContinueGame));
            
            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(() => OnButtonClick(ShowLoadGamePanel));
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnButtonClick(ShowSettingsPanel));
            
            if (creditsButton != null)
                creditsButton.onClick.AddListener(() => OnButtonClick(ShowCreditsPanel));
            
            if (quitButton != null)
                quitButton.onClick.AddListener(() => OnButtonClick(ShowQuitConfirmation));
            
            // New game panel buttons
            if (startNewGameButton != null)
                startNewGameButton.onClick.AddListener(() => OnButtonClick(StartNewGame));
            
            if (backFromNewGameButton != null)
                backFromNewGameButton.onClick.AddListener(() => OnButtonClick(ShowMainMenuPanel));
            
            // Load game panel buttons
            if (backFromLoadButton != null)
                backFromLoadButton.onClick.AddListener(() => OnButtonClick(ShowMainMenuPanel));
            
            // Settings panel buttons
            if (backFromSettingsButton != null)
                backFromSettingsButton.onClick.AddListener(() => OnButtonClick(ShowMainMenuPanel));
            
            // Credits panel buttons
            if (backFromCreditsButton != null)
                backFromCreditsButton.onClick.AddListener(() => OnButtonClick(ShowMainMenuPanel));
            
            // Quit confirmation buttons
            if (confirmQuitButton != null)
                confirmQuitButton.onClick.AddListener(() => OnButtonClick(QuitGame));
            
            if (cancelQuitButton != null)
                cancelQuitButton.onClick.AddListener(() => OnButtonClick(ShowMainMenuPanel));
        }
        
        private void OnButtonClick(System.Action action)
        {
            // Play button click sound
            if (audioManager != null && buttonClickSound != null)
            {
                audioManager.PlaySFX(buttonClickSound);
            }
            
            // Execute action
            action?.Invoke();
        }
        
        private void ShowPanel(GameObject panelToShow)
        {
            // Hide all panels
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (newGamePanel != null) newGamePanel.SetActive(false);
            if (loadGamePanel != null) loadGamePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
            
            // Show the requested panel
            if (panelToShow != null)
                panelToShow.SetActive(true);
        }
        
        #region Panel Show Methods
        
        private void ShowMainMenuPanel()
        {
            ShowPanel(mainMenuPanel);
            UpdateContinueButton();
        }
        
        private void ShowNewGamePanel()
        {
            ShowPanel(newGamePanel);
        }
        
        private void ShowLoadGamePanel()
        {
            ShowPanel(loadGamePanel);
            PopulateSaveSlots();
        }
        
        private void ShowSettingsPanel()
        {
            ShowPanel(settingsPanel);
            if (settingsUI != null)
                settingsUI.RefreshUI();
        }
        
        private void ShowCreditsPanel()
        {
            ShowPanel(creditsPanel);
            if (creditsScrollRect != null)
                creditsScrollRect.verticalNormalizedPosition = 1f; // Start at top
        }
        
        private void ShowQuitConfirmation()
        {
            ShowPanel(quitConfirmPanel);
        }
        
        #endregion
        
        #region Game Actions
        
        private void StartNewGame()
        {
            // Get selected difficulty
            int difficulty = difficultyDropdown != null ? difficultyDropdown.value : 1;
            
            // Store difficulty in player prefs for the game to use
            PlayerPrefs.SetInt("GameDifficulty", difficulty);
            PlayerPrefs.Save();
            
            // Load the first level or tutorial
            string sceneToLoad = string.IsNullOrEmpty(tutorialSceneName) ? firstLevelSceneName : tutorialSceneName;
            StartCoroutine(LoadSceneWithFade(sceneToLoad));
        }
        
        private void ContinueGame()
        {
            if (saveSystem != null && saveSystem.HasSaveFile(0))
            {
                saveSystem.LoadGame(0);
            }
        }
        
        private void LoadGame(int slot)
        {
            if (saveSystem != null && saveSystem.HasSaveFile(slot))
            {
                saveSystem.LoadGame(slot);
            }
        }
        
        private void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        #endregion
        
        #region Save Slot Management
        
        private void PopulateSaveSlots()
        {
            if (saveSlotContainer == null || saveSlotPrefab == null) return;
            
            // Clear existing slots
            foreach (Transform child in saveSlotContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create save slots
            for (int i = 0; i < 5; i++) // 5 save slots
            {
                CreateSaveSlot(i);
            }
        }
        
        private void CreateSaveSlot(int slot)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
            SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
            
            if (slotUI != null)
            {
                if (saveSystem != null && saveSystem.HasSaveFile(slot))
                {
                    var saveData = saveSystem.GetSaveInfo(slot);
                    slotUI.Setup(slot, saveData, () => LoadGame(slot), () => DeleteSave(slot));
                }
                else
                {
                    slotUI.SetupEmpty(slot);
                }
            }
        }
        
        private void DeleteSave(int slot)
        {
            if (saveSystem != null)
            {
                saveSystem.DeleteSave(slot);
                PopulateSaveSlots(); // Refresh the list
            }
        }
        
        #endregion
        
        private void UpdateContinueButton()
        {
            if (continueButton != null && saveSystem != null)
            {
                continueButton.interactable = saveSystem.HasSaveFile(0);
            }
        }
        
        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            // Fade out
            // You can implement a fade transition here
            
            yield return new WaitForSeconds(0.5f);
            
            // Load scene
            SceneManager.LoadScene(sceneName);
        }
        
        private void Update()
        {
            // Handle escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (mainMenuPanel != null && mainMenuPanel.activeSelf)
                {
                    ShowQuitConfirmation();
                }
                else
                {
                    ShowMainMenuPanel();
                }
            }
        }
    }
}
