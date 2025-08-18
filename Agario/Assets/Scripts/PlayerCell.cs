using UnityEngine;
using System.Collections;
using TMPro;
public class PlayerCell : MonoBehaviour
{
	[Header("Recombine Settings")]
	[SerializeField] private float recombineDelay = 8f;
	private float recombineReadyTime = 0f;
	
	[Header("Cell Properties")]
	public float amount = 0f; // Public variable for amount tracking
	public TMP_Text amountText;
	private CharcterMovement movement;
	private FoodEater eater;
	private SpriteRenderer spriteRenderer;
	
	void Awake()
	{
		movement = GetComponent<CharcterMovement>();
		eater = GetComponent<FoodEater>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}
	
	public float GetPoints()
	{
		return movement != null ? movement.Points : 0f;
	}
	
	public void SetPoints(float points)
	{
		if (movement != null)
		{
			movement.Points = points;
			movement.RefreshSpeed();
		}
		if (eater != null)
		{
			eater.SetSizeFromPoints(GetPoints());
		}
	}
	
	public float GetRadius()
	{
		return eater != null ? eater.GetCurrentEatRadius() : 0.5f;
	}
	
	// Visual collision radius (without extra baseEatRadius padding)
	public float GetCollisionRadius()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
		}
		if (spriteRenderer != null)
		{
			Bounds b = spriteRenderer.bounds;
			return Mathf.Max(b.extents.x, b.extents.y);
		}
		// Fallback
		return Mathf.Max(GetRadius() - 0.1f, 0.1f);
	}
	
	public void StartRecombineCooldown()
	{
		recombineReadyTime = Time.time + recombineDelay;
	}
	
	public bool CanRecombine()
	{
		return Time.time >= recombineReadyTime;
	}
	
	public void LaunchTowards(Vector3 worldTarget, float speed, float duration)
	{
		StopAllCoroutines();
		StartCoroutine(LaunchRoutine(worldTarget, speed, duration));
	}
	
	private IEnumerator LaunchRoutine(Vector3 worldTarget, float speed, float duration)
	{
		Vector3 start = transform.position;
		Vector3 dir = (worldTarget - start);
		dir.z = 0f;
		dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
		float endTime = Time.time + duration;
		while (Time.time < endTime)
		{
			transform.position += dir * speed * Time.deltaTime;
			yield return null;
		}
	}
	void Update(){
		amountText.text="$"+amount.ToString();
	}
}
