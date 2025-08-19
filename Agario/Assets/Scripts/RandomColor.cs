using UnityEngine;

public class RandomColor : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Material[] materials;
    void Start()
    {
        sprite.material = materials[Random.Range(0, materials.Length)];
    }
}
