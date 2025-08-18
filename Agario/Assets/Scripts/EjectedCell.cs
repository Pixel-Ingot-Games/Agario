using UnityEngine;

public class EjectedCell : MonoBehaviour
{
	[Header("Ejected Cell Properties")]
	[SerializeField] private float fixedPoints = 5f; // Fixed points this cell gives when eaten
	[SerializeField] private float fixedSize = 0.5f; // Fixed size (scale) of this cell
	[SerializeField] private float lifetime = 10f; // How long before auto-destroy
	
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 15f; // Speed of movement
	[SerializeField] private float moveDuration = 2f; // How long to move in the initial direction
	
	private Vector3 moveDirection;
	private float moveEndTime;
	private bool isMoving = true;
	
	void Start()
	{
		// Set fixed size
		transform.localScale = Vector3.one * fixedSize;
		
		// Auto-destroy after lifetime
		Destroy(gameObject, lifetime);
	}
	
	// Set the movement direction and start moving
	public void SetMoveDirection(Vector3 direction)
	{
		moveDirection = direction.normalized;
		moveEndTime = Time.time + moveDuration;
	}
	
	void Update()
	{
		if (isMoving && Time.time < moveEndTime)
		{
			// Move in the set direction
			transform.position += moveDirection * moveSpeed * Time.deltaTime;
		}
		else if (isMoving)
		{
			// Stop moving after duration
			isMoving = false;
			// Set to Food layer (layer 8) so it can be eaten
			gameObject.layer = 8; // Food layer
		}
	}
	
	// Called when this cell is eaten
	public float GetPoints()
	{
		return fixedPoints;
	}
	
	// Optional: Add visual effects when spawned
	void OnEnable()
	{
		// You can add spawn effects here (particles, sound, etc.)
	}
	
	// Optional: Add cleanup effects
	void OnDestroy()
	{
		// You can add destruction effects here (particles, sound, etc.)
	}
}
