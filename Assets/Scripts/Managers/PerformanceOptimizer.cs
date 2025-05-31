using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;

namespace FPS.Managers
{
    public class PerformanceOptimizer : MonoBehaviour
    {
        public static PerformanceOptimizer Instance { get; private set; }
        
        [Header("Performance Settings")]
        [SerializeField] private bool enableLODSystem = true;
        [SerializeField] private bool enableOcclusionCulling = true;
        [SerializeField] private bool enableFrustumCulling = true;
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private bool enableBatching = true;
        [SerializeField] private bool enableAsyncLoading = true;
        
        [Header("LOD Settings")]
        [SerializeField] private float lodBias = 1.0f;
        [SerializeField] private int maximumLODLevel = 0;
        [SerializeField] private float lodFadeTransitionWidth = 0.1f;
        
        [Header("Culling Settings")]
        [SerializeField] private float cullDistance = 100f;
        [SerializeField] private LayerMask cullLayers = -1;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Memory Management")]
        [SerializeField] private bool enableGarbageCollection = true;
        [SerializeField] private float gcInterval = 30f;
        [SerializeField] private int maxTextureSize = 1024;
        [SerializeField] private bool compressTextures = true;
        
        [Header("Quality Scaling")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private float frameRateThreshold = 0.8f;
        [SerializeField] private bool enableDynamicQuality = true;
        
        // Performance tracking
        private float averageFrameTime = 0f;
        private Queue<float> frameTimeHistory = new Queue<float>();
        private const int frameHistorySize = 60;
        
        // Culling system
        private Camera mainCamera;
        private List<Renderer> cullableRenderers = new List<Renderer>();
        private Coroutine cullingCoroutine;
        
        // Quality levels
        private int currentQualityLevel;
        private float qualityAdjustmentCooldown = 5f;
        private float lastQualityAdjustment = 0f;
        
        // Object pools
        private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> poolPrefabs = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePerformanceSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            currentQualityLevel = QualitySettings.GetQualityLevel();
            
            if (enableOcclusionCulling)
            {
                StartCullingSystem();
            }
            
            if (enableGarbageCollection)
            {
                StartCoroutine(PeriodicGarbageCollection());
            }
            
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
        }
        
        private void Update()
        {
            TrackPerformance();
            
            if (enableDynamicQuality)
            {
                AdjustQualityBasedOnPerformance();
            }
        }
        
        private void InitializePerformanceSettings()
        {
            // Configure LOD settings
            if (enableLODSystem)
            {
                QualitySettings.lodBias = lodBias;
                QualitySettings.maximumLODLevel = maximumLODLevel;
            }
            
            // Configure rendering settings
            QualitySettings.vSyncCount = 0; // Disable VSync for better performance control
            
            // Configure batching
            if (enableBatching)
            {
                // Unity handles static batching automatically for marked objects
                // Dynamic batching is handled by the engine
            }
            
            // Configure shadow settings for performance
            QualitySettings.shadowDistance = 50f;
            QualitySettings.shadowCascades = 2;
            
            // Configure particle system settings
            QualitySettings.particleRaycastBudget = 64;
            
            // Configure skin weights for character models
            QualitySettings.skinWeights = SkinWeights.TwoBones;
        }
        
        private void TrackPerformance()
        {
            float frameTime = Time.unscaledDeltaTime;
            frameTimeHistory.Enqueue(frameTime);
            
            if (frameTimeHistory.Count > frameHistorySize)
            {
                frameTimeHistory.Dequeue();
            }
            
            // Calculate average frame time
            float totalTime = 0f;
            foreach (float time in frameTimeHistory)
            {
                totalTime += time;
            }
            averageFrameTime = totalTime / frameTimeHistory.Count;
        }
        
        private void AdjustQualityBasedOnPerformance()
        {
            if (Time.time - lastQualityAdjustment < qualityAdjustmentCooldown)
                return;
            
            float targetFrameTime = 1f / targetFrameRate;
            float currentFPS = 1f / averageFrameTime;
            float targetFPS = targetFrameRate * frameRateThreshold;
            
            if (currentFPS < targetFPS && currentQualityLevel > 0)
            {
                // Decrease quality
                DecreaseQuality();
                lastQualityAdjustment = Time.time;
            }
            else if (currentFPS > targetFrameRate * 1.1f && currentQualityLevel < QualitySettings.names.Length - 1)
            {
                // Increase quality
                IncreaseQuality();
                lastQualityAdjustment = Time.time;
            }
        }
        
