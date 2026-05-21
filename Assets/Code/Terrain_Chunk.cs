using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;

// Terrain chunk class //
public class Terrain_Chunk : MonoBehaviour
{
    private Terrain_Generator terrainGenerator; // Terrain generator reference

    private Global_Terrain_Settings terrainSettings; // Global settings that apply to all chunks/biomes 
    private List<Biome_Settings> currentBiomes; // Biomes being used
    private bool spawnAssets = false; // Whether to spawn assets on this chunk, set in the world generator when creating the chunk

    private int chunkX, chunkZ; // Coordinates of the chunk

    private Mesh mesh; // Mesh of the chunk
    private Vector3[] vertices; // Vertices of the mesh
    private int[] triangles; // Triangles of the mesh, each group of 3 integers represents a triangle

    private float minTerrainheight; // Used for color gradient, minimum height of the terrain
    private float maxTerrainheight; // Used for color gradient, maximum height of the terrain

    private Color[] colours; // Colours of the mesh, each vertex has a corresponding colour used in the material

    private float[,] heightMap; // Height map of the chunk, used for asset spawning
    private BiomeBlend[,] biomeMap; // Biome map of the chunk, used for asset spawning

    // Assign mesh to the chunk, if it doesn't already have one
    private void AssignMesh()
    {
        if (mesh == null)
        {
            //Debug.Log("Creating new mesh");
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    // Set the min and max heights of the terrain, used for the color gradient
    private void SetMinMaxHeights(float noiseHeight)
    {
        // Set min and max height of map for color gradient
        if (noiseHeight > maxTerrainheight)
            maxTerrainheight = noiseHeight;
        if (noiseHeight < minTerrainheight)
            minTerrainheight = noiseHeight;
    }

    // Create the actual mesh shape by assigning vertices
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

                // Get the biome blend for this point
                BiomeBlend biomeBlend = terrainGenerator.GetBiomeBlend(worldX, worldZ);
                biomeMap[x, z] = biomeBlend;

                // Get the noise height for both biomes and blend them together
                float noiseHeightA = terrainGenerator.GenerateNoiseHeight(worldX, worldZ, biomeBlend.biomeA);
                float noiseHeightB = terrainGenerator.GenerateNoiseHeight(worldX, worldZ, biomeBlend.biomeB);

                float blendedHeight = Mathf.Lerp(noiseHeightA, noiseHeightB, biomeBlend.blendValue);
                float threshold = Mathf.Lerp(biomeBlend.biomeA.lowerThreshold, biomeBlend.biomeB.lowerThreshold, biomeBlend.blendValue);

                // Set height to 0 if it's below the threshold, creating flat areas in the terrain
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
    // Create triangles of the mesh by assigning integers to the triangle array
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
    // Colour the terrain by assigning a colour to each vertex based on the height and biome blend at that point
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
    // Update the mesh with the new vertices, triangles and colours, and recalculate normals and tangents for lighting
    private void UpdateMesh()
    {
        ClearMesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colours;

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

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
        bool useAssetSpawning,
        int x,int z) //Coordinates of the chunk
    {
        //Assign variables

        terrainGenerator = generator;
        terrainSettings = settings;

        spawnAssets = useAssetSpawning;

        chunkX = x;
        chunkZ = z;

        //Call generate mesh
        GenerateMesh();

        return true;

    }

    // Main function that calls all the other functions in the correct order to generate the mesh and spawn assets
    private void GenerateMesh()
    {
        //Generate Mesh
        AssignMesh();
        CreateMeshShape();
        CreateTriangles();
        ColourTerrain();
        UpdateMesh();

        if (spawnAssets)
        {
            terrainGenerator.SpawnAssetsInChunk(this, biomeMap, heightMap, chunkX, chunkZ);
        }
    }




}
