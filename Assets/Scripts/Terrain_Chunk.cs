using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class Terrain_Chunk : MonoBehaviour
{
    private int chunkX;
    private int chunkZ;

    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;


    private float GenerateNoiseHeight(float x, float z, Vector2[] offsetSeed)
    {
        float frequency = baseFrequency;
        float persistence = basePersistence;
        float amplitude = baseAmplitude;

        float noiseValue = 0f;
        float heightValue = 0;

        //loop through each octave and calculate the noise value
        for (int i = 0; i < octaves; i++)
        {
            float sampleZ = z / scale * frequency + offsetSeed[i].y;
            float sampleX = x / scale * frequency + offsetSeed[i].x;


            noiseValue = (Mathf.PerlinNoise(sampleZ, sampleX)) * 2 - 1;
            heightValue += heightCurve.Evaluate(noiseValue) * amplitude;

            amplitude *= persistence; // Decrease amplitude for next octave
            frequency *= lacunarity; // Increase frequency for next octave

        }
        return heightValue;
    }


    private void CreateMeshShape(Vector2[] offsetSeed)
    {
        Vector2[] octaveOffsets = offsetSeed;


        vertices = new Vector3[(terrainX + 1) * (terrainZ + 1)];

        for (int i = 0, z = 0; z <= terrainZ; z++)
        {
            for (int x = 0; x <= terrainX; x++)
            {
                // Assign and set height of each vertices
                float noiseHeight = GenerateNoiseHeight(z, x, octaveOffsets);
                if (noiseHeight <= lowerThreshold)
                    noiseHeight = 0;

                SetMinMaxHeights(noiseHeight);
                vertices[i] = new Vector3(x, noiseHeight, z);
                i++;
            }
        }
    }

}
