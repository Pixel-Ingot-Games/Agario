using UnityEngine;
using System.Collections.Generic;

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
		best.SetPoints(half);
		best.StartRecombineCooldown();
		
		// Instantiate new cell
		Vector3 spawnPos = best.transform.position;
		PlayerCell newCell = Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellsParent != null ? cellsParent : transform);
		newCell.SetPoints(half);
		newCell.StartRecombineCooldown();
		cells.Add(newCell);
		
		// Recalculate movement speeds explicitly on both cells
		var bestMove = best.GetComponent<CharcterMovement>();
		if (bestMove != null) bestMove.RefreshSpeed();
		var newMove = newCell.GetComponent<CharcterMovement>();
		if (newMove != null) newMove.RefreshSpeed();
		
		// Launch new cell toward cursor
		Vector3 worldTarget = GetCursorWorldPosition(spawnPos.z);
		newCell.LaunchTowards(worldTarget, splitLaunchSpeed, splitLaunchDuration);
		
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
		
		// Merge: add points, keep 'a', remove 'b'
		float mergedPoints = a.GetPoints() + b.GetPoints();
		a.SetPoints(mergedPoints);
		
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
}
