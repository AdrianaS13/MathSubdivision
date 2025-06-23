using UnityEngine;

public class CurveCreator : MonoBehaviour
{
    [Header("Curve Settings")]
    public Material curveMaterial;
    public float curveWidth = 0.1f;
    public int curveResolution = 100;

    [ContextMenu("Create Test Curves")]
    public void CreateTestCurves()
    {
        // Create 4 curves that form a square with some curvature
        CreateCurve("BottomCurve", CreateBottomCurve());
        CreateCurve("RightCurve", CreateRightCurve());
        CreateCurve("TopCurve", CreateTopCurve());
        CreateCurve("LeftCurve", CreateLeftCurve());
    }

    void CreateCurve(string name, Vector3[] points)
    {
        GameObject curveObj = new GameObject(name);
        LineRenderer lr = curveObj.AddComponent<LineRenderer>();

        // Add a collider so we can click on it
        BoxCollider collider = curveObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.2f, 0.2f, 5f); // Adjust as needed

        // Configure LineRenderer
        lr.material = curveMaterial;

        lr.startWidth = curveWidth;
        lr.endWidth = curveWidth;

        lr.positionCount = points.Length;
        lr.useWorldSpace = true;

        lr.startColor = Color.white;
        lr.endColor = Color.white;

        // Set positions
        for (int i = 0; i < points.Length; i++)
        {
            lr.SetPosition(i, points[i]);
        }

        // Position the collider along the curve
        Vector3 center = Vector3.zero;
        foreach (var point in points)
        {
            center += point;
        }
        center /= points.Length;
        collider.center = curveObj.transform.InverseTransformPoint(center);
    }

    Vector3[] CreateBottomCurve()
    {
        Vector3[] points = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            float x = Mathf.Lerp(-2f, 2f, t);
            float y = -2f + 0.3f * Mathf.Sin(t * Mathf.PI); // Slight curve
            float z = 0.5f * Mathf.Sin(t * Mathf.PI);
            points[i] = new Vector3(x, y, z);
        }
        return points;
    }

    Vector3[] CreateRightCurve()
    {
        Vector3[] points = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            float x = 2f + 0.3f * Mathf.Sin(t * Mathf.PI);
            float y = Mathf.Lerp(-2f, 2f, t);
            float z = 0.5f * Mathf.Sin(t * Mathf.PI);
            points[i] = new Vector3(x, y, z);
        }
        return points;
    }

    Vector3[] CreateTopCurve()
    {
        Vector3[] points = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            float x = Mathf.Lerp(2f, -2f, t); // Reverse direction
            float y = 2f + 0.3f * Mathf.Sin(t * Mathf.PI);
            float z = 0.5f * Mathf.Sin(t * Mathf.PI);
            points[i] = new Vector3(x, y, z);
        }
        return points;
    }

    Vector3[] CreateLeftCurve()
    {
        Vector3[] points = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            float x = -2f + 0.3f * Mathf.Sin(t * Mathf.PI);
            float y = Mathf.Lerp(2f, -2f, t); // Reverse direction
            float z = 0.5f * Mathf.Sin(t * Mathf.PI);
            points[i] = new Vector3(x, y, z);
        }
        return points;
    }
}