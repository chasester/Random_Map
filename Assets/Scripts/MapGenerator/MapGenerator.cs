using System.Collections.Generic;
using UnityEngine;


public class MapGenerator : MonoBehaviour
{
    [Header("Random Seeds")]
    [Range(-9999, 9999)]
    public int Seed = 100;
    [Range(-9999, 9999)]
    public int Variant = 100;
    [Range(-9999, 9999)]
    public int MapSeed;
    [Header("Map Properties")]
    public Rect Bounds = new Rect(0, 0, 200, 200);
    [Range(0.001f,1.0f)]
    public float WaterHeight = 0.2f;
    [Range(0.0f, 1.0f)]
    public float PerlinWeight;
    [Range(0.0f, 1.0f)]
    public float CenterWeight;
    [Range(1, 50)]
    public int CoastCleanIrrations = 10;
    [Range(0.01f, 50.0f)]
    public float CellPercentage = 3f; //percent of the map that cells contain on average. The closer to 100 The smaller the cells and the larger the detail (and the longer the time to process)
    [Range(0.0001f, 15f)]
    public float MinDistance = 5f;
    [Range(0.0001f, 5f)]
    public float CoastalRoughness = 1.0f;
    [Range(-1, 5)]
    public int NormalizationCyles = 3;
    [Range(0.0f, 1.0f)]
    public float LakeMoisture = 0.8f;
    [Range(0.00001f, 0.2f)]
    public float MoistureThreshold = 0.01f;
    [Range(0.0f, 10.0f)]
    public float RainFallAverage = 4f;


    [Range(0, 10)]
    [Header("Voronoi Properties")]
    public int LLOYD_Irrations = 2;
    [Range(0, 10)]
    public int PATEL_Irrations = 4;

    [Header("HighPoint Properties")]
    public Vector2Int NumHighPoints = new Vector2Int(1,3);
    public Vector2Int MaxHighPointHeight = new Vector2Int(500, 2500);
    public Vector2Int MaxHighPointWidth = new Vector2Int(500, 2500);
    public Vector2 MaxElevation = new Vector2(0.6f, 1.0f);
    public Vector2 ElevationDropOff = new Vector2(0.8f, 1.8f);

    [Header("Renderer Properties")]
    //public bool Preview = true;
    public bool ShowCells = true;
    public bool ShowEdges = true;
    public bool ShowCenters = true;
    public bool ShowCorners = false;
    
    public void Awake() //call the map when the game starts.
    {
        GenerateMap();
    }
    public void GenerateMap()
    {
        
        List<Cell> cells = new List<Cell>(); List<Edge> edges = new List<Edge>(); List<Corner> corners = new List<Corner>();
        //set up random so we can save the seed back so we always can get back to were we where
        Mathf.Clamp(CellPercentage, 0.001f, 0.9f); //cap this at 90% cuz otherwise there wont be any map
        Random.InitState(Variant);
        List<Vector2> points = generaterandompoints((uint)Mathf.RoundToInt((CellPercentage * 0.001f) * (Bounds.width * Bounds.height)), Bounds); //maybe add a random here to make it a lil more crazy

        Random.InitState(Seed);
        improverandompoints(ref points, LLOYD_Irrations, PATEL_Irrations, MinDistance, Bounds, ref cells, ref edges, ref corners);

        //Divide Land and water, then define ocean from lake, then assign coast, then assign elevation to all land modules (looking at neighbors to level out posiblities)
        //List<Corner> landcorners, watercorners;
        assigncornerelevation(ref cells, ref corners, Bounds, Variant);

        //renderer;
        Color[,] displaymap = drawvoronoi(cells, edges, corners, Bounds);
        //do disown then clear
        points.Clear(); cells.Clear(); edges.Clear(); corners.Clear(); System.GC.Collect();
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawColorMap(displaymap);
    }

    //private Color[] readymaptexture(List<Texture2D> mapTextures, Rect bounds, int mapid = -1)
    //{
    //    Random.State prerandom = Random.state;
    //    Random.InitState(MapSeed);
    //    if (mapid < 0 || mapid >= mapTextures.Count) mapid = (int)(Random.Range(0, mapTextures.Count)); //let the user force a map if not we will randomly pick one

    //    Random.state = prerandom;

    //    Texture2D maptex = new Texture2D(mapTextures[mapid].width, mapTextures[mapid].height);
    //    maptex.SetPixels(mapTextures[mapid].GetPixels());
    //    maptex.Apply();
    //    //maptex.Resize((int)bounds.width, (int)bounds.height); resizing will nullify all pixles
    //    return maptex.GetPixels();
    //}

