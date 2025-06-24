using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Slider uSlider;
    public Slider vSlider;
    public Slider iterationsSlider;
    public TextMeshProUGUI uValueText;
    public TextMeshProUGUI vValueText;
    public Button generateButton;
    public TextMeshProUGUI iterationsValueText;

    private ChaikinCurve activeChaikinCurve;

    private float u = 1f / 3f;
    private float v = 1f / 4f;
    private int iter = 1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        uSlider.value = u;
        vSlider.value = v;
        iterationsSlider.value = iter;

        uSlider.onValueChanged.AddListener(OnSliderChanged);
        vSlider.onValueChanged.AddListener(OnSliderChanged);
        iterationsSlider.onValueChanged.AddListener(OnSliderChanged);

        generateButton.onClick.AddListener(OnGenerateClicked);

        UpdateUIText();
    }

    public void SetActiveChaikinCurve(ChaikinCurve chaikinCurve)
    {
        activeChaikinCurve = chaikinCurve;
        if (activeChaikinCurve != null)
        {
            // Inicializar sliders con valores de la curva
            u = activeChaikinCurve.u;
            v = activeChaikinCurve.v;
            iter = activeChaikinCurve.iterations;
            uSlider.SetValueWithoutNotify(u);
            vSlider.SetValueWithoutNotify(v);
            iterationsSlider.SetValueWithoutNotify(iter);
            UpdateUIText();
        }
    }

    private void OnSliderChanged(float val)
    {
        // Ajustar u y v para que sumen <= 1
        float sum = uSlider.value + vSlider.value;
        if (sum > 1f)
        {
            float scale = 1f / sum;
            u = uSlider.value * scale;
            v = vSlider.value * scale;
            uSlider.SetValueWithoutNotify(u);
            vSlider.SetValueWithoutNotify(v);
        }
        else
        {
            u = uSlider.value;
            v = vSlider.value;
        }

        // Actualizar iterations (redondear a entero)
        iter = Mathf.RoundToInt(iterationsSlider.value);

        UpdateUIText();

        if (activeChaikinCurve != null)
        {
            activeChaikinCurve.SetUV(u, v);
            activeChaikinCurve.iterations = iter;
        }
    }

    private void UpdateUIText()
    {
        uValueText.text = $"u = {u:F2}";
        vValueText.text = $"v = {v:F2}";
        if (iterationsValueText != null)
            iterationsValueText.text = $"Iterations = {iter}";
    }

    private void OnGenerateClicked()
    {
        if (activeChaikinCurve != null)
        {
            activeChaikinCurve.GenerateChaikinCurve();
        }
    }
}
