using UnityEngine;

[CreateAssetMenu(fileName = "new_biome", menuName = "New Biome", order = 3)]
public class BiomeSettings : ScriptableObject
{
    public Material material;
    public AnimationCurve heightCurve;
    public float baseAmplitude; //Amplitude that scales height of the terrian
    public Gradient gradient; //Colour gradient that applies to the material
}
