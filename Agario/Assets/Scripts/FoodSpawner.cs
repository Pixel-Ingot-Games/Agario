using UnityEngine;
using System.Collections.Generic;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private FoodObjectPool foodPool;
    [SerializeField] private int initialSpawnCount = 100;
    [SerializeField] private float spawnRate = 0.5f; // Spawn every 0.5 seconds
    [SerializeField] private int maxFoodOnMap = 200;
    
    [Header("Initial Spawn Settings")]
    [SerializeField] private int spawnsPerFrame = 10; // How many food items to spawn per frame
    [SerializeField] private float initialSpawnDelay = 0.1f; // Delay between initial spawn batches
    
    [Header("Spawn Bounds")]
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -50f;
    [SerializeField] private float maxY = 50f;
    [SerializeField] private float zPosition = 0f;
    
    [Header("Food Properties")]
    [SerializeField] private float minFoodSize = 0.5f;
    [SerializeField] private float maxFoodSize = 1.5f;
    [SerializeField] private Color[] foodColors = {
        Color.red, Color.blue, Color.green, Color.yellow, 
        Color.magenta, Color.cyan, Color.white, Color.gray
    };
    
    private List<GameObject> spawnedFood = new List<GameObject>();
    private float nextSpawnTime;
    private Camera mainCamera;
    
    // Initial spawn variables
    private bool isInitialSpawning = false;
    private int initialSpawnIndex = 0;
    private float nextInitialSpawnTime;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // Check if food pool is assigned
        if (foodPool == null)
        {
            foodPool = FindObjectOfType<FoodObjectPool>();
            if (foodPool == null)
            {
                Debug.LogError("FoodObjectPool not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Start initial burst spawn over multiple frames
        StartInitialSpawn();
        
        // Set first spawn time for continuous spawning
        nextSpawnTime = Time.time + spawnRate;
    }
    
    void Update()
    {
        // Handle initial spawning over multiple frames
        if (isInitialSpawning)
        {
            HandleInitialSpawn();
        }
        
        // Continuous spawning (only after initial spawn is complete)
        if (!isInitialSpawning && Time.time >= nextSpawnTime && spawnedFood.Count < maxFoodOnMap)
        {
            SpawnFood();
            nextSpawnTime = Time.time + spawnRate;
        }
        
        // Clean up destroyed food from the list
        CleanupDestroyedFood();
    }
    
    void StartInitialSpawn()
    {
        isInitialSpawning = true;
        initialSpawnIndex = 0;
        nextInitialSpawnTime = Time.time + initialSpawnDelay;
    }
    
    void HandleInitialSpawn()
    {
        if (Time.time < nextInitialSpawnTime) return;
        
        // Spawn a batch of food items
        int itemsToSpawn = Mathf.Min(spawnsPerFrame, initialSpawnCount - initialSpawnIndex);
        
        for (int i = 0; i < itemsToSpawn; i++)
        {
            SpawnFood();
            initialSpawnIndex++;
        }
        
        // Check if initial spawn is complete
        if (initialSpawnIndex >= initialSpawnCount)
        {
            isInitialSpawning = false;
            Debug.Log($"Initial spawn complete! Spawned {initialSpawnCount} food items over {Mathf.CeilToInt((float)initialSpawnCount / spawnsPerFrame)} frames.");
        }
        else
        {
            // Schedule next batch
            nextInitialSpawnTime = Time.time + initialSpawnDelay;
        }
    }
    
    void SpawnFood()
    {
        if (foodPool == null) return;
        
        // Get food from pool
        GameObject newFood = foodPool.GetFood();
        if (newFood == null)
        {
            Debug.LogWarning("No food available in pool!");
            return;
        }
        
        // Generate random position within bounds
        Vector3 randomPosition = GetRandomPosition();
        
        // Position and activate the food
        newFood.transform.position = randomPosition;
        newFood.SetActive(true);
        
        // Randomize food properties
        RandomizeFoodProperties(newFood);
        
        // Add to spawned food list
        spawnedFood.Add(newFood);
    }
    
    Vector3 GetRandomPosition()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        
        return new Vector3(randomX, randomY, zPosition);
    }
    
    void RandomizeFoodProperties(GameObject food)
    {
        // Random size
        float randomSize = Random.Range(minFoodSize, maxFoodSize);
        food.transform.localScale = Vector3.one * randomSize;
        
        // Random color
        if (foodColors.Length > 0)
        {
            Color randomColor = foodColors[Random.Range(0, foodColors.Length)];
            SpriteRenderer spriteRenderer = food.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = randomColor;
            }
        }
    }
    
    void CleanupDestroyedFood()
    {
        // Remove destroyed food from the list
        spawnedFood.RemoveAll(food => food == null);
    }
    
    // Public method to manually spawn food at a specific position
    public void SpawnFoodAtPosition(Vector3 position)
    {
        if (foodPool == null || spawnedFood.Count >= maxFoodOnMap) return;
        
        GameObject newFood = foodPool.GetFood();
        if (newFood == null) return;
        
        newFood.transform.position = position;
        newFood.SetActive(true);
        RandomizeFoodProperties(newFood);
        spawnedFood.Add(newFood);
    }
    
    // Public method to return food to pool (call this when food is eaten)
    public void ReturnFoodToPool(GameObject food)
    {
        if (food == null || foodPool == null) return;
        
        // Remove from spawned list
        spawnedFood.Remove(food);
        
        // Return to pool
        foodPool.ReturnFood(food);
    }
    
    // Public method to get current food count
    public int GetCurrentFoodCount()
    {
        return spawnedFood.Count;
    }
    
    // Public method to check if still doing initial spawn
    public bool IsInitialSpawning()
    {
        return isInitialSpawning;
    }
    
    // Public method to get initial spawn progress
    public float GetInitialSpawnProgress()
    {
        if (initialSpawnCount <= 0) return 1f;
        return (float)initialSpawnIndex / initialSpawnCount;
    }
    
    // Public method to clear all food
    public void ClearAllFood()
    {
        foreach (GameObject food in spawnedFood)
        {
            if (food != null && foodPool != null)
            {
                foodPool.ReturnFood(food);
            }
        }
        spawnedFood.Clear();
    }
    
    // Public method to set spawn bounds
    public void SetSpawnBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
    }
    
    // Public method to adjust spawn rate
    public void SetSpawnRate(float newRate)
    {
        spawnRate = newRate;
    }
    
    // Public method to adjust max food count
    public void SetMaxFoodCount(int newMax)
    {
        maxFoodOnMap = newMax;
    }
    
    // Public method to adjust initial spawn settings
    public void SetInitialSpawnSettings(int spawnsPerFrame, float delay)
    {
        this.spawnsPerFrame = spawnsPerFrame;
        this.initialSpawnDelay = delay;
    }
    
    // Draw spawn bounds in the scene view (for debugging)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, zPosition);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}
