using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CoonPatchGenerator : MonoBehaviour
{
    [Header("Curve Selection")]
    public List<LineRenderer> selectedCurves = new List<LineRenderer>();

    [Header("Patch Settings")]
    public int patchResolution = 20;
    public Material patchMaterial;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private GameObject currentPatch;

    // Boundary curves following course notation
    private Vector3[] C1, C2; // Opposite curves (u direction)
    private Vector3[] d1, d2; // Opposite curves (v direction)

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectCurveWithMouse();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
        }
    }

    void SelectCurveWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.name.Contains("CoonPatch"))
            {
                Destroy(hitObject);
                return;
            }

            LineRenderer lr = hit.collider.GetComponent<LineRenderer>();
            if (lr != null)
            {
                if (selectedCurves.Contains(lr))
                {
                    selectedCurves.Remove(lr);
                    lr.startColor = Color.white;
                    lr.endColor = Color.white;
                }
                else if (selectedCurves.Count < 4)
                {
                    selectedCurves.Add(lr);
                    lr.startColor = Color.red;
                    lr.endColor = Color.red;
                }
            }
        }
    }

    void ClearSelection()
    {
        foreach (var curve in selectedCurves)
        {
            if (curve != null)
                curve.startColor = Color.white;
                curve.endColor = Color.white;
        }
        selectedCurves.Clear();
    }

    [ContextMenu("Generate Coon Patch")]
    public void GenerateCoonPatch()
    {
        if (selectedCurves.Count != 4)
        {
            Debug.LogError("You must select exactly 4 curves!");
            return;
        }

        if (!ValidateAndOrderCurves())
        {
            Debug.LogError("Curves don't form a proper closed loop!");
            return;
        }

        CreateCoonPatchMesh();
    }

    bool ValidateAndOrderCurves()
    {
        var curves = selectedCurves.Select(GetCurvePoints).ToArray();

        // Find curve connections by checking endpoints
        List<CurveConnection> connections = new List<CurveConnection>();
        float tolerance = 0.5f; // Increased tolerance

        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                var curve1 = curves[i];
                var curve2 = curves[j];

                // Check all possible endpoint connections
                if (Vector3.Distance(curve1[0], curve2[0]) < tolerance)
                    connections.Add(new CurveConnection(i, j, 0, 0));
                else if (Vector3.Distance(curve1[0], curve2[curve2.Length - 1]) < tolerance)
                    connections.Add(new CurveConnection(i, j, 0, 1));
                else if (Vector3.Distance(curve1[curve1.Length - 1], curve2[0]) < tolerance)
                    connections.Add(new CurveConnection(i, j, 1, 0));
                else if (Vector3.Distance(curve1[curve1.Length - 1], curve2[curve2.Length - 1]) < tolerance)
                    connections.Add(new CurveConnection(i, j, 1, 1));
            }
        }

        if (connections.Count != 4)
        {
            Debug.LogError($"Found {connections.Count} connections, need exactly 4 for a closed loop");
            return false;
        }

        // Order curves properly - find a starting curve and follow the loop
        List<int> orderedIndices = new List<int>();
        orderedIndices.Add(0); // Start with first curve

        int currentCurve = 0;
        int currentEnd = 1; // Start from end of first curve

        for (int step = 0; step < 3; step++)
        {
            // Find next connected curve
            var connection = connections.Find(c =>
                (c.curve1 == currentCurve && c.end1 == currentEnd) ||
                (c.curve2 == currentCurve && c.end2 == currentEnd));

            if (connection == null) break;

            // Determine next curve and which end to use
            if (connection.curve1 == currentCurve)
            {
                currentCurve = connection.curve2;
                currentEnd = 1 - connection.end2; // Use opposite end
            }
            else
            {
                currentCurve = connection.curve1;
                currentEnd = 1 - connection.end1; // Use opposite end
            }

            orderedIndices.Add(currentCurve);
        }

        // Assign curves in proper order
        C1 = curves[orderedIndices[0]]; // Bottom
        d2 = curves[orderedIndices[1]]; // Right  
        C2 = curves[orderedIndices[2]]; // Top
        d1 = curves[orderedIndices[3]]; // Left

        // Reverse C2 and d1 to match parameter directions
        C2 = ReverseArray(C2);
        d1 = ReverseArray(d1);

        return true;
    }

    class CurveConnection
    {
        public int curve1, curve2, end1, end2;
        public CurveConnection(int c1, int c2, int e1, int e2)
        {
            curve1 = c1; curve2 = c2; end1 = e1; end2 = e2;
        }
    }

    Vector3[] ReverseArray(Vector3[] array)
    {
        Vector3[] reversed = new Vector3[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            reversed[i] = array[array.Length - 1 - i];
        }
        return reversed;
    }

    void CreateCoonPatchMesh()
    {
        if (currentPatch != null)
        {
            DestroyImmediate(currentPatch);
        }

        currentPatch = new GameObject("CoonPatch");
        MeshFilter meshFilter = currentPatch.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = currentPatch.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = currentPatch.AddComponent<MeshCollider>();

        if (patchMaterial != null)
            meshRenderer.material = patchMaterial;

        Mesh mesh = GenerateCoonPatchMesh();
        meshFilter.mesh = mesh;

        Debug.Log("Coon patch generated!");
    }

    Mesh GenerateCoonPatchMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i <= patchResolution; i++)
        {
            for (int j = 0; j <= patchResolution; j++)
            {
                float u = (float)i / patchResolution;
                float v = (float)j / patchResolution;

                // Following course formula: S(u,v) = rc(u,v) + rd(u,v) - rcd(u,v)
                Vector3 point = CalculateCoonPatch(u, v);
                vertices.Add(point);
                uvs.Add(new Vector2(u, v));

                if (i < patchResolution && j < patchResolution)
                {
                    int current = i * (patchResolution + 1) + j;
                    int next = current + patchResolution + 1;

                    triangles.Add(current);
                    triangles.Add(current + 1);
                    triangles.Add(next);

                    triangles.Add(current + 1);
                    triangles.Add(next + 1);
                    triangles.Add(next);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    Vector3 CalculateCoonPatch(float u, float v)
    {
        // Basic linear blending functions from course: f1(u) = 1-u, f2(u) = u
        float f1_u = 1 - u;  // f1(u) = 1-u
        float f2_u = u;      // f2(u) = u
        float g1_v = 1 - v;  // g1(v) = 1-v  
        float g2_v = v;      // g2(v) = v

        // Sample boundary curves at parameter values
        Vector3 C1_u = SampleCurve(C1, u);     // Bottom curve S(u,0)
        Vector3 C2_u = SampleCurve(C2, u);     // Top curve S(u,1)
        Vector3 d1_v = SampleCurve(d1, v);     // Left curve S(0,v)
        Vector3 d2_v = SampleCurve(d2, v);     // Right curve S(1,v)

        // Corner points - make sure these are consistent
        Vector3 S00 = SampleCurve(C1, 0);      // S(0,0) - bottom-left
        Vector3 S10 = SampleCurve(C1, 1);      // S(1,0) - bottom-right
        Vector3 S01 = SampleCurve(C2, 0);      // S(0,1) - top-left
        Vector3 S11 = SampleCurve(C2, 1);      // S(1,1) - top-right

        // Following course formula:
        // rc(u,v): ruled surface interpolating C1, C2 (u-direction)
        Vector3 rc = g1_v * C1_u + g2_v * C2_u;

        // rd(u,v): ruled surface interpolating d1, d2 (v-direction)
        Vector3 rd = f1_u * d1_v + f2_u * d2_v;

        // rcd(u,v): bilinear surface from corner points
        Vector3 rcd = f1_u * g1_v * S00 + f2_u * g1_v * S10 +
                      f1_u * g2_v * S01 + f2_u * g2_v * S11;

        // Final Coon patch: S(u,v) = rc(u,v) + rd(u,v) - rcd(u,v)
        return rc + rd - rcd;
    }

    Vector3[] GetCurvePoints(LineRenderer curve)
    {
        Vector3[] points = new Vector3[curve.positionCount];
        for (int i = 0; i < curve.positionCount; i++)
        {
            points[i] = curve.GetPosition(i);
        }
        return points;
    }

    Vector3 SampleCurve(Vector3[] curve, float t)
    {
        t = Mathf.Clamp01(t);
        float exactIndex = t * (curve.Length - 1);
        int index = Mathf.FloorToInt(exactIndex);

        if (index >= curve.Length - 1)
            return curve[curve.Length - 1];

        float fraction = exactIndex - index;
        return Vector3.Lerp(curve[index], curve[index + 1], fraction);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Selected Curves: {selectedCurves.Count}/4");

        if (GUILayout.Button("Generate Coon Patch"))
        {
            GenerateCoonPatch();
        }

        if (GUILayout.Button("Clear Selection"))
        {
            ClearSelection();
        }

        GUILayout.Label("Controls:");
        GUILayout.Label("Left Click: Select curve");
        GUILayout.Label("Right Click: Clear selection");
        GUILayout.Label("S: Apply subdivision (smooth)");
        GUILayout.Label("B: Apply Butterfly subdivision");
        GUILayout.Label("R: Restore original mesh");

        GUILayout.EndArea();
    }
}