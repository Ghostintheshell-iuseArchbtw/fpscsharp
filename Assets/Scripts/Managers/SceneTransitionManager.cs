using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace FPS.Managers
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }
        
        [Header("Loading Screen")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        
        [Header("Transition Settings")]
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private float minLoadingTime = 2f;
        [SerializeField] private bool showLoadingTips = true;
        
        [Header("Loading Tips")]
        [SerializeField] private List<string> loadingTips = new List<string>
        {
            "Use cover to avoid enemy fire",
            "Check your hunger and thirst levels regularly",
            "Craft better weapons to increase your survival chances",
            "Listen for enemy footsteps to detect threats",
            "Radiation zones deal continuous damage",
            "Temperature affects your stamina regeneration",
            "Scavenge for resources in abandoned buildings",
            "Conserve ammunition by making every shot count"
        };
        
        [Header("Scene Backgrounds")]
        [SerializeField] private List<SceneBackground> sceneBackgrounds = new List<SceneBackground>();
        
        // Scene loading state
        private bool isLoading = false;
        private AsyncOperation currentLoadOperation;
        
        // Events
        public System.Action<string> OnSceneLoadStarted;
        public System.Action<string> OnSceneLoadCompleted;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Make sure loading screen is initially hidden
                if (loadingScreen != null)
                {
                    loadingScreen.SetActive(false);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning("Scene is already loading!");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        public void LoadScene(int sceneIndex)
        {
            if (isLoading)
            {
                Debug.LogWarning("Scene is already loading!");
                return;
            }
            
            string sceneName = GetSceneName(sceneIndex);
            StartCoroutine(LoadSceneAsync(sceneName, sceneIndex));
        }
        
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }
        
        public void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }
        
        public void LoadNextLevel()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;
            
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("No next level available!");
            }
        }
        
        public void LoadPreviousLevel()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int previousIndex = currentIndex - 1;
            
            if (previousIndex >= 0)
            {
                LoadScene(previousIndex);
            }
            else
            {
                Debug.LogWarning("No previous level available!");
            }
        }
        
        private IEnumerator LoadSceneAsync(string sceneName, int sceneIndex = -1)
        {
            isLoading = true;
            
            // Show loading screen
            yield return StartCoroutine(ShowLoadingScreen(sceneName));
            
            // Trigger event
            OnSceneLoadStarted?.Invoke(sceneName);
            
            // Pause the game
            Time.timeScale = 1f;
            
            // Start async loading
            if (sceneIndex >= 0)
            {
                currentLoadOperation = SceneManager.LoadSceneAsync(sceneIndex);
            }
            else
            {
                currentLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            }
            
            currentLoadOperation.allowSceneActivation = false;
            
            float startTime = Time.time;
            
            // Update progress
            while (!currentLoadOperation.isDone)
            {
                // Calculate progress (0.0 to 0.9 for loading, 0.9 to 1.0 for activation)
                float progress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
                
                UpdateLoadingProgress(progress);
                
                // Check if loading is complete and minimum time has passed
                if (currentLoadOperation.progress >= 0.9f)
                {
                    float elapsedTime = Time.time - startTime;
                    if (elapsedTime >= minLoadingTime)
                    {
                        UpdateLoadingProgress(1f);
                        yield return new WaitForSeconds(0.5f); // Brief pause to show 100%
                        currentLoadOperation.allowSceneActivation = true;
                    }
                }
                
                yield return null;
            }
            
            // Hide loading screen
            yield return StartCoroutine(HideLoadingScreen());
            
            isLoading = false;
            
            // Trigger event
            OnSceneLoadCompleted?.Invoke(sceneName);
        }
        
        private IEnumerator ShowLoadingScreen(string sceneName)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                
                // Set background image for scene
                SetBackgroundForScene(sceneName);
                
                // Show random loading tip
                if (showLoadingTips && tipText != null && loadingTips.Count > 0)
                {
                    string randomTip = loadingTips[Random.Range(0, loadingTips.Count)];
                    tipText.text = randomTip;
                }
                
                // Fade in
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 0f;
                    
                    while (loadingCanvasGroup.alpha < 1f)
                    {
                        loadingCanvasGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
                        yield return null;
                    }
                    
                    loadingCanvasGroup.alpha = 1f;
                }
            }
        }
        
        private IEnumerator HideLoadingScreen()
        {
            if (loadingScreen != null && loadingCanvasGroup != null)
            {
                // Fade out
                while (loadingCanvasGroup.alpha > 0f)
                {
                    loadingCanvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
                    yield return null;
                }
                
                loadingCanvasGroup.alpha = 0f;
                loadingScreen.SetActive(false);
            }
        }
        
        private void UpdateLoadingProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            
            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }
        }
        
        private void SetBackgroundForScene(string sceneName)
        {
            if (backgroundImage == null) return;
            
            SceneBackground sceneBackground = sceneBackgrounds.Find(bg => bg.sceneName == sceneName);
            if (sceneBackground != null && sceneBackground.backgroundImage != null)
            {
                backgroundImage.sprite = sceneBackground.backgroundImage;
            }
        }
        
        private string GetSceneName(int sceneIndex)
        {
            if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                return System.IO.Path.GetFileNameWithoutExtension(scenePath);
            }
            return "";
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}");
            
            // Ensure proper time scale
            Time.timeScale = 1f;
            
            // Find and setup scene-specific managers
            SetupSceneManagers();
        }
        
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"Scene unloaded: {scene.name}");
            
            // Clean up resources
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
        
        private void SetupSceneManagers()
        {
            // Initialize scene-specific systems
            string currentScene = SceneManager.GetActiveScene().name;
            
            switch (currentScene)
            {
                case "MainMenu":
                    SetupMainMenuScene();
                    break;
                case "Level1":
                case "Level2":
                case "Level3":
                    SetupGameplayScene();
                    break;
                default:
                    SetupDefaultScene();
                    break;
            }
        }
        
        private void SetupMainMenuScene()
        {
            // Setup main menu specific systems
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Disable player-specific managers
            if (PerformanceOptimizer.Instance != null)
            {
                PerformanceOptimizer.Instance.SetTargetFrameRate(60);
            }
        }
        
        private void SetupGameplayScene()
        {
            // Setup gameplay specific systems
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // Initialize performance optimization for gameplay
            if (PerformanceOptimizer.Instance != null)
            {
                PerformanceOptimizer.Instance.RefreshCullableRenderers();
            }
            
            // Start tutorial if needed
            if (TutorialSystem.Instance != null)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                if (currentScene == "Level1" && !TutorialSystem.Instance.HasCompletedTutorial("BasicMovement"))
                {
                    TutorialSystem.Instance.StartTutorial("BasicMovement");
                }
            }
        }
        
        private void SetupDefaultScene()
        {
            // Default scene setup
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        #region Public Methods
        
        public bool IsLoading()
        {
            return isLoading;
        }
        
        public float GetLoadingProgress()
        {
            if (currentLoadOperation != null)
            {
                return currentLoadOperation.progress;
            }
            return 0f;
        }
        
        public void AddLoadingTip(string tip)
        {
            if (!loadingTips.Contains(tip))
            {
                loadingTips.Add(tip);
            }
        }
        
        public void RemoveLoadingTip(string tip)
        {
            loadingTips.Remove(tip);
        }
        
        public void SetLoadingBackground(string sceneName, Sprite backgroundSprite)
        {
            SceneBackground existing = sceneBackgrounds.Find(bg => bg.sceneName == sceneName);
            if (existing != null)
            {
                existing.backgroundImage = backgroundSprite;
            }
            else
            {
                sceneBackgrounds.Add(new SceneBackground { sceneName = sceneName, backgroundImage = backgroundSprite });
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
    
    [System.Serializable]
    public class SceneBackground
    {
        public string sceneName;
        public Sprite backgroundImage;
    }
}
