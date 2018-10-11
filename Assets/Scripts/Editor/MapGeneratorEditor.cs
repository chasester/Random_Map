using UnityEngine;
using UnityEditor;
using System.Diagnostics;

[CustomEditor(typeof(MapGenerator))]

public class MapGeneratorEditor : Editor
{
    Stopwatch sp = new System.Diagnostics.Stopwatch();
    long LastTimeElapsed = 0;

    public override void OnInspectorGUI()
    {

        MapGenerator mapgen = (MapGenerator)target;
        if (DrawDefaultInspector())
        {
            float t = Mathf.Max(mapgen.CenterWeight + mapgen.PerlinWeight, 0.0001f);
            mapgen.CenterWeight /= t; mapgen.PerlinWeight /= t;
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            sp.Start();
            mapgen.GenerateMap();
            sp.Stop();
            LastTimeElapsed = sp.ElapsedMilliseconds; sp.Reset();
        }

        if(GUILayout.Button("Randomize"))
        {
            sp.Start();
            mapgen.Seed    = Random.Range(99999, -99999);
            mapgen.Variant = Random.Range(99999, -99999);
            mapgen.MapSeed = Random.Range(99999, -99999);
            mapgen.GenerateMap();
            sp.Stop();
            LastTimeElapsed = sp.ElapsedMilliseconds; sp.Reset();
        }
        GUILayout.EndHorizontal();
        
       if (LastTimeElapsed >= 1) GUILayout.Label("Time Elaspe: " + LastTimeElapsed / 1000 + " secs");

    }

}
