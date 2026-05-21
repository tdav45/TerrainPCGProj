using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

//GUI//
[CustomEditor(typeof(Terrain_Generator))]
public class Terrain_Generator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        // Inspector GUI with buttons to generate and clear terrain, and reset the isGenerating boolean if needed

        DrawDefaultInspector();

        Terrain_Generator terrainGenerator = (Terrain_Generator)target;
        
        if (GUILayout.Button("Generate Terrain"))
        {
            terrainGenerator.CreateNewTerrain();
        }

        if(GUILayout.Button("Clear Terrain"))
        {
            terrainGenerator.RemovePreviousGeneration();
        }

       
        if (GUILayout.Button("Reset Is Generating"))
        {
            terrainGenerator.ResetIsGenerating();
        }

    }
}

// TERRAIN GENERATION //
public class Terrain_Generator : MonoBehaviour
{
    #region Variables
    [SerializeField] 
    private GameObject terrainChunkPrefab; // Prefab for the terrain chunk, with the necessary components attached
    [SerializeField]
    [Range(1, 16)]
    private int gridSize = 10; // Size of the grid of terrain chunks to generate, capped for performance
    [SerializeField]
    private GameObject terrainHolder; // Parent object to hold all generated terrain chunks, used for organisation and to easily clear previous generations
    [SerializeField]
    private Global_Terrain_Settings globalTerrainSettings; //Global settings that apply to all chunks/biomes
    [SerializeField]
    private Biome_Settings[] biomePool; // Array of biomes to randomly select from when generating terrain
    
    // Booleans for generation
    [SerializeField]
    private bool useAllBiomes; // Whether to use all biomes found in the Biomes folder, or just the ones in the biomePool array
    [SerializeField]
    private bool isRandomSeed; // Whether to randomise the seed value for each generation, or use the previous seed
    [SerializeField]
    private bool useAssetSpawning = false; // Whether to spawn assets on the terrain

    private List<Biome_Settings> allBiomes; // All biome assets
    private List<GameObject> generatedTerrainChunks; // All generated terrain chunk objects

    private bool isGenerating = false; // Boolean to prevent multiple terrain generations at the same time

    private Vector2[] octaveOffsets; // Offset seed for each octave, used to create variation in the noise
    private Vector2 biomeOffsetSeed; // Offset seed for the biome noise, used to create variation in the biome distribution

    private Asset_Spawner assetSpawner; // Asset spawner reference, used to spawn assets on the terrain chunks

    #endregion

    #region BiomeNoise
    // Get the biome noise value at a given world position, used to determine which biomes to use at that point
    public float GetBiomeNoise(float worldX, float worldZ)
    {
        float frequency = globalTerrainSettings.biomeFrequency;
        float amplitude = 1f;

        float biomeNoise = 0f;
        float maxPossible = 0f;

        for (int i = 0; i < globalTerrainSettings.biomeOctaves; i++)
        {
            // Calculate the sample position for the noise, using the world position, biome noise scale, frequency, and biome offset seed
            float sampleX = worldX / globalTerrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.x;
            float sampleZ = worldZ / globalTerrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.y;


            FastNoiseLite noise = new FastNoiseLite();
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            
            biomeNoise += noise.GetNoise(sampleX, sampleZ) * amplitude;

            maxPossible += amplitude;

            amplitude *= globalTerrainSettings.biomePersistence;
            frequency *= globalTerrainSettings.biomeLacunarity;
        }

        // Normalize the biome noise value to be between 0 and 1
        return biomeNoise / maxPossible;
    }
    // Get the biomes to blend between and the blend value at a given world position, used to determine the biome to use at that point
    public BiomeBlend GetBiomeBlend(float worldX, float worldZ)
    {
        float biomeNoise = GetBiomeNoise(worldX, worldZ);

        int biomeIndex = GetBiomeIndex(biomeNoise);

        Biome_Settings biomeA = allBiomes[biomeIndex];
        Biome_Settings biomeB = allBiomes[biomeIndex];

        float blend = 0f;

        // If the biome noise value is close enough to the next biome's threshold, blend between the two biomes
        if (biomeIndex < allBiomes.Count - 1)
        {
            Biome_Settings nextBiome = allBiomes[biomeIndex + 1];

            float thresholdA = biomeA.noiseThreshold;
            float thresholdB = nextBiome.noiseThreshold;

            // Inverse lerp the biome noise value between the two biome thresholds to get a blend value between 0 and 1
            float t = Mathf.InverseLerp(thresholdA, thresholdB, biomeNoise);

            float edge = GetBiomeEdgeFactor(biomeNoise);

            blend = t * edge;

            biomeB = nextBiome;
        }

        return new BiomeBlend
        {
            biomeA = biomeA,
            biomeB = biomeB,
            blendValue = blend
        };
    }
    // Get the index of the biome that corresponds to a given biome noise value
    private int GetBiomeIndex(float biomeNoise)
    {
        for (int i = 0; i < allBiomes.Count - 1; i++)
        {
            if (biomeNoise < allBiomes[i + 1].noiseThreshold)
                return i;
        }

        return allBiomes.Count - 1;
    }
    // Get a factor between 0 and 1 to determine how close the biome noise value is to the edge of a biome, used to create smoother transitions between biomes
    private float GetBiomeEdgeFactor(float biomeNoise)
    {
        float minDist = float.MaxValue;

        foreach (var biome in allBiomes)
        {
            //Mathf.Abs to get the distance from the biome noise value to the biome's noise threshold, and find the minimum distance to any biome threshold
            float dist = Mathf.Abs(biomeNoise - biome.noiseThreshold);
            minDist = Mathf.Min(minDist, dist);
        }
        // Clamp the minimum distance to the biome edge width and invert it to get a factor between 0 and 1, where 1 is at the edge of the biome and 0 is far from the edge
        return 1f - Mathf.Clamp01(minDist / globalTerrainSettings.biomeEdgeWidth);
    }
    // Generate a random offset seed for the biome noise
    private Vector2 GenerateBiomeOffsetSeed()
    {
        System.Random prng = new System.Random(globalTerrainSettings.seed);
        Vector2 biomeOffset;

        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);

