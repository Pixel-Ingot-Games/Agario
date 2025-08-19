using UnityEngine;

public class Spike : MonoBehaviour
{
	[Header("Spike Settings")]
	[SerializeField] private float destroyThreshold = 20f; // Points above which cells get destroyed
	[SerializeField] private int foodPiecesSpawned = 8; // How many food pieces to spawn when cell is destroyed
	[SerializeField] private float foodSpawnRadius = 2f; // Radius around spike to spawn food
	[SerializeField] private GameObject foodPrefab; // Food prefab to spawn
	[SerializeField] private float checkRadius = 3f; // Radius to check for cells
	[SerializeField] private float checkRate = 0.2f; // How often to check for cells
	[SerializeField] private LayerMask playerLayerMask = 1 << 6; // Player cells layer (adjust as needed)
	
	[Header("Visual")]
	[SerializeField] private Color spikeColor = Color.red;
	[SerializeField] private float pulseSpeed = 2f;
	[SerializeField] private float pulseAmount = 0.1f;
	
	private SpriteRenderer spriteRenderer;
	private float originalScale;
	private float nextCheckTime;
	
	void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer != null)
		{
			spriteRenderer.color = spikeColor;
		}
		originalScale = transform.localScale.x;
	}
	
	void Update()
	{
		// Pulsing effect to make spike more visible
		if (spriteRenderer != null)
		{
			float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
			transform.localScale = Vector3.one * (originalScale + pulse);
		}
		
		// Periodic check for nearby cells
		if (Time.time >= nextCheckTime)
		{
			CheckForNearbyCells();
			nextCheckTime = Time.time + checkRate;
		}
	}
	
	void CheckForNearbyCells()
	{
		Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, checkRadius, playerLayerMask);
		
		foreach (Collider2D col in colliders)
		{
			PlayerCell playerCell = col.GetComponent<PlayerCell>();
			if (playerCell != null)
			{
				float cellPoints = playerCell.GetPoints();
				
				// Only destroy cells above the threshold
				if (cellPoints >= destroyThreshold)
				{
					DestroyCell(playerCell);
				}
			}
		}
	}
	
	void DestroyCell(PlayerCell cell)
	{
		// Get the cell's position and points
		Vector3 cellPos = cell.transform.position;
		float cellPoints = cell.GetPoints();
		
		// Notify PlayerAggregate to perform a virus-style split
		PlayerAggregate aggregate = cell.GetComponentInParent<PlayerAggregate>();
		if (aggregate != null)
		{
			aggregate.VirusSplit(cell, transform.position);
		}
		
		// Log event (you can remove this in production)
		Debug.Log($"Virus triggered on cell with {cellPoints} points at {transform.position}");
	}
	
	void SpawnFoodPieces(Vector3 centerPos, float totalPoints)
	{
		if (foodPrefab == null) return;
		
		float pointsPerPiece = totalPoints / foodPiecesSpawned;
		
		for (int i = 0; i < foodPiecesSpawned; i++)
		{
			// Random position around the spike
			Vector2 randomDir = Random.insideUnitCircle.normalized;
			float randomDistance = Random.Range(0.5f, foodSpawnRadius);
			Vector3 spawnPos = centerPos + new Vector3(randomDir.x, randomDir.y, 0) * randomDistance;
			
			// Spawn food
			GameObject food = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
			
			// Set food points
			Food foodScript = food.GetComponent<Food>();
			if (foodScript != null)
			{
				foodScript.point = pointsPerPiece;
			}
		}
	}
	
	// Visual debugging - draw the check radius and spawn radius
	void OnDrawGizmosSelected()
	{
		// Draw check radius (area where cells are detected)
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, checkRadius);
		
		// Draw food spawn radius
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, foodSpawnRadius);
	}
}
