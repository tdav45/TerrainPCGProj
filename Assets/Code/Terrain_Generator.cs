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

//TERRAIN GENERATION//
public class Terrain_Generator : MonoBehaviour
{
    [SerializeField] 
    private GameObject terrainChunkPrefab;

    [SerializeField]
    [Range(1, 16)]
    private int gridSize = 10;
    [SerializeField]
    private GameObject terrainHolder;
    [SerializeField]
    private Asset_Spawner assetSpawner;
    [SerializeField]
    private Global_Terrain_Settings globalTerrainSettings; //Global settings that apply to all chunks/biomes
    [SerializeField]
    private Biome_Settings[] biomePool; // Array of biome settings to randomly select from when generating terrain
    [SerializeField]
    private bool useAllBiomes;
    [SerializeField]
    private bool isRandomSeed;

    private List<Biome_Settings> allBiomes; //All biome assets
    private List<GameObject> generatedTerrainChunks;
  
    private bool isGenerating = false;
   
    private Vector2[] octaveOffsets;
    private Vector2 biomeOffsetSeed;


    public float GetBiomeNoise(float worldX, float worldZ)
    {
        float frequency = globalTerrainSettings.biomeFrequency;
        float amplitude = 1f;

        float biomeNoise = 0f;
        float maxPossible = 0f;

        for (int i = 0; i < globalTerrainSettings.biomeOctaves; i++)
        {
            float sampleX = worldX / globalTerrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.x;
            float sampleZ = worldZ / globalTerrainSettings.biomeNoiseScale * frequency + biomeOffsetSeed.y;

            biomeNoise += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;

            maxPossible += amplitude;

            amplitude *= globalTerrainSettings.biomePersistence;
            frequency *= globalTerrainSettings.biomeLacunarity;
        }

        return biomeNoise / maxPossible;
    }
    public BiomeBlend GetBiomeBlend(float worldX, float worldZ)
    {
        float biomeNoise = GetBiomeNoise(worldX, worldZ);

        int biomeIndex = GetBiomeIndex(biomeNoise);

        Biome_Settings biomeA = allBiomes[biomeIndex];
        Biome_Settings biomeB = allBiomes[biomeIndex];

        float blend = 0f;

        if (biomeIndex < allBiomes.Count - 1)
        {
            Biome_Settings nextBiome = allBiomes[biomeIndex + 1];

            float thresholdA = biomeA.noiseThreshold;
            float thresholdB = nextBiome.noiseThreshold;

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
    private int GetBiomeIndex(float biomeNoise)
    {
        for (int i = 0; i < allBiomes.Count - 1; i++)
        {
            if (biomeNoise < allBiomes[i + 1].noiseThreshold)
                return i;
        }

        return allBiomes.Count - 1;
    }
    private float GetBiomeEdgeFactor(float biomeNoise)
    {
        float minDist = float.MaxValue;

        foreach (var biome in allBiomes)
        {
            float dist = Mathf.Abs(biomeNoise - biome.noiseThreshold);
            minDist = Mathf.Min(minDist, dist);
        }

        float edgeWidth = 0.08f;

        return 1f - Mathf.Clamp01(minDist / edgeWidth);
    }
    public float GenerateNoiseHeight(float x, float z, Biome_Settings biome)
    {
        float frequency = globalTerrainSettings.baseFrequency;
        float persistence = globalTerrainSettings.basePersistence;
        float amplitude = biome.baseAmplitude;

        float noiseValue = 0f;
        float heightValue = 0f;

        for (int i = 0; i < globalTerrainSettings.octaves; i++)
        {
            float sampleZ = z / biome.noiseScale * frequency + octaveOffsets[i].y;
            float sampleX = x / biome.noiseScale * frequency + octaveOffsets[i].x;

            noiseValue = Mathf.PerlinNoise(sampleZ, sampleX) * 2 - 1;

            heightValue += biome.heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence;
            frequency *= globalTerrainSettings.lacunarity;
        }

        return heightValue;
    }

    //Get the offset seed for each octave
    private Vector2[] GetOffsetSeed()
    {
        Vector2[] offsetSeed = new Vector2[globalTerrainSettings.octaves];

        System.Random prng = new System.Random(globalTerrainSettings.seed);


        for (int i = 0; i < globalTerrainSettings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            offsetSeed[i] = new Vector2(offsetX, offsetY);
        }


        return offsetSeed;
    }

    private Vector2 GenerateBiomeOffsetSeed()
    {
        System.Random prng = new System.Random(globalTerrainSettings.seed);
        Vector2 biomeOffset;


        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);

        biomeOffset = new Vector2(offsetX, offsetY);


        return biomeOffset;
    }

    public void RandomiseSeed()
    {
        globalTerrainSettings.seed = Random.Range(0, 1000);
    }

    public void ResetIsGenerating()
    {
        isGenerating = false;
    }

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

    private void MakeBiomePool()
    {
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

    private void AssignBiomeThresholds()
    {
        var groups = allBiomes.GroupBy(b => b.order).OrderBy(g => g.Key) .ToList();

        float current = 0f; 

        foreach (var group in groups)
        {

            float totalPriority = group.Sum(b => b.priority);


            foreach (var biome in group)
            {
                float slice = biome.priority / totalPriority;

                biome.thresholdStart = current;
                biome.thresholdEnd = current + slice;

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

        allBiomes.Sort((a, b) => a.noiseThreshold.CompareTo(b.noiseThreshold));

    }

    //GENERATE TERRAIN (ENTRY POINT)//
    public void CreateNewTerrain()
    {
        if (isGenerating == false)
        {
            isGenerating = true;

            RemovePreviousGeneration();


            if (isRandomSeed)
            {
                RandomiseSeed();
            }

            SetupBiomes();

            octaveOffsets = GetOffsetSeed();
            biomeOffsetSeed = GenerateBiomeOffsetSeed();

            //Generate grid
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    //  NewChunk(i, j, terrainHolder, offsetSeed, biomeOffsetSeed);
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

    private void NewChunk(int x, int z)
    {
        var chunk = Instantiate(terrainChunkPrefab);
        chunk.transform.parent = terrainHolder.transform;

        if (chunk.GetComponent<Terrain_Chunk>().CreateNewTerrainChunk(
           this,
           globalTerrainSettings,
           x, z))
        {
            // Debug.Log("New Chunk generated at: " + x + " " + z);

        }

        generatedTerrainChunks.Add(chunk);
    }



}
