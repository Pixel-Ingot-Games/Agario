using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteColorSync : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private Color lastColor;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        lastColor = spriteRenderer.color;
        UpdateMaterialColor();
    }
    
    void Update()
    {
        // Only update if the color has changed
        if (spriteRenderer.color != lastColor)
        {
            lastColor = spriteRenderer.color;
            UpdateMaterialColor();
        }
    }
    
    void UpdateMaterialColor()
    {
        // Set the color in the material property block
        propertyBlock.SetColor(ColorProperty, lastColor);
        
        // Apply the property block to the SpriteRenderer
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
