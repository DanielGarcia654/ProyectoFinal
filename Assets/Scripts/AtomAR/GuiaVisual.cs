// AtomAR - Guía animada sobre la cámara (sustituye al tutorial de texto).
// • Oscurece la pantalla y deja iluminado el marco donde va la tarjeta.
// • Dentro del marco se repite un ejemplo en 3D (DioramaGuia): la tarjeta real con luz y sombra, el átomo que aparece
//   y un dedo que enseña a girarlo y acercarlo; en Combinar, cómo se agrega un átomo y se forma la molécula.
// • Se cierra sola en cuanto el alumno lo logra de verdad, o con "Entendido". El botón "?" la vuelve a abrir.
// • Además da dos pistas de una sola vez en Explorar: "toca la carta" y cómo mover el átomo.
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
    [Header("Ejemplo en 3D")]
    public DioramaGuia diorama;
    public TMP_Text leyenda;
    public Button entendido;
    [Tooltip("Velocidad del ejemplo animado: 1 = normal, menos = más lento")]
    [Range(0.2f, 1.5f)] public float velocidad = 0.6f;
    [Header("Pista de la carta")]
    public FundidoUI pistaCarta;
    public RectTransform pistaPunto;

    static readonly Color Cian = new(0.18f, 0.90f, 1f);
    static readonly string[] TextosExplorar =
    {
        "Pon una tarjeta dentro del marco", "Deja el teléfono quieto un momento", "¡Y aparece su átomo!",
        "Gira el átomo con un dedo", "Acércalo con dos dedos",
    };
    static readonly string[] TextosCombinar =
    {
        "Junta dos tarjetas dentro del marco", "Con + agregas el átomo que falta", "¡Y se forma la molécula!",
    };
    const string ClavePista = "AtomAR.pista.carta", ClaveGestos = "AtomAR.pista.gestos";

    ModoApp modo; bool manual, pistaVista, gestosVista;
    TMP_Text pistaTexto; CanvasGroup grupo;
    float t; int fase = -1;

    static string Clave(ModoApp m) => "AtomAR.guia." + m;
    public static bool Vista(ModoApp m) => PlayerPrefs.GetInt(Clave(m), 0) == 1;
    public static void Olvidar()
    {
        PlayerPrefs.DeleteKey(Clave(ModoApp.Explorar)); PlayerPrefs.DeleteKey(Clave(ModoApp.Combinar));
        PlayerPrefs.DeleteKey(ClavePista); PlayerPrefs.DeleteKey(ClaveGestos);
    }

    void Awake()
    {
        entendido.onClick.AddListener(() => Cerrar(true));
        pistaVista = PlayerPrefs.GetInt(ClavePista, 0) == 1;
        gestosVista = PlayerPrefs.GetInt(ClaveGestos, 0) == 1;
        pistaTexto = pistaCarta.GetComponentInChildren<TMP_Text>(true);
        grupo = fundido.GetComponent<CanvasGroup>();
        Activa = false;
    }

    /// manual = la abrió el alumno con "?" (entonces no se cierra sola).
    public void Mostrar(ModoApp m, bool esManual)
    {
        modo = m; manual = esManual; t = 0f; fase = -1; Activa = true;
        var tam = m == ModoApp.Combinar ? new Vector2(900f, 470f) : new Vector2(560f, 620f);
        PonerHueco(tam, m == ModoApp.Combinar ? 300f : 110f);
        diorama.Preparar(m, tam);
        fundido.Mostrar(true);
    }

    public void Cerrar(bool marcarVista)
    {
        if (!Activa) return;
        Activa = false;
        if (marcarVista) { PlayerPrefs.SetInt(Clave(modo), 1); PlayerPrefs.Save(); }
        fundido.Mostrar(false);
    }

    void PonerHueco(Vector2 tam, float cy)
    {
        float w = tam.x, h = tam.y;
        veloArriba.offsetMin = new Vector2(0f, cy + h * 0.5f); veloArriba.offsetMax = Vector2.zero;
        veloAbajo.offsetMin = Vector2.zero; veloAbajo.offsetMax = new Vector2(0f, cy - h * 0.5f);
        veloIzq.offsetMin = new Vector2(0f, cy - h * 0.5f); veloIzq.offsetMax = new Vector2(-w * 0.5f, cy + h * 0.5f);
        veloDer.offsetMin = new Vector2(w * 0.5f, cy - h * 0.5f); veloDer.offsetMax = new Vector2(0f, cy + h * 0.5f);
        marco.anchoredPosition = escena.anchoredPosition = marcoImagen.rectTransform.anchoredPosition = new Vector2(0f, cy);
        marco.sizeDelta = marcoImagen.rectTransform.sizeDelta = tam;
        leyenda.rectTransform.anchoredPosition = new Vector2(0f, cy - h * 0.5f - 86f);
        ((RectTransform)entendido.transform).anchoredPosition = new Vector2(0f, cy - h * 0.5f - 214f);
    }

    void Update()
    {
        ActualizarPista();
        diorama.Activar(Activa || grupo.alpha > 0.01f);       // la escena 3D solo se dibuja mientras se ve
        if (!Activa) return;
        if (AppAtomAR.Modo != modo) { Cerrar(false); return; }

        // ¿Ya lo logró de verdad? Entonces la guía sobra.
        int visibles = 0;
        foreach (var x in ElementoTarget.Todos) if (x && x.Visible) visibles++;
        if (!manual && ((modo == ModoApp.Explorar && visibles >= 1) || (modo == ModoApp.Combinar && visibles >= 2))) { Cerrar(true); return; }

        t += Time.unscaledDeltaTime * velocidad;
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        marcoImagen.color = Color.Lerp(Cian, Color.white, pulso * 0.45f);
        marcoImagen.rectTransform.localScale = Vector3.one * (1f + 0.012f * pulso);

        int f = diorama.Animar(modo, t);
        if (f != fase)
        {
            fase = f;
            var textos = modo == ModoApp.Combinar ? TextosCombinar : TextosExplorar;
            leyenda.text = textos[Mathf.Clamp(f, 0, textos.Length - 1)];
        }
    }

    // Pistas de una sola vez en Explorar: primero "toca la carta" y, ya que la abrió, cómo mover el átomo
    void ActualizarPista()
    {
        bool enExplorar = AppAtomAR.Modo == ModoApp.Explorar;
        if (!pistaVista && enExplorar && PanelElemento.Desplegada) { pistaVista = true; PlayerPrefs.SetInt(ClavePista, 1); PlayerPrefs.Save(); }
        if (!gestosVista && GestosAtomo.Usado) { gestosVista = true; PlayerPrefs.SetInt(ClaveGestos, 1); PlayerPrefs.Save(); }
        bool puede = enExplorar && !Activa && PanelElemento.HayCarta && !PanelElemento.Desplegada;
        bool verCarta = puede && !pistaVista, verGestos = puede && pistaVista && !gestosVista, ver = verCarta || verGestos;
        if (ver)
        {
            string txt = verCarta ? "Toca la carta para ver más" : "Gira el átomo con un dedo y acércalo con dos";
            if (pistaTexto && pistaTexto.text != txt)
            {
                pistaTexto.text = txt;
                var rt = (RectTransform)pistaCarta.transform;
                rt.sizeDelta = new Vector2(pistaTexto.GetPreferredValues(txt).x + 130f, rt.sizeDelta.y);
            }
            pistaPunto.localScale = Vector3.one * (1f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)));
        }
        if (pistaCarta.Visible != ver) pistaCarta.Mostrar(ver);
    }
}
