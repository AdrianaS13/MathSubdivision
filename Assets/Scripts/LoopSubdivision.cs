using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LoopSubdivision : MonoBehaviour
{
    [Header("Loop Subdivision Settings")]
    public int subdivisionLevels = 1;
    public Material subdivisionMaterial;

    [Header("Selection")]
    public bool isSelectionModeActive = false;
    public Material highlightMaterial;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private GameObject currentSubdividedMesh;
    private Mesh originalCoonMesh;
    private Camera playerCamera;
    private GameObject currentHighlightedObject;
    private Material originalMaterial;

    private Dictionary<string, Mesh> originalMeshes = new Dictionary<string, Mesh>();

    public class Edge
    {
        public int vertex1, vertex2;
        public List<int> adjacentTriangles = new List<int>();
        public Vector3 newPoint;

        public Edge(int v1, int v2)
        {
            vertex1 = Mathf.Min(v1, v2);
            vertex2 = Mathf.Max(v1, v2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
                return vertex1 == other.vertex1 && vertex2 == other.vertex2;
            return false;
        }

        public override int GetHashCode()
        {
            return vertex1.GetHashCode() ^ vertex2.GetHashCode();
        }
    }

    public class Vertex
    {
        public Vector3 position;
        public Vector3 newPosition;
        public List<int> adjacentVertices = new List<int>();
        public int valency => adjacentVertices.Count;

        public Vertex(Vector3 pos)
        {
            position = pos;
        }
    }

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ApplyLoopSubdivisionToCoonPatch();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestoreOriginalCoonPatch();
        }
        if (isSelectionModeActive)
        {
            HandleObjectSelection();
        }
    }

    public void StartObjectSelection()
    {
        isSelectionModeActive = true;
        Debug.Log("Click on an object to apply loop subdivision. Press ESC to cancel.");
    }

    void HandleObjectSelection()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSelection();
            return;
        }
        HandleMouseHover();

        if (Input.GetMouseButtonDown(0))
        {
            SelectObjectAtMousePosition();
        }
    }

    void HandleMouseHover()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hoveredObject = hit.collider.gameObject;

            if (CanObjectBeSubdivided(hoveredObject))
            {
                if (currentHighlightedObject != hoveredObject)
                {
                    RemoveHighlight();

                    HighlightObject(hoveredObject);
                }
            }
            else
                RemoveHighlight();
        }
        else
            RemoveHighlight();
    }

    void SelectObjectAtMousePosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject selectedObject = hit.collider.gameObject;

            if (CanObjectBeSubdivided(selectedObject))
            {
                ApplyLoopSubdivisionToObject(selectedObject);
                CancelSelection();
            }
            else
            {
                Debug.LogWarning($"Selected object '{selectedObject.name}' cannot be subdivided (no MeshFilter or Mesh found).");
            }
        }
    }

    bool CanObjectBeSubdivided(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        return meshFilter != null && meshFilter.mesh != null;
    }

    void ApplyLoopSubdivisionToObject(GameObject targetObject)
    {
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError($"Selected object '{targetObject.name}' has no mesh!");
            return;
        }

        string objectKey = targetObject.name;
        if (!originalMeshes.ContainsKey(objectKey))
        {
            originalMeshes[objectKey] = meshFilter.mesh;
        }

        Debug.Log($"Applying loop subdivision to '{targetObject.name}'");

        Mesh subdividedMesh = ApplyLoopSubdivision(meshFilter.mesh, subdivisionLevels);
        CreateSubdividedMeshObject(subdividedMesh, targetObject);
    }

    void HighlightObject(GameObject obj)
    {
        if (highlightMaterial == null) return;

        currentHighlightedObject = obj;
        Renderer renderer = obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            originalMaterial = renderer.material;
            renderer.material = highlightMaterial;
        }
    }

    void RemoveHighlight()
    {
        if (currentHighlightedObject != null && originalMaterial != null)
        {
            Renderer renderer = currentHighlightedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = originalMaterial;
            }
        }

        currentHighlightedObject = null;
        originalMaterial = null;
    }

    void CancelSelection()
    {
        isSelectionModeActive = false;
        RemoveHighlight();
        Debug.Log("Object selection cancelled.");
    }

    [ContextMenu("Apply Loop Subdivision to Coon Patch")]
    public void ApplyLoopSubdivisionToCoonPatch()
    {
        GameObject coonPatch = GameObject.Find("CoonPatch");
        if (coonPatch == null)
        {
            Debug.LogError("No Coon patch found! Generate a Coon patch first.");
            return;
        }

        MeshFilter CoonMeshFilter = coonPatch.GetComponent<MeshFilter>();
        if (CoonMeshFilter == null || CoonMeshFilter.mesh == null)
        {
            Debug.LogError("Coon patch has no mesh!");
            return;
        }

        if (originalCoonMesh == null)
        {
            originalCoonMesh = CoonMeshFilter.mesh;
        }

        Mesh subdividedMesh = ApplyLoopSubdivision(originalCoonMesh, subdivisionLevels);

        CreateSubdividedMeshObject(subdividedMesh, coonPatch);
    }

    [ContextMenu("Restore Original Coon Patch")]
    public void RestoreOriginalCoonPatch()
    {
        GameObject coonPatch = GameObject.Find("CoonPatch");
        if (coonPatch != null && originalCoonMesh != null)
        {
            MeshFilter meshFilter = coonPatch.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = originalCoonMesh;

                MeshCollider meshCollider = coonPatch.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = originalCoonMesh;
                }

                Debug.Log("Restored original Coon patch mesh.");
            }
        }
    }

    public void RestoreOriginalMesh(GameObject targetObject)
    {
        string objectKey = targetObject.name;
        if (originalMeshes.ContainsKey(objectKey))
        {
            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = originalMeshes[objectKey];

                MeshCollider meshCollider = targetObject.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = originalMeshes[objectKey];
                }

                Debug.Log($"Restored original mesh for '{targetObject.name}'.");
            }
        }
        else
        {
            Debug.LogWarning($"No original mesh stored for '{targetObject.name}'.");
        }
    }

    public Mesh ApplyLoopSubdivision(Mesh originalMesh, int levels)
    {
        Mesh currentMesh = originalMesh;

        for (int level = 0; level < levels; level++)
        {
            currentMesh = PerformOneLoopSubdivision(currentMesh);
        }

        return currentMesh;
    }

    Mesh PerformOneLoopSubdivision(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        List<Vertex> vertexList = new List<Vertex>();
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexList.Add(new Vertex(vertices[i]));
        }

        Dictionary<Edge, Edge> edgeDict = new Dictionary<Edge, Edge>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            AddAdjacency(vertexList, v1, v2);
            AddAdjacency(vertexList, v2, v3);
            AddAdjacency(vertexList, v3, v1);

            AddEdge(edgeDict, v1, v2, i / 3);
            AddEdge(edgeDict, v2, v3, i / 3);
            AddEdge(edgeDict, v3, v1, i / 3);
        }

        foreach (var edge in edgeDict.Values)
        {
            edge.newPoint = ComputeNewEdgePoint(edge, vertexList, triangles);
        }

        foreach (var vertex in vertexList)
        {
            vertex.newPosition = ComputeNewVertexPoint(vertex, vertexList);
        }

        return GenerateSubdividedMesh(vertexList, edgeDict, triangles, uvs);
    }

    void AddAdjacency(List<Vertex> vertices, int v1, int v2)
    {
        if (!vertices[v1].adjacentVertices.Contains(v2))
            vertices[v1].adjacentVertices.Add(v2);
        if (!vertices[v2].adjacentVertices.Contains(v1))
            vertices[v2].adjacentVertices.Add(v1);
    }

    void AddEdge(Dictionary<Edge, Edge> edgeDict, int v1, int v2, int triangleIndex)
    {
        Edge edge = new Edge(v1, v2);
        if (edgeDict.ContainsKey(edge))
        {
            edgeDict[edge].adjacentTriangles.Add(triangleIndex);
        }
        else
        {
            edge.adjacentTriangles.Add(triangleIndex);
            edgeDict[edge] = edge;
        }
    }

    Vector3 ComputeNewEdgePoint(Edge edge, List<Vertex> vertices, int[] triangles)
    {
        Vector3 v1 = vertices[edge.vertex1].position;
        Vector3 v2 = vertices[edge.vertex2].position;

        if (edge.adjacentTriangles.Count != 2)
        {
            return (v1 + v2) * 0.5f;
        }

        Vector3 vleft = Vector3.zero, vright = Vector3.zero;
        bool foundLeft = false;

        foreach (int triIndex in edge.adjacentTriangles)
        {
            int baseIndex = triIndex * 3;
            for (int i = 0; i < 3; i++)
            {
                int vertexIndex = triangles[baseIndex + i];
                if (vertexIndex != edge.vertex1 && vertexIndex != edge.vertex2)
                {
                    if (!foundLeft)
                    {
                        vleft = vertices[vertexIndex].position;
                        foundLeft = true;
                    }
                    else
                    {
                        vright = vertices[vertexIndex].position;
                    }
                    break;
                }
            }
        }

        //e = (3/8)(v1 + v2) + (1/8)(vleft + vright)
        return (3.0f / 8.0f) * (v1 + v2) + (1.0f / 8.0f) * (vleft + vright);
    }

    Vector3 ComputeNewVertexPoint(Vertex vertex, List<Vertex> allVertices)
    {
        int n = vertex.valency;

        if (n == 0) return vertex.position;

        float alpha;
        if (n == 3)
        {
            alpha = 3.0f / 16.0f;
        } else
        {
            // alpha = (1/n)[5/8 - (3/8 + 1/4 * cos(2pi/n))^2]
            float cosValue = Mathf.Cos(2.0f * Mathf.PI / n);
            float term = 3.0f / 8.0f + 0.25f * cosValue;
            alpha = (1.0f / n) * (5.0f / 8.0f - term * term);
        }

        Vector3 adjacentSum = Vector3.zero;
        foreach (int adjIndex in vertex.adjacentVertices)
        {
            adjacentSum += allVertices[adjIndex].position;
        }

        //v' = (1 - n.alpha)v + alpha * sum(adjacent vertices)
        return (1.0f - n * alpha) * vertex.position + alpha * adjacentSum;
    }

    Mesh GenerateSubdividedMesh(List<Vertex> vertices, Dictionary<Edge, Edge> edges, int[] originalTriangles, Vector2[] originalUVs)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector2> newUVs = new List<Vector2>();

        foreach (var vertex in vertices)
        {
            newVertices.Add(vertex.newPosition);
        }

        for (int i = 0; i < originalUVs.Length; i++)
        {
            newUVs.Add(originalUVs[i]);
        }

        // Add edge vertices
        Dictionary<Edge, int> edgeToNewVertexIndex = new Dictionary<Edge, int>();
        foreach (var edge in edges.Values)
        {
            edgeToNewVertexIndex[edge] = newVertices.Count;
            newVertices.Add(edge.newPoint);

            // Interpolate UV coordinates for edge points
            Vector2 uv1 = originalUVs[edge.vertex1];
            Vector2 uv2 = originalUVs[edge.vertex2];
            Vector2 interpolatedUV = (uv1 + uv2) * 0.5f;
            newUVs.Add(interpolatedUV);
        }

        // Generate new triangles
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            int v1 = originalTriangles[i];
            int v2 = originalTriangles[i + 1];
            int v3 = originalTriangles[i + 2];

            // Get edge vertex indices
            int e1 = edgeToNewVertexIndex[new Edge(v2, v3)]; //opposé à v1
            int e2 = edgeToNewVertexIndex[new Edge(v3, v1)]; //opposé à v2
            int e3 = edgeToNewVertexIndex[new Edge(v1, v2)]; //opposé à v3

            //créer les 4 triangles
            AddTriangle(newTriangles, v1, e3, e2);
            AddTriangle(newTriangles, v2, e1, e3);
            AddTriangle(newTriangles, v3, e2, e1);
            AddTriangle(newTriangles, e1, e2, e3); //triangle au centre
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        return newMesh;
    }

    void AddTriangle(List<int> triangles, int v1, int v2, int v3)
    {
        triangles.Add(v1);
        triangles.Add(v2);
        triangles.Add(v3);
    }

    void CreateSubdividedMeshObject(Mesh mesh, GameObject targetObject = null)
    {
        if (targetObject != null)
        {
            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;

                MeshCollider meshCollider = targetObject.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = mesh;
                }

                Debug.Log($"{targetObject.name} smoothed with Loop subdivision! Mesh now has {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
                return;
            }
        }

        GameObject coonPatch = GameObject.Find("CoonPatch");
        if (coonPatch != null)
        {
            MeshFilter meshFilter = coonPatch.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // Update the existing mesh
                meshFilter.mesh = mesh;

                // Update collider if it exists
                MeshCollider meshCollider = coonPatch.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = mesh;
                }

                Debug.Log($"Coon patch smoothed with Loop subdivision! Mesh now has {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
                return;
            }
        }

        Debug.Log($"New mesh has {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(320, 10, 300, 250));

        GUILayout.Label($"Subdivision Levels: {subdivisionLevels}");
        subdivisionLevels = (int)GUILayout.HorizontalSlider(subdivisionLevels, 1, 4);

        if (GUILayout.Button("Apply Loop Subdivision (Smooth)"))
        {
            ApplyLoopSubdivisionToCoonPatch();
        }

        if (GUILayout.Button("Restore Original Mesh"))
        {
            RestoreOriginalCoonPatch();
        }

        GUILayout.Space(10);

        if (isSelectionModeActive)
        {
            GUI.color = Color.yellow;
            if (GUILayout.Button("Cancel Selection (ESC)"))
            {
                CancelSelection();
            }
            GUI.color = Color.white;
            GUILayout.Label("Click on an object to subdivide it");
        }
        else
        {
            GUI.color = Color.green;
            if (GUILayout.Button("Select Object to Subdivide"))
            {
                StartObjectSelection();
            }
            GUI.color = Color.white;
        }

        GUILayout.EndArea();
    }
}