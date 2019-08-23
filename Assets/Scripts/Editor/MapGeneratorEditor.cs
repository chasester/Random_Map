using UnityEngine;
using UnityEditor;
using System.Diagnostics;

[CustomEditor(typeof(MapGenerator))]


public class MapGeneratorEditor : Editor
{
    Stopwatch sp = new System.Diagnostics.Stopwatch();
    private long LastTimeElapsed = 0;
    private bool startRender = false;
    private void roundVector2ToInt(ref Vector2 vec)
    {
        vec.x = Mathf.RoundToInt(vec.x);
        vec.y = Mathf.RoundToInt(vec.y);
    }
    void GuiLine(int i_height = 1)
    { 
        Rect rect = EditorGUILayout.GetControlRect(false, i_height);

        rect.height = i_height;

        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
    }
    public override void OnInspectorGUI()
    {
        MapGenerator mapgen = (MapGenerator)target;
        startRender = mapgen.IsGenerating();
        mapgen.RenderUpdate();
        if (sp.IsRunning) LastTimeElapsed = sp.ElapsedMilliseconds;

        if (!startRender && sp.IsRunning)
        {
            sp.Stop();
            sp.Reset();
        }

        
        this.roundVector2ToInt(ref mapgen.NumHighPoints);
        this.roundVector2ToInt(ref mapgen.MaxHighPointHeight);
        this.roundVector2ToInt(ref mapgen.MaxHighPointWidth);
        
        //if (DrawDefaultInspector())
        {
            float t = Mathf.Max(mapgen.CenterWeight + mapgen.PerlinWeight, 0.0001f);
            mapgen.CenterWeight /= t; mapgen.PerlinWeight /= t;
        }

        EditorGUILayout.BeginVertical(/*GUILayout.MaxHeight(1000f)*/); //set this max hieght due to bug when editing this, buttons become unclickable until restart
        #region Map Bounds
        EditorGUILayout.LabelField("Map Bounds", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  Map Size (2^n)");
        mapgen.MapScale = EditorGUILayout.IntSlider(mapgen.MapScale, 6, 20);
        #endregion

        #region Random Seeds
        EditorGUILayout.LabelField("Random Seeds", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("  Base Seed");
        mapgen.Seed = EditorGUILayout.IntSlider(mapgen.Seed, -9999, 9999);

        EditorGUILayout.LabelField("  Variant");
        mapgen.Variant = EditorGUILayout.IntSlider(mapgen.Variant, -9999, 9999);

        EditorGUILayout.LabelField("  Map Seed");
        mapgen.MapSeed = EditorGUILayout.IntSlider(mapgen.MapSeed, -9999, 9999);
        #endregion

        #region Map Props
        EditorGUILayout.LabelField("Map Properties:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  Water Height");
        mapgen.WaterHeight = EditorGUILayout.Slider(mapgen.WaterHeight, 0.00001f, 1f);

        EditorGUILayout.LabelField("  Perlin Weight");
        mapgen.PerlinWeight = EditorGUILayout.Slider(mapgen.PerlinWeight, 0f, 1f);

        EditorGUILayout.LabelField("  Center Weight");
        mapgen.CenterWeight = EditorGUILayout.Slider(mapgen.CenterWeight, 0f, 1f);

        EditorGUILayout.LabelField("  Coast Clean Irrations");
        mapgen.CoastCleanIrrations = EditorGUILayout.IntSlider(mapgen.CoastCleanIrrations, 0, 50);

        EditorGUILayout.LabelField("  Cell Percentage");
        mapgen.CellPercentage = EditorGUILayout.Slider(mapgen.CellPercentage, 0.0000001f, 50f);

        EditorGUILayout.LabelField("  Min Distance");
        mapgen.MinDistance = EditorGUILayout.Slider(mapgen.MinDistance, 0.001f, 15f);

        EditorGUILayout.LabelField("  Coastal Roughness");
        mapgen.CoastalRoughness = EditorGUILayout.Slider(mapgen.CoastalRoughness, 0.000001f, 5f);

        EditorGUILayout.LabelField("  Normalization Cyles");
        mapgen.NormalizationCyles = EditorGUILayout.IntSlider(mapgen.NormalizationCyles, -1, 5);

        EditorGUILayout.LabelField("  Lake Moisture");
        mapgen.LakeMoisture = EditorGUILayout.Slider(mapgen.LakeMoisture, 0f, 1f);

        EditorGUILayout.LabelField("  Moisture Threshold");
        mapgen.MoistureThreshold = EditorGUILayout.Slider(mapgen.MoistureThreshold, 0.0000001f, 0.2f);

        EditorGUILayout.LabelField("  Average Rain Fall");
        mapgen.RainFallAverage = EditorGUILayout.Slider(mapgen.RainFallAverage, 0f, 10f);
        #endregion

        #region Voronoi Props
        EditorGUILayout.LabelField("Voronoi Properties:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("  LLOYD Irrations");
        mapgen.LLOYD_Irrations = EditorGUILayout.IntSlider(mapgen.LLOYD_Irrations, 0, 10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("  Patel Irrations");
        mapgen.PATEL_Irrations = EditorGUILayout.IntSlider(mapgen.PATEL_Irrations, 0, 10);
        EditorGUILayout.EndHorizontal();
        #endregion

        #region High Points
        EditorGUILayout.LabelField("High Points Properties:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  Number High Points:", new Vector2Int((int)(mapgen.NumHighPoints.x), (int)(mapgen.NumHighPoints.y)).ToString());
        EditorGUILayout.MinMaxSlider(ref mapgen.NumHighPoints.x, ref mapgen.NumHighPoints.y, 0, 200);


        EditorGUILayout.LabelField("  High Point Height:", new Vector2Int((int)(mapgen.MaxHighPointHeight.x), (int)(mapgen.MaxHighPointHeight.y)).ToString());
        EditorGUILayout.MinMaxSlider(ref mapgen.MaxHighPointHeight.x, ref mapgen.MaxHighPointHeight.y, 0, 3000);

        EditorGUILayout.LabelField("  High Point Width:", new Vector2Int((int)(mapgen.MaxHighPointWidth.x), (int)(mapgen.MaxHighPointWidth.y)).ToString());
        EditorGUILayout.MinMaxSlider(ref mapgen.MaxHighPointWidth.x, ref mapgen.MaxHighPointWidth.y, 0, 3000);

        EditorGUILayout.LabelField("  High Point Elevation:", mapgen.MaxElevation.ToString());
        EditorGUILayout.MinMaxSlider(ref mapgen.MaxElevation.x, ref mapgen.MaxElevation.y, 0.0f, 3.0f);

        EditorGUILayout.LabelField("  Elevation Drop Off:", mapgen.ElevationDropOff.ToString());
        EditorGUILayout.MinMaxSlider(ref mapgen.ElevationDropOff.x, ref mapgen.ElevationDropOff.y, 0.0f, 3.0f);
        #endregion

        #region Render Props
        EditorGUILayout.LabelField("Renderer Properties:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        mapgen.ShowCells = EditorGUILayout.Toggle("  Show Cells", mapgen.ShowCells);
        mapgen.ShowEdges = EditorGUILayout.Toggle("Show Edges", mapgen.ShowEdges);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        mapgen.ShowCenters = EditorGUILayout.Toggle("  Show Centers", mapgen.ShowCenters);
        mapgen.ShowCorners = EditorGUILayout.Toggle("Show Corners", mapgen.ShowCorners);
        EditorGUILayout.EndHorizontal();
        #endregion

        #region User Input
        EditorGUILayout.Space();
        this.GuiLine();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (!mapgen.IsGenerating())
        {
            if (GUILayout.Button("Generate"))
            {
                sp.Start();
                mapgen.GenerateMapToRenderTarget();
            }

            if (GUILayout.Button("Randomize"))
            {
                sp.Start();
                this.startRender = true;
                mapgen.Seed = Random.Range(99999, -99999);
                mapgen.Variant = Random.Range(99999, -99999);
                mapgen.MapSeed = Random.Range(99999, -99999);
                mapgen.GenerateMapToRenderTarget();
            }
        } else
        {
            if(GUILayout.Button("Abort"))
            {
                mapgen.GenerateMapToRenderTarget();
            }
        }
        EditorGUILayout.EndHorizontal();
        #endregion

        if(mapgen.Status != "") GUILayout.Label(mapgen.Status);
        if (LastTimeElapsed >= 100) GUILayout.Label("Time Elaspe: " + LastTimeElapsed / 1000 + " secs");
        EditorGUILayout.EndVertical();

    }

}
