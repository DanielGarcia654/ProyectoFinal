// AtomAR - Interfaz del Modo Elemento.
// • Carta del elemento (hoja inferior) con el color de su tarjeta, su frase y su mini átomo de Bohr.
// • ¿Qué elemento se muestra?  1) el que tocaste en la cámara (mientras siga a la vista);
//   2) si no, el que está más cerca del centro de la pantalla (al que apuntas).
//   Con 2 o más tarjetas a la vista aparecen pastillas arriba de la carta para cambiar entre ellas.
// • Movimiento con resorte críticamente amortiguado (SmoothDamp): parte del valor y la velocidad actuales,
//   así que se puede interrumpir sin saltos. Entra y sale por el mismo camino; la salida es más rápida.
// • Arrastre 1:1 con resistencia en los bordes y proyección de inercia al soltar.
// • Colección "4 de 10" con aviso de celebración la primera vez que se descubre un elemento.
// • Escáner que respira suavemente cuando no hay tarjeta a la vista.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelElemento : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    static PanelElemento instancia;

    [Header("Hoja")]
    public RectTransform hoja;
    public CanvasGroup grupoHoja;
    public CanvasGroup grupoContenido;
    [Tooltip("Bloques que aparecen escalonados (en orden)")]
    public RectTransform[] filas;
    public float alturaCompacta = 300f;
    public float margenInferior = 24f;

    [Header("Carta")]
    public Image banda;
    public TMP_Text simboloFantasma, nombre, apodo, chipZ, masa;
    public Image chipZFondo;
    public RectTransform iconoMas;
    public Image botonMasFondo;

    [Header("Detalle")]
    public DiagramaBohr diagrama;
    public TMP_Text cuentaP, cuentaN, cuentaE, etiquetaP, etiquetaN, etiquetaE, frase, sabiasTitulo, dato;

    [Header("Selector (2 o más tarjetas)")]
    public CanvasGroup selector;
    public Button[] pastillas;
    public Image[] pastillasFondo;
    public TMP_Text[] pastillasTexto;

    [Header("Colección")]
    public TMP_Text coleccionTexto;
    public RectTransform coleccionPastilla;
    public CanvasGroup aviso;
    public RectTransform avisoRT;
    public Image avisoPunto;
    public TMP_Text avisoTexto;

    [Header("Escáner")]
    public CanvasGroup escaner;
    public RectTransform escanerMarco;

    [Header("Movimiento")]
    public float respuestaEntrada = 0.11f;
    public float respuestaSalida = 0.07f;
    [Tooltip("Segundos sin tarjeta antes de ocultar el panel")]
    public float gracia = 0.5f;
    public float escalonado = 0.045f;
    public float duracionFila = 0.26f;
    [Tooltip("Cada cuánto se revisa qué tarjeta está más al centro (s)")]
    public float intervaloCentro = 0.25f;

    static readonly Color TintaOscura = new(0.08f, 0.09f, 0.20f);

    readonly List<ElementoTarget> visibles = new();
    ElementoTarget elegido, fijado;        // fijado = el que el alumno tocó
    ElementoData actual, pendiente;
    bool mostrando, expandido, arrastrando;
    float perdidoDesde = -1f, sinTarjetaDesde, proximaRevision;
    float y, vy, vAlpha, contenidoA = 1f, vContenido, escA, vEsc, selA, vSel, tFilas = 10f, velArrastre;
    float giroMas, vGiro;
    float tAviso = 99f, tPop = 99f; int coleccionMostrada;
    readonly List<ElementoTarget> enPastillas = new();
    float[] escPastilla, vEscPastilla;
    Vector2[] basesFilas; CanvasGroup[] gruposFilas;
    Vector2 avisoBase;
    Canvas canvas;
    Camera camara;

    // ───────── API para las tarjetas ─────────
    public static void Encontrado(ElementoTarget t)
    {
        if (!instancia || !t.datos) return;
        if (!instancia.visibles.Contains(t)) instancia.visibles.Add(t);
        instancia.perdidoDesde = -1f;
        if (!instancia.elegido) instancia.Elegir(t);     // si no había nada, se muestra de inmediato
        else instancia.proximaRevision = 0f;              // si ya había otra, decide el criterio del centro
        if (Coleccion.Registrar(t.datos.targetName)) instancia.Celebrar(t.datos);
    }

    public static void Perdido(ElementoTarget t)
    {
        if (!instancia) return;
        instancia.visibles.Remove(t);
        if (instancia.fijado == t) instancia.fijado = null;
        if (instancia.elegido == t)
        {
            instancia.elegido = null;
            if (instancia.visibles.Count > 0) instancia.Elegir(instancia.MasAlCentro());
            else instancia.perdidoDesde = Time.unscaledTime;
        }
    }

    /// El alumno tocó un átomo o una pastilla: se queda seleccionado mientras siga a la vista.
    public static void Seleccionar(ElementoTarget t)
    {
        if (!instancia || !t || !instancia.visibles.Contains(t)) return;
        instancia.fijado = t;
        instancia.Elegir(t);
    }

    public static ElementoData Actual => instancia ? instancia.actual : null;
    /// Hay una carta a la vista / la carta está desplegada (los usa la guía para dar pistas).
    public static bool HayCarta => instancia && instancia.mostrando;
    public static bool Desplegada => instancia && instancia.expandido;

    // ───────── Ciclo de vida ─────────
    void Awake()
    {
        instancia = this;
        canvas = GetComponentInParent<Canvas>();
        basesFilas = new Vector2[filas.Length];
        gruposFilas = new CanvasGroup[filas.Length];
        for (int i = 0; i < filas.Length; i++)
        {
            basesFilas[i] = filas[i].anchoredPosition;
            if (!filas[i].TryGetComponent(out gruposFilas[i])) gruposFilas[i] = filas[i].gameObject.AddComponent<CanvasGroup>();
        }
        int n = pastillas?.Length ?? 0;
        escPastilla = new float[n]; vEscPastilla = new float[n];
        for (int i = 0; i < n; i++)
        {
            int k = i;
            pastillas[i].onClick.AddListener(() => { if (k < enPastillas.Count) Seleccionar(enPastillas[k]); });
            pastillas[i].gameObject.SetActive(false);
            escPastilla[i] = 1f;
        }
        y = PosOculta();
        hoja.anchoredPosition = new Vector2(hoja.anchoredPosition.x, y);
        grupoHoja.alpha = 0f; grupoHoja.blocksRaycasts = false;
        if (selector) { selector.alpha = 0f; selector.blocksRaycasts = false; }
        if (aviso) { aviso.alpha = 0f; avisoBase = avisoRT.anchoredPosition; }
        if (escaner) escaner.alpha = 0f;
        coleccionMostrada = Coleccion.Cantidad;
        ActualizarColeccion();
        sinTarjetaDesde = Time.unscaledTime;
    }

    void OnDestroy() { if (instancia == this) instancia = null; }

    void Elegir(ElementoTarget t)
    {
        if (!t) return;
        elegido = t;
        var d = t.datos;
        if (!mostrando)
        {
            Rellenar(d);
            actual = d; pendiente = null;
            mostrando = true;
            tFilas = 0f;
        }
        else if (d != actual) pendiente = d;
        else pendiente = null;
    }

    ElementoTarget MasAlCentro()
    {
        if (!camara) camara = Camera.main;
        var candidatos = visibles.Where(v => v && v.Rastreado).ToList();
        if (candidatos.Count == 0) candidatos = visibles.Where(v => v).ToList();
        if (candidatos.Count == 0) return null;
        if (!camara) return candidatos[^1];
        return candidatos.OrderBy(v =>
        {
            var p = camara.WorldToViewportPoint(v.transform.position);
            return p.z < 0 ? float.MaxValue : (new Vector2(p.x, p.y) - new Vector2(0.5f, 0.5f)).sqrMagnitude;
        }).First();
    }

    public void AlternarDetalle()
    {
        if (!mostrando) return;
        expandido = !expandido;
    }

    void Celebrar(ElementoData d)
    {
        if (!aviso) return;
        avisoTexto.text = $"¡Descubriste el {d.nombre}!";
        avisoPunto.color = d.color;
        tAviso = 0f;
        tPop = -0.35f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime, ahora = Time.unscaledTime;

        if (perdidoDesde > 0f && ahora - perdidoDesde > gracia)
        {
            perdidoDesde = -1f; mostrando = false; expandido = false; sinTarjetaDesde = ahora;
        }

        // Criterio del centro (solo si el alumno no fijó uno tocándolo)
        if (ahora >= proximaRevision)
        {
            proximaRevision = ahora + intervaloCentro;
            visibles.RemoveAll(v => !v);
            if (!fijado && visibles.Count > 1)
            {
                var c = MasAlCentro();
                if (c && c != elegido) Elegir(c);
            }
        }

        // Cambio de elemento
        contenidoA = Mathf.SmoothDamp(contenidoA, pendiente ? 0f : 1f, ref vContenido, 0.04f, Mathf.Infinity, dt);
        if (pendiente && contenidoA < 0.05f)
        {
            Rellenar(pendiente);
            actual = pendiente; pendiente = null; tFilas = 0f;
        }
        grupoContenido.alpha = contenidoA;

        // Hoja (solo se ve en el modo Explorar)
        bool enModo = AppAtomAR.Modo == ModoApp.Explorar, ver = mostrando && enModo;
        if (!arrastrando)
        {
            float objetivo = !ver ? PosOculta() : expandido ? PosExpandida() : PosCompacta();
            y = Mathf.SmoothDamp(y, objetivo, ref vy, ver ? respuestaEntrada : respuestaSalida, Mathf.Infinity, dt);
        }
        hoja.anchoredPosition = new Vector2(hoja.anchoredPosition.x, y);
        grupoHoja.alpha = Mathf.SmoothDamp(grupoHoja.alpha, ver ? 1f : 0f, ref vAlpha, ver ? 0.06f : 0.04f, Mathf.Infinity, dt);
        grupoHoja.blocksRaycasts = ver;

        giroMas = Mathf.SmoothDamp(giroMas, expandido ? 45f : 0f, ref vGiro, 0.07f, Mathf.Infinity, dt);
        if (iconoMas) iconoMas.localRotation = Quaternion.Euler(0f, 0f, -giroMas);

        tFilas += dt;
        for (int i = 0; i < filas.Length; i++)
        {
            float e = Mathf.Clamp01((tFilas - i * escalonado) / duracionFila);
            e = 1f - (1f - e) * (1f - e) * (1f - e);
            gruposFilas[i].alpha = e;
            filas[i].anchoredPosition = basesFilas[i] + new Vector2(0f, -16f * (1f - e));
        }

        ActualizarSelector(dt);

        bool buscar = enModo && !GuiaVisual.Activa && !mostrando && visibles.Count == 0 && ahora - sinTarjetaDesde > 0.6f;
        if (escaner)
        {
            escA = Mathf.SmoothDamp(escA, buscar ? 1f : 0f, ref vEsc, buscar ? 0.12f : 0.04f, Mathf.Infinity, dt);
            escaner.alpha = escA;
            float respiro = 1f + 0.025f * Mathf.Sin(ahora * Mathf.PI * 2f * 0.7f);
            escanerMarco.localScale = Vector3.one * (respiro * (0.94f + 0.06f * escA));
        }

        AnimarAviso(dt);
    }

    // Pastillas para cambiar entre tarjetas visibles (ordenadas por número atómico para que no brinquen de lugar)
    void ActualizarSelector(float dt)
    {
        if (!selector || pastillas == null) return;
        var lista = visibles.Where(v => v && v.datos).OrderBy(v => v.datos.numeroAtomico).Take(pastillas.Length).ToList();
        bool mostrar = mostrando && lista.Count > 1 && AppAtomAR.Modo == ModoApp.Explorar;

        if (!lista.SequenceEqual(enPastillas))
        {
            enPastillas.Clear(); enPastillas.AddRange(lista);
            for (int i = 0; i < pastillas.Length; i++)
            {
                bool activa = i < lista.Count;
                pastillas[i].gameObject.SetActive(activa);
                if (!activa) continue;
                var d = lista[i].datos;
                pastillasFondo[i].color = d.color;
                pastillasTexto[i].text = d.simbolo;
                pastillasTexto[i].color = Tinta(d.color);
            }
        }

        selA = Mathf.SmoothDamp(selA, mostrar ? 1f : 0f, ref vSel, mostrar ? 0.08f : 0.04f, Mathf.Infinity, dt);
        selector.alpha = selA;
        selector.blocksRaycasts = mostrar;
        selector.interactable = mostrar;

        for (int i = 0; i < enPastillas.Count && i < pastillas.Length; i++)
        {
            bool sel = enPastillas[i] == elegido;
            escPastilla[i] = Mathf.SmoothDamp(escPastilla[i], sel ? 1f : 0.86f, ref vEscPastilla[i], 0.06f, Mathf.Infinity, dt);
            var t = pastillas[i].transform;
            t.localScale = Vector3.one * escPastilla[i];
            var c = pastillasFondo[i].color; c.a = sel ? 1f : 0.5f; pastillasFondo[i].color = c;
        }
    }

    void AnimarAviso(float dt)
    {
        if (!aviso) return;
        tAviso += dt; tPop += dt;
        const float entra = 0.18f, dura = 2.4f, sale = 0.2f;
        if (tAviso < dura + sale)
        {
            float a, s, dy = 0f;
            if (tAviso < dura)
            {
                a = Mathf.Clamp01(tAviso / entra);
                s = 1f - 0.08f * Mathf.Exp(-tAviso * 11f) * Mathf.Cos(tAviso * 20f);   // rebote leve: momento de celebración
            }
            else
            {
                float k = (tAviso - dura) / sale;
                a = 1f - k; s = 1f; dy = 24f * k * k;
            }
            aviso.alpha = a;
            avisoRT.localScale = Vector3.one * s;
            avisoRT.anchoredPosition = avisoBase + new Vector2(0f, dy);
        }
        else aviso.alpha = 0f;

        if (tPop >= 0f && coleccionMostrada != Coleccion.Cantidad) { coleccionMostrada = Coleccion.Cantidad; ActualizarColeccion(); tPop = 0f; }
        if (coleccionPastilla)
        {
            float p = tPop >= 0f ? 1f + 0.14f * Mathf.Exp(-tPop * 9f) * Mathf.Cos(tPop * 17f) : 1f;
            coleccionPastilla.localScale = Vector3.one * p;
        }
    }

    void ActualizarColeccion()
    {
        if (coleccionTexto) coleccionTexto.text = $"{coleccionMostrada} de {Coleccion.TotalElementos}";
    }

    // ───────── Posiciones ─────────
    float Alto => hoja.rect.height;
    float PosExpandida() => margenInferior;
    float PosCompacta() => margenInferior - (Alto - alturaCompacta);
    float PosOculta() => -Alto - 140f;

    // ───────── Gestos ─────────
    public void OnPointerClick(PointerEventData e) { if (!e.dragging) AlternarDetalle(); }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!mostrando) return;
        arrastrando = true; velArrastre = 0f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!arrastrando) return;
        float delta = e.delta.y / (canvas ? canvas.scaleFactor : 1f);
        float max = PosExpandida(), min = PosCompacta(), nuevo = y + delta;
        if (nuevo > max) nuevo = max + Banda(nuevo - max);
        else if (nuevo < min) nuevo = min - Banda(min - nuevo);
        y = nuevo;
        velArrastre = Mathf.Lerp(velArrastre, delta / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.5f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!arrastrando) return;
        arrastrando = false;
        const float d = 0.99f;
        float proyectado = y + (velArrastre / 1000f) * d / (1f - d);
        expandido = Mathf.Abs(proyectado - PosExpandida()) < Mathf.Abs(proyectado - PosCompacta());
        vy = velArrastre;
    }

    float Banda(float exceso) { const float c = 0.55f; float d = Alto; return exceso * d * c / (d + c * exceso) * 0.35f; }

    // ───────── Contenido ─────────
    static Color Tinta(Color c)
    {
        float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        return lum > 0.5f ? TintaOscura : Color.white;
    }

    void Rellenar(ElementoData d)
    {
        var c = d.color;
        Color tinta = Tinta(c);
        Color tintaSuave = new(tinta.r, tinta.g, tinta.b, 0.78f);

        banda.color = c;
        simboloFantasma.text = d.simbolo; simboloFantasma.color = new Color(tinta.r, tinta.g, tinta.b, 0.16f);
        nombre.text = d.nombre; nombre.color = tinta;
        apodo.text = d.apodo; apodo.color = tintaSuave;
        chipZ.text = d.numeroAtomico.ToString(); chipZ.color = tinta;
        chipZFondo.color = new Color(tinta.r, tinta.g, tinta.b, 0.14f);
        masa.text = d.masaAtomica + " u"; masa.color = tintaSuave;
        botonMasFondo.color = new Color(tinta.r, tinta.g, tinta.b, 0.16f);
        foreach (var g in iconoMas.GetComponentsInChildren<Graphic>()) g.color = tinta;

        int z = d.numeroAtomico;
        cuentaP.text = z.ToString(); etiquetaP.text = z == 1 ? "protón" : "protones";
        cuentaN.text = d.neutrones.ToString(); etiquetaN.text = d.neutrones == 1 ? "neutrón" : "neutrones";
        cuentaE.text = z.ToString(); etiquetaE.text = z == 1 ? "electrón" : "electrones";
        frase.text = $"Es un {d.categoria.ToLower()}. {FraseCapas(d.electronesPorCapa)}";

        sabiasTitulo.color = Color.Lerp(c, Color.white, 0.4f);
        dato.text = d.dato;
        diagrama.Construir(d.electronesPorCapa);
    }

    static string FraseCapas(int[] capas)
    {
        if (capas == null || capas.Length == 0) return "";
        if (capas.Length == 1) return capas[0] == 1 ? "Tiene un solo electrón en una capa." : $"Sus {capas[0]} electrones están en una sola capa.";
        var partes = capas.Select(x => x.ToString()).ToArray();
        string lista = string.Join(", ", partes, 0, partes.Length - 1) + " y " + partes[^1];
        return $"Sus electrones se acomodan en {capas.Length} capas: {lista}.";
    }
}
