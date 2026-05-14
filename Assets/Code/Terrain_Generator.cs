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

    /*Old variables
     * [SerializeField]
    private Material grassMaterial;
    
    //Terrian mesh settings
    [Tooltip("Curve controls the range of height of the terrain")]
    [SerializeField]
    private AnimationCurve heightCurve;
    [Tooltip("Size of the terrain in the X axis")]
    [SerializeField]
    private int terrainX = 100;
    [Tooltip("Size of the terrain in the Z axis")]
    [SerializeField]
    private int terrainZ = 100;
    
    //NOISE SETTINGS//

    [SerializeField]
    private int terrainScale = 100;
    [Tooltip("Number of layers of noise to add detail to the terrain")]
    [SerializeField]
    private int octaves = 4;
    [Tooltip("Controls the frequency of the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float scale = 20f;
    [Tooltip("Seed for random number generator, changing this will create a different terrain")]
    [SerializeField]
    private int seed = 0;

    [Tooltip("Base amplitude, scales the height of the terrain, higher values will create taller terrain")]
    [SerializeField]
    private float baseAmplitude;

    [Tooltip("Base frequency for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float baseFrequency = 1f;
    [Tooltip("Base persistence for the noise, higher values will create more rugged terrain")]
    [SerializeField]
    private float basePersistence = 0.5f;
    [Tooltip("Lacunarity for the noise, higher values will create more detailed terrain")]
    [SerializeField]
    private float lacunarity = 2f;
    [Tooltip("Height threshold for the terrain, any height below this value will be set to 0")]
    [SerializeField]
    private float lowerThreshold = 0.04f;*/
    // [SerializeField] private Gradient gradient;


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

    private List<Biome_Settings> allBiomes;
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
            foreach (GameObject terrain in generatedTerrainChunks)
            {
                DestroyImmediate(terrain);
            }
        }
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
            }


            allBiomes.Add(biomeSettings);

        }
    }

    private void BiomeNoise()
    {
       // Generate noise map for biomes
       //Intergrate GetBiome()

    }

    private Biome_Settings GetBiome()
    {
        if (useAllBiomes)
        {
            MakeBiomePool();
        }

        else
        {
            allBiomes = new List<Biome_Settings>(biomePool);
        }

        if (allBiomes.Count == 0)
        {
            Debug.LogError("No biomes assigned to the terrain generator");
            return null;
        }
        int r = Random.Range(0, allBiomes.Count);
        Debug.Log("Selected biome: " + allBiomes[r].name);
        return allBiomes[r];
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

            //Generate grid
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    NewChunk(i, j, terrainHolder, GetBiome());
                }
            }

            isGenerating = false;
        }
        else
        {
            Debug.LogError("Already Generating");
        }


    }

    private void NewChunk(int x, int z, GameObject parent, Biome_Settings biome)
    {
        var chunk = Instantiate(terrainChunkPrefab);
        chunk.transform.parent = parent.transform;

        if (chunk.GetComponent<Terrain_Chunk>().CreateNewTerrainChunk(globalTerrainSettings, biome,
            GetOffsetSeed(), x, z))
        {
            Debug.Log("New Chunk generated at: " + x + " " + z);
        }

        generatedTerrainChunks.Add(chunk);
    }



}
