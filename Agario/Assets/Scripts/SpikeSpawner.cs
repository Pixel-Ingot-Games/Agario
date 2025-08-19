using UnityEngine;

public class SpikeSpawner : MonoBehaviour
{
	[Header("Spike Spawning")]
	[SerializeField] private GameObject spikePrefab;
	[SerializeField] private int maxSpikes = 10;
	[SerializeField] private float spawnRadius = 50f;
	[SerializeField] private float minDistanceFromCenter = 10f;
	[SerializeField] private float minDistanceBetweenSpikes = 5f;
	
	[Header("Spawning Behavior")]
	[SerializeField] private bool spawnOnStart = true;
	[SerializeField] private float spawnDelay = 2f;
	[SerializeField] private bool continuousSpawning = false;
	
	private GameObject[] spawnedSpikes;
	private int currentSpikeCount = 0;
	
	void Start()
	{
		if (spawnOnStart)
		{
			SpawnInitialSpikes();
		}
		
		if (continuousSpawning)
		{
			InvokeRepeating(nameof(SpawnSpikeIfNeeded), spawnDelay, spawnDelay);
		}
	}
	
	void SpawnInitialSpikes()
	{
		spawnedSpikes = new GameObject[maxSpikes];
		
		for (int i = 0; i < maxSpikes; i++)
		{
			SpawnSpike();
		}
	}
	
	void SpawnSpikeIfNeeded()
	{
		// Count active spikes
		currentSpikeCount = 0;
		for (int i = 0; i < spawnedSpikes.Length; i++)
		{
			if (spawnedSpikes[i] != null)
			{
				currentSpikeCount++;
			}
		}
		
		// Spawn new spike if we're below max
		if (currentSpikeCount < maxSpikes)
		{
			SpawnSpike();
		}
	}
	
	void SpawnSpike()
	{
		if (spikePrefab == null) return;
		
		Vector3 spawnPos = GetValidSpawnPosition();
		if (spawnPos != Vector3.zero)
		{
			GameObject spike = Instantiate(spikePrefab, spawnPos, Quaternion.identity, transform);
			
			// Store reference
			for (int i = 0; i < spawnedSpikes.Length; i++)
			{
				if (spawnedSpikes[i] == null)
				{
					spawnedSpikes[i] = spike;
					break;
				}
			}
			
			Debug.Log($"Spawned spike at {spawnPos}");
		}
	}
	
	Vector3 GetValidSpawnPosition()
	{
		int attempts = 0;
		const int maxAttempts = 100;
		
		while (attempts < maxAttempts)
		{
			// Random position within spawn radius
			Vector2 randomDir = Random.insideUnitCircle.normalized;
			float randomDistance = Random.Range(minDistanceFromCenter, spawnRadius);
			Vector3 candidatePos = new Vector3(randomDir.x, randomDir.y, 0) * randomDistance;
			
			// Check if position is valid (not too close to other spikes)
			if (IsValidPosition(candidatePos))
			{
				return candidatePos;
			}
			
			attempts++;
		}
		
		Debug.LogWarning("Could not find valid spike spawn position after 100 attempts");
		return Vector3.zero;
	}
	
	bool IsValidPosition(Vector3 position)
	{
		// Check distance from existing spikes
		for (int i = 0; i < spawnedSpikes.Length; i++)
		{
			if (spawnedSpikes[i] != null)
			{
				float distance = Vector3.Distance(position, spawnedSpikes[i].transform.position);
				if (distance < minDistanceBetweenSpikes)
				{
					return false;
				}
			}
		}
		
		return true;
	}
	
	// Visual debugging - draw spawn area
	void OnDrawGizmosSelected()
	{
		// Draw spawn radius
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, spawnRadius);
		
		// Draw minimum distance from center
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, minDistanceFromCenter);
	}
}
