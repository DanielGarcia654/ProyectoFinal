// AtomAR - Controla en qué modo está la app y qué pantallas se ven.
//   Inicio   → recorrido con los tres modos y el avance de cada uno
//   Explorar → cámara + carta del elemento (PanelElemento)
//   Combinar → cámara + panel para armar moléculas (ModoCombinar)
//   Reto     → preguntas e insignias, sin cámara (Quiz)
// Ningún modo está bloqueado: el inicio solo marca con luz cuál conviene seguir, y en la barra superior
// hay un botón para saltar al siguiente modo sin volver al inicio. Al completar un modo aparece la celebración.
// Progreso: al cambiar VersionDatos se borra el progreso guardado de instalaciones anteriores (una sola vez).
// Para demos y pruebas con usuarios: mantener presionado el título "AtomAR" del inicio 3 s borra todo el progreso.
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ModoApp { Inicio, Explorar, Combinar, Reto }

public class AppAtomAR : MonoBehaviour
{
    public static ModoApp Modo { get; private set; }
    static AppAtomAR instancia;

    [Header("Pantallas")]
    public FundidoUI pantallaInicio;
    public FundidoUI barra, pantallaReto;
    public GuiaVisual guia;
    public Celebracion celebracion;
    public Quiz quiz;

    [Header("Inicio (en orden: Explorar, Combinar, Reto)")]
    public Button[] botonesModo;
    public TMP_Text[] progresos;
    public AnilloUI[] anillos;
    public CanvasGroup[] halos;
    public GameObject[] etiquetasSigue;

    [Header("Barra superior")]
    public Button botonAtras;
    public Button botonAyuda, botonSiguiente;
    public TMP_Text tituloBarra, siguienteTexto;
    public Image siguienteMarco, siguienteHaloImagen;
    public CanvasGroup siguienteHalo;
    public GameObject pastillaColeccion, pastillaMoleculas;

    static readonly ModoApp[] Orden = { ModoApp.Explorar, ModoApp.Combinar, ModoApp.Reto };
    static readonly Color Cian = new(0.18f, 0.90f, 1f), Rojo = new(0.95f, 0.27f, 0.31f), Oro = new(1f, 0.78f, 0.24f);
    static readonly Color TintaOscura = new(0.08f, 0.09f, 0.20f);
    const string ClaveElementos = "AtomAR.celebrado.elementos", ClaveMoleculas = "AtomAR.celebrado.moleculas";
    const string ClaveVersion = "AtomAR.versionDatos";
    const int VersionDatos = 2;                 // súbelo para que una nueva versión empiece desde cero
    const float SegundosReinicio = 3f;

    int recomendado;
    float esperaLogro = -1f;
    TMP_Text tituloInicio; Color tituloColor; float presionado = -1f; bool habiaToque;

    /// Borra colección, moléculas, puntos, guías vistas y celebraciones.
    public static void BorrarProgreso()
    {
        Coleccion.Reiniciar(); ProgresoQuiz.Reiniciar(); GuiaVisual.Olvidar(); OlvidarLogros();
        PlayerPrefs.Save();
    }

    // Antes de cargar la escena: si el progreso guardado es de otra versión de la app, se empieza de cero
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RevisarVersionDatos()
    {
        if (PlayerPrefs.GetInt(ClaveVersion, 0) == VersionDatos) return;
        BorrarProgreso();
        PlayerPrefs.SetInt(ClaveVersion, VersionDatos); PlayerPrefs.Save();
    }

    /// Color que identifica a cada modo.
    public static Color ColorModo(ModoApp m) => m == ModoApp.Combinar ? Rojo : m == ModoApp.Reto ? Oro : Cian;

    /// Texto oscuro sobre colores claros y blanco sobre colores oscuros.
    public static Color Tinta(Color fondo)
    {
        float lum = 0.2126f * fondo.r + 0.7152f * fondo.g + 0.0722f * fondo.b;
        return lum > 0.5f ? TintaOscura : Color.white;
    }

