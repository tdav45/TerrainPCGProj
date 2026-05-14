using UnityEngine;

[CreateAssetMenu(fileName = "new_biome", menuName = "New Biome", order = 3)]
public class Biome_Settings : ScriptableObject
{
    public Material material; //(Terrain)
    public AnimationCurve heightCurve; //(Terrain)
    public float baseAmplitude; //Amplitude that scales height of the terrian (Noise)
    public Gradient gradient; //Colour gradient that applies to the material (Terrain)
    public float noiseScale;
    public float lowerThreshold; //Height threshold, any height lower will be set to 0
    [Range(0.0f, 1.0f)]
    public float noiseThreshold;
}
