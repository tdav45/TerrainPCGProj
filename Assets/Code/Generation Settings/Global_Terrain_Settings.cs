using UnityEngine;


[CreateAssetMenu(fileName = "new_global_terrain_settings", menuName = "New Globabl Terrain Settings", order = 1)]
public class Global_Terrain_Settings : ScriptableObject
{
    public int sizeX, sizeZ; //Size of terrain along each axis
    public int meshScale; //Scale multiplier
    public int octaves; //How many octaves of noise
    public int seed; //Seed for random number generation
    public float baseFrequency; //Base frequency for the noise
    public float basePersistence; //Base persistence for the noise
    public float lacunarity; //Lacunarity for the nosie


}