    private void assigncornerelevation(ref List<Cell> cells, ref List<Corner> corners, Rect bounds, int perlinseed)
    {
        Corner q; Vector2 center = bounds.center;
        List<Corner> LandCorners = new List<Corner>(),//list of all the land
            LakeCorners = new List<Corner>(), //list of all the lowest land we will later make this lake (in phase 1 this is used to help build the ocean then all the remaining points are put back into the land corners) in Phase water sheding we wiull use this to make starting points for the lakes and then build rivers out of these resivors
            OceanCorners = new List<Corner>(), //list of all the ocean corners
            CoastCorners = new List<Corner>(), //list of all the coastal corners, these corners are also part of the land corners. This will help build our islands and help create our cool coast line using the coastal cleaning algorithm
            HighCorners = new List<Corner>(), //list of all the highest points on the map (this is where water shading will happen from.
            neighbors; //used a few times as a place holder
        Random.State prerandom = Random.state;
        Vector2 pos; /*float maxdist = Vector2.Distance(center, bounds.min);*/
        float width = bounds.width;
        float[,] perlinheightmap = PerlinGenerator.GenerateNoise(bounds, 0.1f, (uint)perlinseed);
        float[,] perlinmoisturemap = PerlinGenerator.GenerateNoise(bounds, 0.1f, (uint)perlinseed + 1);

        float t = Mathf.Max(CenterWeight + PerlinWeight, 0.0001f); //so we dont divide by 0;
        CenterWeight /= t; PerlinWeight /= t;

        #region Phase 1: Land Building
        {
            List<Highpoint> HighPoints = new List<Highpoint>();
            {
                int num = (int)Random.Range(Mathf.Min(NumHighPoints.x, NumHighPoints.y), Mathf.Max(NumHighPoints.x, NumHighPoints.y));
                //Rect b = new Rect(100, 100, 800, 800);
                Rect b = new Rect(bounds.xMin + bounds.width * 0.08f,
                                  bounds.yMin + bounds.height * 0.08f, //half of the average
                                  bounds.width - bounds.width * 0.16f,
                                  bounds.height - bounds.height * 0.16f
                                  );

                List<Cell> c = new List<Cell>(); List<Edge> e = new List<Edge>(); List<Corner> cr = new List<Corner>();
                List<Vector2> pt = generaterandompoints((uint)num * 10, b);
                improverandompoints(ref pt, 5, 6, MinDistance, b, ref c, ref e, ref cr);
                for (int i = 0; i < num; i++)
                {
                    int index = (int)Random.Range(0, c.Count - 1);
                    Cell cl = c[index];
                    c.RemoveAt(index); //once we grab one we want to remove it
                    float w = Random.Range((MaxHighPointHeight.x), (MaxHighPointHeight.y)),
                          z = Random.Range((MaxHighPointWidth.x), (MaxHighPointWidth.y)),
                          x = cl.getpos().x,
                          y = cl.getpos().y,
                          v = Random.Range(MaxElevation.x, MaxElevation.y),
                          u = Random.Range(ElevationDropOff.x, ElevationDropOff.y);
                    HighPoints.Add(new Highpoint(x, y, z, w, v, u));
                }
                c.Clear(); e.Clear(); cr.Clear(); pt.Clear(); //double down on making sure we free the memory
            }


            for (int i = 0; i < corners.Count; i++)
            {

                q = corners[i];
                pos = q.getposition();
                if (q.HasTerrianValue()) continue; //basically if we have already processed this value then we want to skip it. this is mainly for corners;
                if (q.IsBoundary(bounds))
                {
                    //handel edge of map;
                    neighbors = q.GetNeighbors();
                    q.SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_OCEAN);
                    OceanCorners.Add(q);
                    continue; //when we are done we wont need to do anything else here
                }

                float elevation = (Random.Range(0.0f, 1.0f) * PerlinWeight) + (calculatefromhighpoints(HighPoints, q.getposition()) * CenterWeight);
                if (elevation < WaterHeight)
                {
                    neighbors = q.GetNeighbors();
                    int chance = 0;
                    for (int j = 0; j < neighbors.Count; j++) if (neighbors[j].terrian == TerrianType.TERRIAN_LAND) chance += 3; else if (neighbors[j].terrian == TerrianType.TERRIAN_LAKE) chance--; else if (neighbors[j].terrian == TerrianType.TERRIAN_OCEAN) chance++; //add a chance that land will clump a lil

                    //this will be water
                    if (Random.Range(0, 100 * chance) * 0.01f < 1.5f) //maybe bring this back 
                    {
                        q.SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_LAKE); //we may change this to give it a lil evelation varience but for now lets leave it alone
                        LakeCorners.Add(q); //we dont know if this is a lake or not but we will determind that after we calculate all the corners
                        continue; //we are done now 
                    }
                }
                else if (elevation > 0.9f)
                {
                    //q.SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_LAKE);
                    //continue;
                }
                //so this must be land so lets calculate some pregenerated moisture ideals
                float moisture = 0.0f;//(perlinmoisturemap[(int)pos.x, (int)pos.y] * 0.2f) + (Random.Range(0.0f, tex[(int)(width * pos.y + pos.x)].r)) * 0.5f + ((1.0f - (Mathf.Abs(pos.y - center.y) / bounds.width)) * 0.3f);
                q.SetTerrianProperties(elevation, moisture, TerrianType.TERRIAN_LAND);
                LandCorners.Add(q);

            }
        }
        #endregion
        #region Phase 2: Coast Cleanup
        {
            //phase 2: we need to now find our map edge corners (conviently placed in the oceans terrain) then cycle through there neighbors till we get to corners marked as land. We will change each of these corners from lake to ocean and move them from the Lake list to the ocean list.
            //this will be similar to a b* algorithm were we will follow a branch til we have reach a dead end then we will go to the next branch
            List<Corner> que = new List<Corner>(OceanCorners.Count); //see up this cuz we are gonna copy it in a sec
            que.AddRange(OceanCorners);
            CollectOceanCorners(ref que, ref OceanCorners, ref LakeCorners, ref CoastCorners);
            for (int i = 0; i < CoastCleanIrrations; i++)
            {
                que = CleanCoastCorners(ref OceanCorners, ref CoastCorners);
                if (que.Count == 0) { break; }
                CollectOceanCorners(ref que, ref OceanCorners, ref LakeCorners, ref CoastCorners);
            }
            //remove the lake corners and calculate them later during water sheding
            for (int i = 0; i < LakeCorners.Count; i++) LakeCorners[i].terrian = TerrianType.TERRIAN_LAND;

            LandCorners.AddRange(LakeCorners);
            LakeCorners.Clear(); //well use thes for water sheding
        }
        #endregion
        #region Phase 3: Normalization
        {
            //Phase 3: We want to run a normalization
            for (int i = 0; i < cells.Count; i++) //first cycle assign the average for all the corners to the center(aka the cell)
            {
                Cell c = cells[i];
                List<Corner> cr = c.GetCorners();
                float eavg = 0, etotal = 0;
                bool isocean = true;
                bool hasocean = false;
                bool hascoast = false;
                for (int j = 0; j < cr.Count; j++)
                {
                    if (cr[j].terrian == TerrianType.TERRIAN_OCEAN) {hasocean = true; continue; }
                    if (cr[j].terrian != TerrianType.TERRIAN_COAST) isocean = false;
                    if (!hascoast) hascoast = cr[j].terrian == TerrianType.TERRIAN_COAST; //does it contain a coast

                    eavg += cr[j].elevation;//*Random.Range(0.6f, 1.2f)*(cr[j].terrian == TerrianType.TERRIAN_COAST ? 0.5f:1.0f);
                    etotal += 1.0f;//*Random.Range(0.8f, 1.2f);
                }
                if (hasocean && hascoast) c.coast = true;
                if (isocean && !hascoast)
                {
                    c.ocean = true;//is 100% ocean;
                    c.elevation = 1.0f;
                    c.moisture = 1.0f;
                }
                eavg = eavg / etotal;
                c.elevation = eavg;
            }
            #region Old method
            //cycles 2: take the cell average from the closest cell and from the farther and farther cells and weight them to assign normalize elevation for all corners
            //for (int i = 0; i < cells.Count; i++)
            //{
            //    if (cells[i].ocean) continue; //we dont need to do anything if this is coast
            //    Cell c = cells[i];
            //    List<Corner> cr = c.GetCorners();
            //    float eavg = 0.0f, etotal = 0.0f;
            //    int irrations = 0;

            //    for (int j = 1; j < irrations + 1; j++) { float p = (1 / (Mathf.Pow(2, (j)))); eavg += getcellelevationaverage(j, c) * p; etotal += p; }
            //    if (etotal < 1) { eavg = c.elevation; }
            //    else eavg /= etotal;

            //    for (int j = 0; j < cr.Count; j++)
            //    {
            //        if (cr[j].terrian == TerrianType.TERRIAN_OCEAN) continue;
            //        float ce = cr[j].elevation * 0.3f + c.elevation * 0.3f + eavg * 0.4f;
            //        if (ce > cr[j].elevation) cr[j].elevation = ce;
            //        //else cr[j].elevation = 0.0f;


            //    }
            //}
            #endregion
            if (NormalizationCyles > -1) for (int i = 0; i < corners.Count; i++)
                {

                    if (corners[i].terrian == TerrianType.TERRIAN_OCEAN) continue;
                    List<Cell> cn = corners[i].GetCellNeighbors();
                    float elvg = 0.000f, elnum = 0.0f;
                    for (int j = 0; j < cn.Count; j++)
                    {
                        if (cn[j].ocean) continue;
                        for (int k = 1; k < NormalizationCyles + 1; k++) { float p = (1 / (Mathf.Pow(2, (k)))); elvg += getcellelevationaverage(k, cn[j]) * p; elnum += p; }
                        elvg += cn[j].elevation; elnum++;
                    }
                    elvg += corners[i].elevation; elnum++;
                    //if (elnum == 0) continue; the above makes this worthless;
                    elvg /= elnum;
                    if (elvg > corners[i].elevation) corners[i].elevation = elvg;
                }
        }
        #endregion

        //for (int i = 0; i < cells.Count; i++) if (i != cells[i].id) Debug.Log("id: " + cells[i].id + " does not match Cell " + i);
        //for (int i =0; i < corners.Count; i++)if(i != corners[i].id) Debug.Log("id: " + corners[i].id + " does not match Corner " + i);
        #region Phase 5: Tempature Rough Run
        { //this will be the way we determind rainfall plus a random factor. This is due to Rain being less posible in lower tempature climates
            //We will probably also have rainfall influenced by normalized elevation compared to point elevation. Or posibly creating a wind map.
            //establish a new tempature system where 0 is -20 degrees f (-28c) and 1.0f is 120 degrees f (48c)
            for(int i =0; i < cells.Count; i++)
            {
                cells[i].temperature = cells[i].elevation - 1.0f;
            }
        }
        #endregion
        #region Phase 6: Water Sheding
        {
            //Phase 6: Do water sheding starting at the highest points and Dropping the water down letting it go down the slops. We want to favor the side of the mountain that the wind would blow against (according to the sphereical rotation)
            //cyle 01: drop water from Highpoints (if no high points then do some guess work to make high points [well have to do this at a later point])
            //List<int> Waypoints = new List<int>(corners.Count);
            //for (int i = 0; i < corners.Count; i++) Waypoints.Add(-1);

            Corner c, cn; List<Corner> neigh;
            //List<Corner> waycorners = new List<Corner>();
            List<Cell> cneigh;
            Cell wc;
            List<Cell> LakeCell = new List<Cell>();
            float rainpercent; // random rainfall for this region

            for (int i = 0; i < corners.Count; i++)
            {
                rainpercent = Random.Range(0, 100 * RainFallAverage) / 100;
                wc = null;
                c = corners[i];
                if (c.terrian != TerrianType.TERRIAN_LAND) continue;
                for (int k = 0; k < 1000; k++)
                { //basically a while loop with a option to die
                    //if (c.terrian == TerrianType.TERRIAN_OCEAN) { cneigh = c.GetCellNeighbors(); for (int j = 0; j < cneigh.Count; j++) if (cneigh[j].ocean || cneigh[j].coast) { wc = cneigh[j]; if (cneigh[j].ocean) break; } break; }
                    neigh = c.GetNeighbors();
                    cn = neigh[0];
                    if (Random.Range(0, 100) < 100) c.moisture += MoistureThreshold * rainpercent;
                    //if (Waypoints[cn.id] != -1) { wc = cells[Waypoints[cn.id]]; break; }

                    for (int j = 1; j < neigh.Count; j++) //find the lowest elevation
                        //if (Waypoints[neigh[j].id] != -1) { wc = cells[Waypoints[neigh[j].id]]; break; }
                        if (cn.elevation > neigh[j].elevation) cn = neigh[j];

                    if (cn.elevation > c.elevation) //if we are in a valley or if we have found the ocean;
                    {
                        //find lowest cell
                        cneigh = c.GetCellNeighbors();
                        wc = cneigh[0];
                        for (int j = 1; j < cneigh.Count; j++)
                            if (wc.elevation > cneigh[j].elevation) wc = cneigh[j];
                        break;
                    }

                    //waycorners.Add(c);
                    c = cn;
                }

                if (wc == null) { continue; }
                if (!wc.ocean) { LakeCell.Add(wc); wc.moisture += MoistureThreshold; }
            }
            //LandCorners.Clear();
            for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i].terrian != TerrianType.TERRIAN_LAND) continue;
                if (corners[i].moisture < LakeMoisture) continue;
                LakeCorners.Add(corners[i]);
                corners[i].terrian = TerrianType.TERRIAN_LAKE;
            }

            #region Old shit
            //corners = LandCorners;
            //for (int i = 0; i < LakeCell.Count; i++) if (LakeCell[i].moisture < LakeMoisture) LakeCell.RemoveAt(i); else LakeCell[i].ocean = true;
            //cycle 02: check all corners to see if they recieved moisture. If not drop from each of these After wards.(most likely these will be islands so all the water should filter to the ocean.)
            //for(int i = 0; i < HighCorners.Count; i++)
            //{
            //    if (Random.Range(0, 10) < 5.0f) continue;
            //    c = HighCorners[i];
            //    neigh = c.GetNeighbors();
            //    cn = neigh[0];
            //    for (int j = 1; j < neigh.Count; j++)
            //    {
            //        if (c.terrian == TerrianType.TERRIAN_OCEAN) break;
            //        if (cn.elevation < neigh[j].elevation)
            //            cn = neigh[j];
            //        if (cn.elevation >= c.elevation)
            //            break;

            //        c = cn;

            //    }
            //}
            #endregion 

        }
        #endregion
        #region Phase 7: Normalize Moisture And create outlets of lakes to Ocean

        #endregion  

        //Phase 8: 
    }

    private float getcellelevationaverage(int irrations, Cell c)
    {
        float el = 0.0f, elnum = 0.0f;
        List<Cell> neigh = c.GetNeighbors();
        irrations--;
        for (int i = 0; i < neigh.Count; i++)
        {
            if (c.ocean) continue;
            el += neigh[i].elevation;
            elnum += 1;
            if (irrations > 0) { float a = getcellelevationaverage(irrations, neigh[i]); if (!float.IsNaN(a)) el += a; elnum += 1; }
        }
        el /= elnum;
        return el;
    }

    private float calculatefromhighpoints(List<Highpoint> highPoints, Vector2 point)
    {
        int index = -1;
        float leastdist = float.MaxValue;
        float dist;
        for(int i = 0; i < highPoints.Count;i++ )
        {
            Vector2 pos = highPoints[i].pos;
            dist = Vector2.Distance(pos, point);
            if ( dist < leastdist && Random.Range(0, 5) >= CoastalRoughness)
            {
                leastdist = dist;  
                index = i;
            }
        }
        if(index < 0) return 0.0f;
        float avg = 0.0f;
        for (int i = 0; i < Random.Range(1, 1); i++)
        {
            Vector2 df = new Vector2((highPoints[index].size.x * Random.Range(0.7f, 1.4f)), (highPoints[index].size.y * Random.Range(0.7f, 1.4f)));
            Vector2 ds = (point - highPoints[index].pos), norm = ds; norm.Normalize(); norm *= df;
            float mdist = Vector2.Distance(norm, ds);
            avg = (avg + (1 - Mathf.Clamp01(Mathf.Pow(Mathf.Abs(leastdist / mdist), highPoints[index].dispation))) * highPoints[index].elevation);
            if (i > 0) avg /= 2;
        }
        return avg;
        //leastdist = ((point.x - highPoints[index].x) -radx + (point.y - highPoints[index].y) -rady)/2;
        //return Mathf.Lerp(1.0f, 0.0f, leastdist/(highPoints[index].z + highPoints[index].w)/2);
    }

    private List<Corner> CleanCoastCorners(ref List<Corner> OceanCorners, ref List<Corner> CoastCorners)
    {
        List<Corner> que = new List<Corner>(), neighbors;
        Corner q;
        for (int i = 0; i < CoastCorners.Count; i++) // get the ones that are near the ocean and add them to the ocen list;
        {
            int chance = 0;
            q = CoastCorners[i];
            neighbors = q.GetNeighbors();
            for (int j = 0; j < neighbors.Count; j++) if (neighbors[j].terrian == TerrianType.TERRIAN_LAKE) chance++;
            if (chance == 0) continue; 
            q.terrian = TerrianType.TERRIAN_OCEAN;
            que.Add(q); //add it to the list to be re calculated
            OceanCorners.Add(q); //add it to the ocean tiles
            //add it to a new que so we can recheck if we want to do this. maybe make it random weither these become fresh or salt water
            CoastCorners.RemoveAt(i);
        }
        return que;
    }
    private void CollectOceanCorners(ref List<Corner> que, ref List<Corner> OceanCorners, ref List<Corner> LakeCorners, ref List<Corner> CoastCorners)
    {
        List<Corner> neighbors;
        Corner q, s;
        while (que.Count > 0) //we are just gonna set up a simple que algorithm each cycle we will remove an item from the que until all items are gone and we have our map
        {
            q = que[0];
            //if (q.isEdge(bounds)) { que.RemoveAt(0); continue; } //above we already added all of the map edges to be part of the ocean group so we dont need to recycle these ones
            neighbors = q.GetNeighbors();
            for (int i = 0; i < neighbors.Count; i++)
            {
                s = neighbors[i];
                if (s.terrian == TerrianType.TERRIAN_OCEAN) continue;
                if (s.terrian == TerrianType.TERRIAN_LAND) { CoastCorners.Add(s); s.terrian = TerrianType.TERRIAN_COAST; continue; }
                s.terrian = TerrianType.TERRIAN_OCEAN;

                que.Add(s); //we add this to the que so that we can check its neighbors
                LakeCorners.Remove(s); //we want to make sure to keep the lake list clean so it only contains the lake elements. This item is an ocean element os we dotn want it here
                OceanCorners.Add(s);
            }
            que.RemoveAt(0); //once we checked all our neighbors then we are done and we can move on
        }
        que.Clear();//should be empty but just in case
    }
    private List<Vector2> generaterandompoints(uint NumPoints, Rect bounds)
    {
        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < NumPoints; i++)
            points.Add(new Vector2(Mathf.Round(Random.Range(bounds.xMin + bounds.size.x * 0.01f , bounds.xMax - bounds.size.x * 0.01f)),
                Mathf.Round(Random.Range(bounds.yMin + bounds.size.y * 0.01f, bounds.yMax - bounds.size.y * 0.01f)))); // we add a 1% boarder + 10 units so that we dont get any points to close to the edge
        return points;
    }
    private void drawline(ref Color[,] displaymap, Vector2 to, Vector2 from, Color c)
    {
        //float x0 = Mathf.Round(from.x);
        //float y0 = Mathf.Round(from.y);
        //float x1 = Mathf.Round(to.x);
        //float y1 = Mathf.Round(to.y);
        //float dx = Mathf.Abs(x1 - x0);
        //float dy = Mathf.Abs(y1 - y0);
        //int sx = x0 < x1 ? 1 : -1;
        //int sy = y0 < y1 ? 1 : -1;
        //float err = dx - dy;

        //int itt = 0;
        //while (true)
        //{
        //    displaymap[(int)(x0), (int)(y0)] = c;
        //    itt++;
        //    if (x0 == x1 && y0 == y1) break;
        //    float e2 = 2 * err;
        //    if (e2 > -dy)
        //    {
        //        err -= dy;
        //        x0 += sx;
        //    }
        //    if (e2 < dx)
        //    {
        //        err += dx;
        //        y0 += sy;
        //    }
        //}
        float xPix = Mathf.Round(to.x);
        float yPix = Mathf.Round(to.y);
        float width = (float)from.x - (float)to.x;
        float height = (float)from.y - (float)to.y;
        float length = Mathf.Abs(width);
        if (Mathf.Abs(height) > length) length = Mathf.Abs(height);
        int intLength = (int)(length);
        float dx = width / (float)length;
        float dy = height / (float)length;
        for (int u = 0; u <= intLength; u++)
        {
            if ((xPix) < Bounds.width && (yPix) < Bounds.height && xPix >= 0 && yPix >= 0)
                displaymap[(int)xPix, (int)yPix] = c;
            xPix += dx;
            yPix += dy;
        }
    }
    private Color[,] drawvoronoi(List<Cell> cells, List<Edge> edges, List<Corner> corners, Rect bounds)
    {
        Color[,] displaymap = new Color[(int)bounds.width, (int)bounds.height];
        //for (int i = 0; i < (int)bounds.width; i++) for (int j = 0; j < (int)bounds.height; j++) displaymap[i, j] = new Color(0.3f, 0.3f, 0.9f);
        Cell[] c;
        Corner[] cr;
        if(ShowCells)
        for (int i = 0; i < edges.Count; i++)
        {
            cr = edges[i].GetCorners();
            c = edges[i].getcells();
            for (int j = 0; j < 2; j++)
            {
                Color cl = /*new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));*/
                    c[j].coast ? new Color(0.7f, 0.9f, 0.3f) : c[j].ocean ? new Color(0.3f, 0.3f, 0.9f) : new Color(0.3f, c[j].elevation, c[j].moisture);
                drawline(ref displaymap, c[j].getpos(), cr[0].getposition(), cl);
                drawline(ref displaymap, c[j].getpos(), cr[1].getposition(), cl);
                drawtriangle(ref displaymap, cr[0].getposition(), cr[1].getposition(), c[j].getpos(), cl);
            }

        }


        //drawtriangle(ref displaymap, new Vector2(Random.Range(Bounds.xMin, Bounds.xMax), Random.Range(Bounds.yMin, Bounds.yMax)), new Vector2(Random.Range(Bounds.xMin, Bounds.xMax), Random.Range(Bounds.yMin, Bounds.yMax)), new Vector2(Random.Range(Bounds.xMin, Bounds.xMax), Random.Range(Bounds.yMin, Bounds.yMax)), Color.wh);
        if (ShowCenters) for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].coast) displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color(0.8f, 0.5f + (cells[i].elevation), 0.07f);
                else if (cells[i].ocean) displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color((1 - cells[i].moisture) * 0.5f, (1 - cells[i].moisture) * 0.5f, cells[i].moisture);
                //else if (cells[i].terrian == TerrianType.TERRIAN_LAKE) displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color(0.2f, 1.0f - Mathf.Clamp((cells[i].elevation), 0.0f, 1.0f), 1.0f - Mathf.Clamp((cells[i].elevation), 0.0f, 1.0f));
                else displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color((cells[i].elevation - 0.7f) * 3.34f, cells[i].elevation + 0.1f, (cells[i].elevation - 0.7f) * 3.34f);
            }
        if (ShowEdges) for (int i = 0; i < edges.Count; i++) drawline(ref displaymap, edges[i].getto(), edges[i].getfrom(), new Color(0.2f, 0.2f, 0.2f));

        if (ShowCorners) for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i].terrian == TerrianType.TERRIAN_LAND) displaymap[(int)corners[i].getposition().x, (int)corners[i].getposition().y] = new Color((corners[i].elevation - 0.7f) * 3.34f, corners[i].elevation + 0.1f, (corners[i].elevation - 0.7f) * 3.34f);
                else if (corners[i].terrian == TerrianType.TERRIAN_OCEAN) displaymap[(int)corners[i].getposition().x, (int)corners[i].getposition().y] = new Color(0, 0, 1.0f);
                else if (corners[i].terrian == TerrianType.TERRIAN_LAKE) displaymap[(int)corners[i].getposition().x, (int)corners[i].getposition().y] = (corners[i].moisture - LakeMoisture * 0.25f) * new Color(0.6f, 0.8f, 1.0f);
                else if (corners[i].terrian == TerrianType.TERRIAN_COAST) displaymap[(int)corners[i].getposition().x, (int)corners[i].getposition().y] = new Color(0.8f, 0.5f + (corners[i].elevation), 0.07f);
            }




        return displaymap;
    }
    private List<Cell> createcells(List<Voronoi2.GraphEdge> oedges, List<Vector2> points, ref List<Edge> edges, ref List<Corner> corners)
    {
        List<Cell> cells = new List<Cell>();
        Edge e; Voronoi2.GraphEdge gedge; Cell c1, c2;
        for (int i = 0; i < points.Count; i++) cells.Add(new Cell(points[i], i));
        for (int i = 0; i < oedges.Count; i++)
        {
            gedge = oedges[i]; c1 = cells[gedge.site1]; c2 = cells[gedge.site2];
            e = new Edge(new Vector2((float)gedge.x1, (float)gedge.y1), new Vector2((float)gedge.x2, (float)gedge.y2), c1, c2, i, ref corners);
            edges.Add(e);
            if (gedge.site1 == gedge.site2) Debug.Log("dub site");
        }

        float avr = 0; List<Edge> borders;
        for (int i = 0; i < cells.Count; i++)
        {
            borders = cells[i].GetBorders(); avr += borders.Count;

        }
        for(int i = 0; i < corners.Count; i++)
        corners[i].GatherNeighbors(); //doing the neighbor gathering after cuz of some issue;
        //Debug.Log(a);
        return cells;
    }
    private void improverandompoints(ref List<Vector2> points, int lloyd_irrations, float patel_irrations, float mindistance, Rect bounds, ref List<Cell> cells, ref List<Edge> edges, ref List<Corner> corners)
    {
        { //create a blank scope so we can reuse local varible names
            cells.Clear(); edges.Clear(); corners.Clear();//make sure our Returning lists are empty
            Vector2 p = new Vector2();  List<Cell> region = new List<Cell>(); Voronoi2.Voronoi v = new Voronoi2.Voronoi(0.002);
            List<Voronoi2.GraphEdge> ge;
            bruteforceremove(ref points, ref cells, mindistance);
            for (int i = 0; i < lloyd_irrations; i++)
            {
                ge = v.generateVoronoi(points, bounds);
                cells = createcells(ge, points, ref edges, ref corners);
                for (int k = 0; k < cells.Count; k++)
                {
                    p = points[k];
                    cells[k].IsEdgeCell(bounds, true); //this would normally be a bool response but with the 2nd param this will just add neighbors for us so we can artifially clamp the edges
                    region = cells[k].GetNeighbors();
                    if (region.Count == 0) continue;
                    p.x = p.y = 0f;
                    for (int j = 0; j < region.Count; j++) p += region[j].getpos();
                    p /= region.Count;
                    p.x = Mathf.Clamp(p.x, (bounds.xMin + bounds.width * 0.01f), (bounds.xMax - 1 - bounds.width * 0.01f)); // clamp it within our bounds
                    p.y = Mathf.Clamp(p.y, (bounds.yMin + bounds.height * 0.01f), (bounds.yMax - 1 - bounds.height * 0.01f));
                    points[k] = p;
                    region = null;
                }
                bruteforceremove(ref points, ref cells, mindistance);
                for (int j = 0; j < cells.Count; j++) cells[j].Disown();
                cells.Clear(); edges.Clear(); corners.Clear(); ge.Clear();
                v.reset();
                System.GC.Collect();
                //rememer all we are changing is points so everything needs removed so gc can remove the unnessary info
               
            }
            v.reset();
            
            cells = createcells(v.generateVoronoi(points, bounds), points, ref edges, ref corners);//this will create everything the last time so we get the result we want.
        }

        //phase two
        Vector2[] newcorners = new Vector2[corners.Count];
        Corner q; Vector2 p1; List<Cell> cellneighbors;
        for (int k = 0; k < patel_irrations; k++)   
        {
            for (int i = 0; i < corners.Count; i++)
            {
                q = corners[i];
                if (q.IsBoundary(bounds)) { newcorners[i] = q.getposition(); continue; }

                p1 = new Vector2();
                cellneighbors = q.GetCellNeighbors();
                if (cellneighbors.Count <= 0) { newcorners[i] = q.getposition(); Debug.Log("cell neighbor not assigned"); continue; }

                for (int j = 0; j < cellneighbors.Count; j++) p1 += cellneighbors[j].getpos();
                p1 /= cellneighbors.Count;
                newcorners[i] = p1;
            }
            for (int i = 0; i < newcorners.GetLength(0); i++) corners[i].SetPosition(newcorners[i]); // now we assign the points
        }
        //phase 3: create edge lines
        //List<Corner> corns;
        //for (int i = 0; i < corners.Count; i++)
        //{
        //    if (!corners[i].IsBoundary(bounds)) continue;
        //    cellneighbors =  corners[i].GetCellNeighbors();
        //    for(int j = 0; j < cellneighbors.Count; j++)
        //    {
        //        corns = cellneighbors[j].GetCorners();
        //        for(int k = 0; k < corns.Count; j++)
        //        {
        //            if (corns[k].getposition().y == corners[i].getposition().y || corns[j].getposition().x == corners[i].getposition().x)
        //            {
        //                if (corns[k].GetEdgeIdFromCorners(corners[i]) > 0) continue;
        //                List<Corner> c = new List<Corner> { corners[i], corns[k] };
        //                edges.Add(new Edge(corns[k].getposition(), corners[i].getposition(), cellneighbors[j], cellneighbors[j], edges.Count, ref c));
        //            }
                        
        //        }
        //    }
        //}
    }

    void fillBottomFlatTriangle(ref Color[,] colormap, Vector2 v1, Vector2 v2, Vector2 v3, Color c)
    {
        float invslope1 = (v2.x - v1.x) / (v2.y - v1.y);
        float invslope2 = (v3.x - v1.x) / (v3.y - v1.y);

        float curx1 = v1.x;
        float curx2 = v1.x;

        for (int scanlineY = Mathf.RoundToInt(v1.y); scanlineY <=v2.y; scanlineY++)
        {
            drawline(ref colormap, new Vector2(Mathf.RoundToInt(curx1), Mathf.RoundToInt(scanlineY)), new Vector2(Mathf.RoundToInt(curx2), Mathf.RoundToInt(scanlineY)), c);
            curx1 += invslope1;
            curx2 += invslope2;
        }
    }
    void fillTopFlatTriangle(ref Color[,] colormap, Vector2 v1, Vector2 v2, Vector2 v3, Color c)
    {
        float invslope1 = (v3.x - v1.x) / (v3.y - v1.y);
        float invslope2 = (v3.x - v2.x) / (v3.y - v2.y);

        float curx1 = v3.x;
        float curx2 = v3.x;

        for (int scanlineY = Mathf.RoundToInt(v3.y); scanlineY > v1.y; scanlineY--)
        {
            drawline(ref colormap, new Vector2(Mathf.RoundToInt(curx1), Mathf.RoundToInt(scanlineY)), new Vector2(Mathf.RoundToInt(curx2), Mathf.RoundToInt(scanlineY)), c);
            curx1 -= invslope1;
            curx2 -= invslope2;
        }
    }
    void drawtriangle(ref Color[,] colormap, Vector2 v1, Vector2 v2, Vector2 v3, Color c)
    {
        //sort so that thee larget v is v1 -> v3
        Vector2 v4;
        if (v1.y > v2.y && v3.y > v2.y) //if v2 is the largest
        {
            v4 = v1; v1 = v2; v2 = v4;
            if (v2.y > v3.y) { v4 = v2; v2 = v3; v3 = v4; } //test the other 2
        }
        else if (v1.y > v3.y) //if v3 is the largest
        {
            v4 = v1; v1 = v3; v3 = v4;
            if (v2.y > v3.y) { v4 = v2; v2 = v3; v3 = v4; } //test the other 2
        }
        else if (v2.y > v3.y) { v4 = v2; v2 = v3; v3 = v4; } //if v1 is the largest check the other 2;

        if (Mathf.Round(v2.y) == Mathf.Round(v3.y)) { fillBottomFlatTriangle(ref colormap, v1, v2, v3, c); return; }
        if (Mathf.Round(v2.y) == Mathf.Round(v1.y)) { fillTopFlatTriangle(ref colormap, v1, v2, v3, c);    return; }
        
        v4 = new Vector2((int)(v1.x + ((float)(v2.y - v1.y) / (float)(v3.y - v1.y)) * (v3.x - v1.x)), v2.y);
        fillBottomFlatTriangle(ref colormap, v1, v4, v2, c);
        fillTopFlatTriangle(ref colormap, v2, v4, v3, c);
    }
    void bruteforceremove(ref List<Vector2> points, ref List<Cell> cells, float mindistance) //removes cells that have centers to close to eachother
    {
        List<Cell> neighbors; int removed = 0;
        for(int i = 0; i < cells.Count; i++)
        {
            neighbors = cells[i].GetNeighbors();
            for(int j = 0; j < neighbors.Count; j++)
            {
                if (Vector2.Distance(neighbors[j].getpos(), cells[i].getpos()) < mindistance)
                {
                    cells[i].RemoveNeighbors(); //keep in mind that this wont fix the corners or edges
                    points.RemoveAt(i - removed);
                    removed++;       
                    break;
                }
            }
        }
    }

    public void reset() { }

    enum Biome
    {
        NONE = 0
    }
    private class Cell
    {
        Vector2 center; //center is the middle of the cell. Midpoint is the middle of the line.

        public int id { get; set; }
        public bool water = false, ocean = false, coast = false, border = false;
        Biome biome = Biome.NONE;

        List<Edge> borders;
        //List<Midpoint> midpts; //this is the middle of the edge. We will use this mainly for relaxing the edges and making noisy lines and a few other small things
        List<Cell> neighbors; // cell you share one edge with
        List<Corner> corners;
        public float elevation { get; set; }
        public float temperature { get; set; }
        public float moisture { get; set; }

        public Cell(Vector2 position, int index=-1)
        {
            center = position;
            borders = new List<Edge>();
            corners = new List<Corner>();
            //midpts = new List<Midpoint>();
            neighbors = new List<Cell>();
            id = index;
        }
        public List<Corner> GetCorners() { return corners; }
        public List<Cell> GetNeighbors() { return neighbors; }
        public Vector2 getpos() { return center; }
        public void RemoveNeighbors()
        {
            for (int i = 0; i < neighbors.Count; i++)
                neighbors[i].RemoveCell(this);
            neighbors.Clear();
        }
        public void RemoveCell(Cell c)
        {
            neighbors.Remove(c);
            Disown();
        }
        public bool IsEdgeCell(Rect bounds, bool clamp = false)
        {
            float radius = 0;
            List<Vector2> en = new List<Vector2>();
            if ((bounds.xMax-1 - bounds.width * 0.01f) <= center.x || (bounds.xMin + bounds.width * 0.01f) >= center.x ||
                (bounds.yMax-1 - bounds.height * 0.01f) <= center.y || (bounds.yMin + bounds.height * 0.01f) >= center.y) clamp = false; //dont clamp if we are to close to the edge
            for (int i = 0; i < borders.Count; i++)
                if (borders[i].IsEdge(bounds))
                {
                    if (!clamp) return true;
                    if (radius == 0) for (int j = 0; j < neighbors.Count; j++) radius = Mathf.Max(Vector2.Distance(this.getpos(), neighbors[j].getpos()), radius);
                    en = borders[i].ClampEdge(bounds, this.center, radius);
                    for (int j = 0; j < en.Count; j++) neighbors.Add(new Cell(en[j], -1)); //add in false neighbors do balance out everything
                }
            return false;
        }
        public List<Edge> GetBorders() { return borders; }
        public bool AddEdge(Edge edge, Cell neighbor)
        {
            neighbors.Add(neighbor);
            //for (int i = 0; i < borders.Count; i++) if (edge.isequal(edge)) return false; //so we dont add the same edge twice (idk how we could);
            borders.Add(edge);
            Corner[] cr = edge.GetCorners();
            bool flag = false;
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < corners.Count; j++)
                    if (corners[j] == cr[i])
                        flag = true;
                if (!flag) corners.Add(cr[i]);
                flag = false;
            }
            return true;
        }
        public void Disown() //only used to destroy the entire cell list
        {
            //todo: maybe add a way to remove one cell from the list then rebuild that part of the list. for now you will just remove that point from the list and then rebuild the entire graph
            if(neighbors!=null) neighbors.Clear();
            for (int i = 0; i < borders.Count; i++) borders[i].Disown();
            if (borders != null) borders.Clear();
        }
    }

    private class Edge
    {
        //Vector2 from, to; //from is always the lowest Y then lowest X
        Cell c1, c2; //belongs to 2 points on either side to of it;
        List<Edge> neighbors;
        public int id { get; private set; }
        Corner[] ends; 
        public int riverlevel { get; private set; }
        public float tempature { get; set; }
        Midpoint m;

        public Edge(Vector2 f1, Vector2 t1, Cell c1, Cell c2, int index, ref List<Corner> corners)
        {
            this.c1 = c1;
            this.c2 = c2;
            calculatemidpoint(f1, t1);
            ends = new Corner[2];
            id = index;
            Edge e; Vector2 t2, f2; List<Edge> borders = c1.GetBorders();
            borders.AddRange(c2.GetBorders()); //add the other cell as well. but keep in mind there is technically a one over lap between the two
            //Debug.Log(borders.Count);
            for (int i = 0; i < borders.Count; i++)
            {
                e = borders[i]; t2 = e.getto(); f2 = e.getfrom();
                if (ends[0] == null) //only run this one time after a true is fired
                {
                    if (t1 == t2) { ends[0] = e.GetCorners()[0]; ends[0].AddEdge(this); continue; }
                    else if (t1 == f2) { ends[0] = e.GetCorners()[1]; ends[0].AddEdge(this); continue; }
                    
                }
                if (ends[1] == null) //same as above
                {
                    if (f1 == t2) { ends[1] = e.GetCorners()[0]; ends[1].AddEdge(this); continue; }
                    else if (f1 == f2) { ends[1] = e.GetCorners()[1]; ends[1].AddEdge(this); continue; }
                    
                }
                if (ends[0] != null && ends[1] != null) { break; } //we found a corner for both ends so we are done here
            }
            if (ends[0] == null) { ends[0] = new Corner(t1, corners.Count,this); corners.Add(ends[0]); }
            if (ends[1] == null) { ends[1] = new Corner(f1, corners.Count, this); corners.Add(ends[1]); }
            c1.AddEdge(this, c2); c2.AddEdge(this, c1);
        }

        internal void setcorner(Vector2 pt, List<Corner> corners)
        {
            //int i;
            //for (i = 0; i < 3; i++)
            //{
            //    if (i == 2) return; //this shouldnt happen but this will be if both slots are full and we try adding another object
            //    if (ends[i] == null) break;
            //    if (ends[i].getposition() == pt) return;
            //}
            //ends[i] = new Corner(pt, corners.Count);
            //corners.Add(ends[i]);
            ////for (int i = 0; i < 2; i++)
            ////    if (pt == ends[i].getposition()) return;
            ////Corner c = new Corner(pt);
            ////ends.Add(c); corners.Add(c);
        }
        internal Cell[] getcells() { return new Cell[] { c1, c2 }; }
        internal bool IsEdge(Rect bounds)
        {
            if (ends[0] == null || ends[1] == null) return false;
            Vector2 to = ends[0].getposition(), from = ends[1].getposition();
            if (bounds.xMin >= to.x || bounds.xMin >= from.x || bounds.xMax - 1 <= to.x || bounds.xMax - 1 <= from.x ||
                bounds.yMin >= to.y || bounds.yMin >= from.y || bounds.yMax - 1 <= to.y || bounds.yMax - 1 <= from.y) //do we fall on the edge or outside the bounds
                return true; //if so we are a bordering cell and we need special treatment
            return false;
        }
        internal Vector2 getto() { return ends[0] != null ? ends[0].getposition() : Vector2.zero; }
        internal Vector2 getfrom() { return ends[1] != null ? ends[1].getposition() : Vector2.zero; }
        public Corner[] GetCorners() { return ends; }
        internal List<Vector2> ClampEdge(Rect bounds, Vector2 center, float radius)
        {
            List<Vector2> ps = new List<Vector2>();
            radius *= 0.5f; //so we dont over pull the objects back to the bounds edge
            //do to vector
            if (ends[0] == null || ends[1] == null) return null;
            Vector2 to = ends[0].getposition(), from = ends[1].getposition();
            if (bounds.xMin >= to.x) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x - radius, center.y + radius * i));
            else if (bounds.xMax - 1 <= to.x) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius, center.y + radius * i));
            else if (bounds.yMin >= to.y) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius * i, center.y - radius));
            else if (bounds.yMax - 1 <= to.y) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius * i, center.y + radius));

            //check from vector
            if (bounds.xMin >= from.x) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x - radius, center.y + radius * i));
            else if (bounds.xMax - 1 <= from.x) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius, center.y + radius * i));
            else if (bounds.yMin >= from.y) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius * i, center.y - radius));
            else if (bounds.yMax - 1 <= from.y) for (int i = -1; i < 2; i += 2) ps.Add(new Vector2(center.x + radius * i, center.y + radius));

            return ps;
        }
        void calculatemidpoint(Vector2 to, Vector2 from)
        {
            m = new Midpoint(this, Vector2.Lerp(to, from, 0.5f));
        }
        public void Disown()
        {
            for (int i = 0; i < 2; i++) { if (ends[i] != null) ends[i].Disown(); ends[i] = null; }
            m = null;
        }
        internal bool isequal(Edge e)
        {
            if (e.getto() == this.getto())
                if (e.getfrom() == this.getfrom())
                    return true;
            if (e.getto() == this.getfrom())
                if (e.getfrom() == this.getto())
                    return true;
            return false;
        }
    }

    private class Midpoint
    {
        Vector2 pos;
        Edge e;
        
        public Midpoint (Edge edge, Vector2 position)
        {
            e = edge; pos = position;
        }
    }

    enum TerrianType
    {
        TERRIAN_NULL = 0,
        TERRIAN_OCEAN,
        TERRIAN_COAST,
        TERRIAN_LAND,
        TERRIAN_LAKE
    }
    private class Corner
    {
        
        Vector2 position;
        public List<Edge> edges;
        public List<Cell> touches;
        public float elevation { get; set; }
        public float moisture { get; set; }
        public float tempature { get; set; }
        public TerrianType terrian;
        public int id { get; set; }
        List<Corner> neighbors;

        public Corner (Vector2 point, int index, Edge e) { position = point; neighbors = new List<Corner>(); id = index; edges = new List<Edge>(); neighbors = new List<Corner>(); touches = new List<Cell>(); terrian = TerrianType.TERRIAN_NULL; AddEdge(e); }
        public Vector2 getposition()
        {
            return position;
        }
        public Corner AddEdge(Edge e)
        {
            this.edges.Add(e);
            Cell[] cells = e.getcells();
            bool flag = false;
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < touches.Count; i++)
                    if (touches[i] == cells[j]) { flag = true; break; }
                if(!flag)touches.Add(cells[j]);
                flag = false;
            }
            return this;
        }
        public void GatherNeighbors()
        {
            for (int j = 0; j < edges.Count; j++)
            {
                Corner[] c = edges[j].GetCorners();
                for (int i = 0; i < 2; i++)
                {
                    if (c[i] == null)
                    {
                         continue;
                    }
                    if (c[i].getposition() == this.getposition()) { continue; }
                    neighbors.Add(c[i]);
                }
            }
        }
        public void SetPosition(Vector2 pos) { position = pos; }
        public void SetTerrianProperties(float elevation, float moisture, TerrianType type)
        {
            this.elevation = elevation;
            this.moisture = moisture;
            this.terrian = type;
        }
        public void SetTerrianType(TerrianType type) { this.terrian = type;  }
        public int GetEdgeIdFromCorners(Corner c)
        {
            Corner[] cn;
            for (int i = 0; i < edges.Count; i++)
            {
                cn = edges[i].GetCorners();
                for (int k = 0; k < 2; k++)
                    if (cn[k] == c) return edges[i].id;
            }
            //else we didnt find one so return null
           return -1;
        }
        public bool IsBoundary(Rect bounds)
        {
            if (bounds.xMin >= position.x || bounds.yMin >= position.y || bounds.xMax - 1 <= position.x || bounds.yMax-1 <= position.y) return true;
            return false;
        }
        public List<Corner> GetNeighbors()
        {
            return neighbors;
        }
        public List<Cell> GetCellNeighbors()
        {
            return touches;
        }
        public void Disown()
        {
            neighbors.Clear();
        }

        public bool HasTerrianValue()
        {
            return terrian != TerrianType.TERRIAN_NULL;
        }
    }

    class IslandProperty
    {
        public float elelvation = 0.0f;
        public float moisture = 0.0f;
        public float landchance = 0.0f;
    }

    class Highpoint
    {
        public Highpoint(float x, float y, float sizex, float sizey, float elev, float dispat)
        {
            pos = new Vector2(x, y);
            size = new Vector2(sizex, sizey);
            elevation = Mathf.Clamp(elev, 0.0f, 1.0f);
            dispation = dispat;
        }
        public Vector2 pos { get; set; } //point on which the highpoint sits
        public Vector2 size { get; set; } //size of the elipse shape
        public float elevation { get; set; } //the highest value that a corner can recieve (say it shares the position of the highpoint)
        public float dispation { get; set; } //how fast does the highpoint decay (elevation)^dispation
    }

}