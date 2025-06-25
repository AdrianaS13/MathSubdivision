using System.Collections.Generic;
using UnityEngine;

public class SqrtKobbeltSubdivision : MonoBehaviour
{
    [Range(0, 7)] public int subdivisionLevel2 = 0;
    public Material meshMaterial;
    public bool showDebugGizmos = true;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int lastSubdivision = -1;

    private List<Vector3> debugVertices;
    private List<List<int>> debugFaces;
    private List<(Vector3, Vector3)> debugFlipConnections;
    private List<Vector3> debugCenters;

    private class MeshData
    {
        public List<Vector3> vertices;
        public List<List<int>> faces;

        public MeshData(List<Vector3> v, List<List<int>> f)
        {
            vertices = v;
            faces = f;
        }
    }

    private class Edge
    {
        public int v1, v2;
        public List<int> adjacentFaceIndices = new List<int>();

        public Edge(int a, int b, int faceIndex)
        {
            v1 = Mathf.Min(a, b);
            v2 = Mathf.Max(a, b);
            adjacentFaceIndices.Add(faceIndex);
        }

        public bool Matches(int a, int b)
        {
            return v1 == Mathf.Min(a, b) && v2 == Mathf.Max(a, b);
        }
    }

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
        MeshData mesh = CreateTriangulatedCube();
        for (int i = 0; i < subdivisionLevel2; i++)
            mesh = ApplySqrt3Subdivision(mesh);
        meshFilter.mesh = ToUnityMesh(mesh);
    }

    MeshData CreateTriangulatedCube()
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

        return new MeshData(v, f);
    }

    // Función auxiliar para verificar orientación de triángulo
    bool IsTriangleCounterClockwise(Vector3 a, Vector3 b, Vector3 c, Vector3 viewDir)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        return Vector3.Dot(normal, viewDir) > 0;
    }

    // Función para determinar la orientación correcta de un triángulo flip
    void CreateFlipTriangles(List<List<int>> finalFaces, List<Vector3> vertices, int c1, int c2, int v1, int v2, List<int> face1, List<int> face2)
    {
        // Calcular normales de las caras originales para determinar orientación correcta
        Vector3 normal1 = Vector3.Cross(vertices[face1[1]] - vertices[face1[0]], vertices[face1[2]] - vertices[face1[0]]).normalized;
        Vector3 normal2 = Vector3.Cross(vertices[face2[1]] - vertices[face2[0]], vertices[face2[2]] - vertices[face2[0]]).normalized;

        // Punto medio de la arista compartida
        Vector3 edgeMidpoint = (vertices[v1] + vertices[v2]) * 0.5f;
        Vector3 center1Pos = vertices[c1];
        Vector3 center2Pos = vertices[c2];

        // Vector desde el punto medio hacia cada centro
        Vector3 toCenter1 = (center1Pos - edgeMidpoint).normalized;
        Vector3 toCenter2 = (center2Pos - edgeMidpoint).normalized;

        // Determinar orientación basada en las normales originales
        // Primer triángulo: c1, v1, c2 o c1, c2, v1
        Vector3 testNormal1 = Vector3.Cross(vertices[v1] - center1Pos, center2Pos - center1Pos);
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

    MeshData ApplySqrt3Subdivision(MeshData input)
    {
        var oldVerts = input.vertices;
        var oldFaces = input.faces;

        var newVerts = new List<Vector3>(oldVerts);
        var faceCenters = new List<int>();
        var edgeList = new List<Edge>();
        var debugCentersTemp = new List<Vector3>();

        // Étape 1 : calculer les centres de chaque triangle
        for (int i = 0; i < oldFaces.Count; i++)
        {
            var f = oldFaces[i];
            Vector3 center = (oldVerts[f[0]] + oldVerts[f[1]] + oldVerts[f[2]]) / 3f;
            newVerts.Add(center);
            faceCenters.Add(newVerts.Count - 1);
            debugCentersTemp.Add(center);
        }

        // Étape 2 : construire la liste des arêtes
        for (int i = 0; i < oldFaces.Count; i++)
        {
            var face = oldFaces[i];
            for (int j = 0; j < 3; j++)
            {
                int a = face[j];
                int b = face[(j + 1) % 3];
                bool found = false;
                foreach (var edge in edgeList)
                {
                    if (edge.Matches(a, b))
                    {
                        edge.adjacentFaceIndices.Add(i);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    edgeList.Add(new Edge(a, b, i));
            }
        }

        var finalFaces = new List<List<int>>();
        var flipLines = new List<(Vector3, Vector3)>();

        // Étape 3 : ajouter les faces de flip (centres connectés) con orientación correcta
        foreach (var edge in edgeList)
        {
            if (edge.adjacentFaceIndices.Count == 2)
            {
                int f1 = edge.adjacentFaceIndices[0];
                int f2 = edge.adjacentFaceIndices[1];
                int c1 = faceCenters[f1];
                int c2 = faceCenters[f2];
                int v1 = edge.v1;
                int v2 = edge.v2;

                CreateFlipTriangles(finalFaces, newVerts, c1, c2, v1, v2, oldFaces[f1], oldFaces[f2]);

                flipLines.Add((newVerts[c1], newVerts[c2]));
            }
        }

        // Étape 4 : ajouter les 3 nouveaux triangles par triangle original
        

        // Étape 5 : lissage des sommets originaux
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

        // Débogage visuel
        debugVertices = newVerts;
        debugFaces = finalFaces;
        debugCenters = debugCentersTemp;
        debugFlipConnections = flipLines;

        return new MeshData(newVerts, finalFaces);
    }

    Mesh ToUnityMesh(MeshData mesh)
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