using UnityEngine;
using UnityEditor;

//GUI//
[CustomEditor(typeof(Terrain_Generator))]
public class Terrain_Generator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Terrain_Generator terrainGenerator = (Terrain_Generator)target;
        
        if (GUILayout.Button("Generate Terrain"))
        {
            terrainGenerator.CreateNewTerrain();
        }

        if (GUILayout.Button("Radomise Seed"))
        {
            terrainGenerator.RandomiseSeed();
        }

    }
}








//TERRAIN GENERATION//
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Terrain_Generator : MonoBehaviour
{

    /*Old variables
     * [SerializeField]
    private Material grassMaterial;
    
    //Terrian mesh settings
    [Tooltip("Curve controls the range of height of the terrain")]
    [SerializeField]
    private AnimationCurve heightCurve;
    [Tooltip("Size of the terrain in the X axis")]
    [SerializeField]
    private int terrainX = 100;
    [Tooltip("Size of the terrain in the Z axis")]
    [SerializeField]
    private int terrainZ = 100;
    
    //NOISE SETTINGS//

    [SerializeField]
    private int terrainScale = 100;
    [Tooltip("Number of layers of noise to add detail to the terrain")]
    [SerializeField]
    private int octaves = 4;
    [Tooltip("Controls the frequency of the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float scale = 20f;
    [Tooltip("Seed for random number generator, changing this will create a different terrain")]
    [SerializeField]
    private int seed = 0;

    [Tooltip("Base amplitude, scales the height of the terrain, higher values will create taller terrain")]
    [SerializeField]
    private float baseAmplitude;

    [Tooltip("Base frequency for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float baseFrequency = 1f;
    [Tooltip("Base persistence for the noise, higher values will create more rugged terrain")]
    [SerializeField]
    private float basePersistence = 0.5f;
    [Tooltip("Lacunarity for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float lacunarity = 2f;
    [Tooltip("Height threshold for the terrain, any height below this value will be set to 0")]
    [SerializeField]
    private float lowerThreshold = 0.04f;*/


    // [SerializeField] private Gradient gradient;

    public Terrain_Generation_Settings terrain;
    public Noise_Settings nosie;

    private Color[] colours;


    private float minTerrainheight;
    private float maxTerrainheight;


    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;




    //Get the offset seed for each octave
    private Vector2[] GetOffsetSeed()
    {
        Vector2[] offsetSeed = new Vector2[nosie.octaves];

        System.Random prng = new System.Random(nosie.seed);


        for (int i = 0; i < nosie.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            offsetSeed[i] = new Vector2(offsetX, offsetY);
        }


        return offsetSeed;
    }
   
    public void RandomiseSeed()
    {
        nosie.seed = Random.Range(0, 1000);
    }

    
   //GENERATE TERRAIN (ENTRY POINT)//
    public void CreateNewTerrain()
    {
        /*        AssignMesh();
                CreateMeshShape();
                CreateTriangles();
                ColourTerrain();
                UpdateMesh();*/

        Instantiate(gameObject.AddComponent<Terrain_Chunk>());

    }


}
