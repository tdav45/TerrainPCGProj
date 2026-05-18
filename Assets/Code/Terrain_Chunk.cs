using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;


public class Terrain_Chunk : MonoBehaviour
{

    private Global_Terrain_Settings terrainSettings;

    private Terrain_Generator terrainGenerator;

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
            //Debug.Log("Creating new mesh");
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
    private float GenerateNoiseHeight(
        float x, float z,
        Vector2[] offsetSeed,
        float amplitude,
        float noiseScale,
        AnimationCurve curve)
    {
        float frequency = terrainSettings.baseFrequency;
        float persistence = terrainSettings.basePersistence;

        float noiseValue = 0f;
        float amp = amplitude;

        for (int i = 0; i < terrainSettings.octaves; i++)
        {
            float sampleX = x / noiseScale * frequency + offsetSeed[i].x;
            float sampleZ = z / noiseScale * frequency + offsetSeed[i].y;

            float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;

            noiseValue += curve.Evaluate(perlin) * amp;

            amp *= persistence;
            frequency *= terrainSettings.lacunarity;
        }

        return noiseValue;
    } 
    
    private void SetMinMaxHeights(float noiseHeight)
    {
        // Set min and max height of map for color gradient
        if (noiseHeight > maxTerrainheight)
            maxTerrainheight = noiseHeight;
        if (noiseHeight < minTerrainheight)
            minTerrainheight = noiseHeight;
    }

    private float GenerateBlendedHeight(
    float x, float z,
    Vector2[] offsetSeed,
    Biome_Settings biomeA,
    Biome_Settings biomeB,
    float t)
    {
        float frequency = terrainSettings.baseFrequency;

        float amplitudeA = biomeA.baseAmplitude;
        float amplitudeB = biomeB.baseAmplitude;

        float scaleA = biomeA.noiseScale;
        float scaleB = biomeB.noiseScale;

        float noiseValue = 0f;
        float amp = Mathf.Lerp(amplitudeA, amplitudeB, t);
        float scale = Mathf.Lerp(scaleA, scaleB, t);

        for (int i = 0; i < terrainSettings.octaves; i++)
        {
            float sampleX = x / scale * frequency + offsetSeed[i].x;
            float sampleZ = z / scale * frequency + offsetSeed[i].y;

            float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;

            float curveA = biomeA.heightCurve.Evaluate(perlin);
            float curveB = biomeB.heightCurve.Evaluate(perlin);

            float curve = Mathf.Lerp(curveA, curveB, t);

            noiseValue += curve * amp;

            amp *= terrainSettings.basePersistence;
            frequency *= terrainSettings.lacunarity;
        }

        return noiseValue;
    }


    // Create the actual mesh shape by assigning vertices, uses GenerateNoiseHeight and SetMinMaxHeights //
    private void CreateMeshShape(Vector2[] offsetSeed)
    {

        vertices = new Vector3[(terrainSettings.sizeX + 1) * (terrainSettings.sizeZ + 1)];

        minTerrainheight = float.MaxValue;
        maxTerrainheight = float.MinValue;


        for (int i = 0, z = 0; z <= terrainSettings.sizeZ; z++)
        {
            for (int x = 0; x <= terrainSettings.sizeX; x++)
            {
                // Assign and set height of each vertices

                float worldX = (chunkX * terrainSettings.sizeX) + x;
                float worldZ = (chunkZ * terrainSettings.sizeZ) + z;


                BiomeBlend biomeBlend = terrainGenerator.SampleBiome(worldX, worldZ);

                Biome_Settings biomeA = biomeBlend.biomeA;
                Biome_Settings biomeB = biomeBlend.biomeB;
                float t = biomeBlend.blendValue;

                float amplitude = Mathf.Lerp(biomeA.baseAmplitude, biomeB.baseAmplitude, t);
                float noiseScale = Mathf.Lerp(biomeA.noiseScale, biomeB.noiseScale, t);

                AnimationCurve curve = t < 0.5f ? biomeA.heightCurve : biomeB.heightCurve;

                float height = GenerateBlendedHeight(worldX, worldZ, offsetSeed, biomeA, biomeB, t);


                vertices[i] = new Vector3(x, height, z);

                SetMinMaxHeights(height);

                if (height > maxTerrainheight) maxTerrainheight = height;
                if (height < minTerrainheight) minTerrainheight = height;

                i++;
            }
        }
    }
    private void CreateTriangles()
    {
        // Need 6 vertices to create a square (2 triangles)
        triangles = new int[terrainSettings.sizeX * terrainSettings.sizeZ * 6];
        int vert = 0;
        int tris = 0;

        // loop through rows
        for (int z = 0; z < terrainSettings.sizeZ; z++)
        {
            // fill all columns in row
            for (int x = 0; x < terrainSettings.sizeX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + terrainSettings.sizeX + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + terrainSettings.sizeX + 1;
                triangles[tris + 5] = vert + terrainSettings.sizeX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }


    private void ColourTerrain()
    {
        colours = new Color[vertices.Length];

        for (int i = 0, z = 0; z <= terrainSettings.sizeZ; z++)
        {
            for (int x = 0; x <= terrainSettings.sizeX; x++)
            {
                float worldX = (chunkX * terrainSettings.sizeX) + x;

                float worldZ = (chunkZ * terrainSettings.sizeZ) + z;

                BiomeBlend biomeBlend = terrainGenerator.SampleBiome(worldX, worldZ);

                float height = Mathf.InverseLerp(minTerrainheight, maxTerrainheight, vertices[i].y);

                Color colorA = biomeBlend.biomeA.gradient.Evaluate(height);

                Color colorB = biomeBlend.biomeB.gradient.Evaluate(height);

                Color finalColor = Color.Lerp(colorA, colorB, biomeBlend.blendValue);

                colours[i] = finalColor;

                i++;
            }
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
        mesh.RecalculateUVDistributionMetrics();

        mesh.name = "terrain_mesh";

        GetComponent<MeshCollider>().sharedMesh = mesh;

        gameObject.transform.localScale = new Vector3(
            terrainSettings.meshScale,
            terrainSettings.meshScale,
            terrainSettings.meshScale);

        //Offset chunk position
        transform.position = new Vector3(chunkX * terrainSettings.sizeX * terrainSettings.meshScale, 0,
            chunkZ * terrainSettings.sizeZ * terrainSettings.meshScale);

        GetComponent<MeshRenderer>().material = terrainSettings.material;

    }


    //  *ENTRY POINT* //
    public bool CreateNewTerrainChunk(
        Terrain_Generator terrain_generator, 
        Global_Terrain_Settings global_terrain_settings,
        Vector2[] offset_seed,
        int x, int z) //coordinates of the chunk
    {
        //Assign variables
        terrainGenerator = terrain_generator;
        terrainSettings = global_terrain_settings;
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
