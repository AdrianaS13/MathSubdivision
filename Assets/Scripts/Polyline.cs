using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class Polyline : MonoBehaviour
{
    public Camera mainCamera;
    public float zDepth = 0f;
    public GameObject pointPrefab;
    public Material lineMaterial;

    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> pointObjects = new List<GameObject>();
    public LineRenderer lineRenderer;
    public ChaikinCurve chaikinCurve;


    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if (points == null) points = new List<Vector3>();
        if (pointObjects == null) pointObjects = new List<GameObject>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;

        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.green;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    public bool isActive = false;

    void Update()
    {
        if (!isActive) return; // Solo agrega puntos si esta curva está activa

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            AddPointFromMouseClick();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearLine();
        }
    }

    public int GetClickedPointIndex(float radiusPixels = 50f)
    {
        Vector3 mousePos = Input.mousePosition;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(points[i]);

            if (screenPos.z > 0)
            {
                float screenDistance = Vector2.Distance(
                    new Vector2(mousePos.x, mousePos.y),
                    new Vector2(screenPos.x, screenPos.y)
                );

                if (screenDistance < radiusPixels)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public bool IsMouseOverPoint(out int pointIndex, float radiusPixels = 50f)
    {
        pointIndex = GetClickedPointIndex(radiusPixels);
        return pointIndex != -1;
    }

    public int GetClickedPointIndex(Vector3 worldPos, float radius = 2f)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector3.Distance(worldPos, points[i]) < radius)
            {
                return i;
            }
        }
        return -1;
    }

    public Vector3 GetPointWorldPosition(int index)
    {
        if (index >= 0 && index < points.Count)
            return points[index];
        return Vector3.zero;
    }

    void AddPointFromMouseClick()
    {
        // First check if we're clicking on an existing point (using screen space detection)
        if (IsMouseOverPoint(out int clickedPointIndex, 50f)) // 50 pixels radius
        {
            Debug.Log($"Clicked on existing point {clickedPointIndex} at position {points[clickedPointIndex]}");
            return;
        }

        // Create the ray from mouse position
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // Place point at fixed distance from camera
        float distanceFromCamera = 10f; 
        Vector3 worldPos = ray.GetPoint(distanceFromCamera);

        // Add the point
        points.Add(worldPos);
        UpdateLineRenderer();

        if (pointPrefab != null)
        {
            GameObject point = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
            pointObjects.Add(point);
        }

        if (chaikinCurve != null)
        {
            chaikinCurve.GenerateChaikinCurve();
        }
    }

    //to place points on an invisible ground plane:
    void AddPointFromMouseClickOnGround()
    {
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // Cast ray and hit anything on the default layer (like a ground plane)
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 worldPos = hit.point;

            points.Add(worldPos);
            UpdateLineRenderer();

            if (pointPrefab != null)
            {
                GameObject point = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                pointObjects.Add(point);
            }

            if (chaikinCurve != null)
            {
                chaikinCurve.GenerateChaikinCurve();
            }
        }
    }

    void UpdateLineRenderer()
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    void ClearLine()
    {
        points.Clear();
        lineRenderer.positionCount = 0;

        foreach (GameObject obj in pointObjects)
        {
            Destroy(obj);
        }
        pointObjects.Clear();
    }

    public List<Vector3> GetPoints()
    {
        return points;
    }


    public void AddInitialPoint(Vector3 point)
    {
        if (points == null)
            points = new List<Vector3>();

        points.Clear();
        points.Add(point);

        if (pointPrefab != null)
        {
            GameObject pointObj = Instantiate(pointPrefab, point, Quaternion.identity, this.transform);
            pointObjects.Add(pointObj);
        }
        else
        {
            Debug.LogWarning("pointPrefab es null en AddInitialPoint");
        }

        UpdateLineRenderer();
    }

    public void AddFinalPoint(Vector3 point)
    {
        points.Add(point);
        if (pointPrefab != null)
        {
            GameObject pointObj = Instantiate(pointPrefab, point, Quaternion.identity, this.transform);
            pointObjects.Add(pointObj);
        }
        UpdateLineRenderer();
    }

}
