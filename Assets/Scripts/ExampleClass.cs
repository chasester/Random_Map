using UnityEngine;
using System.Collections;

public class ExampleClass : MonoBehaviour {
    Mesh Object;
    public Vector3[] newVertices;
    public Vector2[] newUV;
    public int[] newTriangles;
    void Update() {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = newVertices;
        mesh.uv = newUV;
        mesh.triangles = newTriangles;
        
    }
}