    public static void Ir(ModoApp modo) { if (instancia) instancia.Cambiar(modo, false); }

    public static void OlvidarLogros() { PlayerPrefs.DeleteKey(ClaveElementos); PlayerPrefs.DeleteKey(ClaveMoleculas); }

    static ModoApp Siguiente(ModoApp m) => m == ModoApp.Explorar ? ModoApp.Combinar : m == ModoApp.Combinar ? ModoApp.Reto : ModoApp.Explorar;

    static bool Completo(ModoApp m) =>
        (m == ModoApp.Explorar && Coleccion.Cantidad >= Coleccion.TotalElementos) ||
        (m == ModoApp.Combinar && Coleccion.Moleculas >= Coleccion.TotalMoleculas);

    void Awake()
    {
        instancia = this;
        Modo = ModoApp.Inicio;
        for (int i = 0; i < botonesModo.Length; i++) { var m = Orden[i]; botonesModo[i].onClick.AddListener(() => Cambiar(m, false)); }
        botonAtras.onClick.AddListener(Atras);
        botonAyuda.onClick.AddListener(() => guia.Mostrar(Modo, true));
        botonSiguiente.onClick.AddListener(() => Cambiar(Siguiente(Modo), false));
        foreach (var t in pantallaInicio.GetComponentsInChildren<TMP_Text>(true))
            if (t.text == "AtomAR") { tituloInicio = t; tituloColor = t.color; break; }
    }

    void Start() => Cambiar(ModoApp.Inicio, true);

    void OnDestroy() { if (instancia == this) instancia = null; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime, pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Atras();       // botón "atrás" de Android

        // En el inicio se ilumina el modo que conviene seguir
        for (int i = 0; i < halos.Length; i++)
            halos[i].alpha = Mathf.MoveTowards(halos[i].alpha, i == recomendado ? 0.30f + 0.35f * pulso : 0f, dt * 3f);

        // Si el modo actual ya está completo, el botón para pasar al siguiente se ilumina
        siguienteHalo.alpha = Mathf.MoveTowards(siguienteHalo.alpha, Completo(Modo) ? 0.35f + 0.5f * pulso : 0f, dt * 3f);

        RevisarLogros(dt);
        RevisarReinicioOculto(dt);
    }

