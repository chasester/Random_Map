using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public void Awake() //call the map when the game starts.
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        _T_GenerateMap();
    }
    public void _T_GenerateMap()
    {
        int width = 200;
        int height = 200;
        Color[,] displaymap = new Color[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                displaymap[x,y] = Color.Lerp(Color.black, Color.white, Random.RandomRange(0f,1f));

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawColorMap(displaymap);
    }
}
