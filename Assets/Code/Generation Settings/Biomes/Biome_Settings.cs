using UnityEngine;

public enum BiomeType
{
    Mountains,
    Plains,
    Ocean,
    Desert,
    Hills,
    XTRMountains

}

public struct BiomeBlend
{
    public Biome_Settings biomeA;
    public Biome_Settings biomeB;
    public float blendValue;
}


[CreateAssetMenu(fileName = "new_biome", menuName = "New Biome", order = 3)]
public class Biome_Settings : ScriptableObject
{
    public BiomeType biomeName;
    public AnimationCurve heightCurve; //(Terrain)
    public float baseAmplitude; //Amplitude that scales height of the terrian (Noise)
    public Gradient gradient; //Colour gradient that applies to the material (Terrain)
    public float noiseScale;
    public float lowerThreshold; //Height threshold, any height lower will be set to 0
    public int priority = 1;
    [Range (0, 1f)]
    public float order = 0;
    public float noiseThreshold = 0;

    [Header("Asset Spawning")]
    public GameObject[] spawnPrefabs;
    [Range(0f, 1f)]
    public float spawnThreshold = 0.7f;
    public float spawnNoiseScale = 0.05f;
    public float spawnNoiseOffset = 1000f;


    [HideInInspector]
    public float thresholdStart = 0;
    [HideInInspector]
    public float thresholdEnd = 1;





}
