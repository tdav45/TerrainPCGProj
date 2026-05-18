using UnityEngine;

public struct BiomeBlend
{
    public Biome_Settings biomeA;
    public Biome_Settings biomeB;
    public float blendValue;
}


[CreateAssetMenu(fileName = "new_biome", menuName = "New Biome", order = 3)]
public class Biome_Settings : ScriptableObject
{
    public AnimationCurve heightCurve; //(Terrain)
    public float baseAmplitude; //Amplitude that scales height of the terrian (Noise)
    public Gradient gradient; //Colour gradient that applies to the material (Terrain)
    public float noiseScale;
    public float lowerThreshold; //Height threshold, any height lower will be set to 0
    public int priority = 1;
    [Range (0, 1f)]
    public float order = 0;

    public float noiseThreshold = 0;

    [HideInInspector]
    public float thresholdStart = 0;
    [HideInInspector]
    public float thresholdEnd = 1;





}
