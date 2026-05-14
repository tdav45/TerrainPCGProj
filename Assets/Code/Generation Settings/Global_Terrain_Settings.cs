using UnityEngine;


[CreateAssetMenu(fileName = "new_terrain_settings", menuName = "New Terrain Settings", order = 1)]
public class Global_Terrain_Settings : ScriptableObject
{
    public int sizeX, sizeZ; //Size of terrain along each axis
    public int meshScale; //Scale multiplier
    public int octaves; //How many octaves of noise
    public int seed; //Seed for random number generation
    public int noiseScale; //Scale of the noise
    public float baseFrequency; //Base frequency for the noise
    public float basePersistence; //Base persistence for the noise
    public float lacunarity; //Lacunarity for the nosie
    public float lowerThreshold; //Height threshold, any height lower will be set to 0 


}
