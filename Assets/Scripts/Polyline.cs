using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class Polyline : MonoBehaviour
{
    public Camera mainCamera;
    public float zDepth = 0f;
    public GameObject pointPrefab;

    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> pointObjects = new List<GameObject>();
    private LineRenderer lineRenderer;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
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
            GameObject point = Instantiate(pointPrefab, worldPos, Quaternion.identity);
            pointObjects.Add(point);
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
}
