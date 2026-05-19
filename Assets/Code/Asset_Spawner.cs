using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class Asset_Spawner : MonoBehaviour
{

    [Header("Spawn Settings")]
    [SerializeField] private int spawnSpacing = 4;

    [SerializeField]
    private float maxSlope = 2f;


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

        float worldX = (chunkX * settings.sizeX) + localX;
        float worldZ = (chunkZ * settings.sizeZ) + localZ;

        float spawnNoise = Mathf.PerlinNoise(
            (worldX + biome.spawnNoiseOffset) * biome.spawnNoiseScale,
            (worldZ + biome.spawnNoiseOffset) * biome.spawnNoiseScale);

        if (spawnNoise < biome.spawnThreshold)
            return;

        int hash = worldX.GetHashCode() ^ worldZ.GetHashCode();

        System.Random rng = new System.Random(hash);

        int prefabIndex = rng.Next(0, biome.spawnPrefabs.Length);

        GameObject prefab = biome.spawnPrefabs[prefabIndex];

        Vector3 localPosition = new Vector3(
            localX,
            terrainHeight,
            localZ);

        Vector3 worldPosition = chunk.transform.TransformPoint(localPosition);

        Instantiate(
            prefab,
            worldPosition,
            prefab.transform.rotation,
            chunk.transform);
    }


    public void SpawnAssets(
        Terrain_Chunk chunk,
        Global_Terrain_Settings settings,
        BiomeBlend[,] biomeMap,
        float[,] heightMap,
        int chunkX, int chunkZ)
    {
        int width = settings.sizeX;
        int height = settings.sizeZ;

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
