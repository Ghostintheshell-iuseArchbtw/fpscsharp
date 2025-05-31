using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool expandable = true;
    }
    
    public static ObjectPool Instance;
    
    [SerializeField] private List<Pool> pools;
    
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;
    private Dictionary<string, Transform> poolParentDictionary;
    
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
        
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();
        poolParentDictionary = new Dictionary<string, Transform>();
        
        // Create the pool for each prefab
        foreach (Pool pool in pools)
        {
            // Create parent transform for organization
            GameObject poolParent = new GameObject($"Pool_{pool.tag}");
            poolParent.transform.parent = transform;
            poolParentDictionary.Add(pool.tag, poolParent.transform);
            
            Queue<GameObject> objectPool = new Queue<GameObject>();
            
            // Create initial objects for the pool
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab, poolParent.transform);
                objectPool.Enqueue(obj);
            }
            
            // Add the pool to the dictionary
            poolDictionary.Add(pool.tag, objectPool);
            prefabDictionary.Add(pool.tag, pool.prefab);
        }
    }
    
    private GameObject CreateNewObject(GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false);
        
        // If the object has a PooledObject component, set it up
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if (pooledObject == null)
        {
            pooledObject = obj.AddComponent<PooledObject>();
        }
        
        return obj;
    }
    
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        // Check if the pool exists
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist!");
            return null;
        }
        
        // Check if there are objects available in the pool
        Queue<GameObject> pool = poolDictionary[tag];
        
        // If the pool is empty and not expandable, return null
        if (pool.Count == 0)
        {
            // Find the pool configuration
            Pool poolConfig = pools.Find(p => p.tag == tag);
            
            // If the pool is expandable, create a new object
            if (poolConfig != null && poolConfig.expandable)
            {
                GameObject newObj = CreateNewObject(prefabDictionary[tag], poolParentDictionary[tag]);
                return SpawnObject(newObj, position, rotation);
            }
            else
            {
                Debug.LogWarning($"Pool with tag {tag} is empty and not expandable!");
                return null;
            }
        }
        
        // Get the next object from the pool
        GameObject objectToSpawn = pool.Dequeue();
        
        // If the object has been destroyed (scene change, etc.), create a new one
        if (objectToSpawn == null)
        {
            objectToSpawn = CreateNewObject(prefabDictionary[tag], poolParentDictionary[tag]);
        }
        
        // Spawn the object
        return SpawnObject(objectToSpawn, position, rotation);
    }
    
    private GameObject SpawnObject(GameObject obj, Vector3 position, Quaternion rotation)
    {
        // Set position and rotation
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        // Activate the object
        obj.SetActive(true);
        
        // Get the pooled object component
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        
        // Initialize the pooled object
        if (pooledObject != null)
        {
            pooledObject.OnObjectSpawn();
        }
        
        return obj;
    }
    
    public void ReturnToPool(string tag, GameObject obj)
    {
        // Check if the pool exists
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist!");
            return;
        }
        
        // Get the pooled object component
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        
        // Let the object clean up
        if (pooledObject != null)
        {
            pooledObject.OnObjectReturn();
        }
        
        // Deactivate the object
        obj.SetActive(false);
        
        // Return to parent transform for organization
        obj.transform.parent = poolParentDictionary[tag];
        
        // Return to the pool
        poolDictionary[tag].Enqueue(obj);
    }
    
    // Utility method to automatically return object to pool after a delay
    public void ReturnToPoolAfterDelay(string tag, GameObject obj, float delay)
    {
        StartCoroutine(ReturnAfterDelayCoroutine(tag, obj, delay));
    }
    
    private System.Collections.IEnumerator ReturnAfterDelayCoroutine(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null && obj.activeInHierarchy)
        {
            ReturnToPool(tag, obj);
        }
    }
    
    // Helper method to clear all pools (useful for scene transitions)
    public void ClearAllPools()
    {
        foreach (var pool in poolDictionary.Values)
        {
            pool.Clear();
        }
    }
}

// Component to handle pooled object lifecycle
public class PooledObject : MonoBehaviour
{
    [SerializeField] private string poolTag;
    [SerializeField] private float autoReturnTime = -1f; // -1 means no auto return
    
    private bool hasBeenInitialized = false;
    
    // This will be called when the object is spawned from the pool
    public virtual void OnObjectSpawn()
    {
        hasBeenInitialized = true;
        
        // If auto return is enabled, schedule it
        if (autoReturnTime > 0)
        {
            ObjectPool.Instance.ReturnToPoolAfterDelay(poolTag, gameObject, autoReturnTime);
        }
    }
    
    // This will be called when the object is returned to the pool
    public virtual void OnObjectReturn()
    {
        hasBeenInitialized = false;
        
        // Reset object state here
        // For example, you might reset animations, particle systems, etc.
    }
    
    // Helper method to manually return this object to its pool
    public void ReturnToPool()
    {
        if (hasBeenInitialized && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
    }
    
    // Setup the pool tag
    public void SetPoolTag(string tag)
    {
        poolTag = tag;
    }
}
