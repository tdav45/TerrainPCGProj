using UnityEngine;

// Names for the different biomes
public enum BiomeType
{
    Mountains,
    Plains,
    Forest,
    Ocean,
    Desert,
    Hills,
    XTRMountains

}

// Struct used to blend between two biomes
public struct BiomeBlend
{
    public Biome_Settings biomeA;
    public Biome_Settings biomeB;
    // Blend value between the two biomes, with 0 being fully biomeA and 1 being fully biomeB
    public float blendValue;
}


[CreateAssetMenu(fileName = "new_biome", menuName = "New Biome", order = 3)]
public class Biome_Settings : ScriptableObject
{
    public BiomeType biomeName; // Name of the biome
    public AnimationCurve heightCurve; // Curve that applies to the height of the terrain
    public float baseAmplitude; // Amplitude that scales height of the terrian
    public Gradient gradient; // Colour gradient that applies to the material
    public float noiseScale; // Scale of the noise applied to the terrain
    public float lowerThreshold; // Height threshold, any height lower will be set to 0
    public int priority = 1; // Priority of the biome, higher priority will blend over lower priority biomes
    [Range (0, 1f)]
    public float order = 0; // Order of the biome, dictating which biomes blends with which
    public float noiseThreshold = 0; // Threshold value used in the noise to determine which biome is used at a point. Not hidden in order for debugging

    [Header("Asset Spawning")]
    public GameObject[] spawnPrefabs; // GameObjects that can be spawned in this biome
    [Range(0f, 1f)]
    public float spawnThreshold = 0.7f; // Threshold value used in the noise to determine whether to spawn an object at a point
    public float spawnNoiseScale = 0.05f; // Scale of the noise used to determine where to spawn objects
    public float spawnNoiseOffset = 1000f; // Offset of the noise used to determine where to spawn objects
    public float spawnRarity = 0.2f; //How rare spawning items is in the biome

    [HideInInspector]
    public float thresholdStart = 0; // Used in calculating the noise threshold
    [HideInInspector]
    public float thresholdEnd = 1; // Used in calculating the noise threshold





}
