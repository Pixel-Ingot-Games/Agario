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
	Transform _followProxy; // centroid of all cells
	
	void Start(){
		if (camera != null)
		{
			camera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
			camera.Lens.OrthographicSize = minZoom;
			_targetZoom = minZoom;
		}
		EnsureFollowProxy();
		if (camera != null && _followProxy != null)
		{
			camera.Follow = _followProxy;
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
		EnsureFollowProxy();
		if (camera != null && _followProxy != null && camera.Follow != _followProxy)
		{
			camera.Follow = _followProxy;
		}
		// Update proxy position to centroid of all cells, keep z clamped to -10
		if (_followProxy != null)
		{
			const float desiredZ = -10f;
			bool positioned = false;
			if (score != null)
			{
				var cells = score.GetCells();
				Vector3 sum = Vector3.zero;
				int count = 0;
				for (int i = 0; i < cells.Count; i++)
				{
					var c = cells[i];
					if (c == null) continue;
					sum += c.transform.position;
					count++;
				}
				if (count > 0)
				{
					Vector3 center = sum / Mathf.Max(count, 1);
					center.z = desiredZ;
					_followProxy.position = center;
					positioned = true;
				}
			}
			if (!positioned && target != null)
			{
				Vector3 center = target.transform.position;
				center.z = desiredZ;
				_followProxy.position = center;
			}
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

	void LateUpdate(){
		// Enforce camera z at -10 every frame
		if (camera != null && camera.transform != null)
		{
			var pos = camera.transform.position;
			pos.z = -10f;
			camera.transform.position = pos;
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

	void EnsureFollowProxy(){
		if (_followProxy == null)
		{
			var go = new GameObject("CameraFollowProxy");
			go.transform.SetParent(transform);
			go.transform.position = transform.position;
			_followProxy = go.transform;
		}
	}
}

