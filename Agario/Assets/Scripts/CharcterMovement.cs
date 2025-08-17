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
    
    private Vector3 targetPosition;
    private Camera mainCamera;
    private bool hasTarget = false;
    private float currentMoveSpeed;
    
    // Player stats
    public int Points;
    
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
    }
    
    void Update()
    {
        HandleInput();
        MoveTowardsTarget();
        UpdateMovementSpeed();
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
}
