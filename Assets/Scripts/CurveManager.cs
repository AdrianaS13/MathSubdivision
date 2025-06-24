using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CurveManager : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject pointPrefab;
    private Polyline activeCurve;
    public GameObject pointSpherePrefab;



    public enum CurveCreationMode
    {
        None,
        CreatingFromPoint,
        AwaitingEndPoint
    }

    private CurveCreationMode mode = CurveCreationMode.None;

    public class CurvePair
    {
        public GameObject parentGO;
        public Polyline polyline;
        public ChaikinCurve chaikinCurve;
    }

    public List<CurvePair> allCurves = new List<CurvePair>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(mainCamera.transform.position.z)));

            switch (mode)
            {
                case CurveCreationMode.CreatingFromPoint:
                    TrySelectStartPoint(mouseWorld);
                    break;
                case CurveCreationMode.AwaitingEndPoint:
                    TrySelectEndPoint(mouseWorld);
                    break;
            }
        }
    }
    public void CreateNewCurveStandalone()
    {
        CreateNewCurvePair(); 
    }

    public void EnterStartPointMode()
    {
        mode = CurveCreationMode.CreatingFromPoint;
    }

    public void EnterEndPointMode()
    {
        mode = CurveCreationMode.AwaitingEndPoint;
    }

    void TrySelectStartPoint(Vector3 mouseWorld)
    {
        foreach (var pair in allCurves)
        {
            int index = pair.polyline.GetClickedPointIndex(mouseWorld);
            if (index >= 0)
            {
                Vector3 p = pair.polyline.GetPoints()[index];
                CreateNewCurveFromPoint(p);
                mode = CurveCreationMode.None;
                return;
            }
        }
    }

    void TrySelectEndPoint(Vector3 mouseWorld)
    {
        foreach (var pair in allCurves)
        {
            int index = pair.polyline.GetClickedPointIndex(mouseWorld);
            if (index >= 0)
            {
                Vector3 p = pair.polyline.GetPoints()[index];
                CompleteCurveAtPoint(p);
                mode = CurveCreationMode.None;
                return;
            }
        }
    }

    void CreateNewCurveFromPoint(Vector3 point)
    {
        CreateNewCurvePair();

        if (allCurves.Count == 0)
        {
            Debug.LogError("No se ha creado correctamente la nueva curva.");
            return;
        }

        var newCurve = allCurves[allCurves.Count - 1].polyline;

        if (newCurve == null)
        {
            Debug.LogError("Polyline es null en la nueva curva.");
            return;
        }

        newCurve.AddInitialPoint(point);
        activeCurve = newCurve;
        activeCurve.isActive = true;


    }

    void CompleteCurveAtPoint(Vector3 point)
    {
        if (activeCurve != null)
        {
            activeCurve.AddFinalPoint(point);  // Agrega el punto a la polilínea
            activeCurve.isActive = false;

            // Ahora actualiza la curva Chaikin vinculada
            if (activeCurve.chaikinCurve != null)
            {
                activeCurve.chaikinCurve.GenerateChaikinCurve();
            }

            activeCurve = null;
        }
        PrintAllChaikinCurvePoints();
    }

    void CreateNewCurvePair()
    {
        // Desactivar todas las polylines activas actuales
        foreach (var pair1 in allCurves)
        {
            pair1.polyline.isActive = false;
        }

        GameObject parentGO = new GameObject("CurvePair");

        // Crear hijo para Polyline
        GameObject polylineGO = new GameObject("Polyline");
        polylineGO.transform.parent = parentGO.transform;
        Polyline polyline = polylineGO.AddComponent<Polyline>();
        polyline.pointPrefab = pointPrefab;
        polyline.isActive = true;
        activeCurve = polyline;
        activeCurve.isActive = true;
        LineRenderer lrp = polylineGO.GetComponent<LineRenderer>();
        if (lrp == null)
            lrp = polylineGO.AddComponent<LineRenderer>();

        polyline.lineRenderer = lrp;
        // Crear hijo para ChaikinCurve
        GameObject chaikinGO = new GameObject("ChaikinCurve");
        chaikinGO.transform.parent = parentGO.transform;
        ChaikinCurve chaikinCurve = chaikinGO.AddComponent<ChaikinCurve>();
        chaikinCurve.sourcePolyline = polyline;
        chaikinCurve.pointSpherePrefab = pointSpherePrefab;
        LineRenderer lr = chaikinGO.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = chaikinGO.AddComponent<LineRenderer>();
        }
        chaikinCurve.lineRenderer = lr;

        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.green;
        lr.positionCount = 0;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.useWorldSpace = true;

        // Referencias cruzadas
        polyline.chaikinCurve = chaikinCurve;

        CurvePair pair = new CurvePair
        {
            parentGO = parentGO,
            polyline = polyline,
            chaikinCurve = chaikinCurve
        };

        UIManager.Instance.SetActiveChaikinCurve(chaikinCurve);

        allCurves.Add(pair);
    }

    public void PrintAllChaikinCurvePoints()
    {
        for (int i = 0; i < allCurves.Count; i++)
        {
            var pair = allCurves[i];
            var chaikinCurve = pair.chaikinCurve;

            if (chaikinCurve == null || chaikinCurve.currentCurvePoints == null || chaikinCurve.currentCurvePoints.Count == 0)
            {
                Debug.Log($"Curva {i + 1} no tiene puntos Chaikin generados.");
                continue;
            }

            Debug.Log($"Puntos curva {i + 1}:");

            foreach (var p in chaikinCurve.currentCurvePoints)
            {
                Debug.Log(p);
            }
        }
    }

}
