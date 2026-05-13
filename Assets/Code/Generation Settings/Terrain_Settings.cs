using UnityEngine;



[CreateAssetMenu(fileName = "new_terrain_settings", menuName = "New Terrain Settings", order = 1)]
public class Terrain_Generation_Settings : ScriptableObject
{
    public Material material;
    public AnimationCurve heightCurve;
    public int sizeX, sizeZ; //Size of terrain along each axis
    public int scale; //Scale multiplier
    public Gradient gradient; //Colour gradient that applies to the material
}
