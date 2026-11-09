// AtomAR - Anillo para UI con grosor constante (no se deforma al cambiar de tamaño, a diferencia de una imagen).
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AnilloUI : MaskableGraphic
{
    [Min(0.5f)] public float grosor = 3f;
    [Range(16, 256)] public int segmentos = 96;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        float rExt = Mathf.Min(r.width, r.height) * 0.5f, rInt = Mathf.Max(0f, rExt - grosor);
        Vector2 c = r.center;
        var v = UIVertex.simpleVert; v.color = color;
        for (int i = 0; i <= segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
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
