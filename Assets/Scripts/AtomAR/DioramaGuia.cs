// AtomAR - Ejemplo en 3D para la guía animada.
// Una mini escena con las tarjetas reales acostadas en perspectiva, luz cálida, un contraluz frío, sombras suaves
// y los mismos átomos y moléculas 3D de la app. Se dibuja en una RenderTexture que la guía muestra dentro del marco
// iluminado, así el alumno ve exactamente lo que va a pasar en su cámara.
// Encima, dos "yemas de dedo" translúcidas con sombra enseñan los gestos (girar, pellizcar, tocar +).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DioramaGuia : MonoBehaviour
{
    public const int Capa = 28;

    [Header("Dónde se ve")]
    public RawImage vista;
    public RectTransform[] dedos;
    public RectTransform botonMas;
    [Tooltip("Resolución de la imagen respecto al tamaño del marco")]
    public float resolucion = 1.5f;

    [Header("Piezas")]
    public Texture cartaC;
    public Texture cartaH, cartaO, sombra;
    public GameObject atomoC, atomoH, atomoO, moleculaAgua;

    class Pieza
    {
        public Transform t, sombra; public Material mat;
        public void Ver(bool v)
        {
            if (t.gameObject.activeSelf != v) t.gameObject.SetActive(v);
            if (sombra && sombra.gameObject.activeSelf != v) sombra.gameObject.SetActive(v);
        }
    }

    static readonly Color Tinta = new(0.01f, 0.02f, 0.07f, 1f);
    static readonly Color Cian = new(0.18f, 0.90f, 1f, 1f);

    Transform raiz, escaneo; Material escaneoMat;
    Camera cam; RenderTexture rt;
    Pieza cC, cH, cO, aC, aH1, aH2, aO, mol;

    // ───────── Control desde la guía ─────────
    public void Activar(bool si)
    {
        if (!raiz) return;
        if (raiz.gameObject.activeSelf != si) raiz.gameObject.SetActive(si);
        cam.enabled = si;
    }

    public void Preparar(ModoApp modo, Vector2 tam)
    {
        Construir();
        vista.rectTransform.sizeDelta = tam;
        int w = Mathf.RoundToInt(tam.x * resolucion), h = Mathf.RoundToInt(tam.y * resolucion);
        if (!rt || rt.width != w || rt.height != h)
        {
            if (rt) { cam.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt; vista.texture = rt;
        }
        bool comb = modo == ModoApp.Combinar;
        cam.transform.localPosition = comb ? new Vector3(0f, 1.95f, -2.95f) : new Vector3(0f, 1.75f, -2.55f);
        cam.transform.LookAt(raiz.TransformPoint(0f, comb ? 0.52f : 0.5f, 0f));
        cam.fieldOfView = comb ? 30f : 33f;
        cC.Ver(!comb); cH.Ver(comb); cO.Ver(comb);
        aC.Ver(false); aH1.Ver(false); aH2.Ver(false); aO.Ver(false); mol.Ver(false);
        escaneo.gameObject.SetActive(false);
        Excluir();
        Activar(true);
    }

    /// Coloca todo para el instante u (segundos) del ejemplo y devuelve en qué paso va.
    public int Animar(ModoApp modo, float u) => modo == ModoApp.Combinar ? Combinar(u) : Explorar(u);

    void OnDestroy()
    {
        if (rt) { rt.Release(); Destroy(rt); }
        if (raiz) Destroy(raiz.gameObject);
    }

    // ───────── Explorar: tarjeta → se lee → átomo → girar → acercar ─────────
    int Explorar(float u)
    {
        const float T = 8.6f; float s = u % T, fade = Fade(s, T);
        vista.color = new Color(1f, 1f, 1f, fade);

        float a = Suave(s / 1.3f);
        cC.t.localPosition = Vector3.Lerp(new Vector3(0.95f, 0f, -0.9f), Vector3.zero, a);
        cC.t.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-34f, -7f, a), 0f);

        // el teléfono "lee" la tarjeta: una línea de luz la recorre
        bool lee = s > 1.3f && s < 2.7f;
        escaneo.gameObject.SetActive(lee);
        if (lee)
        {
            float k0 = (s - 1.3f) / 1.4f;
            escaneo.localPosition = new Vector3(0f, 0.02f, Mathf.Lerp(-0.47f, 0.47f, Mathf.PingPong(k0 * 2f, 1f)));
            escaneoMat.color = new Color(Cian.r, Cian.g, Cian.b, 0.85f * Mathf.Sin(k0 * Mathf.PI));
        }

        // aparece el átomo; luego un dedo lo gira y dos lo acercan
        float k = Mathf.Clamp01((s - 2.7f) / 0.7f);
        float giro = 200f * SuaveIO(Mathf.Clamp01((s - 3.9f) / 1.1f)) + (s > 5f ? 45f * (1f - Mathf.Exp(-(s - 5f) * 4f)) : 0f);
        float zoom = 1f + 0.35f * SuaveIO(Mathf.Clamp01((s - 5.9f) / 1.0f));
        aC.Ver(k > 0f);
        if (k > 0f)
        {
            aC.t.localPosition = new Vector3(0f, Mathf.Lerp(0.25f, 0.72f, Suave(k)) + (zoom - 1f) * 0.2f, 0f);
            aC.t.localRotation = Quaternion.Euler(0f, -giro, 0f);
            aC.t.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Rebote(k)) * zoom;
            Sombra(aC, 0.02f, k, 0.8f);
        }

        Vector2 centro = Vp(new Vector3(0f, 0.72f, 0f));
        if (s < 5.5f)
        {
            float g = SuaveIO(Mathf.Clamp01((s - 3.9f) / 1.1f));
            Dedo(0, centro + new Vector2(Mathf.Lerp(-0.17f, 0.17f, g), -0.03f), Ventana(s, 3.6f, 5.1f) * fade, Presion(s, 3.8f, 5.05f), s - 3.8f);
            Dedo(1, centro, 0f, 0f, -1f);
        }
        else
        {
            var sep = Vector2.Lerp(new Vector2(0.05f, 0.05f), new Vector2(0.19f, 0.16f), SuaveIO(Mathf.Clamp01((s - 5.9f) / 1.0f)));
            float al = Ventana(s, 5.6f, 7.0f) * fade, pr = Presion(s, 5.8f, 6.95f);
            Dedo(0, centro - sep, al, pr, s - 5.8f);
            Dedo(1, centro + sep, al, pr, s - 5.8f);
        }
        Mas(Vector2.zero, 0f, 1f);
        return s < 1.3f ? 0 : s < 2.7f ? 1 : s < 3.6f ? 2 : s < 5.6f ? 3 : 4;
    }

    // ───────── Combinar: dos tarjetas → un átomo en cada una → +1 → se forma la molécula ─────────
    int Combinar(float u)
    {
        const float T = 9f; float s = u % T, fade = Fade(s, T);
        vista.color = new Color(1f, 1f, 1f, fade);

        float a = Suave(s / 1.3f);
        cH.t.localPosition = Vector3.Lerp(new Vector3(-1.7f, 0f, -0.4f), new Vector3(-0.62f, 0f, 0f), a);
        cH.t.localRotation = Quaternion.Euler(0f, Mathf.Lerp(28f, 5f, a), 0f);
        cO.t.localPosition = Vector3.Lerp(new Vector3(1.7f, 0f, -0.4f), new Vector3(0.62f, 0f, 0f), a);
        cO.t.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-28f, -5f, a), 0f);

        float k = Mathf.Clamp01((s - 1.2f) / 0.6f), k2 = Mathf.Clamp01((s - 3.45f) / 0.45f);
        float m = SuaveIO(Mathf.Clamp01((s - 4.4f) / 0.8f)), q = Mathf.Clamp01((s - 5.2f) / 0.55f);
        var centro = new Vector3(0f, 0.6f, 0f);
        float y = Mathf.Lerp(0.2f, 0.55f, Suave(k)), giro = s * 25f, par = Mathf.Lerp(1f, 0.8f, Suave(k2));
        float encoge = Mathf.Lerp(1f, 0.3f, m), suelo = Mathf.Lerp(0.02f, -0.013f, m);
        var pH1 = new Vector3(Mathf.Lerp(-0.62f, -0.79f, Suave(k2)), y, 0f);
        var pH2 = new Vector3(Mathf.Lerp(-0.62f, -0.45f, Suave(k2)), y, 0f);
        Colocar(aH1, k > 0f && m < 1f, Vector3.Lerp(pH1, centro, m), Mathf.Lerp(0.3f, 1f, Rebote(k)) * par * encoge, giro, k * (1f - m), suelo);
        Colocar(aH2, k2 > 0f && m < 1f, Vector3.Lerp(pH2, centro, m), Mathf.Lerp(0.3f, 1f, Rebote(k2)) * par * encoge, -giro, k2 * (1f - m), suelo);
        Colocar(aO, k > 0f && m < 1f, Vector3.Lerp(new Vector3(0.62f, y, 0f), centro, m), Mathf.Lerp(0.3f, 1f, Rebote(k)) * encoge, giro * 0.8f, k * (1f - m), suelo);
        Colocar(mol, q > 0f, centro + new Vector3(0f, 0.03f * Mathf.Sin(s * 2f), 0f), Mathf.Lerp(0.2f, 1f, Rebote(q)), (s - 5.2f) * 40f, q, -0.013f);

        // el botón +1 aparece sobre el hidrógeno y un dedo lo toca
        var vpMas = Vp(new Vector3(-0.62f, 0.98f, 0f));
        float pop = Rebote(Mathf.Clamp01((s - 2.2f) / 0.35f));
        Mas(vpMas, Ventana(s, 2.2f, 4.0f) * fade, Mathf.Lerp(0.6f, 1f, pop) * Mathf.Lerp(1f, 0.9f, Presion(s, 3.3f, 3.45f)));
        float d = Suave(Mathf.Clamp01((s - 2.7f) / 0.5f));
        Dedo(0, Vector2.Lerp(vpMas + new Vector2(0.10f, -0.22f), vpMas + new Vector2(0.01f, -0.01f), d), Ventana(s, 2.6f, 3.6f) * fade, Presion(s, 3.3f, 3.45f), s - 3.3f);
        Dedo(1, Vector2.zero, 0f, 0f, -1f);
        return s < 2.2f ? 0 : s < 4.4f ? 1 : 2;
    }

    // ───────── Construcción de la mini escena ─────────
    void Construir()
    {
        if (raiz) return;
        raiz = new GameObject("DioramaGuia").transform;
        raiz.position = new Vector3(0f, -1000f, 0f);
        var camGO = new GameObject("Camara"); camGO.transform.SetParent(raiz, false);
        cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = 1 << Capa; cam.nearClipPlane = 0.05f; cam.farClipPlane = 20f; cam.allowHDR = false; cam.depth = -50f;
        Luz("Luz", new Vector3(50f, -35f, 0f), new Color(1f, 0.96f, 0.9f), 1.15f);          // luz cálida desde arriba
        Luz("Contraluz", new Vector3(15f, 150f, 0f), new Color(0.55f, 0.8f, 1f), 0.55f);     // borde frío que separa del fondo

        cC = Carta(cartaC); cH = Carta(cartaH); cO = Carta(cartaO);
        aC = Atomo(atomoC, 0.78f); aH1 = Atomo(atomoH, 0.5f); aH2 = Atomo(atomoH, 0.5f); aO = Atomo(atomoO, 0.62f);
        mol = Atomo(moleculaAgua, 0.78f);

        escaneo = Plano("Escaneo", cC.t, null, Cian);
        escaneo.localPosition = new Vector3(0f, 0.02f, 0f); escaneo.localScale = new Vector3(1.04f, 0.045f, 1f);
        escaneoMat = escaneo.GetComponent<Renderer>().sharedMaterial;
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Capa;
    }

    void Luz(string nombre, Vector3 rot, Color c, float intensidad)
    {
        var go = new GameObject(nombre); go.transform.SetParent(raiz, false); go.transform.localRotation = Quaternion.Euler(rot);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional; l.color = c; l.intensity = intensidad; l.cullingMask = 1 << Capa; l.shadows = LightShadows.None;
    }

    Pieza Carta(Texture tex)
    {
        var h = new GameObject("Tarjeta").transform; h.SetParent(raiz, false);
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube); cubo.name = "Carton";
        Destroy(cubo.GetComponent<Collider>());
        cubo.transform.SetParent(h, false); cubo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); cubo.transform.localScale = new Vector3(1f, 0.03f, 1f);
        var m = new Material(Shader.Find("Standard")) { mainTexture = tex }; m.SetFloat("_Glossiness", 0.35f);
        var r = cubo.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        var s = Plano("Sombra", h, sombra, new Color(Tinta.r, Tinta.g, Tinta.b, 0.6f));
        s.localPosition = new Vector3(0.06f, -0.02f, 0.06f); s.localScale = Vector3.one * 1.55f;
        return new Pieza { t = h };
    }

    Pieza Atomo(GameObject prefab, float tam)
    {
        var h = new GameObject(prefab ? prefab.name : "Atomo").transform; h.SetParent(raiz, false);
        if (prefab)
        {
            var m = Instantiate(prefab, h).transform;
            m.localPosition = Vector3.zero; m.localRotation = Quaternion.identity; m.localScale = Vector3.one;
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (max > 0f) { float f = tam / max; m.localScale = Vector3.one * f; m.localPosition = -h.InverseTransformPoint(b.center) * f; }
            }
            foreach (var r in rs) r.shadowCastingMode = ShadowCastingMode.Off;
            if (!m.GetComponent<AtomoAnimado>()) m.gameObject.AddComponent<AtomoAnimado>();
        }
        var s = Plano("SombraAtomo", raiz, sombra, new Color(Tinta.r, Tinta.g, Tinta.b, 0f));
        return new Pieza { t = h, sombra = s, mat = s.GetComponent<Renderer>().sharedMaterial };
    }

    Transform Plano(string nombre, Transform padre, Texture tex, Color c)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = nombre;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false); q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = c };
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return q.transform;
    }

    /// Que las demás cámaras y luces de la escena no vean ni iluminen este ejemplo.
    void Excluir()
    {
        int bit = 1 << Capa;
        foreach (var c in Camera.allCameras) if (c != cam) c.cullingMask &= ~bit;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (!l.transform.IsChildOf(raiz)) l.cullingMask &= ~bit;
    }

    // ───────── Piezas en movimiento ─────────
    void Colocar(Pieza p, bool ver, Vector3 pos, float esc, float giro, float alfaSombra, float suelo)
    {
        p.Ver(ver);
        if (!ver) return;
        p.t.localPosition = pos; p.t.localScale = Vector3.one * esc; p.t.localRotation = Quaternion.Euler(0f, giro, 0f);
        Sombra(p, suelo, alfaSombra, 0.8f);
    }

    // sombra suave: más chica y más intensa cuanto más cerca está del suelo
    void Sombra(Pieza p, float suelo, float alfa, float tam)
    {
        var pos = p.t.localPosition;
        float altura = Mathf.Max(0f, pos.y - suelo);
        p.sombra.localPosition = new Vector3(pos.x + 0.04f, suelo, pos.z + 0.05f);
        p.sombra.localScale = Vector3.one * tam * p.t.localScale.x * (1f + altura * 0.6f);
        p.mat.color = new Color(Tinta.r, Tinta.g, Tinta.b, 0.5f * alfa / (1f + altura * 1.5f));
    }

    void Dedo(int i, Vector2 vp, float alfa, float presion, float tOnda)
    {
        if (dedos == null || i >= dedos.Length || !dedos[i]) return;
        var d = dedos[i];
        d.GetComponent<CanvasGroup>().alpha = alfa;
        if (alfa <= 0.001f) return;
        d.anchoredPosition = Pos(vp);
        d.Find("Yema").localScale = Vector3.one * Mathf.Lerp(1f, 0.86f, presion);              // la yema se aplasta al tocar
        var so = (RectTransform)d.Find("Sombra");
        so.anchoredPosition = Vector2.Lerp(new Vector2(10f, -18f), new Vector2(3f, -5f), presion);  // y su sombra se pega al vidrio
        so.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.78f, presion);
        var onda = d.Find("Onda").GetComponent<Graphic>();
        float q = tOnda >= 0f ? Mathf.Clamp01(tOnda / 0.55f) : 1f;
        onda.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 2.3f, Suave(q));
        onda.color = new Color(1f, 1f, 1f, 0.8f * (1f - q));
    }

    void Mas(Vector2 vp, float alfa, float escala)
    {
        if (!botonMas) return;
        botonMas.GetComponent<CanvasGroup>().alpha = alfa;
        if (alfa <= 0.001f) return;
        botonMas.anchoredPosition = Pos(vp);
        botonMas.localScale = Vector3.one * escala;
    }

    Vector2 Vp(Vector3 local) { var v = cam.WorldToViewportPoint(raiz.TransformPoint(local)); return new Vector2(v.x, v.y); }

    Vector2 Pos(Vector2 vp) { var r = vista.rectTransform.rect; return new Vector2((vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height); }

    // ───────── Curvas ─────────
    static float Fade(float s, float T) => Mathf.Clamp01(s / 0.3f) * (1f - Mathf.Clamp01((s - (T - 0.6f)) / 0.6f));
    static float Ventana(float s, float a, float b) => Mathf.Clamp01((s - a) / 0.2f) * (1f - Mathf.Clamp01((s - b) / 0.25f));
    static float Presion(float s, float a, float b) => Mathf.Clamp01((s - a) / 0.12f) * (1f - Mathf.Clamp01((s - b) / 0.12f));
    static float Suave(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
    static float SuaveIO(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f; }
    static float Rebote(float x) { x = Mathf.Clamp01(x); const float c = 1.70158f; float y = x - 1f; return 1f + (c + 1f) * y * y * y + c * y * y; }
}
