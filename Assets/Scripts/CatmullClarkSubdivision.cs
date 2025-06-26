using System.Collections.Generic;
using UnityEngine;

public class CatmullClarkSubdivision : MonoBehaviour
{
    [Header("Subdivision Settings")]
    [Range(0, 7)]
    public int subdivisionLevel = 0;

    [Header("Visualization")]
    public Material cubeMaterial;
    public bool showWireframe = true;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private int currentSubdivisionLevel = -1;
    private Mesh3D currentMesh3D;

    //private struct Mesh3D
    //{
    //    public List<Vector3> vertices;
    //    public List<List<int>> faces;

    //    public Mesh3D(List<Vector3> v, List<List<int>> f)
    //    {
    //        vertices = v;
    //        faces = f;
    //    }
    //}

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        if (cubeMaterial != null) meshRenderer.material = cubeMaterial;

        UpdateMesh();
    }

    void Update()
    {
        if (currentSubdivisionLevel != subdivisionLevel)
        {
            UpdateMesh();
        }
    }

    void UpdateMesh()
    {
        currentSubdivisionLevel = subdivisionLevel;

        Mesh3D mesh = CreateBaseCube();

        for (int i = 0; i < subdivisionLevel; i++)
        {
            mesh = ApplyCatmullClark(mesh);
        }

        //meshFilter.mesh = MeshConverter.ToUnityMeshFlatShading(mesh); 
        meshFilter.mesh = ToUnityMesh(mesh);
        currentMesh3D = mesh;
    }

    Mesh3D CreateBaseCube()
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

    public Mesh3D ApplyCatmullClark(Mesh3D inputMesh)
    {
        var newVertices = new List<Vector3>();
        var newFaces = new List<List<int>>();

        // 1. Face points
        var facePoints = new List<Vector3>();
        for (int i = 0; i < inputMesh.faces.Count; i++)
        {
            var face = inputMesh.faces[i];
            Vector3 center = Vector3.zero;
            foreach (var vi in face)
                center += inputMesh.vertices[vi];
            center /= face.Count;
            facePoints.Add(center);
        }

        // 2. Edge points
        var edgeList = new List<int[]>();         // [v1, v2]
        var edgeFaces = new List<List<int>>();    // cada arista lista de caras que la comparten
        var edgePoints = new List<Vector3>();
        // Detectar aristas
        for (int f = 0; f < inputMesh.faces.Count; f++)
        {
            var face = inputMesh.faces[f];
            for (int i = 0; i < face.Count; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % face.Count];//cierra la cara

                bool exists = false;
                for (int e = 0; e < edgeList.Count; e++)
                {
                    var edge = edgeList[e];
                    if ((edge[0] == a && edge[1] == b) || (edge[0] == b && edge[1] == a))
                    {
                        edgeFaces[e].Add(f);//guarda cara adyacente
                        exists = true;
                        break;
                    }
                }
                //guarda aristas y caras nuevas
                if (!exists)
                {
                    edgeList.Add(new int[] { a, b });
                    edgeFaces.Add(new List<int> { f });
                }
            }
        }
        //calculo de edge point
        for (int i = 0; i < edgeList.Count; i++)
        {
            int a = edgeList[i][0];
            int b = edgeList[i][1];
            Vector3 mid = (inputMesh.vertices[a] + inputMesh.vertices[b]) * 0.5f;//promedio vertices aristas

            if (edgeFaces[i].Count == 2)
            {
                Vector3 faceAvg = (facePoints[edgeFaces[i][0]] + facePoints[edgeFaces[i][1]]) * 0.5f;//promedio face points
                edgePoints.Add((mid + faceAvg) * 0.5f);// edge point
            }
            else
            {
                edgePoints.Add(mid);
            }
        }

        // 3. Vertex points
        var newVertexPoints = new List<Vector3>();
        for (int i = 0; i < inputMesh.vertices.Count; i++)
        {
            Vector3 orig = inputMesh.vertices[i];

            var adjacentFaces = new List<int>();
            var connectedEdges = new List<int>();

            for (int f = 0; f < inputMesh.faces.Count; f++)
            {
                if (inputMesh.faces[f].Contains(i))
                    adjacentFaces.Add(f);
            }

            for (int e = 0; e < edgeList.Count; e++)
            {
                if (edgeList[e][0] == i || edgeList[e][1] == i)
                    connectedEdges.Add(e);
            }

            Vector3 Q = Vector3.zero;
            foreach (int fi in adjacentFaces)
                Q += facePoints[fi];
            Q /= adjacentFaces.Count;

            Vector3 R = Vector3.zero;
            foreach (int ei in connectedEdges)
                R += edgePoints[ei];
            R /= connectedEdges.Count;

            int n = adjacentFaces.Count;
            Vector3 newV = (Q + 2 * R + (n - 3) * orig) / n;
            newVertexPoints.Add(newV);
        }

        // 4. Construir nueva malla
        int faceOffset = newVertices.Count;
        newVertices.AddRange(newVertexPoints);   // Original (modificados)
        int edgeOffset = newVertices.Count;
        newVertices.AddRange(edgePoints);        // Edge
        int facePointOffset = newVertices.Count;
        newVertices.AddRange(facePoints);        // Face

        for (int f = 0; f < inputMesh.faces.Count; f++)
        {
            var face = inputMesh.faces[f];//recorre todas las caras
            int fpIndex = facePointOffset + f; // indice del face point

            for (int i = 0; i < face.Count; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % face.Count];//recorre las aristas 

                int edgeAB = -1;
                int edgePrev = -1;

                for (int e = 0; e < edgeList.Count; e++)
                {
                    var eAB = edgeList[e];
                    if ((eAB[0] == a && eAB[1] == b) || (eAB[0] == b && eAB[1] == a))
                        edgeAB = edgeOffset + e; //index edge point siguiente
                    if ((eAB[0] == face[(i - 1 + face.Count) % face.Count] && eAB[1] == a) ||
                        (eAB[1] == face[(i - 1 + face.Count) % face.Count] && eAB[0] == a))
                        edgePrev = edgeOffset + e; //index edge point anterior
                }

                int newA = faceOffset + a; // index nuevo vertice
                newFaces.Add(new List<int> { newA, edgeAB, fpIndex, edgePrev });
            }
        }

        return new Mesh3D(newVertices, newFaces);
    }

    Mesh ToUnityMesh(Mesh3D mesh3D)
    {
        var m = new Mesh();
        m.vertices = mesh3D.vertices.ToArray();
        // se divide cada cara en triangulos
        var tris = new List<int>();
        foreach (var face in mesh3D.faces)
        {
            if (face.Count == 4)
            {
                tris.Add(face[0]); tris.Add(face[1]); tris.Add(face[2]);
                tris.Add(face[0]); tris.Add(face[2]); tris.Add(face[3]);
            }
        }

        // UVs simples: proyectar en plano XY
        Vector2[] uvs = new Vector2[mesh3D.vertices.Count];
        for (int i = 0; i < mesh3D.vertices.Count; i++)
        {
            Vector3 v = mesh3D.vertices[i];
            uvs[i] = new Vector2(v.x + 0.5f, v.y + 0.5f); // offset simple
        }
        m.uv = uvs;

        m.triangles = tris.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
   
    void OnDrawGizmos()
    {
        if (!showWireframe || currentMesh3D.vertices == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        foreach (var face in currentMesh3D.faces)
        {
            for (int i = 0; i < face.Count; i++)
            {
                Vector3 a = currentMesh3D.vertices[face[i]];
                Vector3 b = currentMesh3D.vertices[face[(i + 1) % face.Count]];
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
