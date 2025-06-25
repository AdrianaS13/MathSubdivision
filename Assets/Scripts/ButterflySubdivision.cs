using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ButterflySubdivision : MonoBehaviour
{
    [Header("Butterfly Subdivision Settings")]
    public int subdivisionLevels = 1;
    public Material butterflyMaterial;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private GameObject currentSubdividedMesh;
    private Mesh originalCoonMesh;

    // Mesh topology data structures
    public class HalfEdge
    {
        public int vertex; // The vertex this half-edge points to
        public int face; // The face this half-edge belongs to
        public HalfEdge next; // Next half-edge in the face
        public HalfEdge opposite; // Opposite half-edge
        public Vector3 newPoint; // New point for butterfly subdivision
    }

    public class Face
    {
        public int[] vertices = new int[3]; // Triangle vertices
        public HalfEdge[] halfEdges = new HalfEdge[3]; // Half-edges of this face

        public Face(int v0, int v1, int v2)
        {
            vertices[0] = v0;
            vertices[1] = v1;
            vertices[2] = v2;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ApplyButterflySubdivisionToCoonPatch();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestoreOriginalCoonPatch();
        }
    }

    [ContextMenu("Apply Butterfly Subdivision to Coon Patch")]
    public void ApplyButterflySubdivisionToCoonPatch()
    {
        GameObject coonPatch = GameObject.Find("CoonPatch");
        if (coonPatch == null)
        {
            Debug.LogError("No Coon patch found! Generate a Coon patch first.");
            return;
        }

        MeshFilter meshFilter = coonPatch.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Coon patch has no mesh!");
            return;
        }

        if (originalCoonMesh == null)
        {
            originalCoonMesh = meshFilter.mesh;
        }

        Mesh subdividedMesh = ApplyButterflySubdivision(originalCoonMesh, subdivisionLevels);
        UpdateCoonPatchMesh(subdividedMesh);
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

    public Mesh ApplyButterflySubdivision(Mesh originalMesh, int levels)
    {
        Mesh currentMesh = originalMesh;

        for (int level = 0; level < levels; level++)
        {
            currentMesh = PerformOneButterflySubdivision(currentMesh);
            Debug.Log($"Butterfly subdivision level {level + 1} completed. Vertices: {currentMesh.vertexCount}");
        }

        return currentMesh;
    }

    Mesh PerformOneButterflySubdivision(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        Debug.Log($"Starting Butterfly subdivision on mesh with {vertices.Length} vertices, {triangles.Length / 3} triangles");

        // Build half-edge data structure
        List<Face> faces = new List<Face>();
        Dictionary<(int, int), HalfEdge> halfEdgeMap = new Dictionary<(int, int), HalfEdge>();

        BuildHalfEdgeStructure(vertices, triangles, faces, halfEdgeMap);

        // Compute new edge points using proper butterfly stencil
        foreach (var halfEdge in halfEdgeMap.Values)
        {
            if (halfEdge.newPoint == Vector3.zero) // Only compute once per edge
            {
                Vector3 newPoint = ComputeButterflyPoint(halfEdge, vertices, faces);
                halfEdge.newPoint = newPoint;
                if (halfEdge.opposite != null)
                {
                    halfEdge.opposite.newPoint = newPoint;
                }
            }
        }

        // Generate subdivided mesh
        return GenerateSubdividedMesh(vertices, faces, halfEdgeMap, uvs);
    }

    void BuildHalfEdgeStructure(Vector3[] vertices, int[] triangles, List<Face> faces, Dictionary<(int, int), HalfEdge> halfEdgeMap)
    {
        // Create faces and half-edges
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            Face face = new Face(v0, v1, v2);
            faces.Add(face);
            int faceIndex = faces.Count - 1;

            // Create three half-edges for this face
            HalfEdge he0 = GetOrCreateHalfEdge(v0, v1, faceIndex, halfEdgeMap);
            HalfEdge he1 = GetOrCreateHalfEdge(v1, v2, faceIndex, halfEdgeMap);
            HalfEdge he2 = GetOrCreateHalfEdge(v2, v0, faceIndex, halfEdgeMap);

            // Set next pointers
            he0.next = he1;
            he1.next = he2;
            he2.next = he0;

            // Store in face
            face.halfEdges[0] = he0;
            face.halfEdges[1] = he1;
            face.halfEdges[2] = he2;
        }

        // Set opposite pointers
        foreach (var kvp in halfEdgeMap)
        {
            var (from, to) = kvp.Key;
            var halfEdge = kvp.Value;

            if (halfEdgeMap.TryGetValue((to, from), out HalfEdge opposite))
            {
                halfEdge.opposite = opposite;
                opposite.opposite = halfEdge;
            }
        }

        Debug.Log($"Built half-edge structure: {faces.Count} faces, {halfEdgeMap.Count} half-edges");
    }

    HalfEdge GetOrCreateHalfEdge(int from, int to, int faceIndex, Dictionary<(int, int), HalfEdge> halfEdgeMap)
    {
        var key = (from, to);
        if (!halfEdgeMap.ContainsKey(key))
        {
            halfEdgeMap[key] = new HalfEdge { vertex = to, face = faceIndex };
        }
        return halfEdgeMap[key];
    }

    Vector3 ComputeButterflyPoint(HalfEdge halfEdge, Vector3[] vertices, List<Face> faces)
    {
        // Get the edge vertices
        int startVertex = GetStartVertex(halfEdge, faces);
        int endVertex = halfEdge.vertex;

        Vector3 v1 = vertices[startVertex];
        Vector3 v2 = vertices[endVertex];

        // For boundary edges (no opposite), use midpoint
        if (halfEdge.opposite == null)
        {
            return (v1 + v2) * 0.5f;
        }

        // Get the butterfly stencil vertices
        ButterflyStencil stencil = GetButterflyStencil(halfEdge, vertices, faces);

        if (!stencil.isValid)
        {
            Debug.LogWarning($"Invalid butterfly stencil for edge {startVertex}-{endVertex}, using midpoint");
            return (v1 + v2) * 0.5f;
        }

        // Apply butterfly formula: 1/2(v1 + v2) + 1/8(opp1 + opp2) - 1/16(wing1 + wing2 + wing3 + wing4)
        Vector3 edgeCenter = 0.5f * (v1 + v2);
        Vector3 oppositeSum = 0.125f * (stencil.opposite1 + stencil.opposite2);
        Vector3 wingSum = -0.0625f * (stencil.wing1 + stencil.wing2 + stencil.wing3 + stencil.wing4);

        Vector3 result = edgeCenter + oppositeSum + wingSum;

        // Safety check
        Vector3 midpoint = (v1 + v2) * 0.5f;
        if (Vector3.Distance(result, midpoint) > Vector3.Distance(v1, v2))
        {
            Debug.LogWarning("Butterfly result too extreme, using midpoint");
            return midpoint;
        }

        return result;
    }

    public struct ButterflyStencil
    {
        public bool isValid;
        public Vector3 opposite1, opposite2; // The two vertices opposite to the edge
        public Vector3 wing1, wing2, wing3, wing4; // The four wing vertices
    }

    ButterflyStencil GetButterflyStencil(HalfEdge halfEdge, Vector3[] vertices, List<Face> faces)
    {
        ButterflyStencil stencil = new ButterflyStencil();

        // Get the two faces sharing this edge
        Face face1 = faces[halfEdge.face];
        Face face2 = faces[halfEdge.opposite.face];

        // Get edge vertices
        int edgeStart = GetStartVertex(halfEdge, faces);
        int edgeEnd = halfEdge.vertex;

        // Find opposite vertices in the two triangles
        int opposite1 = GetOppositeVertex(face1, edgeStart, edgeEnd);
        int opposite2 = GetOppositeVertex(face2, edgeStart, edgeEnd);

        if (opposite1 == -1 || opposite2 == -1)
        {
            return stencil; // Invalid
        }

        stencil.opposite1 = vertices[opposite1];
        stencil.opposite2 = vertices[opposite2];

        // Find wing vertices - this is the tricky part
        List<Vector3> wingVertices = FindWingVertices(edgeStart, edgeEnd, opposite1, opposite2, vertices, faces);

        if (wingVertices.Count >= 4)
        {
            stencil.wing1 = wingVertices[0];
            stencil.wing2 = wingVertices[1];
            stencil.wing3 = wingVertices[2];
            stencil.wing4 = wingVertices[3];
            stencil.isValid = true;
        }

        return stencil;
    }

    int GetStartVertex(HalfEdge halfEdge, List<Face> faces)
    {
        Face face = faces[halfEdge.face];
        for (int i = 0; i < 3; i++)
        {
            if (face.halfEdges[i] == halfEdge)
            {
                return face.vertices[i];
            }
        }
        return -1;
    }

    int GetOppositeVertex(Face face, int v1, int v2)
    {
        foreach (int vertex in face.vertices)
        {
            if (vertex != v1 && vertex != v2)
            {
                return vertex;
            }
        }
        return -1;
    }

    List<Vector3> FindWingVertices(int edgeStart, int edgeEnd, int opposite1, int opposite2, Vector3[] vertices, List<Face> faces)
    {
        List<Vector3> wings = new List<Vector3>();

        // Find triangles adjacent to the opposite vertices that form the wings

        HashSet<int> usedVertices = new HashSet<int> { edgeStart, edgeEnd, opposite1, opposite2 };

        // Find faces containing opposite1 but not the edge vertices
        foreach (Face face in faces)
        {
            if (face.vertices.Contains(opposite1) &&
                !face.vertices.Contains(edgeStart) &&
                !face.vertices.Contains(edgeEnd))
            {
                foreach (int vertex in face.vertices)
                {
                    if (!usedVertices.Contains(vertex))
                    {
                        wings.Add(vertices[vertex]);
                        usedVertices.Add(vertex);
                        if (wings.Count >= 2) break;
                    }
                }
                if (wings.Count >= 2) break;
            }
        }

        // Find faces containing opposite2 but not the edge vertices
        foreach (Face face in faces)
        {
            if (face.vertices.Contains(opposite2) &&
                !face.vertices.Contains(edgeStart) &&
                !face.vertices.Contains(edgeEnd))
            {
                foreach (int vertex in face.vertices)
                {
                    if (!usedVertices.Contains(vertex))
                    {
                        wings.Add(vertices[vertex]);
                        usedVertices.Add(vertex);
                        if (wings.Count >= 4) break;
                    }
                }
                if (wings.Count >= 4) break;
            }
        }

        return wings;
    }

    Mesh GenerateSubdividedMesh(Vector3[] originalVertices, List<Face> faces, Dictionary<(int, int), HalfEdge> halfEdgeMap, Vector2[] originalUVs)
    {
        Mesh newMesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector2> newUVs = new List<Vector2>();

        // Add original vertices (unchanged in interpolatory scheme)
        newVertices.AddRange(originalVertices);

        for (int i = 0; i < originalUVs.Length; i++)
        {
            newUVs.Add(originalUVs[i]);
        }

        // Add edge vertices and create mapping
        Dictionary<(int, int), int> edgeToVertexIndex = new Dictionary<(int, int), int>();

        foreach (var kvp in halfEdgeMap)
        {
            var (from, to) = kvp.Key;
            var halfEdge = kvp.Value;

            // Only add each edge once (avoid duplicates from opposite half-edges)
            var edgeKey = from < to ? (from, to) : (to, from);
            if (!edgeToVertexIndex.ContainsKey(edgeKey))
            {
                edgeToVertexIndex[edgeKey] = newVertices.Count;
                newVertices.Add(halfEdge.newPoint);

                //interpolate uv for edge points
                Vector2 uv1 = originalUVs[from];
                Vector2 uv2 = originalUVs[to];
                Vector2 interpolatedUV = (uv1 + uv2) * 0.5f;
                newUVs.Add(interpolatedUV);
            }

        }

        // Generate new triangles (1-to-4 subdivision)
        foreach (Face face in faces)
        {
            int v0 = face.vertices[0];
            int v1 = face.vertices[1];
            int v2 = face.vertices[2];

            // Get edge vertex indices
            int e01 = GetEdgeVertexIndex(v0, v1, edgeToVertexIndex);
            int e12 = GetEdgeVertexIndex(v1, v2, edgeToVertexIndex);
            int e20 = GetEdgeVertexIndex(v2, v0, edgeToVertexIndex);

            if (e01 == -1 || e12 == -1 || e20 == -1)
            {
                Debug.LogError("Failed to find edge vertex indices");
                continue;
            }

            // Create 4 new triangles
            AddTriangle(newTriangles, v0, e01, e20);  // Corner at v0
            AddTriangle(newTriangles, v1, e12, e01);  // Corner at v1
            AddTriangle(newTriangles, v2, e20, e12);  // Corner at v2
            AddTriangle(newTriangles, e01, e12, e20); // Center triangle
        }

        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        Debug.Log($"Generated butterfly mesh: {newMesh.vertexCount} vertices, {newMesh.triangles.Length / 3} triangles");

        return newMesh;
    }

    int GetEdgeVertexIndex(int v1, int v2, Dictionary<(int, int), int> edgeToVertexIndex)
    {
        var edgeKey = v1 < v2 ? (v1, v2) : (v2, v1);
        return edgeToVertexIndex.TryGetValue(edgeKey, out int index) ? index : -1;
    }

    void AddTriangle(List<int> triangles, int v1, int v2, int v3)
    {
        triangles.Add(v1);
        triangles.Add(v2);
        triangles.Add(v3);
    }

    void UpdateCoonPatchMesh(Mesh mesh)
    {
        GameObject coonPatch = GameObject.Find("CoonPatch");
        if (coonPatch != null)
        {
            MeshFilter meshFilter = coonPatch.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;

                MeshCollider meshCollider = coonPatch.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = mesh;
                }

                Debug.Log($"Butterfly subdivision applied! Mesh now has {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(630, 10, 300, 200));

        GUILayout.Label($"Subdivision Levels: {subdivisionLevels}");
        subdivisionLevels = (int)GUILayout.HorizontalSlider(subdivisionLevels, 1, 3);

        if (GUILayout.Button("Apply Butterfly Subdivision"))
        {
            ApplyButterflySubdivisionToCoonPatch();
        }

        if (GUILayout.Button("Restore Original Mesh"))
        {
            RestoreOriginalCoonPatch();
        }
        GUILayout.EndArea();
    }
}