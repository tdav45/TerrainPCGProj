using UnityEngine;


[CreateAssetMenu(fileName = "new_global_terrain_settings", menuName = "New Globabl Terrain Settings", order = 1)]
public class Global_Terrain_Settings : ScriptableObject
{
    public int sizeX = 64, sizeZ = 64; //Size of terrain along each axis
    public int meshScale = 1; //Scale multiplier
    public int octaves = 20; //How many octaves of noise
    public int seed = 1; //Seed for random number generation
    public float baseFrequency = 1; //Base frequency for the noise
    public float basePersistence = 0.5f; //Base persistence for the noise
    public float lacunarity = 2; //Lacunarity for the nosie
    public Material material; //Material for the meshes to use
    public float biomeNoiseScale = 100; //The scale of the biome's noise
    public int biomeOctaves = 4; //How many octaves of the noise for biomes
    public float biomePersistence = 0.5f; //Base persistence for the biome's noise
    public float biomeLacunarity = 2f; //Lacunarity for the biome's nosie
    public float biomeFrequency = 1f; //Base frequency for the biome's noise
    public float biomeBlendWidth = 0.05f;


}
