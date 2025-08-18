using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class PlayerAggregate : MonoBehaviour
{
	[Header("Cells Management")]
	[SerializeField] private PlayerCell cellPrefab;
	[SerializeField] private Transform cellsParent;
	[SerializeField] private List<PlayerCell> cells = new List<PlayerCell>();
	
	[Header("Split Settings")]
	[SerializeField] private float splitThreshold = 20f;
	[SerializeField] private float minPointsPerCell = 5f; // Each resulting half must be at least this many points
	[SerializeField] private float splitLaunchSpeed = 20f;
	[SerializeField] private float splitLaunchDuration = 0.2f;
	[SerializeField] private KeyCode splitKey = KeyCode.Space;
	
	[Header("Eject Settings")]
	[SerializeField] private GameObject ejectPrefab;
	[SerializeField] private float ejectMinPoints = 10f; // Minimum points required to eject
	[SerializeField] private float ejectForce = 15f; // Force applied to ejected cell
	[SerializeField] private float ejectPointCost = 5f; // Points lost when ejecting
	[SerializeField] private KeyCode ejectKey = KeyCode.W;
	
	[Header("Merge Settings")]
	[SerializeField] private float mergeCheckRate = 0.1f;
	private float nextMergeCheck;
	
	[Header("Separation Settings")]
	[SerializeField] private float separationStrength = 5f; // units per second for resolving overlap
	[SerializeField] private float separationSlack = 0.02f; // allow tiny overlap tolerance
	
	public System.Action<float> OnTotalPointsChanged;
	public float lastTotalPoints;
	
	void Start()
	{
		if (cells.Count == 0)
		{
			// Try find an existing cell on this object
			var existing = GetComponent<PlayerCell>();
			if (existing != null)
			{
				cells.Add(existing);
			}
		}
		UpdateTotalPoints(true);
	}
	
	void Update()
	{
		HandleSplitInput();
		HandleEjectInput();
		HandleMergeTick();
		ResolveOverlaps();
		// Recompute totals so HUD updates when any cell eats
		UpdateTotalPoints(false);
	}
	
	float GetRequiredPointsToSplit()
	{
		// Require enough points so both halves are >= minPointsPerCell
		return Mathf.Max(splitThreshold, 2f * minPointsPerCell);
	}
	
	void HandleSplitInput()
	{
		if (!Input.GetKeyDown(splitKey)) return;
		
		float required = GetRequiredPointsToSplit();
		
		// Split the largest eligible cell first
		PlayerCell best = null;
		float bestPoints = 0f;
		foreach (var c in cells)
		{
			float p = c.GetPoints();
			if (p >= required && p > bestPoints)
			{
				best = c;
				bestPoints = p;
			}
		}
		if (best == null) return;
		
		// Halve points
		float half = bestPoints * 0.5f;
		
		// Instantiate new cell
		Vector3 spawnPos = best.transform.position;
		PlayerCell newCell = Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellsParent != null ? cellsParent : transform);
		newCell.StartRecombineCooldown();
		cells.Add(newCell);
		
		// Copy sprite and color from parent to new cell
		var parentSr = best.GetComponent<SpriteRenderer>();
		var newSr = newCell.GetComponent<SpriteRenderer>();
		if (parentSr != null && newSr != null)
		{
			newSr.sprite = parentSr.sprite;
			newSr.color = parentSr.color;
		}
		
		// Halve and transfer 'amount'
		float parentAmount = best.amount;
		float halfAmount = parentAmount * 0.5f;
		best.amount = halfAmount;
		newCell.amount = halfAmount;
		
		// HALVE POINTS FIRST - this will rescale the original cell
		best.SetPoints(half);
		
		// Set points on new cell but DON'T let it scale yet
		if (newCell.GetComponent<CharcterMovement>() != null)
		{
			newCell.GetComponent<CharcterMovement>().Points = half;
		}
		
		// Launch new cell toward cursor
		Vector3 worldTarget = GetCursorWorldPosition(spawnPos.z);
		newCell.LaunchTowards(worldTarget, splitLaunchSpeed, splitLaunchDuration);
		
		// Start coroutine to sync scales in next frame
		StartCoroutine(SyncCellScalesNextFrame(best, newCell));
		
		UpdateTotalPoints(true);
	}
	
	void HandleEjectInput()
	{
		if (!Input.GetKeyDown(ejectKey)) return;
		if (ejectPrefab == null)
		{
			Debug.LogWarning("Eject prefab not assigned!");
			return;
		}
		
		// Find the biggest cell that can eject
		PlayerCell best = null;
		float bestPoints = 0f;
		foreach (var c in cells)
		{
			float p = c.GetPoints();
			if (p >= ejectMinPoints && p > bestPoints)
			{
				best = c;
				bestPoints = p;
			}
		}
		if (best == null) return;
		
		// Spawn eject cell in mouse direction
		Vector3 spawnPos = best.transform.position;
		Vector3 worldTarget = GetCursorWorldPosition(spawnPos.z);
		Vector3 direction = (worldTarget - spawnPos).normalized;
		
		GameObject ejectedCell = Instantiate(ejectPrefab, spawnPos, Quaternion.identity);
		
		// Copy color from parent cell
		var parentSr = best.GetComponent<SpriteRenderer>();
		var ejectedSr = ejectedCell.GetComponent<SpriteRenderer>();
		if (parentSr != null && ejectedSr != null)
		{
			ejectedSr.color = parentSr.color;
		}
		
		// Set movement direction for the ejected cell
		EjectedCell ejectedCellScript = ejectedCell.GetComponent<EjectedCell>();
		if (ejectedCellScript != null)
		{
			ejectedCellScript.SetMoveDirection(direction);
		}
		
		// Reduce parent cell's points by the eject cost
		float newPoints = bestPoints - ejectPointCost;
		best.SetPoints(newPoints);
		
		UpdateTotalPoints(true);
	}
	
	Vector3 GetCursorWorldPosition(float z)
	{
		Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
		Vector3 sp = Input.mousePosition;
		Vector3 wp = cam != null ? cam.ScreenToWorldPoint(sp) : new Vector3(0,0,z);
		wp.z = z;
		return wp;
	}
	
	void HandleMergeTick()
	{
		if (Time.time < nextMergeCheck) return;
		nextMergeCheck = Time.time + mergeCheckRate;
		
		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = i + 1; j < cells.Count; j++)
			{
				TryMerge(cells[i], cells[j]);
			}
		}
	}
	
	void TryMerge(PlayerCell a, PlayerCell b)
	{
		if (a == null || b == null) return;
		if (!a.CanRecombine() || !b.CanRecombine()) return;
		
		float dist = Vector3.Distance(a.transform.position, b.transform.position);
		float sumR = a.GetRadius() + b.GetRadius();
		if (dist > sumR) return;
		
		// Merge: add points and amount, keep 'a', remove 'b'
		float mergedPoints = a.GetPoints() + b.GetPoints();
		float mergedAmount = a.amount + b.amount;
		a.SetPoints(mergedPoints);
		a.amount = mergedAmount;
		
		cells.Remove(b);
		Destroy(b.gameObject);
		
		UpdateTotalPoints(true);
	}
	
	void ResolveOverlaps()
	{
		for (int i = 0; i < cells.Count; i++)
		{
			var a = cells[i];
			if (a == null) continue;
			for (int j = i + 1; j < cells.Count; j++)
			{
				var b = cells[j];
				if (b == null) continue;
				
				float ra = a.GetCollisionRadius();
				float rb = b.GetCollisionRadius();
				float sum = ra + rb - separationSlack;
				Vector3 pa = a.transform.position;
				Vector3 pb = b.transform.position;
				Vector3 delta = pb - pa; delta.z = 0f;
				float d = delta.magnitude;
				if (d < 0.0001f)
				{
					// Same position; push in a fixed direction
					delta = Vector3.right;
					d = 0.0001f;
				}
				if (d < sum)
				{
					// If both can recombine, skip separation and let merge logic handle it
					if (a.CanRecombine() && b.CanRecombine())
					{
						continue;
					}
					
					Vector3 n = delta / d;
					float overlap = sum - d;
					Vector3 correction = n * overlap * 0.5f;
					// Move both cells apart proportionally this frame
					a.transform.position -= correction * separationStrength * Time.deltaTime;
					b.transform.position += correction * separationStrength * Time.deltaTime;
				}
			}
		}
	}
	
	void UpdateTotalPoints(bool force)
	{
		float total = 0f;
		foreach (var c in cells)
		{
			if (c != null) total += c.GetPoints();
		}
		if (force || Mathf.Abs(total - lastTotalPoints) > 0.0001f)
		{
			lastTotalPoints = total;
			OnTotalPointsChanged?.Invoke(total);
		}
	}
	
	public float GetTotalPoints()
	{
		return lastTotalPoints;
	}
	
	public IReadOnlyList<PlayerCell> GetCells()
	{
		return cells;
	}
	
	// Coroutine to sync cell scales in the next frame
	private IEnumerator SyncCellScalesNextFrame(PlayerCell originalCell, PlayerCell newCell)
	{
		// Wait for next frame to ensure original cell has finished scaling
		yield return null;
		
		// Copy the exact scale from original cell to new cell
		if (originalCell != null && newCell != null)
		{
			Vector3 targetScale = originalCell.transform.localScale;
			newCell.transform.localScale = targetScale;
			
			// Sync the FoodEater's internal state to match the original cell
			var originalEater = originalCell.GetComponent<FoodEater>();
			var newEater = newCell.GetComponent<FoodEater>();
			if (originalEater != null && newEater != null)
			{
				// Sync internal state without triggering size recalculation
				newEater.SyncInternalStateFrom(originalEater);
			}
			
			// Sync movement speeds
			var originalMove = originalCell.GetComponent<CharcterMovement>();
			var newMove = newCell.GetComponent<CharcterMovement>();
			if (originalMove != null && newMove != null)
			{
				newMove.RefreshSpeed();
			}
		}
	}
}