    // Mantener presionado el título del inicio: el título se agranda y se tiñe de cian; a los 3 s se borra el progreso
    void RevisarReinicioOculto(float dt)
    {
        if (!tituloInicio) return;
        Vector2? punto = null;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) punto = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed) punto = Mouse.current.position.ReadValue();
        var canvas = tituloInicio.canvas;
        var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        bool sobre = Modo == ModoApp.Inicio && punto.HasValue && RectTransformUtility.RectangleContainsScreenPoint(tituloInicio.rectTransform, punto.Value, cam);

        bool empezo = punto.HasValue && !habiaToque;          // el dedo acaba de bajar (solo cuenta si empieza sobre el título)
        habiaToque = punto.HasValue;
        if (sobre && presionado >= 0f) presionado += dt;
        else if (sobre && empezo) presionado = 0f;
        else if (!sobre) presionado = -1f;

        float k = presionado < 0f ? 0f : Mathf.Clamp01(presionado / SegundosReinicio);
        if (presionado >= SegundosReinicio)
        {
            presionado = -2f;                              // no repetir hasta soltar
            BorrarProgreso(); RefrescarInicio();
            tituloInicio.rectTransform.localScale = Vector3.one * 1.12f;
            tituloInicio.color = Cian;
            return;
        }
        if (presionado == -2f && sobre) return;
        // vuelve suave a su estado normal
        tituloInicio.rectTransform.localScale = Vector3.Lerp(tituloInicio.rectTransform.localScale, Vector3.one * (1f + 0.06f * k), dt * 12f);
        tituloInicio.color = Color.Lerp(tituloInicio.color, Color.Lerp(tituloColor, Cian, k), dt * 12f);
    }

    // Celebra una sola vez cuando se juntan los 10 elementos o las 4 moléculas
    void RevisarLogros(float dt)
    {
        if (celebracion.Abierta) return;
        bool elementos = Modo == ModoApp.Explorar && Completo(Modo) && PlayerPrefs.GetInt(ClaveElementos, 0) == 0;
        bool moleculas = Modo == ModoApp.Combinar && Completo(Modo) && PlayerPrefs.GetInt(ClaveMoleculas, 0) == 0;
        if (!elementos && !moleculas) { esperaLogro = -1f; return; }
        if (esperaLogro < 0f) esperaLogro = elementos ? 2.2f : 3.4f;      // deja ver primero el átomo o la molécula recién logrados
        esperaLogro -= dt;
        if (esperaLogro > 0f) return;
        esperaLogro = -1f;
        PlayerPrefs.SetInt(elementos ? ClaveElementos : ClaveMoleculas, 1); PlayerPrefs.Save();
        celebracion.Mostrar(elementos);
    }

    void Atras()
    {
        if (celebracion.Abierta) { celebracion.Cerrar(); return; }
        if (GuiaVisual.Activa) { guia.Cerrar(true); return; }
        if (Modo == ModoApp.Reto && quiz.Retroceder()) return;          // de una ronda vuelve a la portada del Reto
        if (Modo != ModoApp.Inicio) Cambiar(ModoApp.Inicio, false);
    }

    void Cambiar(ModoApp modo, bool inmediato)
    {
        Modo = modo;
        pantallaInicio.Mostrar(modo == ModoApp.Inicio, inmediato);
        barra.Mostrar(modo != ModoApp.Inicio, inmediato);
        pantallaReto.Mostrar(modo == ModoApp.Reto, inmediato);
        pastillaColeccion.SetActive(modo == ModoApp.Explorar);
        pastillaMoleculas.SetActive(modo == ModoApp.Combinar);

        bool conCamara = modo == ModoApp.Explorar || modo == ModoApp.Combinar;
        botonAyuda.gameObject.SetActive(conCamara);
        if (conCamara && !GuiaVisual.Vista(modo)) guia.Mostrar(modo, false);      // primera vez: guía animada
        else guia.Cerrar(false);

        if (modo == ModoApp.Inicio) RefrescarInicio();
        else PintarBarra(modo);
        if (modo == ModoApp.Reto) quiz.Abrir();
    }

    void PintarBarra(ModoApp modo)
    {
        tituloBarra.text = modo.ToString();
        var sig = Siguiente(modo); Color c = ColorModo(sig);
        siguienteTexto.text = "Ir a " + sig;
        siguienteTexto.color = c; siguienteMarco.color = c; siguienteHaloImagen.color = c;
        // el botón va pegado al título, que cambia de ancho según el modo
        tituloBarra.ForceMeshUpdate(); siguienteTexto.ForceMeshUpdate();
        var rt = (RectTransform)botonSiguiente.transform;
        rt.anchoredPosition = new Vector2(144f + tituloBarra.preferredWidth + 26f, rt.anchoredPosition.y);
        rt.sizeDelta = new Vector2(siguienteTexto.preferredWidth + 52f, rt.sizeDelta.y);
    }

    void RefrescarInicio()
    {
        int e = Coleccion.Cantidad, m = Coleccion.Moleculas, p = ProgresoQuiz.Puntos;
        progresos[0].text = $"{e} de {Coleccion.TotalElementos} elementos";
        progresos[1].text = $"{m} de {Coleccion.TotalMoleculas} moléculas";
        progresos[2].text = p == 0 ? "Aún sin puntos" : $"{p} de 300 puntos";
        anillos[0].PonerRelleno(e / (float)Coleccion.TotalElementos);
        anillos[1].PonerRelleno(m / (float)Coleccion.TotalMoleculas);
        anillos[2].PonerRelleno(p / 300f);
        recomendado = e < Coleccion.TotalElementos ? 0 : m < Coleccion.TotalMoleculas ? 1 : 2;
        for (int i = 0; i < etiquetasSigue.Length; i++) etiquetasSigue[i].SetActive(i == recomendado);
    }
}
