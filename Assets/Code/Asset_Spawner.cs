using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

// Class for asset spawning, on each terrain chunk //
public class Asset_Spawner : MonoBehaviour
{
    private int spawnSpacing = 1; // Spacing between potential spawn points

    private float maxSlope = 1f; // Maximum slope allowed for spawning, calculated as the maximum height difference between a point and its 4 cardinal neighbors

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

        // Use Perlin noise to determine whether to spawn an object at this point, using the world coordinates and the biome's spawn noise settings
        float spawnNoise = Mathf.PerlinNoise(
            (worldX + biome.spawnNoiseOffset) * biome.spawnNoiseScale,
            (worldZ + biome.spawnNoiseOffset) * biome.spawnNoiseScale);

        if (spawnNoise < biome.spawnThreshold)
            return;

        // Use a seeded random number generator to select a prefab to spawn, using the world coordinates as the seed to ensure consistent spawning across runs
        int hash = worldX.GetHashCode() ^ worldZ.GetHashCode();
        System.Random rng = new System.Random(hash);
        int prefabIndex = rng.Next(0, biome.spawnPrefabs.Length);

        GameObject prefab = biome.spawnPrefabs[prefabIndex];

        Vector3 localPosition = new Vector3(localX, terrainHeight, localZ);

        Vector3 worldPosition = chunk.transform.TransformPoint(localPosition);

        Instantiate(prefab, worldPosition, prefab.transform.rotation, chunk.transform);
    }

    // Main function to spawn assets on a chunk
    public void SpawnAssets(
        Terrain_Chunk chunk,
        Global_Terrain_Settings settings,
        BiomeBlend[,] biomeMap,
        float[,] heightMap,
        int chunkX, int chunkZ)
    {
        int width = settings.sizeX;
        int height = settings.sizeZ;

        // Loop through the height map at intervals of spawnSpacing to check potential spawn points
        for (int z = 1; z < height; z += spawnSpacing)
        {
            for (int x = 1; x < width; x += spawnSpacing)
            {
                float terrainHeight = heightMap[x, z];

                if (terrainHeight <= 0)
                    continue;

                float slope = CalculateSlope(heightMap, x, z);

                if (slope > maxSlope)
                    continue;

                BiomeBlend blend = biomeMap[x, z];

                Biome_Settings biome = blend.blendValue < 0.5f
                    ? blend.biomeA
                    : blend.biomeB;

                TrySpawnObject(
                    chunk,
                    biome,
                    terrainHeight,
                    x,
                    z,
                    chunkX,
                    chunkZ,
                    settings);
            }
        }
    }

}
