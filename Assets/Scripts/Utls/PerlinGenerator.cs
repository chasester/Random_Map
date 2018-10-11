using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PerlinGenerator
{
    public static float[,] GenerateNoise(Rect rect, float scale, uint seed = 0) { return GenerateNoise(new Vector2Int((int)rect.width, (int)rect.height), new Vector2(rect.x, rect.y), scale, seed); }
    public static float[,] GenerateNoise(Vector2Int bounds, Vector2 offset, float scale, uint seed = 0)
    {
        float[,] noiseMap = new float[bounds.x, bounds.y];
        if (seed == 0) seed = (uint)Mathf.RoundToInt(Random.Range(1f, 99999f));
        if (scale <= 0) scale = 0.000001f;

        for (int x = 0; x < bounds.x; x++)
            for (int y = 0; y < bounds.y; y++) noiseMap[x, y] = Mathf.PerlinNoise((x / scale) + offset.x, (y / scale) + offset.y);

        return noiseMap;
    }

};