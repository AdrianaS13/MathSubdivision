using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static Mesh3D CreateCube()
    {
        var v = new List<Vector3>
        {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f)
        };

        var f = new List<List<int>>
        {
            new List<int> {0, 1, 2, 3},
            new List<int> {4, 5, 6, 7},
            new List<int> {0, 4, 7, 1},
            new List<int> {2, 6, 5, 3},
            new List<int> {0, 3, 5, 4},
            new List<int> {1, 7, 6, 2}
        };

        return new Mesh3D(v, f);
    }
}