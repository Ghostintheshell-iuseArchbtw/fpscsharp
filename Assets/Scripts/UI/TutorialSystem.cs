using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace FPS.UI
{
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 6)]
        public string description;
        public GameObject highlightTarget;
        public Vector3 tooltipOffset = Vector3.zero;
        public float duration = 0f; // 0 = wait for input
        public bool pauseGame = false;
        public UnityEvent onStepStart;
        public UnityEvent onStepComplete;
        public TutorialTriggerType triggerType = TutorialTriggerType.Manual;
        public string triggerKey = "";
        public KeyCode triggerKeyCode = KeyCode.None;
    }
    
    public enum TutorialTriggerType
    {
        Manual,
        KeyPress,
        Timer,
        Automatic
    }
    
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }
        
        [Header("Tutorial UI")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Highlight System")]
        [SerializeField] private GameObject highlightPrefab;
        [SerializeField] private Canvas highlightCanvas;
        [SerializeField] private Material highlightMaterial;
        
        [Header("Tutorial Sets")]
        [SerializeField] private List<TutorialSet> tutorialSets = new List<TutorialSet>();
        
        [Header("Settings")]
        [SerializeField] private bool showTutorialsOnFirstPlay = true;
        [SerializeField] private bool allowSkipping = true;
        [SerializeField] private float typewriterSpeed = 0.05f;
        [SerializeField] private AudioClip tutorialStartSound;
        [SerializeField] private AudioClip tutorialCompleteSound;
        [SerializeField] private AudioClip stepAdvanceSound;
        
        // Current tutorial state
        private TutorialSet currentTutorialSet;
        private int currentStepIndex = 0;
        private bool tutorialActive = false;
        private bool stepInProgress = false;
        private GameObject currentHighlight;
        private Coroutine typewriterCoroutine;
        private Coroutine stepTimerCoroutine;
        
        // Tutorial completion tracking
        private HashSet<string> completedTutorials = new HashSet<string>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadTutorialProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupUI();
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            
            // Start basic movement tutorial if it's the player's first time
            if (showTutorialsOnFirstPlay && !HasCompletedTutorial("BasicMovement"))
            {
                StartCoroutine(DelayedTutorialStart("BasicMovement", 2f));
            }
        }
        
        private void Update()
        {
            if (tutorialActive && stepInProgress)
            {
                HandleStepInput();
            }
        }
        
        private void SetupUI()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextStep);
            }
            
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipTutorial);
                skipButton.gameObject.SetActive(allowSkipping);
            }
            
            if (prevButton != null)
            {
                prevButton.onClick.AddListener(PreviousStep);
            }
        }
        
        private IEnumerator DelayedTutorialStart(string tutorialName, float delay)
        {
            yield return new WaitForSeconds(delay);
            StartTutorial(tutorialName);
        }
        
        public void StartTutorial(string tutorialName)
        {
            TutorialSet tutorialSet = GetTutorialSet(tutorialName);
            if (tutorialSet == null)
            {
                Debug.LogWarning($"Tutorial '{tutorialName}' not found!");
                return;
            }
            
            if (HasCompletedTutorial(tutorialName) && !tutorialSet.allowReplay)
            {
                return;
            }
            
            currentTutorialSet = tutorialSet;
            currentStepIndex = 0;
            tutorialActive = true;
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            
            PlaySound(tutorialStartSound);
            StartStep();
        }
        
        private void StartStep()
        {
            if (currentTutorialSet == null || currentStepIndex >= currentTutorialSet.steps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            TutorialStep step = currentTutorialSet.steps[currentStepIndex];
            stepInProgress = true;
            
            // Update UI
            UpdateTutorialUI(step);
            
            // Handle highlighting
            SetupHighlight(step);
            
            // Pause game if needed
            if (step.pauseGame)
            {
                Time.timeScale = 0f;
            }
            
            // Trigger step start event
            step.onStepStart?.Invoke();
            
            // Handle automatic progression
            if (step.duration > 0 && step.triggerType == TutorialTriggerType.Timer)
            {
                stepTimerCoroutine = StartCoroutine(StepTimer(step.duration));
            }
            
            PlaySound(stepAdvanceSound);
        }
        
        private void UpdateTutorialUI(TutorialStep step)
        {
            // Update title
            if (titleText != null)
            {
                titleText.text = step.title;
            }
            
            // Update description with typewriter effect
            if (descriptionText != null)
            {
                if (typewriterCoroutine != null)
                {
                    StopCoroutine(typewriterCoroutine);
                }
                typewriterCoroutine = StartCoroutine(TypewriterEffect(step.description));
            }
            
            // Update progress
            UpdateProgress();
            
            // Update button states
            if (prevButton != null)
            {
                prevButton.interactable = currentStepIndex > 0;
            }
            
            if (nextButton != null)
            {
                bool canAdvance = step.triggerType == TutorialTriggerType.Manual || 
                                step.triggerType == TutorialTriggerType.Automatic;
                nextButton.gameObject.SetActive(canAdvance);
            }
        }
        
        private IEnumerator TypewriterEffect(string text)
        {
            descriptionText.text = "";
            
            for (int i = 0; i <= text.Length; i++)
            {
                descriptionText.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }
        }
        
        private void UpdateProgress()
        {
            if (currentTutorialSet == null) return;
            
            float progress = (float)(currentStepIndex + 1) / currentTutorialSet.steps.Count;
            
            if (progressBar != null)
            {
                progressBar.fillAmount = progress;
            }
            
            if (progressText != null)
            {
                progressText.text = $"{currentStepIndex + 1} / {currentTutorialSet.steps.Count}";
            }
        }
        
        private void SetupHighlight(TutorialStep step)
        {
            ClearHighlight();
            
            if (step.highlightTarget != null && highlightPrefab != null)
            {
                // Create highlight overlay
                currentHighlight = Instantiate(highlightPrefab, highlightCanvas.transform);
                
                // Position highlight
                RectTransform highlightRect = currentHighlight.GetComponent<RectTransform>();
                RectTransform targetRect = step.highlightTarget.GetComponent<RectTransform>();
                
                if (targetRect != null)
                {
                    highlightRect.position = targetRect.position + step.tooltipOffset;
                    highlightRect.sizeDelta = targetRect.sizeDelta;
                }
                else
                {
                    // For 3D objects, convert world position to screen position
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(step.highlightTarget.transform.position);
                    highlightRect.position = screenPos + step.tooltipOffset;
                }
            }
        }
        
        private void ClearHighlight()
        {
            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
                currentHighlight = null;
            }
        }
        
        private void HandleStepInput()
        {
            if (currentTutorialSet == null || currentStepIndex >= currentTutorialSet.steps.Count)
                return;
            
            TutorialStep step = currentTutorialSet.steps[currentStepIndex];
            
            switch (step.triggerType)
            {
                case TutorialTriggerType.KeyPress:
                    if (Input.GetKeyDown(step.triggerKeyCode))
                    {
                        CompleteStep();
                    }
                    break;
                    
                case TutorialTriggerType.Automatic:
                    // Check for custom trigger conditions here
                    if (CheckCustomTrigger(step.triggerKey))
                    {
                        CompleteStep();
                    }
                    break;
            }
        }
        
        private bool CheckCustomTrigger(string triggerKey)
        {
            // Implement custom trigger logic based on game state
            switch (triggerKey)
            {
                case "PlayerMoved":
                    return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
                case "PlayerJumped":
                    return Input.GetKeyDown(KeyCode.Space);
                case "WeaponFired":
                    return Input.GetMouseButtonDown(0);
                case "InventoryOpened":
                    return Input.GetKeyDown(KeyCode.Tab);
                default:
                    return false;
            }
        }
        
        private IEnumerator StepTimer(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            CompleteStep();
        }
        
        public void NextStep()
        {
            if (stepInProgress)
            {
                CompleteStep();
            }
        }
        
        public void PreviousStep()
        {
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                StartStep();
            }
        }
        
        private void CompleteStep()
        {
            if (!stepInProgress) return;
            
            stepInProgress = false;
            
            if (stepTimerCoroutine != null)
            {
                StopCoroutine(stepTimerCoroutine);
                stepTimerCoroutine = null;
            }
            
            // Trigger step complete event
            TutorialStep step = currentTutorialSet.steps[currentStepIndex];
            step.onStepComplete?.Invoke();
            
            // Resume game if it was paused
            if (step.pauseGame)
            {
                Time.timeScale = 1f;
            }
            
            currentStepIndex++;
            StartStep();
        }
        
        private void CompleteTutorial()
        {
            tutorialActive = false;
            stepInProgress = false;
            
            if (currentTutorialSet != null)
            {
                MarkTutorialCompleted(currentTutorialSet.tutorialName);
            }
            
            ClearHighlight();
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            
            // Ensure game is running
            Time.timeScale = 1f;
            
            PlaySound(tutorialCompleteSound);
            
            // Show completion message
            if (FPS.UI.UIManager.Instance != null)
            {
                FPS.UI.UIManager.Instance.ShowNotification($"Tutorial completed: {currentTutorialSet.tutorialName}");
            }
            
            currentTutorialSet = null;
        }
        
        public void SkipTutorial()
        {
            if (currentTutorialSet != null)
            {
                MarkTutorialCompleted(currentTutorialSet.tutorialName);
            }
            
            CompleteTutorial();
        }
        
        private TutorialSet GetTutorialSet(string tutorialName)
        {
            return tutorialSets.Find(set => set.tutorialName == tutorialName);
        }
        
        private void MarkTutorialCompleted(string tutorialName)
        {
            completedTutorials.Add(tutorialName);
            PlayerPrefs.SetString("CompletedTutorials", string.Join(",", completedTutorials));
        }
        
        public bool HasCompletedTutorial(string tutorialName)
        {
            return completedTutorials.Contains(tutorialName);
        }
        
        private void LoadTutorialProgress()
        {
            string completed = PlayerPrefs.GetString("CompletedTutorials", "");
            if (!string.IsNullOrEmpty(completed))
            {
                string[] tutorials = completed.Split(',');
                foreach (string tutorial in tutorials)
                {
                    completedTutorials.Add(tutorial);
                }
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && FPS.Audio.AudioManager.Instance != null)
            {
                FPS.Audio.AudioManager.Instance.PlayUISound(clip);
            }
        }
        
        #region Public Methods
        
        public void ResetAllTutorials()
        {
            completedTutorials.Clear();
            PlayerPrefs.DeleteKey("CompletedTutorials");
        }
        
        public void ResetTutorial(string tutorialName)
        {
            completedTutorials.Remove(tutorialName);
            PlayerPrefs.SetString("CompletedTutorials", string.Join(",", completedTutorials));
        }
        
        public bool IsTutorialActive()
        {
            return tutorialActive;
        }
        
        public void SetTutorialSpeed(float speed)
        {
            typewriterSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class TutorialSet
    {
        public string tutorialName;
        public bool allowReplay = false;
        public List<TutorialStep> steps = new List<TutorialStep>();
    }
}
