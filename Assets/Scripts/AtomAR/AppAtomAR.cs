// AtomAR - Controla en qué modo está la app y qué pantallas se ven.
//   Inicio   → pantalla con los tres modos y el avance de cada uno
//   Explorar → cámara + carta del elemento (PanelElemento)
//   Combinar → cámara + panel para armar moléculas (ModoCombinar)
//   Reto     → preguntas e insignias, sin cámara (Quiz)
// Los tres modos están abiertos desde el principio; el orden sugerido se marca con "Empieza aquí".
// La primera vez que se entra a un modo se muestra su tutorial; el botón "?" lo vuelve a abrir.
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
    public Tutorial tutorial;
    public Quiz quiz;

    [Header("Inicio")]
    public Button botonExplorar;
    public Button botonCombinar, botonReto, botonAyudaInicio;
    public TMP_Text progresoExplorar, progresoCombinar, progresoReto;
    public GameObject chipEmpieza;

    [Header("Barra superior")]
    public Button botonAtras;
    public Button botonAyuda;
    public TMP_Text tituloBarra;
    public GameObject pastillaColeccion, pastillaMoleculas;

    static readonly Color Cian = new(0.18f, 0.90f, 1f), Rojo = new(0.95f, 0.27f, 0.31f), Oro = new(1f, 0.78f, 0.24f);
    static readonly Color TintaOscura = new(0.08f, 0.09f, 0.20f);

    /// Color que identifica a cada modo (el mismo en inicio, tutorial y botones).
    public static Color ColorModo(ModoApp m) => m == ModoApp.Combinar ? Rojo : m == ModoApp.Reto ? Oro : Cian;

    /// Texto oscuro sobre colores claros y blanco sobre colores oscuros.
    public static Color Tinta(Color fondo)
    {
        float lum = 0.2126f * fondo.r + 0.7152f * fondo.g + 0.0722f * fondo.b;
        return lum > 0.5f ? TintaOscura : Color.white;
    }

    public static void Ir(ModoApp modo) { if (instancia) instancia.Cambiar(modo, false); }

    void Awake()
    {
        instancia = this;
        Modo = ModoApp.Inicio;
        botonExplorar.onClick.AddListener(() => Cambiar(ModoApp.Explorar, false));
        botonCombinar.onClick.AddListener(() => Cambiar(ModoApp.Combinar, false));
        botonReto.onClick.AddListener(() => Cambiar(ModoApp.Reto, false));
        botonAtras.onClick.AddListener(Atras);
        botonAyuda.onClick.AddListener(() => tutorial.Mostrar(Modo));
        botonAyudaInicio.onClick.AddListener(() => tutorial.Mostrar(ModoApp.Inicio));
    }

    void Start() => Cambiar(ModoApp.Inicio, true);

    void OnDestroy() { if (instancia == this) instancia = null; }

    void Update()
    {
        // Botón "atrás" de Android
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Atras();
    }

    void Atras()
    {
        if (tutorial.Abierto) { tutorial.Cerrar(); return; }
        if (Modo == ModoApp.Reto && quiz.Retroceder()) return;      // de una ronda vuelve a la portada del Reto
        if (Modo != ModoApp.Inicio) Cambiar(ModoApp.Inicio, false);
    }

    void Cambiar(ModoApp modo, bool inmediato)
    {
        Modo = modo;
        pantallaInicio.Mostrar(modo == ModoApp.Inicio, inmediato);
        barra.Mostrar(modo != ModoApp.Inicio, inmediato);
        pantallaReto.Mostrar(modo == ModoApp.Reto, inmediato);

        tituloBarra.text = modo.ToString();
        pastillaColeccion.SetActive(modo == ModoApp.Explorar);
        pastillaMoleculas.SetActive(modo == ModoApp.Combinar);
        // en el Reto no hay pastilla de avance: el botón de ayuda se va a la orilla
        ((RectTransform)botonAyuda.transform).anchoredPosition = new Vector2(modo == ModoApp.Reto ? -40f : -292f, -36f);

        if (modo == ModoApp.Inicio) RefrescarInicio();
        if (modo == ModoApp.Reto) quiz.Abrir();
        if (modo != ModoApp.Inicio && !Tutorial.Visto(modo)) tutorial.Mostrar(modo);
    }

    void RefrescarInicio()
    {
        int e = Coleccion.Cantidad, m = Coleccion.Moleculas, p = ProgresoQuiz.Puntos;
        progresoExplorar.text = $"{e} de {Coleccion.TotalElementos} elementos";
        progresoCombinar.text = $"{m} de {Coleccion.TotalMoleculas} moléculas";
        progresoReto.text = p == 0 ? "Aún sin puntos" : $"{p} puntos";
        chipEmpieza.SetActive(e == 0);
    }
}
