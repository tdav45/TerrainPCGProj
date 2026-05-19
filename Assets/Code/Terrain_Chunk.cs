using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;


public class Terrain_Chunk : MonoBehaviour
{
    private Terrain_Generator terrainGenerator;
    private Global_Terrain_Settings terrainSettings;
    private List<Biome_Settings> currentBiomes; //biomes being used

    private int chunkX, chunkZ;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private float minTerrainheight;
    private float maxTerrainheight;

    private Color[] colours;

    private float[,] heightMap;
    private BiomeBlend[,] biomeMap;

    private Asset_Spawner assetSpawner;

    private void AssignMesh()
    {
        if (mesh == null)
        {
            //Debug.Log("Creating new mesh");
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
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
    private void CreateMeshShape()
    {
        int width = terrainSettings.sizeX + 1;
        int height = terrainSettings.sizeZ + 1;

        vertices = new Vector3[(terrainSettings.sizeX + 1) * (terrainSettings.sizeZ + 1)];

        heightMap = new float[width, height];
        biomeMap = new BiomeBlend[width, height];

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

                BiomeBlend biomeBlend = terrainGenerator.GetBiomeBlend(worldX, worldZ);
                biomeMap[x, z] = biomeBlend;

                float noiseHeightA = terrainGenerator.GenerateNoiseHeight(worldX, worldZ, biomeBlend.biomeA);

                float noiseHeightB = terrainGenerator.GenerateNoiseHeight(worldX, worldZ, biomeBlend.biomeB);

                float blendedHeight = Mathf.Lerp(noiseHeightA, noiseHeightB, biomeBlend.blendValue);

                float threshold = Mathf.Lerp(biomeBlend.biomeA.lowerThreshold, biomeBlend.biomeB.lowerThreshold, biomeBlend.blendValue);

                if (blendedHeight <= threshold)
                {
                    blendedHeight = 0;
                }

                heightMap[x, z] = blendedHeight;

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
                BiomeBlend blend = biomeMap[x, z];

                float height = Mathf.InverseLerp(minTerrainheight, maxTerrainheight, vertices[i].y);

                Color colorA = blend.biomeA.gradient.Evaluate(height);
                Color colorB = blend.biomeB.gradient.Evaluate(height);

                Color finalColor = Color.Lerp(colorA, colorB, blend.blendValue);
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
         Terrain_Generator generator,
        Global_Terrain_Settings settings,
        int x,int z) //Coordinates of the chunk
    {
        //Assign variables

        terrainGenerator = generator;
        terrainSettings = settings;

        chunkX = x;
        chunkZ = z;

        assetSpawner = gameObject.AddComponent<Asset_Spawner>();

        //Call generate mesh
        GenerateMesh();



        return true;

    }

    private void GenerateMesh()
    {
        //Generate Mesh
        AssignMesh();
        CreateMeshShape();
        CreateTriangles();
        ColourTerrain();
        UpdateMesh();
        assetSpawner.SpawnAssets(this, terrainSettings, biomeMap, heightMap, chunkX, chunkZ);
    }




}
