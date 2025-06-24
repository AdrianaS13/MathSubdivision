using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ChaikinCurve : MonoBehaviour
{
    public Polyline sourcePolyline;
    public LineRenderer lineRenderer;
    public List<Vector3> currentCurvePoints = new List<Vector3>();
    public GameObject pointSpherePrefab;
    private List<GameObject> spawnedSpheres = new List<GameObject>();
    public int iterations = 1;
    [HideInInspector] public float u = 0.6f;
    [HideInInspector] public float v = 0.3f;

    public float colliderThickness = 0.2f; 

    private void Start()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.blue;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    public void SetUV(float newU, float newV)
    {
        u = newU;
        v = newV;
    }

    public void GenerateChaikinCurve()
    {
        if (sourcePolyline == null) return;
        List<Vector3> inputPoints = sourcePolyline.GetPoints();
        if (inputPoints.Count < 2) return;

        List<Vector3> refined = new List<Vector3>(inputPoints);
        for (int i = 0; i < iterations; i++)
        {
            refined = ChaikinSubdivision(refined, u, v);
        }

        currentCurvePoints = refined;
        lineRenderer.positionCount = refined.Count;
        lineRenderer.SetPositions(refined.ToArray());

        // Clear previous spheres
        foreach (var s in spawnedSpheres)
            Destroy(s);
        spawnedSpheres.Clear();

        // Create spheres for visualization
        foreach (var p in refined)
        {
            GameObject sphere = Instantiate(pointSpherePrefab, p, Quaternion.identity, this.transform);
            spawnedSpheres.Add(sphere);
        }

        // Create single collider for curve selection
        CreateSingleCurveCollider(refined);
    }

    void CreateSingleCurveCollider(List<Vector3> curvePoints)
    {
        BoxCollider existingCollider = GetComponent<BoxCollider>();
        if (existingCollider != null)
        {
            DestroyImmediate(existingCollider);
        }

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();

        // Calculate bounds of the entire curve
        if (curvePoints.Count > 0)
        {
            Bounds bounds = new Bounds(curvePoints[0], Vector3.zero);
            foreach (var point in curvePoints)
            {
                bounds.Encapsulate(point);
            }

            // Convert to local space and expand bounds for easier selection
            bounds.Expand(colliderThickness);
            collider.center = transform.InverseTransformPoint(bounds.center);
            collider.size = transform.InverseTransformDirection(bounds.size);
        }
    }

    List<Vector3> ChaikinSubdivision(List<Vector3> input, float u, float v)
    {
        List<Vector3> result = new List<Vector3>();
        if (input.Count < 2)
            return new List<Vector3>(input);

        result.Add(input[0]); // conservar primer punto

        for (int i = 0; i < input.Count - 1; i++)
        {
            Vector3 p0 = input[i];
            Vector3 p1 = input[i + 1];

            float firstParam = Mathf.Min(u, v);
            float secondParam = Mathf.Max(u, v);

            Vector3 firstPoint = Vector3.Lerp(p0, p1, firstParam);
            Vector3 secondPoint = Vector3.Lerp(p0, p1, secondParam);

            result.Add(firstPoint);
            result.Add(secondPoint);
        }

        result.Add(input[input.Count - 1]); // conservar último punto
        return result;
    }

    public void SetSelected(bool selected)
    {
        if (lineRenderer != null)
        {
            lineRenderer.material.color = selected ? Color.red : Color.blue;
        }
    }
}