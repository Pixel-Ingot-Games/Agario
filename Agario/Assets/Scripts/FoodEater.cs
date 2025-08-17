using UnityEngine;
using System.Collections.Generic;

public class FoodEater : MonoBehaviour
{
    [Header("Eating Settings")]
    [SerializeField] private float baseEatRadius = 1f;
    [SerializeField] private LayerMask foodLayerMask = 1 << 8; // Default to layer 8 (Food)
    [SerializeField] private float eatCheckRate = 0.1f; // How often to check for food
    
    [Header("Player Growth")]
    [SerializeField] private float sizeMultiplier = 0.1f; // Multiplier for food points to size increase
    [SerializeField] private float maxSize = 10f;
    [SerializeField] private float minSize = 0.5f;
    
    [Header("Eat Radius Scaling")]
    [SerializeField] private float radiusScaleMultiplier = 0.5f; // How much eat radius scales with size
    
    // Private variables
    private float nextEatCheck;
    private int currentScore = 0;
    private float currentSize;
    private Vector3 originalScale;
    private float currentEatRadius;
    
    // References
    private FoodSpawner foodSpawner;
    private SpriteRenderer playerSprite;
    
    void Start()
    {
        // Initialize
        currentSize = 1f;
        originalScale = transform.localScale;
        currentEatRadius = baseEatRadius;
        
        // Get components
        playerSprite = GetComponent<SpriteRenderer>();
        
        // Find food spawner
        foodSpawner = FindObjectOfType<FoodSpawner>();
        if (foodSpawner == null)
        {
            Debug.LogWarning("FoodSpawner not found! Score and pool management may not work properly.");
        }
        
        // Set initial size
        UpdatePlayerSize();
    }
    
    void Update()
    {
        // Check for food to eat at specified rate
        if (Time.time >= nextEatCheck)
        {
            CheckForFood();
            nextEatCheck = Time.time + eatCheckRate;
        }
    }
    
    void CheckForFood()
    {
        // Cast a sphere to detect food objects
        Collider2D[] foodColliders = Physics2D.OverlapCircleAll(transform.position, currentEatRadius, foodLayerMask);
        
        foreach (Collider2D foodCollider in foodColliders)
        {
            if (foodCollider != null && foodCollider.gameObject.activeInHierarchy)
            {
                // Check if food has the Food script and hasn't been eaten yet
                Food foodScript = foodCollider.GetComponent<Food>();
                if (foodScript != null && !foodScript.eaten)
                {
                    EatFood(foodCollider.gameObject, foodScript);
                }
            }
        }
    }
    
    void EatFood(GameObject food, Food foodScript)
    {
        // Mark food as eaten
        foodScript.eaten = true;
        
        // Get points from the food script
        int foodPoints = foodScript.point;
        
        // Increase score
        currentScore += foodPoints;
        
        // Increase player's Points variable from CharcterMovement script
        CharcterMovement playerMovement = GetComponent<CharcterMovement>();
        if (playerMovement != null)
        {
            playerMovement.Points += foodPoints;
        }
        
        // Increase player size based on food points
        float sizeIncrease = foodPoints * sizeMultiplier;
        currentSize += sizeIncrease;
        currentSize = Mathf.Clamp(currentSize, minSize, maxSize);
        
        // Update player size and eat radius
        UpdatePlayerSize();
        
        // Return food to pool or destroy it
        if (foodSpawner != null)
        {
            foodSpawner.ReturnFoodToPool(food);
        }
        else
        {
            Destroy(food);
        }
        
        // Log eating (you can remove this in production)
        Debug.Log($"Ate food worth {foodPoints} points! Score: {currentScore}, Player Points: {playerMovement?.Points}, Size: {currentSize:F2}, Eat Radius: {currentEatRadius:F2}");
    }
    
    void UpdatePlayerSize()
    {
        // Update the player's scale based on current size
        transform.localScale = originalScale * currentSize;
        
        // Automatically calculate eat radius based on actual visual size
        if (playerSprite != null)
        {
            // Get the actual bounds of the sprite after scaling
            Bounds spriteBounds = playerSprite.bounds;
            float actualRadius = Mathf.Max(spriteBounds.extents.x, spriteBounds.extents.y);
            
            // Set eat radius to be slightly larger than the actual visual size
            currentEatRadius = actualRadius + baseEatRadius;
        }
        else
        {
            // Fallback to the old calculation if no sprite renderer
            currentEatRadius = baseEatRadius + (currentSize * radiusScaleMultiplier);
        }
    }
    
    // Public methods for other scripts to access
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public float GetCurrentSize()
    {
        return currentSize;
    }
    
    public float GetCurrentEatRadius()
    {
        return currentEatRadius;
    }
    
    public float GetBaseEatRadius()
    {
        return baseEatRadius;
    }
    
    public void SetBaseEatRadius(float newRadius)
    {
        baseEatRadius = newRadius;
        UpdatePlayerSize(); // This will recalculate currentEatRadius
    }
    
    public void AddScore(int points)
    {
        currentScore += points;
    }
    
    public void SetScore(int newScore)
    {
        currentScore = newScore;
    }
    
    // Visual debugging - draw the eat radius in the scene view
    void OnDrawGizmosSelected()
    {
        // Draw base radius in blue
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseEatRadius);
        
        // Draw current radius in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentEatRadius);
        
        // Draw a filled circle to show the current eating area
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, currentEatRadius);
    }
    
    // Alternative method using Physics2D.OverlapCircleNonAlloc for better performance
    void CheckForFoodOptimized()
    {
        // Pre-allocate array for better performance
        Collider2D[] foodColliders = new Collider2D[20]; // Adjust size based on expected max food nearby
        
        int foodCount = Physics2D.OverlapCircleNonAlloc(transform.position, currentEatRadius, foodColliders, foodLayerMask);
        
        for (int i = 0; i < foodCount; i++)
        {
            if (foodColliders[i] != null && foodColliders[i].gameObject.activeInHierarchy)
            {
                Food foodScript = foodColliders[i].GetComponent<Food>();
                if (foodScript != null && !foodScript.eaten)
                {
                    EatFood(foodColliders[i].gameObject, foodScript);
                }
            }
        }
    }
}
