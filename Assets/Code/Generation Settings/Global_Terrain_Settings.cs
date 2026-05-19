using UnityEngine;


[CreateAssetMenu(fileName = "new_global_terrain_settings", menuName = "New Globabl Terrain Settings", order = 1)]
public class Global_Terrain_Settings : ScriptableObject
{
    public int sizeX, sizeZ; // Size of terrain along each axis
    public int meshScale; // Scale multiplier
    public int octaves; // How many octaves of terrain noise
    public int seed; // Seed for random number generation
    public float baseFrequency; // Base frequency for the terrain noise
    public float basePersistence; // Base persistence for the terrain noise
    public float lacunarity; // Lacunarity for the terrain nosie
    public Material material; // Material used for the terrain
    public float biomeNoiseScale; // Scale of the noise used to determine biome distribution
    public int biomeOctaves = 4; // Number of octaves for the biome noise
    public float biomePersistence = 0.5f; // Persistence for the biome noise
    public float biomeLacunarity = 2f; // Lacunarity for the biome noise
    public float biomeFrequency = 1f; // Frequency for the biome noise
    public float biomeEdgeWidth = 0.08f; // Width of the edge between biomes, used for blending


}
