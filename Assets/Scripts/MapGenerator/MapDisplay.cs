using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

    public Renderer TextureRenerer;

    public void DrawColorMap(float[,] displaymap)
    {
        int width = displaymap.GetLength(0);
        int height = displaymap.GetLength(1);

        Color[] colormap = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                colormap[y * width + x] = Color.Lerp(Color.black, Color.white, displaymap[x, y]);
        DrawColorMap(colormap, width, height);
    }
    public void DrawColorMap(Color[,] displaymap)
    {
        int width = displaymap.GetLength(0);
        int height = displaymap.GetLength(1);
        Color[] colormap = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                colormap[y * width + x] = displaymap[x, y];
        DrawColorMap(colormap, width, height);
    }


    public void DrawColorMap(Color[] colormap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);

        texture.SetPixels(colormap);
        texture.Apply();

        TextureRenerer.sharedMaterial.mainTexture = texture;
        TextureRenerer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
    public void DrawColorMap(Texture2D tex)
    {
        TextureRenerer.sharedMaterial.mainTexture = tex;
        TextureRenerer.transform.localScale = new Vector3(tex.width, 1, tex.height);
    }
}
