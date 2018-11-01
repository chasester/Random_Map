using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle
{
    public Vector2 v1, v2, v3;
    //indecies are v1->v2->v3
    static Triangle[] CreateTriangles(Vector2[] vetexes, int[] indecies)
    {
        int triangles = indecies.GetLength(0) / 3;
        if (indecies.GetLength(0) != vetexes.GetLength(0) * 3 && triangles !=0 && !float.IsNaN(triangles)) //check div by 0 and > 0
        {
            Debug.Log("indecies arent equal to the number of vertexes");
            return null;
        }
        Triangle[] tris = new Triangle[triangles];
        for (int i = 0; i < triangles; i++ )
        {
            tris[i] = new Triangle(vetexes[indecies[i * 3]], vetexes[indecies[i * 3 + 1]], vetexes[indecies[i * 3 + 2]]);
        }
        return tris;
    }
    Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        this.v1 = v1; this.v2 = v2; this.v3 = v3;
    }
}
