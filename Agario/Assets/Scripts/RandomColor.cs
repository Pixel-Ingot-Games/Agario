using UnityEngine;

public class RandomColor : MonoBehaviour
{
    public SpriteRenderer sprite;
    
    void Start()
    {
        sprite.color = Random.ColorHSV();
    }
}
