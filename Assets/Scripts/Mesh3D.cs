using System.Collections.Generic;
using UnityEngine;

public struct Mesh3D
{
    public List<Vector3> vertices;
    public List<List<int>> faces;

    public Mesh3D(List<Vector3> v, List<List<int>> f)
    {
        vertices = v;
        faces = f;
    }
}