using UnityEngine;

[CreateAssetMenu(fileName = "new_noise_settings", menuName = "New Noise Settings", order = 2)]
public class Noise_Settings : ScriptableObject
{
    public int octaves; //How many octaves of noise
    public int seed; //Seed for random number generation
    public int scale; //Scale of the noise
    public float baseAmplitude; //Amplitude that scales height of the terrian
    public float baseFrequency; //Base frequency for the noise
    public float basePersistence; //Base persistence for the noise
    public float lacunarity; //Lacunarity for the nosie
    public float lowerThreshold; //Height threshold, any height lower will be set to 0 

}