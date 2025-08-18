using UnityEngine;
using TMPro;
public class PointsHUD : MonoBehaviour
{
   [SerializeField] private PlayerAggregate player;
    [SerializeField] private TMP_Text pointsText;

    void Awake()
    {
        if (player == null) player = FindObjectOfType<PlayerAggregate>();
    }

    void OnEnable()
    {
        if (player != null) player.OnTotalPointsChanged += UpdateText;
    }

    void Start()
    {
        if (player != null) UpdateText(player.GetTotalPoints());
    }

    void OnDisable()
    {
        if (player != null) player.OnTotalPointsChanged -= UpdateText;
    }

    void UpdateText(float total)
    {
        pointsText.text = Mathf.FloorToInt(total).ToString();
    }
}
