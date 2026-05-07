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
    [SerializeField] 
    private int octaves = 4;
    [SerializeField]
    private int terrainX = 100;
    [SerializeField]
    private int terrainZ = 100;
    [SerializeField]
    private float scale = 20f;
    [SerializeField]
    private int seed = 0;


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
    private float GetNoiseHeight(float x, float z, Vector2[] offsetSeed)
    {
        float noiseValue = 0f;
        float amplitude = 12f;
        float frequency = 1f;

        //loop through each octave and calculate the noise value
        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x / scale * frequency + offsetSeed[i].x;
            float sampleZ = z / scale * frequency + offsetSeed[i].y;

            noiseValue += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
            amplitude *= 0.5f; // Reduce amplitude for next octave
            frequency *= 2f; // Increase frequency for next octave
        }
        return noiseValue; // Normalize the result
    }
    private void AssignMesh()
    {
        if (!mesh)
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
    private void CreateMeshShape()
    {
        Vector2[] offsetSeed = GetOffsetSeed();

        
        vertices = new Vector3[(terrainX + 1) * (terrainZ + 1)];
        for (int i = 0; i < terrainZ; i++)
        {
            for (int j = 0; j < terrainX; j++)
            {
                float noiseValue = GetNoiseHeight(i, j, offsetSeed); // Set the height of the terrain at (x, y) based on the noise value
                vertices[i] = new Vector3(i, noiseValue, j);
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