        biomeOffset = new Vector2(offsetX, offsetY);

        return biomeOffset;
    }


    #endregion

    #region TerrainNoise
    // Generate the height of the terrain at a given point using Perlin noise
    public float GenerateNoiseHeight(float x, float z, Biome_Settings biome)
    {
        float frequency = globalTerrainSettings.baseFrequency;
        float persistence = globalTerrainSettings.basePersistence;
        float amplitude = biome.baseAmplitude;

        float noiseValue = 0f;
        float heightValue = 0f;

        for (int i = 0; i < globalTerrainSettings.octaves; i++)
        {
            // Calculate the sample position for the noise, using the world position, biome noise scale, frequency, and octave offset seed
            float sampleZ = z / biome.noiseScale * frequency + octaveOffsets[i].y;
            float sampleX = x / biome.noiseScale * frequency + octaveOffsets[i].x;


            FastNoiseLite noise = new FastNoiseLite();
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

           

            //noiseValue = Mathf.PerlinNoise(sampleZ, sampleX) * 2 - 1;

             noiseValue = noise.GetNoise(sampleX, sampleZ) * 2 - 1;

            // Evaluate the biome's height curve at the noise value to get a height multiplier, and multiply it by the amplitude to get the height contribution of this octave
            heightValue += biome.heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence;
            frequency *= globalTerrainSettings.lacunarity;
        }

        return heightValue;
    }
    //Get the random offset seed for each octave of terrain noise
    private Vector2[] GetOffsetSeed()
    {
        Vector2[] offsetSeed = new Vector2[globalTerrainSettings.octaves];

        System.Random prng = new System.Random(globalTerrainSettings.seed);

        // For each octave, generate a random offset within a large range to create variation in the noise pattern, and store it in the offsetSeed array
        for (int i = 0; i < globalTerrainSettings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            offsetSeed[i] = new Vector2(offsetX, offsetY);
        }


        return offsetSeed;
    }

    #endregion

    #region Asset Spawning

    private void AssignAssetSpawner()
    {
        if (assetSpawner == null)
        {
            assetSpawner = gameObject.AddComponent<Asset_Spawner>();   
        }

        assetSpawner.SetAssetSpawningSettings(globalTerrainSettings.spawnSpacing, globalTerrainSettings.maxSlope);
    }

    public void SpawnAssetsInChunk(Terrain_Chunk chunk, BiomeBlend[,] biomeMap, float[,] heightMap, int chunkX, int chunkZ)
    {
        if(assetSpawner != null)
        {
            assetSpawner.SpawnAssets(chunk, globalTerrainSettings, biomeMap, heightMap, chunkX, chunkZ);
        }
    }

    #endregion

    #region Utility

    // Randomise the seed value
    public void RandomiseSeed()
    {
        globalTerrainSettings.seed = Random.Range(0, 1000);
    }

    // Reset the isGenerating boolean
    public void ResetIsGenerating()
    {
        isGenerating = false;
    }

    // Remove all previously generated terrain chunks
    public void RemovePreviousGeneration()
    {
        if (generatedTerrainChunks != null)
        {
            if (generatedTerrainChunks.Count > 0)
            {
                foreach (GameObject terrain in generatedTerrainChunks)
                {
                    DestroyImmediate(terrain);
                }
            }

            else
            {
                if (terrainHolder.transform.childCount > 0)
                {
                    List<GameObject> oldChunks = new List<GameObject>(); 
                    
                    foreach (Transform child in terrainHolder.transform)
                    {
                        oldChunks.Add(child.gameObject);
                       
                    }

                    foreach (GameObject obj in oldChunks)
                    {
                        DestroyImmediate(obj.gameObject);
                    }
                }
            }
        }

     

        generatedTerrainChunks = new List<GameObject>();

    }

    #endregion

    #region Biome Setup
    // Make the pool of biomes to use in generation
    private void MakeBiomePool()
    {
        // Find all biome assets in the Biomes folder and add them to the allBiomes list
        foreach (var biome in AssetDatabase.FindAssets("t:Biome_Settings", new[] { "Assets/Code/Generation Settings/Biomes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(biome);
            Biome_Settings biomeSettings = AssetDatabase.LoadAssetAtPath(path, typeof(Biome_Settings)) as Biome_Settings;
            if (biomeSettings != null)
            {
                Debug.Log("Loaded biome: " + biomeSettings.name);

                allBiomes.Add(biomeSettings);
            }
        }
    }

    // Assign the noise thresholds for each biome based on their priority and order values
    private void AssignBiomeThresholds()
    {
        // Group the biomes by their order value, and sort the groups by their order value to ensure the correct hierarchy of biomes
        var groups = allBiomes.GroupBy(b => b.order).OrderBy(g => g.Key) .ToList();

        float current = 0f; 

        foreach (var group in groups)
        {
            // Sum the priority values of the biomes in the group to determine how much of the noise range they should occupy
            float totalPriority = group.Sum(b => b.priority);


            foreach (var biome in group)
            {
                // Calculate the slice of the noise range that this biome should occupy based on its priority
                float slice = biome.priority / totalPriority;

                biome.thresholdStart = current;
                biome.thresholdEnd = current + slice;

                // Increment the current noise threshold by the slice for the next biome
                current += slice;
            }
        }



        foreach (var biome in allBiomes)
        {
            biome.thresholdStart /= current;
            biome.thresholdEnd /= current;

            biome.noiseThreshold = (biome.thresholdStart + biome.thresholdEnd) * 0.5f;
            biome.noiseThreshold = Mathf.Round(biome.noiseThreshold * 10) / 10;
        }

        float minThreshold = allBiomes.Min(b => b.noiseThreshold);

        foreach (var biome in allBiomes)
        {
            biome.noiseThreshold -= minThreshold;
        }

    }

    // Set up the biomes to be used in generation
    private void SetupBiomes()
    {
        allBiomes = new List<Biome_Settings>();

        if (useAllBiomes)
        {
            MakeBiomePool();
        }
        else
        {
            allBiomes.AddRange(biomePool);
        }

        AssignBiomeThresholds();

        // Sort the biomes by their noise threshold to ensure they are in the correct order for biome blending
        allBiomes.Sort((a, b) => a.noiseThreshold.CompareTo(b.noiseThreshold));

    }

    #endregion

    #region New Terrain Generation

    // GENERATE TERRAIN (ENTRY POINT) //
    public void CreateNewTerrain()
    {
        if (isGenerating == false)
        {
            isGenerating = true;

            if(useAssetSpawning)
            {
                AssignAssetSpawner();
            }

            RemovePreviousGeneration();


            if (isRandomSeed)
            {
                RandomiseSeed();
            }

            SetupBiomes();

            // Get the offset seeds for the terrain noise and biome noise
            octaveOffsets = GetOffsetSeed();
            biomeOffsetSeed = GenerateBiomeOffsetSeed();

            //Generate grid
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    NewChunk(i, j);
        
                }
            }

            isGenerating = false;
        }
        else
        {
            Debug.LogError("Already Generating");
        }


    }

    // Generate a new terrain chunk at the given coordinates
    private void NewChunk(int x, int z)
    {
        var chunk = Instantiate(terrainChunkPrefab);
        chunk.transform.parent = terrainHolder.transform;

        if (chunk.GetComponent<Terrain_Chunk>().CreateNewTerrainChunk(
           this,
           globalTerrainSettings,
           useAssetSpawning,
           x, z))
        {
            // Debug.Log("New Chunk generated at: " + x + " " + z);

        }

        generatedTerrainChunks.Add(chunk);
    }

    #endregion

}
