using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ChaikinCurve : MonoBehaviour
{
    public Polyline sourcePolyline; 
    public int iterations = 3;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateChaikinCurve();
        }
    }

    void GenerateChaikinCurve()
    {
        if (sourcePolyline == null) return;

        List<Vector3> inputPoints = sourcePolyline.GetPoints();
        if (inputPoints.Count < 2) return;

        List<Vector3> refined = new List<Vector3>(inputPoints);
        for (int i = 0; i < iterations; i++)
        {
            refined = ChaikinSubdivision(refined);
        }

        lineRenderer.positionCount = refined.Count;
        lineRenderer.SetPositions(refined.ToArray());
    }

    List<Vector3> ChaikinSubdivision(List<Vector3> input)
    {
        List<Vector3> result = new List<Vector3>();

        for (int i = 0; i < input.Count - 1; i++)
        {
            Vector3 p0 = input[i];
            Vector3 p1 = input[i + 1];

            Vector3 q = Vector3.Lerp(p0, p1, 0.25f); // 3/4 p0 + 1/4 p1
            Vector3 r = Vector3.Lerp(p0, p1, 0.75f); // 1/4 p0 + 3/4 p1

            result.Add(q);
            result.Add(r);
        }

        return result;
    }
}
