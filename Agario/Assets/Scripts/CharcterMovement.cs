using UnityEngine;

public class CharcterMovement : MonoBehaviour
{
	[Header("Movement Settings")]
	[SerializeField] private float baseMoveSpeed = 5f;
	[SerializeField] private float minMoveSpeed = 0.5f;
	[SerializeField] private float speedReductionMultiplier = 0.1f; // How much speed decreases per size unit
	
	[Header("Input Settings")]
	[SerializeField] private bool useTouchInput = true;
	[SerializeField] private bool useMouseInput = true;
	
	[Header("Bounds Settings")]
	[SerializeField] private bool useColliderBounds = false; // If true, use movementBounds; else use min/max
	[SerializeField] private BoxCollider2D movementBounds; // Optional: set in inspector
	[SerializeField] private Vector2 minXY = new Vector2(-50f, -50f);
	[SerializeField] private Vector2 maxXY = new Vector2( 50f,  50f);
	
	private Bounds cachedBounds;
	private SpriteRenderer spriteRenderer;
	
	private Vector3 targetPosition;
	private Camera mainCamera;
	private bool hasTarget = false;
	private float currentMoveSpeed;
	
	// Player stats
	public float Points;
	
	void Start()
	{
		mainCamera = Camera.main;
		if (mainCamera == null)
		{
			mainCamera = FindObjectOfType<Camera>();
		}
		
		// Initialize target position to current position
		targetPosition = transform.position;
		
		// Set initial speed
		currentMoveSpeed = baseMoveSpeed;
		
		// Cache references and bounds
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (useColliderBounds && movementBounds != null)
		{
			cachedBounds = movementBounds.bounds;
		}
	}
	
	void Update()
	{
		HandleInput();
		MoveTowardsTarget();
		UpdateMovementSpeed();
	}
	
	void LateUpdate()
	{
		ClampInsideBounds();
	}
	
	void HandleInput()
	{
		// Handle mouse input
		if (useMouseInput && Input.GetMouseButton(0))
		{
			SetTargetFromMouse();
		}
		
		// Handle touch input
		if (useTouchInput && Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
			{
				SetTargetFromTouch(touch);
			}
		}
	}
	
	void SetTargetFromMouse()
	{
		Vector3 mousePosition = Input.mousePosition;
		SetTargetPosition(mousePosition);
	}
	
	void SetTargetFromTouch(Touch touch)
	{
		Vector3 touchPosition = touch.position;
		SetTargetPosition(touchPosition);
	}
	
	void SetTargetPosition(Vector3 screenPosition)
	{
		// Convert screen position to world position
		Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
		worldPosition.z = transform.position.z; // Keep the same Z position
		
		targetPosition = worldPosition;
		hasTarget = true;
	}
	
	void MoveTowardsTarget()
	{
		if (!hasTarget) return;
		
		// Calculate distance to target
		float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
		
		// If we're close enough to the target, stop moving
		if (distanceToTarget <= 0.1f)
		{
			return;
		}
		
		// Calculate direction to target
		Vector3 direction = (targetPosition - transform.position).normalized;
		
		// Move towards target at current speed
		Vector3 newPosition = transform.position + direction * currentMoveSpeed * Time.deltaTime;
		transform.position = newPosition;
	}
	
	void UpdateMovementSpeed()
	{
		// Calculate speed reduction based on player size (points)
		float speedReduction = Points * speedReductionMultiplier;
		
		// Apply speed reduction with minimum limit
		currentMoveSpeed = Mathf.Max(baseMoveSpeed - speedReduction, minMoveSpeed);
	}
	
	void ClampInsideBounds()
	{
		// Compute visual radius from sprite so we don't clip out of bounds
		float radius = 0f;
		if (spriteRenderer != null)
		{
			Vector3 ext = spriteRenderer.bounds.extents;
			radius = Mathf.Max(ext.x, ext.y);
		}
		
		Vector3 p = transform.position;
		if (useColliderBounds && movementBounds != null)
		{
			var b = cachedBounds;
			p.x = Mathf.Clamp(p.x, b.min.x + radius, b.max.x - radius);
			p.y = Mathf.Clamp(p.y, b.min.y + radius, b.max.y - radius);
		}
		else
		{
			p.x = Mathf.Clamp(p.x, minXY.x + radius, maxXY.x - radius);
			p.y = Mathf.Clamp(p.y, minXY.y + radius, maxXY.y - radius);
		}
		transform.position = p;
	}
	
	// Optional: call if the bounds collider moves
	public void RefreshBounds()
	{
		if (movementBounds != null)
		{
			cachedBounds = movementBounds.bounds;
		}
	}
	
	// Public method to force recompute speed immediately when Points changes externally
	public void RefreshSpeed()
	{
		UpdateMovementSpeed();
	}
	
	// Public method to set target position programmatically
	public void SetTarget(Vector3 newTarget)
	{
		targetPosition = newTarget;
		hasTarget = true;
	}
	
	// Public method to stop movement
	public void StopMovement()
	{
		hasTarget = false;
	}
	
	// Public method to get current target
	public Vector3 GetCurrentTarget()
	{
		return targetPosition;
	}
	
	// Public method to check if moving
	public bool IsMoving()
	{
		return hasTarget && Vector3.Distance(transform.position, targetPosition) > 0.1f;
	}
	
	// Public method to get current movement speed
	public float GetCurrentMoveSpeed()
	{
		return currentMoveSpeed;
	}
	
	// Public method to get base movement speed
	public float GetBaseMoveSpeed()
	{
		return baseMoveSpeed;
	}
	
	// Public method to set base movement speed
	public void SetBaseMoveSpeed(float newSpeed)
	{
		baseMoveSpeed = newSpeed;
		UpdateMovementSpeed();
	}
	
	// Public method to set speed reduction multiplier
	public void SetSpeedReductionMultiplier(float newMultiplier)
	{
		speedReductionMultiplier = newMultiplier;
		UpdateMovementSpeed();
	}
	
	// Public method to set minimum move speed
	public void SetMinMoveSpeed(float newMinSpeed)
	{
		minMoveSpeed = newMinSpeed;
		UpdateMovementSpeed();
	}
	
	// Copy movement tunables from another movement component
	public void CopyMoveConfigFrom(CharcterMovement other)
	{
		if (other == null) return;
		this.baseMoveSpeed = other.baseMoveSpeed;
		this.minMoveSpeed = other.minMoveSpeed;
		this.speedReductionMultiplier = other.speedReductionMultiplier;
		this.useMouseInput = other.useMouseInput;
		this.useTouchInput = other.useTouchInput;
		RefreshSpeed();
	}
}
