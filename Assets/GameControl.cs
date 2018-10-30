using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour {

    bool Local = false; //this client is the server .. or its sp etc
    bool SinglePlayer = true; //
	// Use this for initialization
    GameControl(bool isLocal, bool isSP)
    {
        Local = isLocal;
        SinglePlayer = isSP;
    }
	void Start ()
    {
        if (Local) buildworld();
        //instantiate the world
        //Pick a point and then build this
	}
    private void buildworld(int seed = -1)
    {
        if (seed == -1) Random.Range(float.NegativeInfinity, float.PositiveInfinity);
        MapGenerator mg = new MapGenerator();
        mg.GenerateMap(); //should return an array of world cells.
    }
	// Update is called once per frame
	void Update () {
		
	}
}
