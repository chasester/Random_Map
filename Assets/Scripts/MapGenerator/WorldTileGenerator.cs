using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldTileGenerator : MonoBehaviour
{
    public Tilemap Parent = null;
    public Tile [] Tiles;
    public Vector2Int Bounds = new Vector2Int();
    // Start is called before the first frame update
    void Start()
    {
        generateMap();
    }
    void Awake()
    {
        generateMap();
    }
    public void generateMap()
    {
        if (!Parent || Tiles.Length < 1) return;
        for(int i = 0; i < Bounds.y; i++)
        {
            for(int k = 0; k < Bounds.x; k++)
            {
                Parent.SetTile(new Vector3Int(i, k, 1), Tiles[Random.Range(0, Tiles.Length)]);
            }
        }

    }
   
    // Update is called once per frame
    void Update()
    {
        
    }
}