        private void DecreaseQuality()
        {
            if (currentQualityLevel > 0)
            {
                currentQualityLevel--;
                QualitySettings.SetQualityLevel(currentQualityLevel, true);
                Debug.Log($"Decreased quality level to: {QualitySettings.names[currentQualityLevel]}");
            }
        }
        
        private void IncreaseQuality()
        {
            if (currentQualityLevel < QualitySettings.names.Length - 1)
            {
                currentQualityLevel++;
                QualitySettings.SetQualityLevel(currentQualityLevel, true);
                Debug.Log($"Increased quality level to: {QualitySettings.names[currentQualityLevel]}");
            }
        }
        
        private void StartCullingSystem()
        {
            // Find all renderers in the scene
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (((1 << renderer.gameObject.layer) & cullLayers) != 0)
                {
                    cullableRenderers.Add(renderer);
                }
            }
            
            cullingCoroutine = StartCoroutine(CullingLoop());
        }
        
        private IEnumerator CullingLoop()
        {
            while (true)
            {
                if (mainCamera != null)
                {
                    Vector3 cameraPosition = mainCamera.transform.position;
                    
                    foreach (Renderer renderer in cullableRenderers)
                    {
                        if (renderer != null)
                        {
                            float distance = Vector3.Distance(cameraPosition, renderer.transform.position);
                            bool shouldCull = distance > cullDistance;
                            
                            if (enableFrustumCulling)
                            {
                                // Additional frustum culling check
                                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
                                bool inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
                                shouldCull = shouldCull || !inFrustum;
                            }
                            
                            renderer.enabled = !shouldCull;
                        }
                    }
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private IEnumerator PeriodicGarbageCollection()
        {
            while (true)
            {
                yield return new WaitForSeconds(gcInterval);
                
                // Force garbage collection during low activity
                if (Time.timeScale <= 0.1f || Time.deltaTime > 0.05f)
                {
                    System.GC.Collect();
                    Resources.UnloadUnusedAssets();
                }
            }
        }
        
        #region Object Pooling
        
        public void RegisterPool(string poolName, GameObject prefab, int initialSize = 10)
        {
            if (!objectPools.ContainsKey(poolName))
            {
                objectPools[poolName] = new Queue<GameObject>();
                poolPrefabs[poolName] = prefab;
                
                // Pre-populate pool
                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    objectPools[poolName].Enqueue(obj);
                }
            }
        }
        
        public GameObject GetPooledObject(string poolName)
        {
            if (!objectPools.ContainsKey(poolName))
                return null;
            
            if (objectPools[poolName].Count > 0)
            {
                GameObject obj = objectPools[poolName].Dequeue();
                obj.SetActive(true);
                return obj;
            }
            else
            {
                // Create new object if pool is empty
                GameObject obj = Instantiate(poolPrefabs[poolName]);
                obj.SetActive(true);
                return obj;
            }
        }
        
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (objectPools.ContainsKey(poolName))
            {
                obj.SetActive(false);
                objectPools[poolName].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetTargetFrameRate(int frameRate)
        {
            targetFrameRate = frameRate;
            Application.targetFrameRate = frameRate;
        }
        
        public void SetCullDistance(float distance)
        {
            cullDistance = distance;
        }
        
        public void EnableDynamicQuality(bool enable)
        {
            enableDynamicQuality = enable;
        }
        
        public void ForceQualityLevel(int level)
        {
            if (level >= 0 && level < QualitySettings.names.Length)
            {
                currentQualityLevel = level;
                QualitySettings.SetQualityLevel(level, true);
                enableDynamicQuality = false;
            }
        }
        
        public float GetCurrentFPS()
        {
            return averageFrameTime > 0 ? 1f / averageFrameTime : 0f;
        }
        
        public void OptimizeForLowEndDevice()
        {
            // Set conservative settings for low-end devices
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadowDistance = 25f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            
            enableDynamicQuality = true;
            targetFrameRate = 30;
            Application.targetFrameRate = 30;
        }
        
        public void OptimizeForHighEndDevice()
        {
            // Set high quality settings for high-end devices
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            QualitySettings.shadowDistance = 100f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            
            enableDynamicQuality = true;
            targetFrameRate = 60;
            Application.targetFrameRate = 60;
        }
        
        public void RefreshCullableRenderers()
        {
            cullableRenderers.Clear();
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (((1 << renderer.gameObject.layer) & cullLayers) != 0)
                {
                    cullableRenderers.Add(renderer);
                }
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (cullingCoroutine != null)
            {
                StopCoroutine(cullingCoroutine);
            }
        }
    }
}
