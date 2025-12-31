using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HoloEffect : MonoBehaviour
{
    [Header("Shader Config")]
    public float speed = 0.2f;
    [Tooltip("The reference name of the Float property in your Shader Graph")]
    public string propertyName = "_HoloOffset"; 

    private Material instancedMaterial;
    private float offset;
    private int propertyID;

    void Start()
    {
        Image img = GetComponent<Image>();
        if (img.material != null && img.material.HasProperty(propertyName))
        {
            // Create a material instance so cards don't sync up perfectly (looks fake)
            instancedMaterial = new Material(img.material);
            img.material = instancedMaterial;
            
            // Convert string to ID once for performance
            propertyID = Shader.PropertyToID(propertyName);
            
            // Randomize start so they don't all shine at the exact same moment
            offset = Random.Range(0f, 1f);
        }
    }

    void Update()
    {
        if (instancedMaterial != null)
        {
            offset += Time.deltaTime * speed;
            // Loop the value 0-1 if your shader expects UV coordinates
            if (offset > 1f) offset -= 1f;
            
            instancedMaterial.SetFloat(propertyID, offset);
        }
    }
    
    void OnDestroy()
    {
        if (instancedMaterial != null) Destroy(instancedMaterial);
    }
}