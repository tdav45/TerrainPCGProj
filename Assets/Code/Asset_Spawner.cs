using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

// Class for asset spawning, on each terrain chunk //
public class Asset_Spawner : MonoBehaviour
{
    private int spawnSpacing = 1; // Spacing between potential spawn points

    private float maxSlope = 1f; // Maximum slope allowed for spawning, calculated as the maximum height difference between a point and its 4 cardinal neighbors

  
    public void SetAssetSpawningSettings(int spawn_spacing, float max_slope)
    {
        spawnSpacing = spawn_spacing;
        maxSlope = max_slope;
    }
    
    // Calculate the slope at a given point in the height map by comparing the height of the point to its 4 neighbors and returning the maximum height difference
    private float CalculateSlope(float[,] heightMap, int x, int z)
    {
        float current = heightMap[x, z];

        float right = heightMap[x + 1, z];
        float left = heightMap[x - 1, z];

        float up = heightMap[x, z + 1];
        float down = heightMap[x, z - 1];

        float slopeX = Mathf.Max(Mathf.Abs(current - right), Mathf.Abs(current - left));
        float slopeZ = Mathf.Max(Mathf.Abs(current - up), Mathf.Abs(current - down));

        return Mathf.Max(slopeX, slopeZ);
    }

    // Try to spawn an object at a given point, using the biome settings to determine whether to spawn and which object to spawn
    private void TrySpawnObject(
        Terrain_Chunk chunk,
        Biome_Settings biome,
        float terrainHeight,
        int localX,
        int localZ,
        int chunkX,
        int chunkZ,
        Global_Terrain_Settings settings)
    {
        if (biome.spawnPrefabs == null)
            return;

        if (biome.spawnPrefabs.Length == 0)
            return;
        // Calculate world coordinates of the point by adding the local coordinates to the chunk coordinates multiplied by the chunk size
        float worldX = (chunkX * settings.sizeX) + localX;
        float worldZ = (chunkZ * settings.sizeZ) + localZ;


        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);

        // Sample the biome noise at this point to determine the biome distribution, using the world coordinates and the biome's spawn noise settings
        float sampleX = (worldX + biome.spawnNoiseOffset) * biome.spawnNoiseScale;
        float sampleZ = (worldZ + biome.spawnNoiseOffset) * biome.spawnNoiseScale;   
       
/*        float spawnNoise = Mathf.PerlinNoise(
            (worldX + biome.spawnNoiseOffset) * biome.spawnNoiseScale,
            (worldZ + biome.spawnNoiseOffset) * biome.spawnNoiseScale);*/



        // Use a seeded random number generator to select a prefab to spawn, using the world coordinates as the seed to ensure consistent spawning across runs
        int hash = worldX.GetHashCode() ^ worldZ.GetHashCode();
        System.Random rng = new System.Random(hash);
        int prefabIndex = rng.Next(0, biome.spawnPrefabs.Length);
      
        
        float spawnNoise = noise.GetNoise(sampleX, sampleZ);

        float noiseValue = Mathf.InverseLerp(-1f, 1f, spawnNoise); // normalize noise

        float probability = noiseValue * biome.spawnRarity;


        if ((float)rng.NextDouble() > probability)
            return;
        

        float clusterNoise = noise.GetNoise(worldX * 0.02f, worldZ * 0.02f);
        if (clusterNoise < 0.2f)
            return;

        GameObject prefab = biome.spawnPrefabs[prefabIndex];
     
      //  float worldY = terrainHeight * settings.sizeZ;
        Vector3 localPosition = new Vector3(localX, terrainHeight, localZ);

        Vector3 worldPosition = chunk.transform.TransformPoint(localPosition);

        var yRot = rng.Next(0, 360);  

        var newRot = Quaternion.Euler(
            prefab.transform.rotation.x, 
            (yRot),
            prefab.transform.rotation.z);

        //Random scale
        float scale = Mathf.Lerp(0.8f, 1.2f, (float)rng.NextDouble());



        GameObject obj = Instantiate(prefab, worldPosition, newRot, chunk.transform);
        obj.transform.localScale *= scale;
    }

    // Main function to spawn assets on a chunk
    public void SpawnAssets(
        Terrain_Chunk chunk,
        Terrain_Generator generator,
        Global_Terrain_Settings settings,
        BiomeBlend[,] biomeMap,
        float[,] heightMap,
        int chunkX, int chunkZ)
    {
        int width = settings.sizeX;
        int height = settings.sizeZ;

        // Loop through the height map at intervals of spawnSpacing to check potential spawn points
        for (int z = 0; z < height; z += spawnSpacing)
        {
            for (int x = 0; x < width; x += spawnSpacing)
            {
                int hash =
                 x.GetHashCode() ^
                 z.GetHashCode() ^
                 chunkX.GetHashCode() ^
                 chunkZ.GetHashCode();

                System.Random rng = new System.Random(hash);

                float offsetX = ((float)rng.NextDouble() - 0.5f) * spawnSpacing * 0.85f;

                float offsetZ = ((float)rng.NextDouble() - 0.5f) * spawnSpacing * 0.85f;

                float sampleX = x + offsetX;
                float sampleZ = z + offsetZ;

                int ix = Mathf.Clamp(Mathf.RoundToInt(sampleX), 1, width - 1);
                int iz = Mathf.Clamp(Mathf.RoundToInt(sampleZ), 1, height - 1);

                float terrainHeight = heightMap[ix, iz];
                if (terrainHeight <= 0)
                    continue;

                float slope = CalculateSlope(heightMap, ix, iz);
                if (slope > maxSlope)
                    continue;

                BiomeBlend blend = biomeMap[ix, iz];
                // Determine which biome is dominant at this point based on the blend value, and use that biome's settings for spawning
                Biome_Settings biome = blend.blendValue < 0.5f ? blend.biomeA : blend.biomeB;

                TrySpawnObject(
                    chunk,
                    biome,
                    terrainHeight,
                    ix,
                    iz,
                    chunkX,
                    chunkZ,
                    settings);
            }
        }
    }

}
