using UnityEngine;
using System.Collections.Generic;

public class FoodEater : MonoBehaviour
{
	[Header("Eating Settings")]
	[SerializeField] private float baseEatRadius = 1f;
	[SerializeField] private LayerMask foodLayerMask = 1 << 8; // Default to layer 8 (Food)
	[SerializeField] private float eatCheckRate = 0.1f; // How often to check for food
	
	[Header("Player Growth")]
	[SerializeField] private float sizeMultiplier = 1f; // Overall visual scale multiplier for growth above base
	[SerializeField] private float pointsAtScaleOne = 10f; // Normalization for sqrt growth
	[SerializeField] private float sizeBasePoints = 5f; // Points threshold; scale stays 1 at or below this
	[SerializeField] private float maxSize = 10f;
	[SerializeField] private float minSize = 1f; // Visual scale never below 1
	
	[Header("Eat Radius Scaling")]
	[SerializeField] private float radiusScaleMultiplier = 0.5f; // How much eat radius scales with size
	
	// Private variables
	private float nextEatCheck;
	private float currentScore = 0;
	private float currentSize;
	private Vector3 originalScale;
	private float currentEatRadius;
	private Vector3 lastPosition;
	
	// References
	private FoodSpawner foodSpawner;
	private SpriteRenderer playerSprite;
	
	void Awake()
	{
		// Cache renderer and base scale early so SetSizeFromPoints works right after Instantiate
		playerSprite = GetComponent<SpriteRenderer>();
		if (originalScale == Vector3.zero)
		{
			originalScale = transform.localScale;
			if (originalScale == Vector3.zero)
			{
				originalScale = Vector3.one;
			}
		}
		currentEatRadius = baseEatRadius;
		lastPosition = transform.position;
	}
	
	void Start()
	{
		// Initialize
		currentSize = 1f;
		
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
		// Continuous sweep to avoid tunneling when moving fast
		CheckForFoodSweep();
		lastPosition = transform.position;
		
		// Also do periodic area checks
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
	
	void CheckForFoodSweep()
	{
		Vector3 current = transform.position;
		Vector3 delta = current - lastPosition;
		float distance = delta.magnitude;
		if (distance <= 0.0001f) return;
		Vector2 dir = new Vector2(delta.x, delta.y).normalized;
		RaycastHit2D[] hits = Physics2D.CircleCastAll(lastPosition, currentEatRadius, dir, distance, foodLayerMask);
		for (int i = 0; i < hits.Length; i++)
		{
			var col = hits[i].collider;
			if (col == null) continue;
			var go = col.gameObject;
			if (!go.activeInHierarchy) continue;
			Food foodScript = go.GetComponent<Food>();
			if (foodScript != null && !foodScript.eaten)
			{
				EatFood(go, foodScript);
			}
		}
	}
	
	void EatFood(GameObject food, Food foodScript)
	{
		// Mark food as eaten
		foodScript.eaten = true;
		
		// Get points from the food script
		float foodPoints = foodScript.point;
		
		// Increase score
		currentScore += foodPoints;
		
		// Increase player's Points variable from CharcterMovement script
		CharcterMovement playerMovement = GetComponent<CharcterMovement>();
		if (playerMovement != null)
		{
			playerMovement.Points += foodPoints;
			// Recompute size from total points to stay consistent with split/merge
			SetSizeFromPoints(playerMovement.Points);
		}
		else
		{
			// Fallback: if no movement component, approximate by mapping just current points
			SetSizeFromPoints(foodPoints);
		}
		
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
		Debug.Log($"Ate food worth {foodPoints} points! Score: {currentScore}, Size: {currentSize:F2}, Eat Radius: {currentEatRadius:F2}");
	}
	
	void UpdatePlayerSize()
	{
		// Ensure non-zero base scale
		Vector3 baseScale = originalScale;
		if (baseScale == Vector3.zero)
		{
			baseScale = Vector3.one;
		}
		// Update the player's scale based on current size (currentSize is the full visual scale factor)
		transform.localScale = baseScale * Mathf.Max(currentSize, 1f);
		
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
			currentEatRadius = baseEatRadius + ((Mathf.Max(currentSize, 1f) - 1f) * radiusScaleMultiplier);
		}
	}
	
	// Public methods for other scripts to access
	public float GetCurrentScore()
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
	
	public void AddScore(float points)
	{
		currentScore += points;
	}
	
	public void SetScore(float newScore)
	{
		currentScore = newScore;
	}
	
	public float GetSizeMultiplier()
	{
		return sizeMultiplier;
	}
	
	// Force size based on absolute points (used by split/merge)
	public void SetSizeFromPoints(float totalPoints)
	{
		// Keep scale 1 at or below sizeBasePoints
		float surplus = Mathf.Max(totalPoints - sizeBasePoints, 0f);
		float denom = Mathf.Max(pointsAtScaleOne, 0.0001f);
		float norm = surplus / denom;
		float growth = Mathf.Sqrt(norm) * Mathf.Max(sizeMultiplier, 0f);
		float scale = 1f + growth;
		currentSize = Mathf.Clamp(scale, 1f, maxSize);
		UpdatePlayerSize();
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

    // Get the base scale used for size calculations
    public Vector3 GetBaseScale()
    {
        return originalScale == Vector3.zero ? Vector3.one : originalScale;
    }

    // Set the base scale used for size calculations
    public void SetBaseScale(Vector3 baseScale)
    {
        if (baseScale == Vector3.zero) baseScale = Vector3.one;
        originalScale = baseScale;
        UpdatePlayerSize();
    }

    // Clone sizing and radius config from another eater (to keep split cells identical)
    public void CopyConfigFrom(FoodEater other)
    {
        if (other == null) return;
        // Copy serialized tunables
        this.baseEatRadius = other.baseEatRadius;
        this.sizeMultiplier = other.sizeMultiplier;
        this.pointsAtScaleOne = other.pointsAtScaleOne;
        this.sizeBasePoints = other.sizeBasePoints;
        this.radiusScaleMultiplier = other.radiusScaleMultiplier;
        this.minSize = other.minSize;
        this.maxSize = other.maxSize;
        // Copy base scale
        this.originalScale = other.GetBaseScale();
        UpdatePlayerSize();
    }

    // Sync internal state from another eater without triggering size recalculation
    public void SyncInternalStateFrom(FoodEater other)
    {
        if (other == null) return;
        this.currentSize = other.currentSize;
        this.currentEatRadius = other.currentEatRadius;
        // Don't call UpdatePlayerSize() - just sync the values
    }
}
