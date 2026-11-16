// AtomAR - Reto: preguntas de opción múltiple sobre los 10 elementos, sin cámara.
// • 30 preguntas (3 por elemento): su símbolo, sus protones y dónde se encuentra.
// • Rondas de 10; primero salen las que todavía no se han contestado bien.
// • Insignia por elemento: bronce (1 de 3), plata (2 de 3), oro (3 de 3). El progreso se guarda en el teléfono.
// • Para que se sienta como juego: tarjeta misteriosa que se voltea al contestar, átomo animado como pista,
//   opciones que entran en cascada, racha de aciertos, "+10" que sube, aviso al ganar una insignia
//   y resultado que cuenta los aciertos uno por uno.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [Tooltip("Los 10 elementos, ordenados por número atómico")]
    public ElementoData[] elementos;
    [Tooltip("Imagen de la tarjeta de cada elemento, en el mismo orden")]
    public Sprite[] cartas;
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
    public CanvasGroup[] opcionGrupo;
    public FundidoUI retro;
    public TMP_Text retroTitulo, retroTexto;
    public Button siguiente;
    public TMP_Text siguienteTexto;

    [Header("Tarjeta misteriosa / átomo")]
    public RectTransform visual;
    public GameObject carta, tapa;
    public Image cartaImagen;
    public TMP_Text tapaTexto;
    public DiagramaBohr atomo;

    [Header("Racha, puntos e insignia")]
    public RectTransform rachaRT;
    public CanvasGroup rachaGrupo, flotanteGrupo;
    public TMP_Text rachaTexto, flotante, avisoTexto;
    public FundidoUI avisoInsignia;
    public AnilloUI avisoAro;

    [Header("Resultado")]
    public FundidoUI vistaResultado;
    public TMP_Text resultadoNumero, resultadoMensaje, resultadoInsignias;
    public Image[] puntosResultado;
    public Button otraRonda, verInsignias;

    struct Pregunta { public string id, texto, explicacion; public string[] opciones; public int correcta, elemento, tipo; }

    static readonly Color Verde = new(0.21f, 0.82f, 0.50f), Rojo = new(0.95f, 0.27f, 0.31f);
    static readonly Color Bronce = new(0.80f, 0.50f, 0.20f), Plata = new(0.79f, 0.82f, 0.88f), Oro = new(1f, 0.78f, 0.24f);
    static readonly Color Velo = new(1f, 1f, 1f, 0.08f), Papel = new(0.95f, 0.96f, 1f), Tenue = new(0.60f, 0.63f, 0.78f);
    static readonly Color TintaOscura = new(0.08f, 0.09f, 0.20f);
    static readonly string[] NombreNivel = { "", "bronce", "plata", "oro" };

    List<Pregunta> ronda = new();
    bool[] resultados = new bool[0];
    int indice, aciertos, racha, elegida = -1, numeroMostrado = -1;
    bool respondida, bienUltima, voltear;
    int[] nivelesAntes;
    float barra, vBarra, tPregunta, tRespuesta, tRacha = 9f, tFlotante = -1f, tAviso = -1f, tResultado;
    Vector2[] baseOpcion; Vector2 baseFlotante;

    void Awake()
    {
        empezar.onClick.AddListener(EmpezarRonda);
        otraRonda.onClick.AddListener(EmpezarRonda);
        verInsignias.onClick.AddListener(Abrir);
        siguiente.onClick.AddListener(Siguiente);
        baseOpcion = new Vector2[opciones.Length];
        for (int i = 0; i < opciones.Length; i++)
        {
            int k = i; opciones[i].onClick.AddListener(() => Responder(k));
            baseOpcion[i] = ((RectTransform)opciones[i].transform).anchoredPosition;
        }
        rachaGrupo.alpha = 0f; flotanteGrupo.alpha = 0f;
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
            insigniaAro[i].color = ColorNivel(nivel);
            insigniaSimbolo[i].text = e.simbolo;
            insigniaSimbolo[i].color = nivel > 0 ? AppAtomAR.Tinta(e.color) : Tenue;
        }
        avisoInsignia.Mostrar(false); tAviso = -1f;
        portada.Mostrar(true); vistaPregunta.Mostrar(false); vistaResultado.Mostrar(false);
    }

    /// Botón atrás: de una ronda o del resultado vuelve a la portada. Devuelve false si ya estaba en la portada.
    public bool Retroceder()
    {
        if (portada.Visible) return false;
        Abrir();
        return true;
    }

    static Color ColorNivel(int nivel) => nivel == 3 ? Oro : nivel == 2 ? Plata : nivel == 1 ? Bronce : new Color(1f, 1f, 1f, 0.14f);

    // ───────── Preguntas ─────────
    List<Pregunta> TodasLasPreguntas()
    {
        var lista = new List<Pregunta>();
        for (int i = 0; i < elementos.Length; i++)
        {
            var e = elementos[i]; string nombre = e.nombre.ToLower();
            lista.Add(DeNombres(i, 0, $"¿Qué elemento tiene el símbolo {e.simbolo}?", $"{e.simbolo} es el símbolo del {nombre}."));
            lista.Add(DeProtones(i));
            lista.Add(DeNombres(i, 2, e.preguntaUso, e.dato));
        }
        return lista;
    }

    Pregunta DeNombres(int i, int tipo, string texto, string explicacion)
    {
        var e = elementos[i];
        var ops = elementos.Where(x => x != e).OrderBy(_ => Random.value).Take(3).Select(x => x.nombre).ToList();
        ops.Insert(Random.Range(0, ops.Count + 1), e.nombre);
        return new Pregunta { id = e.simbolo + ":" + tipo, texto = texto, explicacion = explicacion, opciones = ops.ToArray(), correcta = ops.IndexOf(e.nombre), elemento = i, tipo = tipo };
    }

    Pregunta DeProtones(int i)
    {
        var e = elementos[i];
        var nums = elementos.Where(x => x != e).OrderBy(_ => Random.value).Take(3).Select(x => x.numeroAtomico).ToList();
        nums.Add(e.numeroAtomico); nums.Sort();
        return new Pregunta
        {
            id = e.simbolo + ":1", elemento = i, tipo = 1,
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
        resultados = new bool[ronda.Count];
        nivelesAntes = elementos.Select(e => ProgresoQuiz.Aciertos(e.simbolo)).ToArray();
        indice = 0; aciertos = 0; racha = 0; barra = 0f;
        portada.Mostrar(false); vistaResultado.Mostrar(false); vistaPregunta.Mostrar(true);
        PintarPregunta();
    }

    void PintarPregunta()
    {
        var p = ronda[indice]; var e = elementos[p.elemento];
        respondida = false; elegida = -1; tPregunta = 0f; tRespuesta = 0f; voltear = false;
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
            opcionGrupo[i].alpha = 0f;
        }

        // Arriba: en las preguntas de protones, el átomo animado sirve de pista;
        // en las demás hay una tarjeta misteriosa que se voltea al contestar.
        visual.localScale = Vector3.one;
        bool conAtomo = p.tipo == 1;
        atomo.gameObject.SetActive(conAtomo); carta.SetActive(!conAtomo); tapa.SetActive(!conAtomo);
        if (conAtomo) atomo.Construir(e.electronesPorCapa);
        else
        {
            var sprite = p.elemento < cartas.Length ? cartas[p.elemento] : null;
            cartaImagen.sprite = sprite; cartaImagen.color = sprite ? Color.white : e.color;
            tapaTexto.text = p.tipo == 0 ? e.simbolo : "?";
        }
        retro.Mostrar(false);
        siguiente.gameObject.SetActive(false);
    }

    void Responder(int i)
    {
        if (respondida) return;
        respondida = true; elegida = i; tRespuesta = 0f;
        var p = ronda[indice]; var e = elementos[p.elemento];
        bool bien = i == p.correcta, yaAcertada = ProgresoQuiz.Acertada(p.id);
        bienUltima = bien; resultados[indice] = bien; voltear = p.tipo != 1;

        if (bien)
        {
            aciertos++; racha++;
            int antes = ProgresoQuiz.Aciertos(e.simbolo);
            ProgresoQuiz.Registrar(p.id);
            int ahora = ProgresoQuiz.Aciertos(e.simbolo);
            if (ahora > antes)          // subió la insignia de este elemento
            {
                avisoAro.color = ColorNivel(ahora);
                avisoTexto.text = $"Insignia de {NombreNivel[ahora]}: {e.nombre}";
                avisoInsignia.Mostrar(true); tAviso = 0f;
            }
            flotante.text = yaAcertada ? "¡Bien!" : "+10";
            baseFlotante = baseOpcion[i] + new Vector2(780f, 40f);      // nace arriba de la opción elegida, a la derecha
            tFlotante = 0f;
            if (racha >= 2) { rachaTexto.text = $"Racha de {racha}"; tRacha = 0f; }
        }
        else racha = 0;

        for (int k = 0; k < p.opciones.Length; k++)
        {
            opciones[k].interactable = false;
            if (k == p.correcta) { opcionFondo[k].color = Verde; opcionTexto[k].color = TintaOscura; }
            else if (k == i) { opcionFondo[k].color = Rojo; opcionTexto[k].color = Color.white; }
            else opcionTexto[k].color = Tenue;
        }
        retroTitulo.text = bien ? (racha >= 3 ? "¡Imparable!" : "¡Correcto!") : "No era esa";
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
        tResultado = 0f; numeroMostrado = -1;
        resultadoMensaje.text = aciertos == total ? "¡Ronda perfecta!"
            : aciertos >= total * 0.7f ? "¡Muy bien!"
            : aciertos >= total * 0.4f ? "Vas por buen camino."
            : "Explora un poco más y vuelve a intentarlo.";
        int subieron = 0;
        for (int i = 0; i < elementos.Length; i++) if (ProgresoQuiz.Aciertos(elementos[i].simbolo) > nivelesAntes[i]) subieron++;
        resultadoInsignias.text = subieron == 0 ? "Esta vez no cambió ninguna insignia."
            : subieron == 1 ? "Mejoraste 1 insignia." : $"Mejoraste {subieron} insignias.";
        for (int i = 0; i < puntosResultado.Length; i++)
        {
            bool hay = i < total; puntosResultado[i].gameObject.SetActive(hay);
            if (hay) puntosResultado[i].color = resultados[i] ? Verde : Rojo;
        }
        avisoInsignia.Mostrar(false); tAviso = -1f;
        vistaPregunta.Mostrar(false); vistaResultado.Mostrar(true);
    }

    // ───────── Animaciones ─────────
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (tAviso >= 0f) { tAviso += dt; if (tAviso > 2.4f) { avisoInsignia.Mostrar(false); tAviso = -1f; } }
        if (vistaPregunta.Visible && ronda.Count > 0) AnimarPregunta(dt);
        if (vistaResultado.Visible) AnimarResultado(dt);
    }

    void AnimarPregunta(float dt)
    {
        tPregunta += dt;
        if (respondida) tRespuesta += dt;

        float objetivo = (indice + (respondida ? 1f : 0f)) / ronda.Count;
        barra = Mathf.SmoothDamp(barra, objetivo, ref vBarra, 0.1f, Mathf.Infinity, dt);
        barraRelleno.anchorMax = new Vector2(Mathf.Max(barra, 0.001f), 1f);

        // Las opciones entran en cascada; la equivocada se sacude
        for (int i = 0; i < opciones.Length; i++)
        {
            float e = Suave((tPregunta - 0.10f - i * 0.06f) / 0.28f), x = 0f;
            if (respondida && i == elegida && !bienUltima) x = 20f * Mathf.Exp(-tRespuesta * 7f) * Mathf.Sin(tRespuesta * 42f);
            opcionGrupo[i].alpha = e;
            ((RectTransform)opciones[i].transform).anchoredPosition = baseOpcion[i] + new Vector2(x, -22f * (1f - e));
        }

        // La tarjeta entra con un pequeño rebote y se voltea al contestar
        float entrada = Rebote(tPregunta / 0.4f), ancho = 1f;
        if (respondida && voltear)
        {
            float f = tRespuesta / 0.36f;
            if (f < 0.5f) ancho = 1f - 2f * f;
            else { if (tapa.activeSelf) tapa.SetActive(false); ancho = Mathf.Min(1f, 2f * f - 1f); }
        }
        float s = Mathf.Lerp(0.8f, 1f, entrada);
        visual.localScale = new Vector3(s * Mathf.Max(ancho, 0.02f), s, 1f);

        // "+10" que sube desde la respuesta
        if (tFlotante >= 0f)
        {
            tFlotante += dt; float k = tFlotante / 0.9f;
            flotanteGrupo.alpha = k < 1f ? 1f - k * k : 0f;
            flotante.rectTransform.anchoredPosition = baseFlotante + new Vector2(0f, 120f * Suave(k));
            if (k >= 1f) tFlotante = -1f;
        }

        // Racha de aciertos
        tRacha += dt;
        rachaGrupo.alpha = Mathf.MoveTowards(rachaGrupo.alpha, racha >= 2 ? 1f : 0f, dt * 6f);
        rachaRT.localScale = Vector3.one * (1f + 0.25f * Mathf.Exp(-tRacha * 8f) * Mathf.Cos(tRacha * 18f));
    }

    void AnimarResultado(float dt)
    {
        tResultado += dt;
        int n = Mathf.RoundToInt(Mathf.Lerp(0f, aciertos, Suave(tResultado / 0.9f)));      // el número cuenta hacia arriba
        if (n != numeroMostrado) { numeroMostrado = n; resultadoNumero.text = $"{n} de {ronda.Count}"; }
        for (int i = 0; i < puntosResultado.Length; i++)                                      // un punto por pregunta, en cascada
        {
            float k = Mathf.Clamp01((tResultado - 0.25f - i * 0.07f) / 0.25f);
            puntosResultado[i].transform.localScale = Vector3.one * (k <= 0f ? 0.001f : Mathf.Lerp(0.5f, 1f, Rebote(k)));
        }
    }

    static float Suave(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
    static float Rebote(float x) { x = Mathf.Clamp01(x); const float c = 1.70158f; float y = x - 1f; return 1f + (c + 1f) * y * y * y + c * y * y; }
}
