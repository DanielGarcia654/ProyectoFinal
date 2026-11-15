// AtomAR - Reto: preguntas de opción múltiple sobre los 10 elementos, sin cámara.
// • 30 preguntas (3 por elemento): su símbolo, sus protones y dónde se encuentra.
// • Rondas de 10; primero salen las que todavía no se han contestado bien.
// • Insignia por elemento: bronce (1 de 3), plata (2 de 3), oro (3 de 3). El progreso se guarda en el teléfono.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [Tooltip("Los 10 elementos, ordenados por número atómico")]
    public ElementoData[] elementos;
    public int preguntasPorRonda = 10;

    [Header("Portada e insignias")]
    public FundidoUI portada;
    public TMP_Text puntosTexto;
    public Image[] insigniaFondo;
    public AnilloUI[] insigniaAro;
    public TMP_Text[] insigniaSimbolo;
    public Button empezar;

    [Header("Pregunta")]
    public FundidoUI vistaPregunta;
    public TMP_Text progresoTexto, preguntaTexto;
    public RectTransform barraRelleno;
    public Button[] opciones;
    public Image[] opcionFondo;
    public TMP_Text[] opcionTexto;
    public FundidoUI retro;
    public TMP_Text retroTitulo, retroTexto;
    public Button siguiente;
    public TMP_Text siguienteTexto;

    [Header("Resultado")]
    public FundidoUI vistaResultado;
    public TMP_Text resultadoNumero, resultadoMensaje, resultadoInsignias;
    public Button otraRonda, verInsignias;

    struct Pregunta { public string id, texto, explicacion; public string[] opciones; public int correcta; }

    static readonly Color Verde = new(0.21f, 0.82f, 0.50f), Rojo = new(0.95f, 0.27f, 0.31f);
    static readonly Color Bronce = new(0.80f, 0.50f, 0.20f), Plata = new(0.79f, 0.82f, 0.88f), Oro = new(1f, 0.78f, 0.24f);
    static readonly Color Velo = new(1f, 1f, 1f, 0.08f), Papel = new(0.95f, 0.96f, 1f), Tenue = new(0.60f, 0.63f, 0.78f);
    static readonly Color TintaOscura = new(0.08f, 0.09f, 0.20f);

    List<Pregunta> ronda = new();
    int indice, aciertos;
    bool respondida;
    int[] nivelesAntes;
    float barra, vBarra;

    void Awake()
    {
        empezar.onClick.AddListener(EmpezarRonda);
        otraRonda.onClick.AddListener(EmpezarRonda);
        verInsignias.onClick.AddListener(Abrir);
        siguiente.onClick.AddListener(Siguiente);
        for (int i = 0; i < opciones.Length; i++) { int k = i; opciones[i].onClick.AddListener(() => Responder(k)); }
    }

    /// Muestra la portada con las insignias.
    public void Abrir()
    {
        puntosTexto.text = ProgresoQuiz.Puntos == 0 ? "Aún sin puntos" : $"{ProgresoQuiz.Puntos} puntos";
        for (int i = 0; i < elementos.Length && i < insigniaFondo.Length; i++)
        {
            var e = elementos[i];
            int nivel = ProgresoQuiz.Aciertos(e.simbolo);
            var c = e.color; c.a = nivel > 0 ? 1f : 0.28f;
            insigniaFondo[i].color = c;
            insigniaAro[i].color = nivel == 3 ? Oro : nivel == 2 ? Plata : nivel == 1 ? Bronce : new Color(1f, 1f, 1f, 0.14f);
            insigniaSimbolo[i].text = e.simbolo;
            insigniaSimbolo[i].color = nivel > 0 ? AppAtomAR.Tinta(e.color) : Tenue;
        }
        portada.Mostrar(true); vistaPregunta.Mostrar(false); vistaResultado.Mostrar(false);
    }

    /// Botón atrás: de una ronda o del resultado vuelve a la portada. Devuelve false si ya estaba en la portada.
    public bool Retroceder()
    {
        if (portada.Visible) return false;
        Abrir();
        return true;
    }

    // ───────── Preguntas ─────────
    List<Pregunta> TodasLasPreguntas()
    {
        var lista = new List<Pregunta>();
        foreach (var e in elementos)
        {
            string nombre = e.nombre.ToLower();
            lista.Add(DeNombres(e, e.simbolo + ":0", $"¿Qué elemento tiene el símbolo {e.simbolo}?", $"{e.simbolo} es el símbolo del {nombre}."));
            lista.Add(DeProtones(e));
            lista.Add(DeNombres(e, e.simbolo + ":2", e.preguntaUso, e.dato));
        }
        return lista;
    }

    Pregunta DeNombres(ElementoData e, string id, string texto, string explicacion)
    {
        var ops = elementos.Where(x => x != e).OrderBy(_ => Random.value).Take(3).Select(x => x.nombre).ToList();
        ops.Insert(Random.Range(0, ops.Count + 1), e.nombre);
        return new Pregunta { id = id, texto = texto, explicacion = explicacion, opciones = ops.ToArray(), correcta = ops.IndexOf(e.nombre) };
    }

    Pregunta DeProtones(ElementoData e)
    {
        var nums = elementos.Where(x => x != e).OrderBy(_ => Random.value).Take(3).Select(x => x.numeroAtomico).ToList();
        nums.Add(e.numeroAtomico); nums.Sort();
        return new Pregunta
        {
            id = e.simbolo + ":1",
            texto = $"¿Cuántos protones tiene un átomo de {e.nombre.ToLower()}?",
            explicacion = $"El número atómico del {e.nombre.ToLower()} es {e.numeroAtomico}: esos son sus protones.",
            opciones = nums.Select(n => n.ToString()).ToArray(),
            correcta = nums.IndexOf(e.numeroAtomico),
        };
    }

    void EmpezarRonda()
    {
        // primero las que faltan por acertar; el resto se completa con repaso
        ronda = TodasLasPreguntas().OrderBy(_ => Random.value).OrderBy(p => ProgresoQuiz.Acertada(p.id) ? 1 : 0)
            .Take(preguntasPorRonda).OrderBy(_ => Random.value).ToList();
        nivelesAntes = elementos.Select(e => ProgresoQuiz.Aciertos(e.simbolo)).ToArray();
        indice = 0; aciertos = 0; barra = 0f;
        portada.Mostrar(false); vistaResultado.Mostrar(false); vistaPregunta.Mostrar(true);
        PintarPregunta();
    }

    void PintarPregunta()
    {
        var p = ronda[indice];
        respondida = false;
        progresoTexto.text = $"Pregunta {indice + 1} de {ronda.Count}";
        preguntaTexto.text = p.texto;
        for (int i = 0; i < opciones.Length; i++)
        {
            bool hay = i < p.opciones.Length;
            opciones[i].gameObject.SetActive(hay);
            if (!hay) continue;
            opciones[i].interactable = true;
            opcionFondo[i].color = Velo;
            opcionTexto[i].text = p.opciones[i];
            opcionTexto[i].color = Papel;
        }
        retro.Mostrar(false);
        siguiente.gameObject.SetActive(false);
    }

    void Responder(int elegida)
    {
        if (respondida) return;
        respondida = true;
        var p = ronda[indice];
        bool bien = elegida == p.correcta;
        if (bien) { aciertos++; ProgresoQuiz.Registrar(p.id); }

        for (int i = 0; i < p.opciones.Length; i++)
        {
            opciones[i].interactable = false;
            if (i == p.correcta) { opcionFondo[i].color = Verde; opcionTexto[i].color = TintaOscura; }
            else if (i == elegida) { opcionFondo[i].color = Rojo; opcionTexto[i].color = Color.white; }
            else opcionTexto[i].color = Tenue;
        }
        retroTitulo.text = bien ? "¡Correcto!" : "No era esa";
        retroTitulo.color = bien ? Verde : Rojo;
        retroTexto.text = bien ? p.explicacion : $"La respuesta es {p.opciones[p.correcta]}. {p.explicacion}";
        retro.Mostrar(true);
        siguienteTexto.text = indice == ronda.Count - 1 ? "Ver resultado" : "Siguiente";
        siguiente.gameObject.SetActive(true);
    }

    void Siguiente()
    {
        if (indice < ronda.Count - 1) { indice++; PintarPregunta(); }
        else MostrarResultado();
    }

    void MostrarResultado()
    {
        int total = ronda.Count;
        resultadoNumero.text = $"{aciertos} de {total}";
        resultadoMensaje.text = aciertos == total ? "¡Ronda perfecta!"
            : aciertos >= total * 0.7f ? "¡Muy bien!"
            : aciertos >= total * 0.4f ? "Vas por buen camino."
            : "Explora un poco más y vuelve a intentarlo.";
        int subieron = 0;
        for (int i = 0; i < elementos.Length; i++) if (ProgresoQuiz.Aciertos(elementos[i].simbolo) > nivelesAntes[i]) subieron++;
        resultadoInsignias.text = subieron == 0 ? "Esta vez no cambió ninguna insignia."
            : subieron == 1 ? "Mejoraste 1 insignia." : $"Mejoraste {subieron} insignias.";
        vistaPregunta.Mostrar(false); vistaResultado.Mostrar(true);
    }

    void Update()
    {
        if (!vistaPregunta.Visible || ronda.Count == 0) return;
        float objetivo = (indice + (respondida ? 1f : 0f)) / ronda.Count;
        barra = Mathf.SmoothDamp(barra, objetivo, ref vBarra, 0.1f, Mathf.Infinity, Time.unscaledDeltaTime);
        barraRelleno.anchorMax = new Vector2(Mathf.Max(barra, 0.001f), 1f);
    }
}
