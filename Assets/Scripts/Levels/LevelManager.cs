using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace FPS.Levels
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }
        
        [Header("Level Settings")]
        [SerializeField] private List<LevelData> availableLevels = new List<LevelData>();
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string loadingSceneName = "LoadingScene";
        
        [Header("Level Transition")]
        [SerializeField] private float transitionFadeDuration = 1f;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        
        [Header("Checkpoint System")]
        [SerializeField] private bool useCheckpoints = true;
        [SerializeField] private string checkpointSaveKey = "CurrentCheckpoint";
        
        // State tracking
        private LevelData currentLevel;
        private int currentLevelIndex = -1;
        private CheckpointData currentCheckpoint;
        private bool isTransitioning = false;
        
        // Cached components
        private UIManager uiManager;
        
        // Events
        public delegate void LevelLoadedHandler(LevelData levelData);
        public event LevelLoadedHandler OnLevelLoaded;
        
        public delegate void CheckpointReachedHandler(CheckpointData checkpoint);
        public event CheckpointReachedHandler OnCheckpointReached;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Initialize fade canvas if not set
            if (fadeCanvasGroup == null)
            {
                GameObject fadeObject = new GameObject("FadeCanvas");
                fadeObject.transform.SetParent(transform);
                Canvas canvas = fadeObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; // Ensure it's on top
                
                // Add canvas scaler
                CanvasScaler scaler = fadeObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Add panel for fading
                GameObject panel = new GameObject("FadePanel");
                panel.transform.SetParent(fadeObject.transform, false);
                RectTransform rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                // Add image component
                UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
                image.color = Color.black;
                
                // Add canvas group for fading
                fadeCanvasGroup = panel.AddComponent<CanvasGroup>();
                fadeCanvasGroup.alpha = 0;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void Start()
        {
            // Find UI Manager
            uiManager = FindObjectOfType<UIManager>();
            
            // Initialize current level data
            InitializeCurrentLevel();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene loaded event
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void InitializeCurrentLevel()
        {
            // Get the current scene name
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // Find matching level data
            for (int i = 0; i < availableLevels.Count; i++)
            {
                if (availableLevels[i].SceneName == currentSceneName)
                {
                    currentLevel = availableLevels[i];
                    currentLevelIndex = i;
                    break;
                }
            }
            
            // If we found a level, initialize it
            if (currentLevel != null)
            {
                InitializeLevel(currentLevel);
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Skip if this is the loading scene
            if (scene.name == loadingSceneName) return;
            
            // Skip if this is the main menu
            if (scene.name == mainMenuSceneName) return;
            
            // Find the matching level data
            LevelData levelData = null;
            for (int i = 0; i < availableLevels.Count; i++)
            {
                if (availableLevels[i].SceneName == scene.name)
                {
                    levelData = availableLevels[i];
                    currentLevelIndex = i;
                    break;
                }
            }
            
            // Initialize the level if found
            if (levelData != null)
            {
                currentLevel = levelData;
                InitializeLevel(levelData);
            }
            
            // Start fade in
            StartCoroutine(FadeIn());
        }
        
        private void InitializeLevel(LevelData levelData)
        {
            // Update UI with level info
            if (uiManager != null)
            {
                uiManager.SetLevelName(levelData.DisplayName);
                uiManager.SetObjectiveText(GetCurrentObjectiveText());
                
                // Optional: Show level intro/briefing
                if (levelData.ShowBriefing)
                {
                    uiManager.ShowBriefing(levelData.BriefingText, levelData.BriefingImage);
                }
            }
            
            // Load checkpoint if available
            if (useCheckpoints)
            {
                LoadCheckpoint();
            }
            
            // Trigger event
            OnLevelLoaded?.Invoke(levelData);
        }
        
        // Public methods for level loading
        public void LoadLevel(string levelName)
        {
            // Find the level data
            LevelData levelToLoad = null;
            for (int i = 0; i < availableLevels.Count; i++)
            {
                if (availableLevels[i].SceneName == levelName)
                {
                    levelToLoad = availableLevels[i];
                    break;
                }
            }
            
            if (levelToLoad != null)
            {
                LoadLevel(levelToLoad);
            }
            else
            {
                Debug.LogWarning("Level not found: " + levelName);
            }
        }
        
        public void LoadLevel(LevelData levelData)
        {
            if (isTransitioning) return;
            
            StartCoroutine(LoadLevelRoutine(levelData));
        }
        
        public void LoadNextLevel()
        {
            if (isTransitioning) return;
            
            // Calculate next level index
            int nextLevelIndex = currentLevelIndex + 1;
            
            // Check if there's a next level
            if (nextLevelIndex < availableLevels.Count)
            {
                LoadLevel(availableLevels[nextLevelIndex]);
            }
            else
            {
                // No more levels, go to main menu or end game screen
                LoadMainMenu();
            }
        }
        
        public void RestartLevel()
        {
            if (isTransitioning) return;
            
            if (currentLevel != null)
            {
                // Reset checkpoint data before restarting
                currentCheckpoint = null;
                PlayerPrefs.DeleteKey(checkpointSaveKey + currentLevel.SceneName);
                
                LoadLevel(currentLevel);
            }
        }
        
        public void LoadMainMenu()
        {
            if (isTransitioning) return;
            
            StartCoroutine(LoadMainMenuRoutine());
        }
        
        // Level transition coroutines
        private IEnumerator LoadLevelRoutine(LevelData levelData)
        {
            isTransitioning = true;
            
            // Fade out
            yield return StartCoroutine(FadeOut());
            
            // Load the loading scene if specified
            if (!string.IsNullOrEmpty(loadingSceneName))
            {
                SceneManager.LoadScene(loadingSceneName);
                
                // Wait a frame to ensure loading scene is active
                yield return null;
                
                // Update loading progress UI if needed
                // This would communicate with a LoadingScreenController in the loading scene
            }
            
            // Load the target scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelData.SceneName);
            asyncLoad.allowSceneActivation = false;
            
            // Wait until the scene is almost loaded
            while (asyncLoad.progress < 0.9f)
            {
                // Update loading progress UI if needed
                yield return null;
            }
            
            // Activate the scene
            asyncLoad.allowSceneActivation = true;
            
            // Wait until scene is fully loaded
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // The OnSceneLoaded callback will handle initialization and fading in
            
            isTransitioning = false;
        }
        
        private IEnumerator LoadMainMenuRoutine()
        {
            isTransitioning = true;
            
            // Fade out
            yield return StartCoroutine(FadeOut());
            
            // Load main menu scene
            SceneManager.LoadScene(mainMenuSceneName);
            
            // Wait until scene is loaded
            yield return null;
            
            // Fade in
            yield return StartCoroutine(FadeIn());
            
            isTransitioning = false;
        }
        
        private IEnumerator FadeOut()
        {
            float elapsedTime = 0;
            fadeCanvasGroup.blocksRaycasts = true;
            
            while (elapsedTime < transitionFadeDuration)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / transitionFadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 1;
        }
        
        private IEnumerator FadeIn()
        {
            float elapsedTime = 0;
            fadeCanvasGroup.alpha = 1;
            
            while (elapsedTime < transitionFadeDuration)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / transitionFadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 0;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        
        // Objective management
        public void CompleteObjective(string objectiveId)
        {
            if (currentLevel == null) return;
            
            // Find the objective
            foreach (var objective in currentLevel.Objectives)
            {
                if (objective.Id == objectiveId && !objective.IsCompleted)
                {
                    // Mark as completed
                    objective.IsCompleted = true;
                    
                    // Update UI
                    if (uiManager != null)
                    {
                        uiManager.ShowObjectiveComplete(objective.DisplayText);
                        uiManager.SetObjectiveText(GetCurrentObjectiveText());
                    }
                    
                    // Check if all objectives are completed
                    if (AreAllObjectivesCompleted())
                    {
                        OnLevelCompleted();
                    }
                    
                    break;
                }
            }
        }
        
        public string GetCurrentObjectiveText()
        {
            if (currentLevel == null) return string.Empty;
            
            // Find the first incomplete objective
            foreach (var objective in currentLevel.Objectives)
            {
                if (!objective.IsCompleted)
                {
                    return objective.DisplayText;
                }
            }
            
            // All objectives completed
            return "All objectives completed";
        }
        
        public bool AreAllObjectivesCompleted()
        {
            if (currentLevel == null) return false;
            
            // Check if all objectives are completed
            foreach (var objective in currentLevel.Objectives)
            {
                if (!objective.IsCompleted)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void OnLevelCompleted()
        {
            // Show level complete UI
            if (uiManager != null)
            {
                uiManager.ShowLevelComplete(currentLevel.DisplayName);
            }
            
            // Unlock next level in progression system
            UnlockNextLevel();
            
            // Auto-advance to next level if specified
            if (currentLevel.AutoAdvanceDelay > 0)
            {
                StartCoroutine(AutoAdvanceToNextLevel(currentLevel.AutoAdvanceDelay));
            }
        }
        
        private IEnumerator AutoAdvanceToNextLevel(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadNextLevel();
        }
        
        private void UnlockNextLevel()
        {
            // Calculate next level index
            int nextLevelIndex = currentLevelIndex + 1;
            
            // Check if there's a next level
            if (nextLevelIndex < availableLevels.Count)
            {
                // Unlock next level in player progress
                string unlockedLevelsKey = "UnlockedLevels";
                int unlockedLevels = PlayerPrefs.GetInt(unlockedLevelsKey, 1); // Default to first level unlocked
                
                if (nextLevelIndex + 1 > unlockedLevels)
                {
                    PlayerPrefs.SetInt(unlockedLevelsKey, nextLevelIndex + 1);
                    PlayerPrefs.Save();
                }
            }
        }
        
        // Checkpoint system
        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            if (!useCheckpoints || checkpoint == null) return;
            
            // Create checkpoint data
            CheckpointData checkpointData = new CheckpointData
            {
                Id = checkpoint.Id,
                Position = checkpoint.transform.position,
                Rotation = checkpoint.transform.rotation,
                LevelSceneName = SceneManager.GetActiveScene().name,
                CompletedObjectives = new List<string>()
            };
            
            // Store completed objectives
            if (currentLevel != null)
            {
                foreach (var objective in currentLevel.Objectives)
                {
                    if (objective.IsCompleted)
                    {
                        checkpointData.CompletedObjectives.Add(objective.Id);
                    }
                }
            }
            
            // Save checkpoint
            currentCheckpoint = checkpointData;
            SaveCheckpoint(checkpointData);
            
            // Show checkpoint notification
            if (uiManager != null)
            {
                uiManager.ShowCheckpointActivated();
            }
            
            // Trigger event
            OnCheckpointReached?.Invoke(checkpointData);
        }
        
        private void SaveCheckpoint(CheckpointData checkpointData)
        {
            if (checkpointData == null) return;
            
            // Convert to JSON
            string json = JsonUtility.ToJson(checkpointData);
            
            // Save to PlayerPrefs
            PlayerPrefs.SetString(checkpointSaveKey + checkpointData.LevelSceneName, json);
            PlayerPrefs.Save();
        }
        
        private void LoadCheckpoint()
        {
            // Get the current scene name
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // Try to load checkpoint data
            string json = PlayerPrefs.GetString(checkpointSaveKey + currentSceneName, string.Empty);
            
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    // Parse checkpoint data
                    CheckpointData checkpointData = JsonUtility.FromJson<CheckpointData>(json);
                    
                    if (checkpointData != null)
                    {
                        // Store current checkpoint
                        currentCheckpoint = checkpointData;
                        
                        // Find the player
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        
                        if (player != null)
                        {
                            // Teleport player to checkpoint position
                            player.transform.position = checkpointData.Position;
                            player.transform.rotation = checkpointData.Rotation;
                            
                            // Restore completed objectives
                            if (currentLevel != null && checkpointData.CompletedObjectives != null)
                            {
                                foreach (var objectiveId in checkpointData.CompletedObjectives)
                                {
                                    foreach (var objective in currentLevel.Objectives)
                                    {
                                        if (objective.Id == objectiveId)
                                        {
                                            objective.IsCompleted = true;
                                        }
                                    }
                                }
                                
                                // Update objective UI
                                if (uiManager != null)
                                {
                                    uiManager.SetObjectiveText(GetCurrentObjectiveText());
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error loading checkpoint: " + e.Message);
                }
            }
        }
        
        // Getters
        public LevelData GetCurrentLevel() => currentLevel;
        public bool IsTransitioning() => isTransitioning;
    }
    
    // Level data class
    [System.Serializable]
    public class LevelData
    {
        public string SceneName;
        public string DisplayName;
        public Sprite LevelImage;
        public string Description;
        
        [Header("Objectives")]
        public List<LevelObjective> Objectives = new List<LevelObjective>();
        
        [Header("Briefing")]
        public bool ShowBriefing = true;
        public string BriefingText;
        public Sprite BriefingImage;
        
        [Header("Level Completion")]
        public bool AutoAdvance = false;
        public float AutoAdvanceDelay = 5f;
        public string NextLevelOverride; // If empty, goes to next level in list
    }
    
    // Level objective class
    [System.Serializable]
    public class LevelObjective
    {
        public string Id;
        public string DisplayText;
        public bool IsOptional = false;
        public bool IsCompleted = false;
        public bool IsHidden = false;
    }
    
    // Checkpoint data class
    [System.Serializable]
    public class CheckpointData
    {
        public string Id;
        public Vector3 Position;
        public Quaternion Rotation;
        public string LevelSceneName;
        public List<string> CompletedObjectives;
    }
    
    // Checkpoint component for placing in the level
    public class Checkpoint : MonoBehaviour
    {
        public string Id;
        public bool IsActive = true;
        
        [SerializeField] private bool activateOnTriggerEnter = true;
        [SerializeField] private GameObject visualObject;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || !activateOnTriggerEnter) return;
            
            // Check if this is the player
            if (other.CompareTag("Player"))
            {
                // Activate the checkpoint
                LevelManager.Instance?.ActivateCheckpoint(this);
                
                // Deactivate visual if it's a one-time checkpoint
                if (visualObject != null)
                {
                    visualObject.SetActive(false);
                }
                
                // Optionally deactivate this checkpoint
                IsActive = false;
            }
        }
        
        public void Activate()
        {
            if (!IsActive) return;
            
            // Activate the checkpoint
            LevelManager.Instance?.ActivateCheckpoint(this);
        }
    }
}
