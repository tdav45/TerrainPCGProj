using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;


public class Terrain_Chunk : MonoBehaviour
{

    private Global_Terrain_Settings terrainSettings;
    private List<Biome_Settings> currentBiomes; //biomes being used

    private int chunkX, chunkZ;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private Vector2[] offsetSeed;
    private Vector2 biomeOffsetSeed;

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
    private float GenerateNoiseHeight(float x, float z, Vector2[] offsetSeed, Biome_Settings biome)
    {
        float frequency = terrainSettings.baseFrequency;
        float persistence = terrainSettings.basePersistence;
        float amplitude = biome.baseAmplitude;

        float noiseValue = 0f;
        float heightValue = 0;

        //loop through each octave and calculate the noise value
        for (int i = 0; i < terrainSettings.octaves; i++)
        {
            float sampleZ = z / biome.noiseScale * frequency + offsetSeed[i].y;
            float sampleX = x / biome.noiseScale * frequency + offsetSeed[i].x;


            noiseValue = (Mathf.PerlinNoise(sampleZ, sampleX)) * 2 - 1;
            heightValue += biome.heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence; // Decrease amplitude for next octave
            frequency *= terrainSettings.lacunarity; // Increase frequency for next octave

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

    // Returns a Biome blend for the two biomes affecting a world point
    private BiomeBlend GetBiomeBlend(float worldX, float worldZ)
    {
        float frequency = terrainSettings.biomeFrequency;
        float amplitude = 1f;

        float biomeNoise = 0f;
        float maxPossibleHeight = 0f;

        for (int i = 0; i < terrainSettings.biomeOctaves; i++)
        {
            float sampleX = worldX / terrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.x;

            float sampleZ = worldZ / terrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.y;

            float perlin = Mathf.PerlinNoise(sampleX, sampleZ);

            biomeNoise += perlin * amplitude;

            maxPossibleHeight += amplitude;

            amplitude *= terrainSettings.biomePersistence;
            frequency *= terrainSettings.biomeLacunarity;
        }

        biomeNoise /= maxPossibleHeight;

        for (int i = 0; i < currentBiomes.Count - 1; i++)
        {
            Biome_Settings a = currentBiomes[i];
            Biome_Settings b = currentBiomes[i + 1];

            float border = b.noiseThreshold;

            float blendStart = border - terrainSettings.biomeBlendWidth;

            float blendEnd = border + terrainSettings.biomeBlendWidth;

            if (biomeNoise < blendStart)
            {
                return new BiomeBlend
                {
                    biomeA = a,
                    biomeB = a,
                    blendValue = 0
                };
            }

            if (biomeNoise >= blendStart && biomeNoise <= blendEnd)
            {
                float blend = Mathf.InverseLerp(blendStart, blendEnd, biomeNoise);

                blend = Mathf.SmoothStep(0, 1, blend);

                return new BiomeBlend
                {
                    biomeA = a,
                    biomeB = b,
                    blendValue = blend
                };
            }
            
        }

        return new BiomeBlend
        {
            biomeA = currentBiomes[0],
            biomeB = currentBiomes[0],
            blendValue = 0
        };
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

                //Biome_Settings biome = GetBiomeAtPoint(worldX, worldZ);

                BiomeBlend biomeBlend = GetBiomeBlend(worldX, worldZ);


                float noiseHeightA = GenerateNoiseHeight(worldX, worldZ, offsetSeed, biomeBlend.biomeA);

                float noiseHeightB = GenerateNoiseHeight(worldX, worldZ, offsetSeed, biomeBlend.biomeB);

                float blendedHeight = Mathf.Lerp(noiseHeightA, noiseHeightB, biomeBlend.blendValue);

                float threshold = Mathf.Lerp(biomeBlend.biomeA.lowerThreshold, biomeBlend.biomeB.lowerThreshold, biomeBlend.blendValue);



                if (blendedHeight <= threshold)
                    blendedHeight = 0;

                vertices[i] = new Vector3(x, blendedHeight, z);
                SetMinMaxHeights(blendedHeight);

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

                BiomeBlend blend = GetBiomeBlend(worldX, worldZ);

                float height = Mathf.InverseLerp(minTerrainheight, maxTerrainheight,vertices[i].y);

                Color colorA = blend.biomeA.gradient.Evaluate(height);
                Color colorB = blend.biomeB.gradient.Evaluate(height);

                // Smooth blend value
                float colorBlend = Mathf.SmoothStep( 0, 1, blend.blendValue);

                // Adds noise to break up colour borders
                float colorNoise = Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f);
                colorBlend += (colorNoise - 0.5f) * 0.08f;
                colorBlend = Mathf.Clamp01(colorBlend);

                Color finalColor =Color.Lerp(colorA, colorB, colorBlend);


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
        //Noise_Settings noise_settings, 
        //Terrain_Generation_Settings terrain_generation_settings, 
        Global_Terrain_Settings global_terrain_settings,
        List<Biome_Settings> biomesInUse,
        Vector2[] offset_seed,
        Vector2 biome_offset_seed,
        int x, int z) //coordinates of the chunk
    {
        //Assign variables
        terrainSettings = global_terrain_settings;
        currentBiomes = biomesInUse;
        offsetSeed = offset_seed;
        biomeOffsetSeed = biome_offset_seed;
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
