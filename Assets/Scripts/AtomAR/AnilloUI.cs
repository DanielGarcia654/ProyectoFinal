// AtomAR - Anillo para UI con grosor constante. Con "relleno" menor a 1 dibuja solo una parte (sirve como anillo de avance).
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AnilloUI : MaskableGraphic
{
    [Min(0.5f)] public float grosor = 3f;
    [Range(16, 256)] public int segmentos = 96;
    [Tooltip("Fracción del anillo que se dibuja, desde arriba y en el sentido del reloj")]
    [Range(0f, 1f)] public float relleno = 1f;

    public void PonerRelleno(float f)
    {
        f = Mathf.Clamp01(f);
        if (Mathf.Approximately(f, relleno)) return;
        relleno = f; SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int n = Mathf.CeilToInt(segmentos * relleno);
        if (n <= 0) return;
        var r = GetPixelAdjustedRect();
        float rExt = Mathf.Min(r.width, r.height) * 0.5f, rInt = Mathf.Max(0f, rExt - grosor);
        Vector2 c = r.center;
        var v = UIVertex.simpleVert; v.color = color;
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.PI * 0.5f - Mathf.Min(i / (float)segmentos, relleno) * Mathf.PI * 2f;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            v.position = c + d * rExt; vh.AddVert(v);
            v.position = c + d * rInt; vh.AddVert(v);
            if (i > 0)
            {
                int k = i * 2;
                vh.AddTriangle(k - 2, k, k - 1);
                vh.AddTriangle(k - 1, k, k + 1);
            }
        }
    }
}
