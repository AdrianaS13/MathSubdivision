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

        //lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
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


    void AddPointFromMouseClick()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - zDepth);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = zDepth;

        points.Add(worldPos);
        UpdateLineRenderer();

        if (pointPrefab != null)
        {
            GameObject point = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
            pointObjects.Add(point);
        }
        if (chaikinCurve != null)
        {
            chaikinCurve.GenerateChaikinCurve(); // Actualiza automáticamente
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

    public int GetClickedPointIndex(Vector3 mousePos, float radius = 0.2f)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector3.Distance(mousePos, points[i]) < radius)
            {
                return i;
            }
        }
        return -1;
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
