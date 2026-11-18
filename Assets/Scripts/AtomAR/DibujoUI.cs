// AtomAR - Ilustraciones vectoriales dibujadas por código (se ven nítidas a cualquier tamaño y pueden moverse).
// figura: "logo" (marca de AtomAR), "agua" (vaso con moléculas), "sal" (salero y cristales), "co2" (refresco con burbujas),
//         "herrumbre" (clavo oxidado), "molecula" (molécula de agua), "medalla".
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class DibujoUI : MaskableGraphic
{
    public string figura = "agua";
    public bool animado = true;
    [Tooltip("Solo para el logo: órbitas oscuras para usarlo sobre fondos claros")]
    public bool sobreClaro;

    static readonly Color Blanco = new(0.95f, 0.96f, 1f), Rojo = new(0.95f, 0.27f, 0.31f), Cian = new(0.18f, 0.90f, 1f);
    static readonly Color Oro = new(1f, 0.78f, 0.24f), Gris = new(0.60f, 0.65f, 0.76f), Noche = new(0.08f, 0.09f, 0.20f);
    static readonly Color Oxido = new(0.71f, 0.27f, 0.11f), OxidoClaro = new(0.85f, 0.41f, 0.17f);

    VertexHelper vh; Vector2 centro; float u, t, giro;

    public void Poner(string f) { if (figura == f) return; figura = f; SetVerticesDirty(); }

    void Update() { if (animado && Application.isPlaying) SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper h)
    {
        h.Clear(); vh = h;
        var r = GetPixelAdjustedRect();
        u = Mathf.Min(r.width, r.height); centro = r.center;
        t = Application.isPlaying ? Time.unscaledTime : 0f; giro = 0f;
        switch (figura)
        {
            case "agua": Vaso(new Color(0.25f, 0.66f, 0.96f, 0.80f), false, true); break;
            case "co2": Vaso(new Color(0.48f, 0.23f, 0.08f, 0.90f), true, false); break;
            case "sal": Salero(); break;
            case "herrumbre": Clavo(); break;
            case "medalla": Medalla(); break;
            case "logo": Logo(); break;
            default: Molecula(); break;
        }
        vh = null;
    }

    // ───────── Figuras (coordenadas de -0.5 a 0.5) ─────────
    void Vaso(Color liquido, bool refresco, bool moleculas)
    {
        const float yt = 0.38f, yb = -0.38f, wt = 0.30f, wb = 0.21f, nivel = 0.13f;
        float Ancho(float y) => Mathf.Lerp(wb, wt, (y - yb) / (yt - yb));
        // líquido con una onda suave arriba
        const int n = 10; float wl = Ancho(nivel) - 0.02f, wf = wb - 0.012f;
        for (int k = 0; k < n; k++)
        {
            float f0 = k / (float)n, f1 = (k + 1) / (float)n;
            float y0 = nivel + 0.012f * Mathf.Sin(t * 2f + k * 0.9f), y1 = nivel + 0.012f * Mathf.Sin(t * 2f + (k + 1) * 0.9f);
            Cuadro(new Vector2(Mathf.Lerp(-wf, wf, f0), yb + 0.02f), new Vector2(Mathf.Lerp(-wl, wl, f0), y0),
                   new Vector2(Mathf.Lerp(-wl, wl, f1), y1), new Vector2(Mathf.Lerp(-wf, wf, f1), yb + 0.02f), liquido);
        }
        if (moleculas)
        {
            var pos = new[] { new Vector2(-0.10f, -0.06f), new Vector2(0.09f, -0.20f), new Vector2(0.04f, 0.01f) };
            for (int i = 0; i < pos.Length; i++)
            {
                var p = pos[i] + new Vector2(0.012f * Mathf.Sin(t * 1.1f + i * 2.1f), 0.016f * Mathf.Sin(t * 1.5f + i * 1.7f));
                Disco(p + new Vector2(-0.046f, -0.034f), 0.028f, new Color(0.82f, 0.95f, 1f));
                Disco(p + new Vector2(0.046f, -0.034f), 0.028f, new Color(0.82f, 0.95f, 1f));
                Disco(p, 0.046f, Blanco);
            }
        }
        if (refresco)
        {
            for (int i = 0; i < 8; i++)
            {
                float f = Mathf.Repeat(t * (0.22f + 0.03f * (i % 3)) + i * 0.37f, 1f);
                float y = Mathf.Lerp(yb + 0.06f, nivel - 0.03f, f);
                float x = (i - 3.5f) * 0.045f + 0.012f * Mathf.Sin(t * 3f + i);
                Disco(new Vector2(x, y), 0.013f + 0.006f * (i % 3), new Color(1f, 1f, 1f, 0.65f), 12);
            }
            Linea(new Vector2(0.03f, -0.26f), new Vector2(0.23f, 0.48f), 0.032f, Rojo);            // popote
        }
        // el vaso
        var vidrio = Blanco;
        Linea(new Vector2(-wt, yt), new Vector2(-wb, yb), 0.028f, vidrio);
        Linea(new Vector2(-wb, yb), new Vector2(wb, yb), 0.028f, vidrio);
        Linea(new Vector2(wb, yb), new Vector2(wt, yt), 0.028f, vidrio);
    }

    void Salero()
    {
        const float ox = -0.09f;
        Vector2 O(float x, float y) => new(x + ox, y);
        Cuadro(O(-0.17f, -0.38f), O(-0.13f, 0.16f), O(0.13f, 0.16f), O(0.17f, -0.38f), new Color(1f, 1f, 1f, 0.20f));     // vidrio
        Cuadro(O(-0.162f, -0.37f), O(-0.146f, -0.10f), O(0.146f, -0.10f), O(0.162f, -0.37f), Blanco);                      // sal
        var borde = Blanco;
        Linea(O(-0.13f, 0.16f), O(-0.17f, -0.38f), 0.024f, borde);
        Linea(O(-0.17f, -0.38f), O(0.17f, -0.38f), 0.024f, borde);
        Linea(O(0.17f, -0.38f), O(0.13f, 0.16f), 0.024f, borde);
        // tapa
        int i0 = vh.currentVertCount; const int seg = 16;
        Vert(O(0f, 0.16f), Gris);
        for (int k = 0; k <= seg; k++) { float a = Mathf.PI * k / seg; Vert(O(0.155f * Mathf.Cos(a), 0.16f + 0.13f * Mathf.Sin(a)), Gris); }
        for (int k = 0; k < seg; k++) vh.AddTriangle(i0, i0 + k + 1, i0 + k + 2);
        Disco(O(-0.055f, 0.215f), 0.016f, Noche, 10); Disco(O(0f, 0.245f), 0.016f, Noche, 10); Disco(O(0.055f, 0.215f), 0.016f, Noche, 10);
        // cristales de sal (son cubitos)
        var cristales = new[] { new Vector3(0.24f, -0.34f, 0.075f), new Vector3(0.36f, -0.27f, 0.055f), new Vector3(0.25f, -0.19f, 0.045f), new Vector3(0.39f, -0.38f, 0.04f) };
        for (int i = 0; i < cristales.Length; i++)
        {
            float s = cristales[i].z * (1f + 0.10f * Mathf.Sin(t * 2.2f + i * 1.9f));
            var c = new Vector2(cristales[i].x, cristales[i].y);
            Cuadro(c + new Vector2(0, s), c + new Vector2(s, 0), c + new Vector2(0, -s), c + new Vector2(-s, 0), Blanco);
        }
    }

    void Clavo()
    {
        giro = -38f;
        var metal = new Color(0.56f, 0.60f, 0.69f); var metalClaro = new Color(0.70f, 0.74f, 0.82f);
        Cuadro(new Vector2(-0.04f, -0.30f), new Vector2(-0.04f, 0.36f), new Vector2(0.04f, 0.36f), new Vector2(0.04f, -0.30f), metal);
        Tri(new Vector2(-0.04f, -0.30f), new Vector2(0.04f, -0.30f), new Vector2(0f, -0.45f), metal);
        Cuadro(new Vector2(-0.13f, 0.36f), new Vector2(-0.13f, 0.42f), new Vector2(0.13f, 0.42f), new Vector2(0.13f, 0.36f), metalClaro);
        // manchas de óxido
        Disco(new Vector2(0.005f, 0.24f), 0.050f, Oxido); Disco(new Vector2(-0.015f, 0.05f), 0.058f, OxidoClaro);
        Disco(new Vector2(0.012f, -0.13f), 0.048f, Oxido); Disco(new Vector2(0.075f, 0.39f), 0.036f, OxidoClaro);
        Disco(new Vector2(-0.01f, -0.25f), 0.032f, OxidoClaro);
        giro = 0f;
        // polvito de óxido que cae
        for (int i = 0; i < 4; i++)
        {
            float f = Mathf.Repeat(t * 0.3f + i * 0.27f, 1f);
            Disco(new Vector2(0.20f + 0.05f * i, -0.20f - 0.22f * f), 0.014f, new Color(Oxido.r, Oxido.g, Oxido.b, 1f - f), 10);
        }
    }

    void Molecula()
    {
        giro = 8f * Mathf.Sin(t * 0.8f);
        Vector2 o = new(0f, 0.07f), h1 = new(-0.26f, -0.15f), h2 = new(0.26f, -0.15f);
        Linea(o, h1, 0.06f, new Color(1f, 1f, 1f, 0.45f), false); Linea(o, h2, 0.06f, new Color(1f, 1f, 1f, 0.45f), false);
        Disco(h1, 0.115f, Blanco, 28); Disco(h2, 0.115f, Blanco, 28);
        Disco(o, 0.20f, Rojo, 32);
        giro = 0f;
    }

    void Medalla()
    {
        Cuadro(new Vector2(-0.19f, 0.47f), new Vector2(-0.05f, 0.47f), new Vector2(0.07f, 0.06f), new Vector2(-0.07f, 0.06f), Rojo);
        Cuadro(new Vector2(0.19f, 0.47f), new Vector2(0.05f, 0.47f), new Vector2(-0.07f, 0.06f), new Vector2(0.07f, 0.06f), Cian);
        var c = new Vector2(0f, -0.14f);
        Disco(c, 0.28f, Oro, 36); Disco(c, 0.22f, new Color(1f, 0.86f, 0.45f), 36);
        // estrella
        int i0 = vh.currentVertCount; var tono = new Color(0.78f, 0.52f, 0.06f);
        Vert(c, tono);
        for (int k = 0; k <= 10; k++)
        {
            float a = Mathf.PI * 0.5f + k * Mathf.PI / 5f, rad = k % 2 == 0 ? 0.15f : 0.065f;
            Vert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad, tono);
        }
        for (int k = 0; k < 10; k++) vh.AddTriangle(i0, i0 + k + 1, i0 + k + 2);
    }

    // Marca de AtomAR: la tarjeta sobre la mesa y el átomo que aparece encima de ella
    void Logo()
    {
        // la tarjeta, vista acostada sobre la mesa, con su recuadro blanco al centro (como las tarjetas reales)
        Vector2 a = new(-0.44f, -0.45f), b = new(0.44f, -0.45f), c = new(0.30f, -0.13f), d = new(-0.30f, -0.13f);
        Cuadro(a, d, c, b, new Color(Cian.r, Cian.g, Cian.b, 0.16f));
        Cuadro(new Vector2(-0.14f, -0.385f), new Vector2(-0.095f, -0.205f), new Vector2(0.095f, -0.205f), new Vector2(0.14f, -0.385f),
               sobreClaro ? new Color(Cian.r, Cian.g, Cian.b, 0.55f) : new Color(1f, 1f, 1f, 0.92f));
        Linea(a, b, 0.042f, Cian); Linea(b, c, 0.042f, Cian); Linea(c, d, 0.042f, Cian); Linea(d, a, 0.042f, Cian);
        // el átomo que aparece encima
        var orbita = sobreClaro ? new Color(Noche.r, Noche.g, Noche.b, 0.30f) : new Color(1f, 1f, 1f, 0.40f);
        var o = new Vector2(0f, 0.205f);
        float g = animado ? t * 40f : 0f;
        Anillo(o, 0.255f, 0.022f, orbita); Anillo(o, 0.15f, 0.018f, orbita);
        Disco(o, 0.08f, Rojo, 40);
        Disco(o + Dir(35f + g) * 0.255f, 0.042f, Cian, 24);
        Disco(o + Dir(215f - g * 1.6f) * 0.15f, 0.036f, Cian, 24);
    }

    static Vector2 Dir(float grados) { float r = grados * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), Mathf.Sin(r)); }

    // ───────── Primitivas ─────────
    void Anillo(Vector2 c, float r, float w, Color col, int seg = 96)
    {
        int i0 = vh.currentVertCount;
        for (int k = 0; k <= seg; k++)
        {
            var d = Dir(k * 360f / seg);
            Vert(c + d * (r + w * 0.5f), col); Vert(c + d * (r - w * 0.5f), col);
        }
        for (int k = 0; k < seg; k++) { int j = i0 + k * 2; vh.AddTriangle(j, j + 2, j + 1); vh.AddTriangle(j + 1, j + 2, j + 3); }
    }

    void Vert(Vector2 p, Color col)
    {
        if (giro != 0f) { float a = giro * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a); p = new Vector2(p.x * c - p.y * s, p.x * s + p.y * c); }
        var v = UIVertex.simpleVert; v.position = centro + p * u; v.color = col * color; vh.AddVert(v);
    }

    void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        int i = vh.currentVertCount; Vert(a, col); Vert(b, col); Vert(c, col); vh.AddTriangle(i, i + 1, i + 2);
    }

    void Cuadro(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
    {
        int i = vh.currentVertCount; Vert(a, col); Vert(b, col); Vert(c, col); Vert(d, col);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }

    void Disco(Vector2 c, float r, Color col, int seg = 20)
    {
        int i0 = vh.currentVertCount; Vert(c, col);
        for (int k = 0; k <= seg; k++) { float a = k * Mathf.PI * 2f / seg; Vert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col); }
        for (int k = 0; k < seg; k++) vh.AddTriangle(i0, i0 + k + 1, i0 + k + 2);
    }

    void Linea(Vector2 a, Vector2 b, float ancho, Color col, bool puntas = true)
    {
        var n = (b - a).normalized; n = new Vector2(-n.y, n.x) * (ancho * 0.5f);
        Cuadro(a - n, a + n, b + n, b - n, col);
        if (puntas && col.a > 0.99f) { Disco(a, ancho * 0.5f, col, 10); Disco(b, ancho * 0.5f, col, 10); }
    }
}
