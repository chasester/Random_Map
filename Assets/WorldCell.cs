﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class WorldCell
{
    //holder for all the data generated by the map generator
    public float Elevation { get; private set; } //average elevation
    public float ElevationRange { get; private set; } //variation in elevation +or-
    public float Moisture { get; private set; }  //average rain fall
    public float MoistureRange { get; private set; } //variation in average rainfall +or-
    public float Tempature { get; private set; } //average tempature
    public float TempatureRange { get; private set; } //variation in tempature (coldest season warmest season etc) +or-
    public Biome_Type Biome { get; private set; }

    private void setbiometype()
    {

    }
    //tempature is in C degrees
    static BiomeType[] BiomeList = new BiomeType[(int)Biome_Type.BIOME_LENGTH] 
    {
        new BiomeType(1.0f, 5.0f, -40.0f, -5.0f), //Frozen Water // always frozen (lots of iceburgs)
        new BiomeType(1.0f, 5.0f, -5.0f, 10.0f), //Lukewarm Water //Some times frozen few to no iceburgs
        new BiomeType(1.0f, 5.0f, 10.0f, 20.0f), //Warm Water //Nv frozen water but some times cold curent
        new BiomeType(1.0f, 5.0f, 20.0f, 90.0f), //Tropic Water //Always warm water generally near tropical areas (say like the amazon river)
        new BiomeType(0.5f, 1.0f, -40.0f, 0.0f), //Snow
        new BiomeType(0.2f, 0.5f, -40.0f, -5.0f), //Tundra
        new BiomeType(0.05f, 0.2f, -40.0f, -5.0f), //Bare
        new BiomeType(0.0f, 0.05f, -40.0f, -5.0f), //Scorched //lava ?
        new BiomeType(0.66f, 1.0f, -5.0f, 5.0f), //Taiga
        new BiomeType(0.33f, 0.66f, 0.0f, 0.0f), //Shrubland
        new BiomeType(0.0f, 0.33f, 5.0f, 15.0f), //Temprate Desert
        new BiomeType(0.83f, 1.0f, 5.0f, 15.0f), //Temprate RainForest
        new BiomeType(0.4f, 0.83f, 5.0f, 15.0f), //Temprate Decidous Forest
        new BiomeType(0.0f, 0.4f, 5.0f, 15.0f), //GrassLand
        new BiomeType(0.6f, 0.1f, 15.0f, 90.0f), //Tropical RainForest
        new BiomeType(0.2f, 0.6f, 15.0f, 90.0f), //Tropical Seasonal Forest
        new BiomeType(0.0f, 0.2f, 15.0f, 90.0f), //SubTropical Desert
    };
    private class BiomeType
    {
        public Vector2 Rain { get; private set; } //min max
        public Vector2 Tempature { get; private set; } //min max

        public BiomeType(float rainmin, float rainmax, float tempmin, float tempmax)
        {
            Rain = new Vector2(rainmin, rainmax);
            Tempature = new Vector2(tempmin, tempmax);
        }
       public bool isequal(WorldCell wc)
        {
            if (wc.Tempature >= Tempature.x && wc.Tempature < Tempature.y && wc.Moisture >= Rain.x && wc.Moisture < Rain.y)
                return true;
            return false;
        }
    }
}

public enum Biome_Type  //Bassed off Whittaker Diagram
{
    //water
    FrozenOcean = 0, //iceburgs
    LukeWarmOcean, //average ocean
    WarmOcean, //warmer current may create some cool femnominum
    FreshWater, //lake or river or creek etc
    //HighElevation = 4
    Snow,
    Tundra,
    Bare,
    Scorched,
    //3
    Taiga,
    ShrubLand,
    TemprateDesert,
    //2
    TemprateRainForest,
    TemprateDecidousForest,
    GrassLand,
    //TemprateDesert
    //1
    TropicalRainForest,
    TropicalSeasonalForest,
    //GrassLand
    SubTropicalDesert,
    BIOME_LENGTH
}


//NOTE: Bit 1 and 2 are ocean Bit 3 and 4 are high elevation etc 