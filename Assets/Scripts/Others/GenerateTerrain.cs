using UnityEngine;

public class GenerateTerrain : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public float depth = 20f;
    public float scale = 20f;
    public float centerRadius = 30f;
    public float centerDepth = -2f;
    Terrain terrain;
    public bool locked = false;
    void Start()
    {
        terrain = GetComponent<Terrain>();
    }
    void Update()
    {
        if (!locked)
            terrain.terrainData = GenerateTerrainData(terrain.terrainData);
    }
    TerrainData GenerateTerrainData(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        Vector2 center = new Vector2(width / 2, height / 2);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                // 计算距离中心的距离
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center);

                // 如果在平原范围内，则设置高度为平原高度，否则为山地高度
                if (distanceToCenter < centerRadius)
                {
                    heights[x, y] = centerDepth;    
                }
                else
                {
                    float normalizedDistance = Mathf.Clamp01((distanceToCenter - centerRadius) / (width / 2 - centerRadius));
                    float mountainHeight = depth * Mathf.PerlinNoise(xCoord * 3f, yCoord * 3f);
                    heights[x, y] = (centerDepth + mountainHeight * normalizedDistance) * distanceToCenter / 256f;
                }
            }
        }
        return heights;
    }
}
