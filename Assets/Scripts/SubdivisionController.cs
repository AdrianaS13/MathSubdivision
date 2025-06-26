using System.Collections.Generic;
using UnityEngine;

public class SubdivisionController : MonoBehaviour
{
    public enum AlgorithmType { CatmullClark, Kobbelt /*, Loop*/ }

    [Header("Configuración")]
    public AlgorithmType algorithm = AlgorithmType.CatmullClark;
    [Range(0, 5)]
    public int subdivisionLevel = 1;
    public Material material;
    public Transform outputParent;

    [Header("Referencia Mesh Input")]
    public MeshFilter inputMeshFilter; // Mesh a subdividir

    [Header("Referencia Script de Subdivisión")]
    public CatmullClarkSubdivision catmullClarkScript;
    public SqrtKobbeltSubdivision kobbeltScript;

    private GameObject currentMeshObj;

    void Start()
    {
        // Opcional: auto-generar al iniciar
        //GenerateAndSubdivideInputMesh();
    }

    // Función que limpia vértices duplicados y convierte Unity Mesh a Mesh3D
    public static Mesh3D CleanAndConvertMesh(Mesh unityMesh)
    {
        var oldVertices = unityMesh.vertices;
        var triangles = unityMesh.triangles;

        List<Vector3> newVertices = new List<Vector3>();
        List<List<int>> newFaces = new List<List<int>>();
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        float epsilon = 0.00000000001f;

        int GetOrAddVertex(Vector3 v)
        {
            foreach (var kvp in vertexMap)
            {
                if ((kvp.Key - v).sqrMagnitude < epsilon)
                    return kvp.Value;
            }
            int newIndex = newVertices.Count;
            newVertices.Add(v);
            vertexMap[v] = newIndex;
            return newIndex;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = GetOrAddVertex(oldVertices[triangles[i]]);
            int i1 = GetOrAddVertex(oldVertices[triangles[i + 1]]);
            int i2 = GetOrAddVertex(oldVertices[triangles[i + 2]]);
            newFaces.Add(new List<int> { i0, i1, i2 });
        }

        return new Mesh3D(newVertices, newFaces);
    }


    public void GenerateAndSubdivideInputMesh()
    {
        if (inputMeshFilter == null)
        {
            Debug.LogError("No input mesh assigned");
            return;
        }

        // Limpiar y convertir mesh de Unity
        Mesh3D mesh3D = CleanAndConvertMesh(inputMeshFilter.mesh);

        for (int i = 0; i < subdivisionLevel; i++)
        {
            switch (algorithm)
            {
                case AlgorithmType.CatmullClark:
                    mesh3D = catmullClarkScript.ApplyCatmullClark(mesh3D);
                    break;
                case AlgorithmType.Kobbelt:
                    mesh3D = kobbeltScript.ApplySqrt3Subdivision(mesh3D);
                    break;
                    // Otros algoritmos si los tienes
            }
        }

        if (currentMeshObj != null)
        {
            Destroy(currentMeshObj);
        }

        currentMeshObj = new GameObject("ResultMesh");
        currentMeshObj.transform.SetParent(outputParent);
        currentMeshObj.transform.localPosition = Vector3.zero;
        currentMeshObj.transform.localRotation = Quaternion.identity;
        currentMeshObj.transform.localScale = Vector3.one;

        MeshFilter mf = currentMeshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = currentMeshObj.AddComponent<MeshRenderer>();

        mf.mesh = MeshConverter.ToUnityMeshFlatShading(mesh3D);
        mr.material = material;
    }
}
