using UnityEngine;
using UnityEditor;

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

        if (GUILayout.Button("Clear Terrain"))
        {
            terrainGenerator.ClearMesh();
        }

    }
}
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Terrain_Generator : MonoBehaviour
{
    [Tooltip("Curve controls the range of height of the terrain")]
    [SerializeField]
    private AnimationCurve heightCurve;
    [Tooltip("Number of layers of noise to add detail to the terrain")]
    [SerializeField] 
    private int octaves = 4;
    [Tooltip("Size of the terrain in the X axis")]
    [SerializeField]
    private int terrainX = 100;
    [Tooltip("Size of the terrain in the Z axis")]
    [SerializeField]
    private int terrainZ = 100;
    [Tooltip("Controls the frequency of the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float scale = 20f;
    [Tooltip("Seed for random number generator, changing this will create a different terrain")]
    [SerializeField]
    private int seed = 0;
    [Tooltip("Base frequency for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float baseFrequency = 1f;
    [Tooltip("Base persistence for the noise, higher values will create more rugged terrain")]
    [SerializeField]
    private float basePersistence = 0.5f;
    [Tooltip("Lacunarity for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float lacunarity = 2f;

    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;





    //Get the offset seed for each octave
    private Vector2[] GetOffsetSeed()
    {
        Vector2[] offsetSeed = new Vector2[octaves];

        System.Random prng = new System.Random(seed);


        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            offsetSeed[i] = new Vector2(offsetX, offsetY);
        }


        return offsetSeed;
    }
    private float GenerateNoiseHeight(float x, float z, Vector2[] offsetSeed)
    {
        float frequency = baseFrequency;
        float persistence = basePersistence;
        float amplitude = 12;

        float noiseValue = 0f;
        float heightValue = 0;

        //loop through each octave and calculate the noise value
        for (int i = 0; i < octaves; i++)
        {
            float sampleZ = z / scale * frequency + offsetSeed[i].y;
            float sampleX = x / scale * frequency + offsetSeed[i].x;


            noiseValue = (Mathf.PerlinNoise(sampleZ, sampleX)) * 2 - 1;
            heightValue += heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence; // Decrease amplitude for next octave
            frequency *= lacunarity; // Increase frequency for next octave

        }
        return heightValue; 
    }
    private void AssignMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
    private void CreateMeshShape()
    {
        Vector2[] octaveOffsets = GetOffsetSeed();

        
        vertices = new Vector3[(terrainX + 1) * (terrainZ + 1)];

        for (int i = 0, z = 0; z <= terrainZ; z++)
        {
            for (int x = 0; x <= terrainX; x++)
            {
                // Assign and set height of each vertices
                float noiseHeight = GenerateNoiseHeight(z, x, octaveOffsets);
                vertices[i] = new Vector3(x, noiseHeight, z);
                i++;
            }
        }
    }
    private void CreateTriangles()
    {
        // Need 6 vertices to create a square (2 triangles)
        triangles = new int[terrainX * terrainZ * 6];
        int vert = 0;
        int tris = 0;

        // loop through rows
        for (int z = 0; z < terrainZ; z++)
        {
            // fill all columns in row
            for (int x = 0; x < terrainX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + terrainX + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + terrainX + 1;
                triangles[tris + 5] = vert + terrainX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }
    private void UpdateMesh()
    {   
        ClearMesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        GetComponent<MeshCollider>().sharedMesh = mesh;

        gameObject.transform.localScale = new Vector3(500, 500, 500);

    }


    public void RandomiseSeed()
    {
        seed = Random.Range(0, 1000);
    }
    public void ClearMesh()
    {
        if (mesh) mesh.Clear();
    }
    public void CreateNewTerrain()
    {
        AssignMesh();
        CreateMeshShape();
        CreateTriangles();
        UpdateMesh();

    }

    
}
