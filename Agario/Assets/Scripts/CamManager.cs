using UnityEngine;
using Unity.Cinemachine;
public class CamManager : MonoBehaviour
{
	public CinemachineCamera camera;
	public GameObject target;
	public PlayerAggregate score;
	public float minZoom,maxZoom;
	[Header("Zoom Mapping Settings")]
	public float minPointsForZoom = 5f; // points at which zoom starts growing
	public float pointsAtMaxZoom = 100f; // points that map to maxZoom
	public float zoomLerpSpeed = 5f; // smoothing speed
	
	float _targetZoom;
	bool _subscribed;
	
	void Start(){
		if (camera != null)
		{
			camera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
			camera.Lens.OrthographicSize = minZoom;
			_targetZoom = minZoom;
		}
	}
	
	void OnEnable(){
		TryWireScore();
	}
	
	void OnDisable(){
		if (score != null && _subscribed){ score.OnTotalPointsChanged -= OnPointsChanged; _subscribed = false; }
	}
	
	void Update(){
		if(!target){
			target=GameObject.FindWithTag("Player");
		}
		if (target && !score){
			score = target.GetComponent<PlayerAggregate>();
			TryWireScore();
		}
		if (target != null && camera != null && camera.Follow != target.transform)
		{
			camera.Follow = target.transform;
		}
		// Smoothly apply target zoom
		if (camera != null)
		{
			camera.Lens.OrthographicSize = Mathf.Lerp(camera.Lens.OrthographicSize, _targetZoom, Time.deltaTime * zoomLerpSpeed);
		}
		// Fallback polling if no event yet
		if (score != null && !_subscribed)
		{
			OnPointsChanged(score.GetTotalPoints());
		}
	}
	
	void TryWireScore(){
		if (score != null && !_subscribed)
		{
			score.OnTotalPointsChanged += OnPointsChanged;
			_subscribed = true;
			OnPointsChanged(score.GetTotalPoints());
		}
	}
	
	void OnPointsChanged(float total){
		// Map points to zoom range [minZoom,maxZoom]
		float denom = Mathf.Max(pointsAtMaxZoom - minPointsForZoom, 0.0001f);
		float t = Mathf.Clamp01((Mathf.Max(total - minPointsForZoom, 0f)) / denom);
		// Use sqrt for softer early growth
		t = Mathf.Sqrt(t);
		_targetZoom = Mathf.Lerp(minZoom, maxZoom, t);
	}
}

