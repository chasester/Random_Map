using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]

public class MapGeneratorEditor : Editor
{
    //private static int changed = -1;
    public override void OnInspectorGUI()
    {
        MapGenerator mapgen = (MapGenerator)target;
        if (DrawDefaultInspector() && mapgen.Preview) mapgen.GenerateMap();
        if (GUILayout.Button("Generate"))
            mapgen.GenerateMap();
    }

}
