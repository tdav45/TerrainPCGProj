using System.Collections.Generic;
using System.IO;
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
    private Global_Terrain_Settings globalTerrainSettings; //Global settings that apply to all chunks/biomes
    [SerializeField]
    private Biome_Settings[] biomePool; // Array of biome settings to randomly select from when generating terrain
    [SerializeField]
    private bool useAllBiomes;
    [SerializeField]
    private bool isRandomSeed;

    private List<Biome_Settings> allBiomes; //All biome assets
    private List<BiomeSeed> biomeSeeds;
    private bool isGenerating = false;
    private List<GameObject> generatedTerrainChunks;



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

    private Vector2 BiomeOffsetSeed()
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

    private Vector2 WarpPosition(float worldX, float worldZ)
    {
        float warpX = Mathf.PerlinNoise(
            worldX / globalTerrainSettings.warpScale,
            worldZ / globalTerrainSettings.warpScale
        ) * 2 - 1;

        float warpZ = Mathf.PerlinNoise(
            (worldX + 1000) / globalTerrainSettings.warpScale,
            (worldZ + 1000) / globalTerrainSettings.warpScale
        ) * 2 - 1;

        return new Vector2(
            worldX + warpX * globalTerrainSettings.warpStrength,
            worldZ + warpZ * globalTerrainSettings.warpStrength
        );
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

    private void SetupBiomes()
    {
        biomeSeeds = new List<BiomeSeed>();
        allBiomes = new List<Biome_Settings>();

        if (useAllBiomes)
        {
            MakeBiomePool();
        }
        else
        {
            allBiomes.AddRange(biomePool);
        }

        System.Random prng = new System.Random(globalTerrainSettings.seed);

        int seedCount = allBiomes.Count * 3; // tweak density

        for (int i = 0; i < seedCount; i++)
        {
            Biome_Settings biome = allBiomes[prng.Next(allBiomes.Count)];

            float worldSizeX = gridSize * globalTerrainSettings.sizeX;
            float worldSizeZ = gridSize * globalTerrainSettings.sizeZ;

            Vector2 pos = new Vector2(prng.Next(0, (int)worldSizeX),prng.Next(0, (int)worldSizeZ));

            biomeSeeds.Add(new BiomeSeed{position = pos,biome = biome});
        }
    }

    private BiomeBlend GetBiomeBlend(float worldX, float worldZ)
    {
        Vector2 pos = WarpPosition(worldX, worldZ);

        float closestDist = float.MaxValue;
        float secondClosestDist = float.MaxValue;

        BiomeSeed closest = null;
        BiomeSeed second = null;

        // find nearest 2 seeds
        foreach (var seed in biomeSeeds)
        {
            float d = Vector2.SqrMagnitude(pos - seed.position);

            if (d < closestDist)
            {
                secondClosestDist = closestDist;
                second = closest;

                closestDist = d;
                closest = seed;
            }
            else if (d < secondClosestDist)
            {
                secondClosestDist = d;
                second = seed;
            }
        }

        // safety fallback
        if (closest == null || second == null)
        {
            return new BiomeBlend
            {
                biomeA = allBiomes[0],
                biomeB = allBiomes[0],
                blendValue = 0
            };
        }

        Biome_Settings biomeA = closest.biome;
        Biome_Settings biomeB = second.biome;

        float distA = Mathf.Sqrt(closestDist);
        float distB = Mathf.Sqrt(secondClosestDist);

        float borderDistance = Mathf.Abs(distA - distB);

        float edgeWidth = 15f;

        float blend = 1f - Mathf.Clamp01(borderDistance / edgeWidth);

        blend = Mathf.SmoothStep(0, 1, blend);

        return new BiomeBlend
        {
            biomeA = biomeA,
            biomeB = biomeB,
            blendValue = blend
        };
    }

    public BiomeBlend SampleBiome(float worldX, float worldZ)
    {
        return GetBiomeBlend(worldX, worldZ);
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

            //Override settings by biome
            //AdjustSettingsByBiome();

            SetupBiomes();

            Vector2[] offsetSeed = GetOffsetSeed();
            Vector2 biomeOffsetSeed = BiomeOffsetSeed();

            

            allBiomes.Sort((a, b) => a.noiseThreshold.CompareTo(b.noiseThreshold));

            //Generate grid
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    NewChunk(i, j, terrainHolder, offsetSeed, biomeOffsetSeed);
                }
            }

            isGenerating = false;
        }
        else
        {
            Debug.LogError("Already Generating");
        }


    }

    private void NewChunk(int x, int z, GameObject parent, Vector2[] offsetSeed, Vector2 biomeOffsetSeed)
    {
        var chunk = Instantiate(terrainChunkPrefab);
        chunk.transform.parent = parent.transform;

        if (chunk.GetComponent<Terrain_Chunk>().CreateNewTerrainChunk(this, globalTerrainSettings,
            offsetSeed, x, z))
        {
           // Debug.Log("New Chunk generated at: " + x + " " + z);
        }

        generatedTerrainChunks.Add(chunk);
    }



}
