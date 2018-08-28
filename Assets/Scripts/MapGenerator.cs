using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Range(-9999,9999)]
    public int Seed = 100;
    [Range(-9999, 9999)]
    public int Variant = 100;
    [Range(-9999, 9999)]
    public int MapSeed;

    public Rect Bounds = new Rect(0, 0, 200, 200);
    [Range(0.01f,50.0f)]
    public float CellPercentage = 3f; //percent of the map that cells contain on average. The closer to 100 The smaller the cells and the larger the detail (and the longer the time to process)
    [Range(0,10)]
    public int LLOYD_Irrations = 2;
    [Range(0.0001f, 15f)]
    
    public float MinDistance = 5f;
    public List<Texture2D> MapTextures;
   
    public bool Preview = true;
    public bool ShowEdges = true;
    public bool ShowCenters = true;
    public bool ShowCorners = false;
    

    public void Awake() //call the map when the game starts.
    {
        GenerateMap();
    }
    public void GenerateMap()
    {
        List<Cell> cells = new List<Cell>(); List<Edge> edges = new List<Edge>() ; List<Corner> corners = new List<Corner>();

        Random.InitState(Seed); //set up random so we can save the seed back so we always can get back to were we where
        Mathf.Clamp(CellPercentage, 0.001f, 0.9f); //cap this at 90% cuz otherwise there wont be any map

        List<Vector2> points = generaterandompoints((uint)Mathf.RoundToInt((CellPercentage *0.001f)* (Bounds.width * Bounds.height)), Bounds); //maybe add a random here to make it a lil more crazy
        
        improverandompoints(ref points, LLOYD_Irrations, MinDistance, Bounds, ref cells, ref edges, ref corners);

        //Divide Land and water, then define ocean from lake, then assign coast, then assign elevation to all land modules (looking at neighbors to level out posiblities)
        //List<Corner> landcorners, watercorners;
        //Color[] maptexture = readymaptexture(MapTextures, Bounds);
        //assigncornerelevation(ref corners, Bounds, maptexture,Variant);
        //assignlandandwater(corners,ref landcorners, ref watercorners); //assign water and land then assign coast and lake and ocean
        //Texture2d maptexture;
        //maptexture = MapTextures[Random.Range(0, MapTextures.Count)];
        //if (!maptexture) return; //maybe instantiate an error here
        //assignelevation(landcorners, watercorners, );
        //assignmoisture(landcorners, watercorners);




        //renderer;
        Color[,] displaymap = drawvoronoi(cells, edges, corners, Bounds);
        //do disown then clear
        points.Clear(); cells.Clear(); edges.Clear(); corners.Clear(); System.GC.Collect();
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawColorMap(displaymap);
    }

    private Color[] readymaptexture(List<Texture2D> mapTextures, Rect bounds, int mapid = -1)
    {
        Random.State prerandom = Random.state;
        Random.InitState(MapSeed);
        if (mapid < 0 || mapid >= mapTextures.Count) mapid = (int)(Random.Range(0, mapTextures.Count)); //let the user force a map if not we will randomly pick one

        Random.state = prerandom;

        Texture2D maptex = new Texture2D(mapTextures[mapid].width, mapTextures[mapid].height);
        maptex.SetPixels(mapTextures[mapid].GetPixels());
        maptex.Resize((int)bounds.width, (int)bounds.height);
        return maptex.GetPixels();
    }

    private void assigncornerelevation(ref List<Corner> corners, Rect bounds, Color[] tex, int perlinseed)
    {
        Corner q, s; Vector2 center = bounds.center;
        List<Corner> LandCorners = new List<Corner>(), LakeCorners = new List<Corner>(), OceanCorners = new List<Corner>(), neighbors; //waterCorners will only hold water that could be ocean or lakes
        Random.State prerandom = Random.state;
        Vector2 pos; float maxdist = Vector2.Distance(center, bounds.min);
        float width = bounds.width;
        float[,] perlinheightmap = PerlinGenerator.GenerateNoise(bounds, 0.1f, (uint)perlinseed);
        float[,] perlinmoisturemap = PerlinGenerator.GenerateNoise(bounds, 0.1f, (uint)perlinseed+1);
        for (int i = 0; i < corners.Count; i++)
       {

            q = corners[i];
            pos = q.getposition();
            if (q.HasTerrianValue()) continue; //basically if we have already processed this value then we want to skip it. this is mainly for corners;
            if (q.isEdge(bounds))
            {
                //handel edge of map;
                neighbors = q.GetNeighbors();
                for (int j = 0; j < neighbors.Count; j++) neighbors[j].SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_OCEAN);
                q.SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_OCEAN);
                OceanCorners.Add(q);
                continue; //when we are done we wont need to do anything else here
            }
            continue;
            //so we are gonna look at the perlinmap giving a 20% bias to that then look at our maptexture and give a 50% bias to that then finally to help the rivers run down hill we will add a small 30% bias based on where an object is from the center of the map
            float texelevation = tex[(int)(width * pos.y + pos.x)].g;
            float elevation = (perlinheightmap[(int)pos.x, (int)pos.y] * 0.01f) + (Random.Range(texelevation*0.5f,texelevation ) * 0.5f) + (((0.5f - Vector2.Distance(pos,center)/maxdist)) * 0.3f);
            if(elevation < 0.2f)
            {
                //this will be water
                q.SetTerrianProperties(0.0f, 1.0f, TerrianType.TERRIAN_LAKE); //we may change this to give it a lil evelation varience but for now lets leave it alone
                LakeCorners.Add(q); //we dont know if this is a lake or not but we will determind that after we calculate all the corners
                continue; //we are done now 
            }
            //so this must be land so lets calculate some pregenerated moisture ideals
            float moisture = (perlinmoisturemap[(int)pos.x, (int)pos.y] * 0.2f) + (Random.Range(0.0f, tex[(int)(width * pos.y + pos.x)].r))* 0.5f + ((1.0f - (Mathf.Abs(pos.y - center.y) / bounds.width)) * 0.3f);
            q.SetTerrianProperties(elevation, moisture, TerrianType.TERRIAN_LAND);
         }
        //phase 2: we need to now find our map edge corners (conviently placed in the oceans terrain) then cycle through there neighbors till we get to corners marked as land. We will change each of these corners from lake to ocean and move them from the Lake list to the ocean list.
        //this will be similar to a b* algorithm were we will follow a branch til we have reach a dead end then we will go to the next branch
        List<Corner> que = new List<Corner>(OceanCorners.Count); //see up this cuz we are gonna copy it in a sec
        que.AddRange(OceanCorners);
        while (que.Count > 0) //we are just gonna set up a simple que algorithm each cycle we will remove an item from the que until all items are gone and we have our map
        {
            q = que[0];
            if (q.isEdge(bounds)) { que.RemoveAt(0); continue; } //above we already added all of the map edges to be part of the ocean group so we dont need to recycle these ones
            neighbors = q.GetNeighbors();
            for (int i = 0; i < neighbors.Count; i++)
            {
                s = neighbors[i];
                if (s.terrian != TerrianType.TERRIAN_LAKE) continue; //we only need to correct the terrian that should be ocean not lake
                s.terrian = TerrianType.TERRIAN_OCEAN;
                que.Add(s); //we add this to the que so that we can check its neighbors
                LakeCorners.Remove(s); //we want to make sure to keep the lake list clean so it only contains the lake elements. This item is an ocean element os we dotn want it here
                OceanCorners.Add(s);
            }
            que.RemoveAt(0); //once we checked all our neighbors then we are done and we can move on
        }

    }

    private void assignlandandwater(List<Corner> corners, ref List<Corner> landcorners, ref List<Corner> watercorners)
    {
        
    }

    private List<Vector2> generaterandompoints(uint NumPoints, Rect bounds)
    {
        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < NumPoints; i++)
            points.Add(new Vector2(Random.Range(bounds.xMin + bounds.size.x * 0.01f , bounds.xMax - bounds.size.x * 0.01f),
                Random.Range(bounds.yMin + bounds.size.y * 0.01f, bounds.yMax - bounds.size.y * 0.01f))); // we add a 1% boarder + 10 units so that we dont get any points to close to the edge
        return points;
    }
    private void drawline(ref Color[,] displaymap, Vector2 to, Vector2 from)
    {
        float xPix = to.x;
        float yPix = to.y;
        bool failed = false;
        float width = (float)from.x - (float)to.x;
        float height = (float)from.y - (float)to.y;
        float length = Mathf.Abs(width);
        if (Mathf.Abs(height) > length) length = Mathf.Abs(height);
        int intLength = (int)length;
        float dx = width / (float)length;
        float dy = height / (float)length;
        for (int u = 0; u <= intLength; u++)
        {
            if (xPix < Bounds.width && yPix < Bounds.height && xPix >= 0 && yPix >= 0)
                displaymap[(int)xPix, (int)yPix] = new Color(0.2f, 0.2f, 0.2f);
            else failed = true;
            xPix += dx;
            yPix += dy;
        }
        if (failed) Debug.Log("Line out of range: " + to.ToString() + from.ToString());
    }
    private Color[,] drawvoronoi(List<Cell> cells, List<Edge> edges, List<Corner> corners, Rect bounds)
    {
        Color[,] displaymap = new Color[(int)bounds.width, (int)bounds.height];

        if (ShowCenters) for (int i = 0; i < cells.Count; i++) displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color(0.5f, 1f, 0.8f);
        if (ShowEdges) for (int i = 0; i < edges.Count; i++) drawline(ref displaymap, edges[i].getto(), edges[i].getfrom());
        if (ShowCorners) for (int i = 0; i < corners.Count; i++) if (corners[i].terrian != TerrianType.TERRIAN_NULL) displaymap[(int)corners[i].getposition().x, (int)corners[i].getposition().y] =
                      new Color(corners[i].terrian == TerrianType.TERRIAN_LAKE ? 1.0f : 0.0f, corners[i].terrian == TerrianType.TERRIAN_LAND ? 1.0f : 0.0f, corners[i].terrian == TerrianType.TERRIAN_OCEAN ? 1.0f : 0.0f);
        //List<Edge> borders;
        //for (int i = 0; i < cells.Count; i++)
        //{
        //    borders = cells[i].GetBorders();
        //    displaymap[(int)cells[i].getpos().x, (int)cells[i].getpos().y] = new Color(borders.Count*0.1f, 1f, 0.8f);

        //    for (int j = 0; j < borders.Count; j++)
        //        drawline(ref displaymap, borders[j].getto(), borders[j].getfrom());
        //}
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
            //for (int j = 0; j < edges.Count; j++) if (edges[j].isequal(e)) Debug.Log("DUBLICATE EDGE");  //check to see if we have doubles in this array           
        }

        //for (int i = 0; i < oedges.Count; i++)
        //{
        //    e = edges[i]; gedge = oedges[i]; c1 = cells[gedge.site1]; c2 = cells[gedge.site2];
        //    c1.AddEdge(e, c2); c2.AddEdge(e, c1);
        //}

        float avr = 0; List<Edge> borders;
        for (int i = 0; i < cells.Count; i++)
        {
            borders = cells[i].GetBorders(); avr += borders.Count;

        }
        //Debug.Log(avr / cells.Count);
        //int a = 0;
        for(int i = 0; i < corners.Count; i++) /*{ Corner[] c = edges[i].GetCorners(); if (c[0] == null) a++; if (c[1] == null) a++; }*/
        corners[i].gatherneighbors(); //doing the neighbor gathering after cuz of some issue;
        //Debug.Log(a);
        return cells;
    }
    private void improverandompoints(ref List<Vector2> points, int lloyd_irrations, float mindistance, Rect bounds, ref List<Cell> cells, ref List<Edge> edges, ref List<Corner> corners)
    {
        { //create a blank scope so we can reuse local varible names
            cells.Clear(); edges.Clear(); corners.Clear();//make sure our Returning lists are empty
            Vector2 p = new Vector2();  List<Cell> region = new List<Cell>(); Voronoi2.Voronoi v = new Voronoi2.Voronoi(0.002);
            List<Voronoi2.GraphEdge> ge;
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
            //bruteforceremove(ref points, ref cells, mindistance);
        }

        //phase two
        Vector2[] newcorners = new Vector2[corners.Count];
        Corner q; Vector2 p1; List<Cell> neighbors;
        bool failed = false;
        for (int k = 0; k < 1; k++)
        {
            for (int i = 0; i < corners.Count; i++)
            {
                q = corners[i];
                if (q.isEdge(bounds)) { newcorners[i] = q.getposition(); continue; }

                p1 = new Vector2();
                neighbors = q.GetCellNeighbors();
                if (neighbors.Count <= 0) { newcorners[i] = q.getposition(); Debug.Log("neighbor not assigned"); continue; }

                for (int j = 0; j < neighbors.Count; j++) p1 += neighbors[j].getpos();
                p1 /= neighbors.Count;
                newcorners[i] = p1;
            }
            for (int i = 0; i < newcorners.GetLength(0); i++) corners[i].SetPosition(newcorners[i]); // now we assign the points
        }
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

        int id = -1;
        public bool water = false, ocean = false, coast = false, border = false;
        float elevation = 0f, moisture = 0f;
        Biome biome = Biome.NONE;

        List<Edge> borders;
        //List<Midpoint> midpts; //this is the middle of the edge. We will use this mainly for relaxing the edges and making noisy lines and a few other small things
        List<Cell> neighbors; // cell you share one edge with
        List<Corner> corners;

        public Cell(Vector2 position, int index=-1)
        {
            center = position;
            borders = new List<Edge>();
            //midpts = new List<Midpoint>();
            neighbors = new List<Cell>();
            id = index;
        }
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

        int id;
        Corner[] ends;
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
                    if (t1 == t2) { ends[0] = e.GetCorners()[0]; ends[0].AddEdge(this); }
                    else if (t1 == f2) { ends[0] = e.GetCorners()[1]; ends[0].AddEdge(this); }
                    Debug.Log("tried to set id 0");
                }
                if (ends[1] == null) //same as above
                {
                    if (f1 == t2) { ends[1] = e.GetCorners()[0]; ends[1].AddEdge(this); }
                    else if (f1 == f2) { ends[1] = e.GetCorners()[1]; ends[1].AddEdge(this); }
                    Debug.Log("tried to set id 1");
                }
                if (ends[0] != null && ends[1] != null) { return; } //we found a corner for both ends so we are done here
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
        public TerrianType terrian;
        int id;
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
            
            
            for(int i = 0; i < touches.Count; i++)
                for (int j = 0; j < 2; j++)
                {
                    if (touches[i].getpos() == cells[j].getpos()) continue;
                    touches.Add(cells[j]);
                }
            return this;
        }
        public void gatherneighbors()
        {
            for (int j = 0; j < edges.Count; j++)
            {
                Corner[] c = edges[j].GetCorners();
                if (c[0] == null && c[1] == null) Debug.Log("cells not registared yet");
                for (int i = 0; i < 2; i++)
                {
                    if (c[i] == null)
                    {
                        Debug.Log("point was null" + i); continue;
                    }
                    if (c[i].getposition() == this.getposition()) { Debug.Log("position was equal: " + i); continue; }
                    neighbors.Add(c[i]);
                }
            }
            Debug.Log(edges.Count);
        }
        public void SetPosition(Vector2 pos) { position = pos; }
        public void SetTerrianProperties(float elevation, float moisture, TerrianType type)
        {
            this.elevation = elevation;
            this.moisture = moisture;
            this.terrian = type;
        }
        public void SetTerrianType(TerrianType type) { this.terrian = type;  }
        public bool isEdge(Rect bounds)
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


}