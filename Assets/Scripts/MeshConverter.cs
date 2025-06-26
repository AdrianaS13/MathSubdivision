using System.Collections.Generic;
using UnityEngine;

public static class MeshConverter
{
    public static Mesh ToUnityMeshFlatShading(Mesh3D mesh3D)
    {
        Mesh mesh = new Mesh();

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        int triIndex = 0;

        foreach (var face in mesh3D.faces)
        {
            // Suponemos caras triangulares o cuádruples
            for (int i = 0; i < face.Count - 2; i++)
            {
                int idx0 = face[0];
                int idx1 = face[i + 1];
                int idx2 = face[i + 2];

                Vector3 v0 = mesh3D.vertices[idx0];
                Vector3 v1 = mesh3D.vertices[idx1];
                Vector3 v2 = mesh3D.vertices[idx2];

                // Calculamos la normal de la cara
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                // Añadimos los vértices duplicados para esta cara
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                // Normales iguales para cada vértice (cara plana)
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                // UVs simples: proyección plana XY + offset 0.5 para que queden en rango [0,1]
                uvs.Add(new Vector2(v0.x + 0.5f, v0.y + 0.5f));
                uvs.Add(new Vector2(v1.x + 0.5f, v1.y + 0.5f));
                uvs.Add(new Vector2(v2.x + 0.5f, v2.y + 0.5f));

                // Triángulo con los nuevos vértices
                triangles.Add(triIndex++);
                triangles.Add(triIndex++);
                triangles.Add(triIndex++);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateBounds();

        return mesh;
    }
}
