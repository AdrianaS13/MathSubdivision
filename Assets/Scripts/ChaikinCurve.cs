using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class ChaikinCurve : MonoBehaviour
{
    public Polyline sourcePolyline;
    public LineRenderer lineRenderer;

    public Slider uSlider;
    public Slider vSlider;
    public TextMeshProUGUI uValueText;
    public TextMeshProUGUI vValueText;
    public Button generateButton;

    public int iterations = 3;

    private float u;
    private float v;

    private void Start()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();

        if (generateButton != null)
            generateButton.onClick.AddListener(GenerateChaikinCurve);

        UpdateUVFromSliders();
    }

    private void Update()
    {
        UpdateUVFromSliders();
        UpdateUIText();
    }

    void UpdateUVFromSliders()
    {
        if (uSlider != null && vSlider != null)
        {
            u = uSlider.value;
            v = vSlider.value;

            // Asegurar que u + v <= 1
            float sum = u + v;
            if (sum > 1f)
            {
                float scale = 1f / sum;
                u *= scale;
                v *= scale;
            }

            // Actualizar sliders si se modificó
            uSlider.SetValueWithoutNotify(u);
            vSlider.SetValueWithoutNotify(v);
        }
    }

    void UpdateUIText()
    {
        if (uValueText != null)
            uValueText.text = $"u = {u:F2}";
        if (vValueText != null)
            vValueText.text = $"v = {v:F2}";
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

        lineRenderer.positionCount = refined.Count;
        lineRenderer.SetPositions(refined.ToArray());
    }

    List<Vector3> ChaikinSubdivision(List<Vector3> input, float u, float v)
    {
        List<Vector3> result = new List<Vector3>();

        for (int i = 0; i < input.Count - 1; i++)
        {
            Vector3 p0 = input[i];
            Vector3 p1 = input[i + 1];

            // Ordenar u y v para agregar puntos en orden correcto
            float firstParam = Mathf.Min(u, v);
            float secondParam = Mathf.Max(u, v);

            Vector3 firstPoint = Vector3.Lerp(p0, p1, firstParam);
            Vector3 secondPoint = Vector3.Lerp(p0, p1, secondParam);

            result.Add(firstPoint);
            result.Add(secondPoint);
        }

        return result;
    }

}
