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

	[Header("Virus Settings")]
	[SerializeField] private int maxCells = 16; // Maximum total cells allowed after virus split
	[SerializeField] private float virusLaunchSpeed = 25f;
	[SerializeField] private float virusLaunchDuration = 0.3f;
	
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
		
		// Collect all eligible cells to split
		List<PlayerCell> eligible = new List<PlayerCell>();
		foreach (var c in cells)
		{
			if (c == null) continue;
			float p = c.GetPoints();
			if (p >= required) eligible.Add(c);
		}
		if (eligible.Count == 0) return;
		
		// Respect max cells cap: only create as many new cells as we have slots for
		int availableSlots = Mathf.Max(0, maxCells - cells.Count);
		if (availableSlots <= 0) return;
		
		// If too many eligible, split the largest ones first
		if (eligible.Count > availableSlots)
		{
			eligible.Sort((a, b) => b.GetPoints().CompareTo(a.GetPoints()));
			eligible = eligible.GetRange(0, availableSlots);
		}
		
		// Perform splits for each selected cell
		foreach (var parent in eligible)
		{
			if (parent == null) continue;
			float parentPoints = parent.GetPoints();
			float half = parentPoints * 0.5f;
			if (half < minPointsPerCell) continue;
			
			Vector3 spawnPos = parent.transform.position;
			PlayerCell newCell = Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellsParent != null ? cellsParent : transform);
			newCell.StartRecombineCooldown();
			cells.Add(newCell);
			
			// Copy sprite and color from parent to new cell
			var parentSr = parent.GetComponent<SpriteRenderer>();
			var newSr = newCell.GetComponent<SpriteRenderer>();
			if (parentSr != null && newSr != null)
			{
				newSr.sprite = parentSr.sprite;
				newSr.color = parentSr.color;
			}
			
			// Copy FoodEater configuration to ensure both cells use identical base scales
			var parentEater = parent.GetComponent<FoodEater>();
			var newEater = newCell.GetComponent<FoodEater>();
			if (parentEater != null && newEater != null)
			{
				newEater.CopyConfigFrom(parentEater);
				Vector3 sharedBaseScale = parentEater.GetBaseScale();
				parentEater.SetBaseScale(sharedBaseScale);
				newEater.SetBaseScale(sharedBaseScale);
			}
			
			// Halve and transfer 'amount'
			float parentAmount = parent.amount;
			float halfAmount = parentAmount * 0.5f;
			parent.amount = halfAmount;
			newCell.amount = halfAmount;
			
			// Apply points
			parent.SetPoints(half);
			var newMove = newCell.GetComponent<CharcterMovement>();
			if (newMove != null)
			{
				newMove.Points = half;
			}
			
			// Launch new cell toward cursor
			Vector3 worldTarget = GetCursorWorldPosition(spawnPos.z);
			newCell.LaunchTowards(worldTarget, splitLaunchSpeed, splitLaunchDuration);
			
			// Sync scales next frame
			StartCoroutine(SyncCellScalesNextFrame(parent, newCell));
		}
		
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
	
	// Called when a cell is destroyed by external forces (like spikes)
	public void RemoveDestroyedCell(PlayerCell destroyedCell)
	{
		if (cells.Contains(destroyedCell))
		{
			cells.Remove(destroyedCell);
			UpdateTotalPoints(true);
			Debug.Log($"Removed destroyed cell from aggregate. Remaining cells: {cells.Count}");
		}
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

	public void VirusSplit(PlayerCell target, Vector3 sourcePos)
	{
		if (target == null) return;
		// Guard: must have prefab
		if (cellPrefab == null) return;
		
		// We'll keep splitting the largest eligible cell each iteration until limits reached
		int safety = 64;
		while (safety-- > 0)
		{
			// Stop if we already have too many cells
			if (cells.Count >= maxCells) break;
			
			// Pick the largest eligible cell (start with provided if still present)
			PlayerCell best = null;
			float bestPoints = 0f;
			foreach (var c in cells)
			{
				if (c == null) continue;
				float p = c.GetPoints();
				if (p >= Mathf.Max(2f * minPointsPerCell, splitThreshold) && p > bestPoints)
				{
					best = c;
					bestPoints = p;
				}
			}
			if (best == null) break;
			
			// Halve points; ensure per-piece >= minPointsPerCell
			float half = bestPoints * 0.5f;
			if (half < minPointsPerCell) break;
			
			// Instantiate new cell at best position
			Vector3 spawnPos = best.transform.position;
			PlayerCell newCell = Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellsParent != null ? cellsParent : transform);
			newCell.StartRecombineCooldown();
			cells.Add(newCell);
			
			// Copy sprite/color
			var parentSr = best.GetComponent<SpriteRenderer>();
			var newSr = newCell.GetComponent<SpriteRenderer>();
			if (parentSr != null && newSr != null)
			{
				newSr.sprite = parentSr.sprite;
				newSr.color = parentSr.color;
			}
			
			// Copy eater config and base scale
			var parentEater = best.GetComponent<FoodEater>();
			var newEater = newCell.GetComponent<FoodEater>();
			if (parentEater != null && newEater != null)
			{
				newEater.CopyConfigFrom(parentEater);
				Vector3 sharedBaseScale = parentEater.GetBaseScale();
				parentEater.SetBaseScale(sharedBaseScale);
				newEater.SetBaseScale(sharedBaseScale);
			}
			
			// Split points and amount
			best.SetPoints(half);
			newCell.SetPoints(half);
			float halfAmount = best.amount * 0.5f;
			best.amount = halfAmount;
			newCell.amount = halfAmount;
			
			// Launch new cell away from virus source position
			Vector3 awayDir = (best.transform.position - sourcePos);
			awayDir.z = 0f;
			awayDir = awayDir.sqrMagnitude > 0.0001f ? awayDir.normalized : Random.insideUnitCircle.normalized;
			Vector3 worldTarget = best.transform.position + awayDir * 3f;
			newCell.LaunchTowards(worldTarget, virusLaunchSpeed, virusLaunchDuration);
		}
		
		UpdateTotalPoints(true);
	}
}
