using UnityEngine;


public class Terrain_Chunk : MonoBehaviour
{
    private Noise_Settings noise;
    private Terrain_Generation_Settings terrain; 
   
    private int chunkX, chunkZ;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private Vector2[] offsetSeed;

    private float minTerrainheight;
    private float maxTerrainheight;

    private Color[] colours;

    private void AssignMesh()
    {
        if (mesh == null)
        {
            Debug.Log("Creating new mesh");
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
    private float GenerateNoiseHeight(float x, float z, Vector2[] offsetSeed)
    {
        float frequency = noise.baseFrequency;
        float persistence = noise.basePersistence;
        float amplitude = noise.baseAmplitude;

        float noiseValue = 0f;
        float heightValue = 0;

        //loop through each octave and calculate the noise value
        for (int i = 0; i < noise.octaves; i++)
        {
            float sampleZ = z / noise.scale * frequency + offsetSeed[i].y;
            float sampleX = x / noise.scale * frequency + offsetSeed[i].x;


            noiseValue = (Mathf.PerlinNoise(sampleZ, sampleX)) * 2 - 1;
            heightValue += terrain.heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence; // Decrease amplitude for next octave
            frequency *= noise.lacunarity; // Increase frequency for next octave

        }
        return heightValue;
    }
    private void SetMinMaxHeights(float noiseHeight)
    {
        // Set min and max height of map for color gradient
        if (noiseHeight > maxTerrainheight)
            maxTerrainheight = noiseHeight;
        if (noiseHeight < minTerrainheight)
            minTerrainheight = noiseHeight;
    }

    // Create the actual mesh shape by assigning vertices, uses GenerateNoiseHeight and SetMinMaxHeights //
    private void CreateMeshShape(Vector2[] offsetSeed)
    {
        Vector2[] octaveOffsets = offsetSeed;


        vertices = new Vector3[(terrain.sizeX + 1) * (terrain.sizeZ + 1)];

        for (int i = 0, z = 0; z <= terrain.sizeZ; z++)
        {
            for (int x = 0; x <= terrain.sizeX; x++)
            {
                // Assign and set height of each vertices

                float worldX = (chunkX * terrain.sizeX) + x;
                float worldZ = (chunkZ * terrain.sizeZ) + z;

                float noiseHeight = GenerateNoiseHeight(worldX, worldZ, octaveOffsets);
                if (noiseHeight <= noise.lowerThreshold)
                    noiseHeight = 0;

                SetMinMaxHeights(noiseHeight);
                vertices[i] = new Vector3(x, noiseHeight, z);
                i++;
            }
        }
    }
    private void CreateTriangles()
    {
        // Need 6 vertices to create a square (2 triangles)
        triangles = new int[terrain.sizeX * terrain.sizeZ * 6];
        int vert = 0;
        int tris = 0;

        // loop through rows
        for (int z = 0; z < terrain.sizeZ; z++)
        {
            // fill all columns in row
            for (int x = 0; x < terrain.sizeX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + terrain.sizeX + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + terrain.sizeX + 1;
                triangles[tris + 5] = vert + terrain.sizeX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    private void ColourTerrain()
    {
        colours = new Color[vertices.Length];

        // Loop over vertices and apply a color from the depending on height (y axis value)
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = Mathf.InverseLerp(minTerrainheight, maxTerrainheight, vertices[i].y);
            colours[i] = terrain.gradient.Evaluate(height);
        }

    }

    public void ClearMesh()
    {
        if (mesh) mesh.Clear();
    }
    private void UpdateMesh()
    {
        ClearMesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colours;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.name = "terrain_mesh";

        GetComponent<MeshCollider>().sharedMesh = mesh;

        gameObject.transform.localScale = new Vector3(
            terrain.scale,
            terrain.scale,
            terrain.scale);

        GetComponent<MeshRenderer>().material = terrain.material;
    }


    //  *ENTRY POINT* //
    public bool CreateNewTerrainChunk(
        Noise_Settings noise_settings, 
        Terrain_Generation_Settings terrain_generation_settings, 
        Vector2[] offset_seed,
        int x, int z) //coordinates of the chunk
    {
        //Assign variables
        noise = noise_settings;
        terrain = terrain_generation_settings;
        offsetSeed = offset_seed;
        chunkX = x;
        chunkZ = z;

        //Call generate mesh
        GenerateMesh();

        return true;

    }

    private void GenerateMesh()
    {
        //Generate Mesh
        AssignMesh();
        CreateMeshShape(offsetSeed);
        CreateTriangles();
        ColourTerrain();
        UpdateMesh();
    }




}
