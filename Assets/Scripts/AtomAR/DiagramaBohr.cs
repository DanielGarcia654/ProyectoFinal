// AtomAR - Mini átomo de Bohr para la interfaz: núcleo + capas con sus electrones, girando como el modelo 3D.
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DiagramaBohr : MonoBehaviour
{
    public Sprite punto;                                   // sprite redondeado (se usa como círculo)
    public Color colorNucleo = new(0.95f, 0.27f, 0.31f);
    public Color colorElectron = new(0.18f, 0.90f, 1f);
    public Color colorOrbita = new(1f, 1f, 1f, 0.16f);
    public float radioNucleo = 20f;
    public float tamElectron = 13f;
    public float velocidadCapa1 = 70f;                     // °/s, las capas exteriores van más lento

    RectTransform[] capas = new RectTransform[0];
    float[] velocidades = new float[0];

    public void Construir(int[] electronesPorCapa)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        int n = electronesPorCapa?.Length ?? 0;
        var rt = (RectTransform)transform;
        float rMax = Mathf.Min(rt.rect.width, rt.rect.height) * 0.5f - tamElectron * 0.5f - 2f;
        float rMin = radioNucleo + 16f;

        capas = new RectTransform[n];
        velocidades = new float[n];
        for (int k = 0; k < n; k++)
        {
            float r = n == 1 ? (rMin + rMax) * 0.5f : Mathf.Lerp(rMin, rMax, k / (float)(n - 1));
            var orbita = Nuevo($"Orbita_{k + 1}", transform, r * 2f);
            var anillo = orbita.gameObject.AddComponent<AnilloUI>();
            anillo.color = colorOrbita; anillo.grosor = 2f; anillo.raycastTarget = false;

            var capa = Nuevo($"Capa_{k + 1}", transform, r * 2f);
            int ne = electronesPorCapa[k];
            float desfase = k * 37f;
            for (int j = 0; j < ne; j++)
            {
                float a = (desfase + j * 360f / ne) * Mathf.Deg2Rad;
                var e = Circulo($"e{j + 1}", capa, tamElectron, colorElectron);
                e.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            capas[k] = capa;
            velocidades[k] = velocidadCapa1 * Mathf.Pow(0.72f, k) * (k % 2 == 0 ? 1f : -1f);
        }
        Circulo("Nucleo", transform, radioNucleo * 2f, colorNucleo);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int k = 0; k < capas.Length; k++)
            if (capas[k]) capas[k].localRotation *= Quaternion.Euler(0f, 0f, velocidades[k] * dt);
    }

    static RectTransform Nuevo(string nombre, Transform padre, float tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tam, tam);
        return rt;
    }

    RectTransform Circulo(string nombre, Transform padre, float tam, Color color)
    {
        var rt = Nuevo(nombre, padre, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = punto; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
        if (punto) img.pixelsPerUnitMultiplier = punto.border.x / (tam * 0.5f);
        return rt;
    }
}
