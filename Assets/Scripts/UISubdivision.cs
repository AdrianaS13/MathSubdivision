using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Subdivision : MonoBehaviour
{
    [Header("UI References")]
    public Slider subdivisionSlider;
    public TextMeshProUGUI subdivisionLabel;
    public Button resetButton;


    public Slider subdivisionSlider2;
    public TextMeshProUGUI subdivisionLabel2;

    [Header("Target Object")]
    public CatmullClarkSubdivision targetCube;
    public SqrtKobbeltSubdivision targetCube2;

    void Start()
    {
        // Configurar slider
        if (subdivisionSlider != null)
        {
            subdivisionSlider.wholeNumbers = true;
            subdivisionSlider.value = 0;
            subdivisionSlider.onValueChanged.AddListener(OnSubdivisionChanged);
        }
        if (subdivisionSlider2 != null)
        {
            subdivisionSlider2.wholeNumbers = true;
            subdivisionSlider2.value = 0;
            subdivisionSlider2.onValueChanged.AddListener(OnSubdivisionChanged2);
        }
        // Configurar botón de reset
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetSubdivision);
        }
        
        // Actualizar label inicial
        UpdateLabel();
        UpdateLabel2();
    }
    
    void OnSubdivisionChanged(float value)
    {
        int subdivisionLevel = Mathf.RoundToInt(value);
        
        if (targetCube != null)
        {
            targetCube.subdivisionLevel = subdivisionLevel;
        }
        
        UpdateLabel();
    }
    void OnSubdivisionChanged2(float value)
    {
        int subdivisionLevel2 = Mathf.RoundToInt(value);

        if (targetCube2 != null)
        {
            targetCube2.subdivisionLevel2 = subdivisionLevel2;
        }

        UpdateLabel2();
    }

    void ResetSubdivision()
    {
        if (subdivisionSlider != null)
        {
            subdivisionSlider.value = 0;
        }
    }
    
    void UpdateLabel()
    {
        if (subdivisionLabel != null)
        {
            int currentLevel = subdivisionSlider != null ? Mathf.RoundToInt(subdivisionSlider.value) : 0;
            subdivisionLabel.text = $"Subdivisiones: {currentLevel}";
        }
    }
    void UpdateLabel2()
    {
        if (subdivisionLabel2 != null)
        {
            int currentLevel = subdivisionSlider2 != null ? Mathf.RoundToInt(subdivisionSlider2.value) : 0;
            subdivisionLabel2.text = $"Subdivisiones: {currentLevel}";
        }
    }

    void Update()
    {
        // Permitir control con teclado para testing
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSubdivision(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSubdivision(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSubdivision(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSubdivision(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetSubdivision(4);
        if (Input.GetKeyDown(KeyCode.R)) ResetSubdivision();
    }
    
    void SetSubdivision(int level)
    {
        if (subdivisionSlider != null)
        {
            subdivisionSlider.value = level;
        }
    }
}