using System.Collections.Generic;
using UnityEngine;

public class SqrtKobbeltSubdivision : MonoBehaviour
{
    [Range(0, 7)] public int subdivisionLevel2 = 0;
    public Material meshMaterial;
    public bool showDebugGizmos = false;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int lastSubdivision = -1;

    private List<Vector3> debugVertices;
    private List<List<int>> debugFaces;
    private List<(Vector3, Vector3)> debugFlipConnections;
    private List<Vector3> debugCenters;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        if (meshMaterial) meshRenderer.material = meshMaterial;
        UpdateMesh();
    }

    void Update()
    {
        if (lastSubdivision != subdivisionLevel2)
            UpdateMesh();
    }

    void UpdateMesh()
    {
        lastSubdivision = subdivisionLevel2;
        Mesh3D mesh = CreateTriangulatedCube();
        for (int i = 0; i < subdivisionLevel2; i++)
            mesh = ApplySqrt3Subdivision(mesh);
        meshFilter.mesh = MeshConverter.ToUnityMeshFlatShading(mesh);
        
    }

    Mesh3D CreateTriangulatedCube()
    {
        var v = new List<Vector3>
        {
            new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0.5f, -0.5f,  0.5f),
            new Vector3(0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f,  0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f)
        };

        var f = new List<List<int>>
        {
            new List<int>{0,1,2}, new List<int>{0,2,3},
            new List<int>{4,5,6}, new List<int>{4,6,7},
            new List<int>{0,4,7}, new List<int>{0,7,1},
            new List<int>{2,6,5}, new List<int>{2,5,3},
            new List<int>{0,3,5}, new List<int>{0,5,4},
            new List<int>{1,7,6}, new List<int>{1,6,2}
        };

        return new Mesh3D(v, f);
    }


    // Función para determinar la orientación correcta de un triángulo flip
    void CreateFlipTriangles(List<List<int>> finalFaces, List<Vector3> vertices, int c1, int c2, int v1, int v2, List<int> face1, List<int> face2)
    {
        // Calcular normales de las caras originales para determinar orientación correcta
        Vector3 normal1 = Vector3.Cross(vertices[face1[1]] - vertices[face1[0]], vertices[face1[2]] - vertices[face1[0]]).normalized;
        Vector3 normal2 = Vector3.Cross(vertices[face2[1]] - vertices[face2[0]], vertices[face2[2]] - vertices[face2[0]]).normalized;


        // Determinar orientación basada en las normales originales
        // Primer triángulo: c1, v1, c2 o c1, c2, v1
        Vector3 testNormal1 = Vector3.Cross(vertices[v1] - vertices[c1], vertices[c2] - vertices[c1]);
        bool useFirstOrder = Vector3.Dot(testNormal1, normal1) > 0 || Vector3.Dot(testNormal1, normal2) > 0;

        if (useFirstOrder)
        {
            finalFaces.Add(new List<int> { c1, v1, c2 });
            finalFaces.Add(new List<int> { c1, c2, v2 });
        }
        else
        {
            finalFaces.Add(new List<int> { c1, c2, v1 });
            finalFaces.Add(new List<int> { c1, v2, c2 });
        }
    }

    public Mesh3D ApplySqrt3Subdivision(Mesh3D input)
    {
        var oldVerts = input.vertices;
        var oldFaces = input.faces;

        var newVerts = new List<Vector3>(oldVerts);
        var faceCenters = new List<int>();

        // Paso 1: calcular centro de cada triángulo y añadirlo
        for (int i = 0; i < oldFaces.Count; i++)
        {
            var f = oldFaces[i];
            Vector3 center = (oldVerts[f[0]] + oldVerts[f[1]] + oldVerts[f[2]]) / 3f;
            newVerts.Add(center);
            faceCenters.Add(newVerts.Count - 1);
        }

        // Paso 2: crear la subdivisión inicial - 3 triángulos por cada triángulo original
        var subdividedFaces = new List<List<int>>();
        for (int i = 0; i < oldFaces.Count; i++)
        {
            var f = oldFaces[i];
            int c = faceCenters[i];
            subdividedFaces.Add(new List<int> { f[0], f[1], c });
            subdividedFaces.Add(new List<int> { f[1], f[2], c });
            subdividedFaces.Add(new List<int> { f[2], f[0], c });
        }

        // Paso 3: construir lista de aristas y caras adyacentes (en la malla original)
        var edgeList = new List<int[]>();
        var edgeFaces = new List<List<int>>();

        for (int f = 0; f < oldFaces.Count; f++)
        {
            var face = oldFaces[f];
            for (int i = 0; i < 3; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % 3]; // siguiente vértice de la cara (cerrando el triángulo)

                // Ordenar la arista para evitar duplicados: (menor, mayor)
                int v1 = Mathf.Min(a, b);
                int v2 = Mathf.Max(a, b);

                bool exists = false;
                for (int e = 0; e < edgeList.Count; e++)
                {
                    int[] edge = edgeList[e];
                    if (edge[0] == v1 && edge[1] == v2)
                    {
                        edgeFaces[e].Add(f);
                        exists = true;
                        break;
                    }
                }

                // Si no existe, la añadimos
                if (!exists)
                {
                    edgeList.Add(new int[] { v1, v2 });
                    edgeFaces.Add(new List<int> { f });
                }
            }
        }


        // Paso 4: construir la malla final con flipping:
        // - eliminar triángulos subdivididos que contienen aristas originales (se hace filtrando)
        // - añadir triángulos de flipping (entre centros y vértices originales)

        var finalFaces = new List<List<int>>();

        bool ContainsOriginalEdge(List<int> tri)
        {
            for (int k = 0; k < 3; k++)
            {
                int v0 = tri[k];
                int v1 = tri[(k + 1) % 3];
                int minV = Mathf.Min(v0, v1);
                int maxV = Mathf.Max(v0, v1);

                for (int e = 0; e < edgeList.Count; e++)
                {
                    if (edgeList[e][0] == minV && edgeList[e][1] == maxV)
                    {
                        return edgeFaces[e].Count == 2; // solo si tiene dos caras se puede flippear
                    }
                }
            }
            return false;
        }


        // Añadir triángulos subdivididos que NO contienen aristas originales (no se eliminan)
        foreach (var tri in subdividedFaces)
        {
            if (!ContainsOriginalEdge(tri))
                finalFaces.Add(tri);
        }

        // Paso 5: añadir triángulos de flipping
        for (int e = 0; e < edgeList.Count; e++)
        {
            if (edgeFaces[e].Count == 2)
            {
                int[] edge = edgeList[e];
                int v1 = edge[0];
                int v2 = edge[1];
                int f1 = edgeFaces[e][0];
                int f2 = edgeFaces[e][1];

                int c1 = faceCenters[f1];
                int c2 = faceCenters[f2];

                CreateFlipTriangles(finalFaces, newVerts, c1, c2, v1, v2, oldFaces[f1], oldFaces[f2]);
            }
        }


        // Paso 6: suavizar vértices originales
        var vertexNeighbors = new List<List<int>>();
        for (int i = 0; i < oldVerts.Count; i++)
            vertexNeighbors.Add(new List<int>());

        foreach (var face in oldFaces)
        {
            for (int i = 0; i < 3; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % 3];
                if (!vertexNeighbors[a].Contains(b)) vertexNeighbors[a].Add(b);
                if (!vertexNeighbors[b].Contains(a)) vertexNeighbors[b].Add(a);
            }
        }

        for (int i = 0; i < oldVerts.Count; i++)
        {
            int n = vertexNeighbors[i].Count;
            if (n < 3) continue;
            float alpha = (4f - 2f * Mathf.Cos(2f * Mathf.PI / n)) / 9f;
            Vector3 avg = Vector3.zero;
            foreach (int nb in vertexNeighbors[i]) avg += oldVerts[nb];
            avg /= n;
            newVerts[i] = (1 - alpha) * oldVerts[i] + alpha * avg;
        }

        debugVertices = newVerts;
        debugFaces = finalFaces;

        return new Mesh3D(newVerts, finalFaces);
    }


    Mesh ToUnityMesh(Mesh3D mesh)
    {
        Mesh m = new Mesh();
        m.name = "KobbeltSubdividedMesh";
        m.SetVertices(mesh.vertices);

        List<int> tris = new List<int>();
        foreach (var face in mesh.faces)
        {
            if (face.Count == 3)
            {
                tris.Add(face[0]);
                tris.Add(face[1]);
                tris.Add(face[2]);
            }
        }
        m.SetTriangles(tris, 0);

        // UVs simples: proyectar en plano XY
        Vector2[] uvs = new Vector2[mesh.vertices.Count];
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            Vector3 v = mesh.vertices[i];
            uvs[i] = new Vector2(v.x + 0.5f, v.y + 0.5f); // offset simple
        }
        m.uv = uvs;

        m.RecalculateNormals();  // Necesario para iluminación difusa y especular
        m.RecalculateBounds();
        return m;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || debugVertices == null || debugFaces == null) return;

        Gizmos.color = Color.yellow;
        foreach (var face in debugFaces)
        {
            if (face.Count >= 3)
            {
                for (int i = 0; i < face.Count; i++)
                {
                    Vector3 a = debugVertices[face[i]];
                    Vector3 b = debugVertices[face[(i + 1) % face.Count]];
                    Gizmos.DrawLine(transform.TransformPoint(a), transform.TransformPoint(b));
                }
            }
        }

        Gizmos.color = Color.red;
        if (debugCenters != null)
        {
            foreach (var c in debugCenters)
                Gizmos.DrawSphere(transform.TransformPoint(c), 0.01f);
        }

        Gizmos.color = Color.cyan;
        if (debugFlipConnections != null)
        {
            foreach (var pair in debugFlipConnections)
                Gizmos.DrawLine(transform.TransformPoint(pair.Item1), transform.TransformPoint(pair.Item2));
        }
    }
}