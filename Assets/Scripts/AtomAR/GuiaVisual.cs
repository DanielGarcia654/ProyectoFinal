// AtomAR - Guía animada sobre la cámara (sustituye al tutorial de texto).
// • Oscurece la pantalla y deja iluminado el marco donde va la tarjeta.
// • Dentro del marco se repite una animación de ejemplo: en Explorar entra una tarjeta y aparece su átomo;
//   en Combinar se juntan dos tarjetas, se agrega un átomo y se forma la molécula.
// • Se cierra sola en cuanto el alumno lo logra de verdad, o con "Entendido". El botón "?" la vuelve a abrir.
// • Además, la primera vez que aparece una carta muestra la pista "Toca la carta para ver más".
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuiaVisual : MonoBehaviour
{
    public static bool Activa { get; private set; }

    public FundidoUI fundido;
    [Header("Marco iluminado")]
    public RectTransform veloArriba;
    public RectTransform veloAbajo, veloIzq, veloDer, marco, escena;
    public Image marcoImagen;
    [Header("Ejemplo animado")]
    public RectTransform tarjetaA;
    public RectTransform tarjetaB, atomo, molecula, mas;
    public CanvasGroup grupoA, grupoB, grupoAtomo, grupoMolecula, grupoMas;
    public TMP_Text leyenda;
    public Button entendido;
    [Header("Pista de la carta")]
    public FundidoUI pistaCarta;
    public RectTransform pistaPunto;

    static readonly Color Cian = new(0.18f, 0.90f, 1f);
    const string ClavePista = "AtomAR.pista.carta";

    ModoApp modo; bool manual, pistaVista;
    float t; int fase = -1;

    static string Clave(ModoApp m) => "AtomAR.guia." + m;
    public static bool Vista(ModoApp m) => PlayerPrefs.GetInt(Clave(m), 0) == 1;
    public static void Olvidar()
    {
        PlayerPrefs.DeleteKey(Clave(ModoApp.Explorar)); PlayerPrefs.DeleteKey(Clave(ModoApp.Combinar)); PlayerPrefs.DeleteKey(ClavePista);
    }

    void Awake()
    {
        entendido.onClick.AddListener(() => Cerrar(true));
        pistaVista = PlayerPrefs.GetInt(ClavePista, 0) == 1;
        Activa = false;
    }

    /// manual = la abrió el alumno con "?" (entonces no se cierra sola).
    public void Mostrar(ModoApp m, bool esManual)
    {
        modo = m; manual = esManual; t = 0f; fase = -1; Activa = true;
        if (m == ModoApp.Combinar) PonerHueco(900f, 470f, 300f); else PonerHueco(520f, 520f, 80f);
        grupoA.alpha = grupoB.alpha = grupoAtomo.alpha = grupoMolecula.alpha = grupoMas.alpha = 0f;
        fundido.Mostrar(true);
    }

    public void Cerrar(bool marcarVista)
    {
        if (!Activa) return;
        Activa = false;
        if (marcarVista) { PlayerPrefs.SetInt(Clave(modo), 1); PlayerPrefs.Save(); }
        fundido.Mostrar(false);
    }

    void PonerHueco(float w, float h, float cy)
    {
        veloArriba.offsetMin = new Vector2(0f, cy + h * 0.5f); veloArriba.offsetMax = Vector2.zero;
        veloAbajo.offsetMin = Vector2.zero; veloAbajo.offsetMax = new Vector2(0f, cy - h * 0.5f);
        veloIzq.offsetMin = new Vector2(0f, cy - h * 0.5f); veloIzq.offsetMax = new Vector2(-w * 0.5f, cy + h * 0.5f);
        veloDer.offsetMin = new Vector2(w * 0.5f, cy - h * 0.5f); veloDer.offsetMax = new Vector2(0f, cy + h * 0.5f);
        marco.anchoredPosition = escena.anchoredPosition = new Vector2(0f, cy);
        marco.sizeDelta = new Vector2(w, h);
        leyenda.rectTransform.anchoredPosition = new Vector2(0f, cy - h * 0.5f - 86f);
        ((RectTransform)entendido.transform).anchoredPosition = new Vector2(0f, cy - h * 0.5f - 214f);
    }

    void Update()
    {
        ActualizarPista();
        if (!Activa) return;
        if (AppAtomAR.Modo != modo) { Cerrar(false); return; }

        // ¿Ya lo logró de verdad? Entonces la guía sobra.
        int visibles = ElementoTarget.Todos.Count(x => x && x.Visible);
        if (!manual && ((modo == ModoApp.Explorar && visibles >= 1) || (modo == ModoApp.Combinar && visibles >= 2))) { Cerrar(true); return; }

        t += Time.unscaledDeltaTime;
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        marcoImagen.color = Color.Lerp(Cian, Color.white, pulso * 0.45f);
        marcoImagen.rectTransform.localScale = Vector3.one * (1f + 0.012f * pulso);

        if (modo == ModoApp.Combinar) AnimarCombinar(); else AnimarExplorar();
    }

    // Explorar: entra la tarjeta → el teléfono la lee → aparece el átomo
    void AnimarExplorar()
    {
        const float ciclo = 5.4f; float u = t % ciclo, salida = u > 4.7f ? 1f - (u - 4.7f) / 0.7f : 1f;
        float a = Suave(u / 0.9f);
        grupoA.alpha = a * 0.92f * salida;
        tarjetaA.anchoredPosition = Vector2.Lerp(new Vector2(250f, -400f), new Vector2(0f, -70f), a);
        tarjetaA.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-22f, -5f, a));
        float k = Mathf.Clamp01((u - 2.1f) / 0.5f);
        grupoAtomo.alpha = k * salida;
        atomo.anchoredPosition = new Vector2(0f, Mathf.Lerp(70f, 150f, Suave(k)));
        atomo.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, Rebote(k));
        grupoB.alpha = grupoMolecula.alpha = grupoMas.alpha = 0f;
        Fase(u < 0.9f ? 0 : u < 2.1f ? 1 : 2,
            "Pon una tarjeta dentro del marco", "Deja el teléfono quieto un momento", "¡Y aparece su átomo!");
    }

    // Combinar: se juntan dos tarjetas → se agrega un átomo con + → se forma la molécula
    void AnimarCombinar()
    {
        const float ciclo = 6.8f; float u = t % ciclo, salida = u > 6.1f ? 1f - (u - 6.1f) / 0.7f : 1f;
        float a = Suave(u / 1f);
        grupoA.alpha = grupoB.alpha = a * 0.92f * salida;
        tarjetaA.anchoredPosition = Vector2.Lerp(new Vector2(-480f, -40f), new Vector2(-150f, -75f), a);
        tarjetaB.anchoredPosition = Vector2.Lerp(new Vector2(480f, -40f), new Vector2(150f, -75f), a);
        tarjetaA.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, -4f, a));
        tarjetaB.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(14f, 4f, a));
        float k = Mathf.Clamp01((u - 1.6f) / 0.35f), fuera = 1f - Mathf.Clamp01((u - 2.9f) / 0.3f);
        grupoMas.alpha = k * fuera * salida;
        mas.anchoredPosition = new Vector2(-150f, 95f);
        mas.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Rebote(k));
        float m = Mathf.Clamp01((u - 3.1f) / 0.5f);
        grupoMolecula.alpha = m * salida;
        molecula.anchoredPosition = new Vector2(0f, Mathf.Lerp(70f, 130f, Suave(m)));
        molecula.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, Rebote(m));
        grupoAtomo.alpha = 0f;
        Fase(u < 1f ? 0 : u < 3.1f ? 1 : 2,
            "Junta dos tarjetas dentro del marco", "Con + agregas los átomos que faltan", "¡Y se forma la molécula!");
    }

    void Fase(int f, string a, string b, string c)
    {
        if (f == fase) return;
        fase = f; leyenda.text = f == 0 ? a : f == 1 ? b : c;
    }

    void ActualizarPista()
    {
        bool enExplorar = AppAtomAR.Modo == ModoApp.Explorar;
        if (!pistaVista && enExplorar && PanelElemento.Desplegada)
        {
            pistaVista = true; PlayerPrefs.SetInt(ClavePista, 1); PlayerPrefs.Save();
        }
        bool ver = enExplorar && !Activa && !pistaVista && PanelElemento.HayCarta && !PanelElemento.Desplegada;
        if (pistaCarta.Visible != ver) pistaCarta.Mostrar(ver);
        if (ver) pistaPunto.localScale = Vector3.one * (1f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)));
    }

    static float Suave(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }             // ease-out
    static float Rebote(float x) { x = Mathf.Clamp01(x); const float c = 1.70158f; float y = x - 1f; return 1f + (c + 1f) * y * y * y + c * y * y; }
}
