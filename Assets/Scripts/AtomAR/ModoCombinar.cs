// AtomAR - Modo Combinar: juntar dos tarjetas y decidir cuántos átomos de cada elemento hacen falta.
// • La cámara detecta dos tarjetas que tengan receta (H+O, Na+Cl, C+O, Fe+O) y que estén cerca.
// • Cada tarjeta empieza con 1 átomo; con + y − el alumno agrega o quita (aparecen copias del átomo en 3D).
// • Si la cantidad no es la correcta, una pista dice qué falta o qué sobra.
// • Cuando la proporción es la de la receta, los átomos se juntan entre las tarjetas y aparece la molécula.
//   (Vuforia no puede rastrear dos copias de la misma tarjeta, por eso los átomos extra se agregan con el botón.)
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModoCombinar : MonoBehaviour
{
    public MoleculaData[] moleculas;

    [Header("Hoja")]
    public FundidoUI hoja;
    public FundidoUI vistaMensaje, vistaArmar, vistaFormada;

    [Header("Mensaje")]
    public TMP_Text mensajeTitulo;
    public TMP_Text mensajeTexto;
    public Image[] chipFondo;
    public TMP_Text[] chipTexto;

    [Header("Armar")]
    public Image puntoA;
    public Image puntoB;
    public TMP_Text nombreA, nombreB, cuentaA, cuentaB, pista;
    public Button masA, menosA, masB, menosB;

    [Header("Formada")]
    public TMP_Text formadaNombre;
    public TMP_Text formadaFormula, formadaReceta, formadaEcuacion, formadaDato;

    [Header("Barra")]
    public TMP_Text moleculasTexto;

    [Header("Ajustes")]
    [Tooltip("Separación máxima entre tarjetas, medida en anchos de tarjeta")]
    public float distanciaMaxima = 3f;
    public int maximoAtomos = 4;
    [Tooltip("Segundos que se tolera perder una tarjeta antes de reiniciar")]
    public float gracia = 0.6f;
    public float duracionUnion = 0.6f;

    enum Estado { Ninguno, Buscar, Una, SinReceta, Lejos, Armar, Formando, Formada }

    class Grupo
    {
        public ElementoTarget t;
        public Transform original;
        public Vector3 basePos, baseEscala;
        public readonly List<Transform> atomos = new();
        public Vector3[] inicioPos, inicioEsc;
    }

    // tamaño de cada átomo según cuántos hay sobre la tarjeta (1, 2, 3 o 4)
    static readonly float[] Factor = { 1f, 0.62f, 0.5f, 0.42f };
    static readonly Color Velo = new(1f, 1f, 1f, 0.08f), Tenue = new(0.60f, 0.63f, 0.78f);

    Estado estado = Estado.Ninguno;
    string claveMensaje;
    Grupo gA, gB;
    MoleculaData receta;
    Transform molecula;
    bool activo;
    float invalidoDesde = -1f, tEstado, giro;

    void Awake()
    {
        masA.onClick.AddListener(() => Cambiar(gA, +1));
        menosA.onClick.AddListener(() => Cambiar(gA, -1));
        masB.onClick.AddListener(() => Cambiar(gB, +1));
        menosB.onClick.AddListener(() => Cambiar(gB, -1));
    }

    void OnDisable() => Soltar();

    void Update()
    {
        if (AppAtomAR.Modo != ModoApp.Combinar)
        {
            if (activo) { activo = false; Soltar(); hoja.Mostrar(false); estado = Estado.Ninguno; }
            return;
        }
        if (!activo) { activo = true; hoja.Mostrar(true); RefrescarProgreso(); }

        float dt = Time.unscaledDeltaTime, ahora = Time.unscaledTime;
        tEstado += dt;
        var visibles = ElementoTarget.Todos.Where(t => t && t.Visible && t.datos).ToList();

        // ¿Sigue a la vista la pareja con la que estábamos trabajando?
        bool parALaVista = gA != null && gA.t && gB.t && gA.t.Visible && gB.t.Visible;
        if (gA != null)
        {
            if (parALaVista) invalidoDesde = -1f;
            else
            {
                if (invalidoDesde < 0f) invalidoDesde = ahora;
                if (ahora - invalidoDesde > gracia) Soltar();
            }
        }
        if (molecula) molecula.gameObject.SetActive(parALaVista && estado == Estado.Formada);

        // Sin pareja: buscar dos tarjetas que tengan receta
        if (gA == null)
        {
            foreach (var m in moleculas)
            {
                var a = visibles.FirstOrDefault(t => t.datos.simbolo == m.simboloA);
                var b = visibles.FirstOrDefault(t => t.datos.simbolo == m.simboloB);
                if (a && b) { Tomar(m, a, b); break; }
            }
            if (gA == null)
            {
                if (visibles.Count == 0)
                    Mensaje(Estado.Buscar, "Junta dos tarjetas", "Ponlas una al lado de la otra y apunta la cámara para que se vean las dos.");
                else if (visibles.Count == 1)
                    Mensaje(Estado.Una, $"Ya veo el {visibles[0].datos.nombre.ToLower()}", "Ahora acerca otra tarjeta para combinarlos.");
                else
                    Mensaje(Estado.SinReceta, $"{visibles[0].datos.nombre} y {visibles[1].datos.nombre.ToLower()} no se combinan aquí",
                        "Prueba con hidrógeno y oxígeno, sodio y cloro, carbono y oxígeno, o hierro y oxígeno.");
                return;
            }
        }
        if (!parALaVista && gA.t.Visible && gB.t.Visible) parALaVista = true;   // recién tomada
        if (!parALaVista) return;                                               // esperando dentro del tiempo de gracia

        // ¿Están suficientemente cerca?
        Transform ta = gA.t.transform, tb = gB.t.transform;
        float ancho = Mathf.Max(ta.lossyScale.x, 1e-4f);
        float distancia = Vector3.Distance(ta.position, tb.position) / ancho;
        bool lejos = distancia > (estado == Estado.Lejos ? distanciaMaxima * 0.9f : distanciaMaxima);
        Vector3 centro = (ta.position + tb.position) * 0.5f + ta.up * (0.5f * ancho);

        if (lejos)
        {
            if (estado == Estado.Formando || estado == Estado.Formada) Reiniciar();     // separarlas deja armar otra vez
            Mensaje(Estado.Lejos, "Acércalas un poco más",
                $"Las tarjetas de {gA.t.datos.nombre.ToLower()} y {gB.t.datos.nombre.ToLower()} tienen que estar juntas para combinarse.");
        }
        else if (estado != Estado.Armar && estado != Estado.Formando && estado != Estado.Formada)
        {
            Poner(Estado.Armar);
            PintarArmar(gA.atomos.Count > 1 || gB.atomos.Count > 1);
        }

        switch (estado)
        {
            case Estado.Armar:
                Acomodar(gA, dt); Acomodar(gB, dt);
                // pequeña espera para que se alcance a ver el átomo nuevo antes de la unión
                if (tEstado > 0.7f && gA.atomos.Count == receta.cantidadA && gB.atomos.Count == receta.cantidadB) EmpezarUnion();
                break;

            case Estado.Lejos:
                Acomodar(gA, dt); Acomodar(gB, dt);
                break;

            case Estado.Formando:
                float k = Mathf.Clamp01(tEstado / duracionUnion);
                float e = k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;      // ease-in-out cúbico
                Unir(gA, centro, e); Unir(gB, centro, e);
                if (k >= 1f) TerminarUnion();
                break;

            case Estado.Formada:
                giro += 30f * dt;
                float pop = 1f - 0.22f * Mathf.Exp(-tEstado * 7f) * Mathf.Cos(tEstado * 13f);          // llega con un rebote leve
                molecula.position = centro + ta.up * (0.03f * ancho * Mathf.Sin(ahora * 1.6f));       // flota despacio
                molecula.rotation = ta.rotation * Quaternion.Euler(0f, giro, 0f);
                molecula.localScale = Vector3.one * (ancho * pop);
                break;
        }
    }

    // ───────── Estados y textos ─────────
    void Poner(Estado nuevo)
    {
        if (estado == nuevo) return;
        estado = nuevo; tEstado = 0f;
        bool armar = nuevo == Estado.Armar || nuevo == Estado.Formando, formada = nuevo == Estado.Formada;
        vistaArmar.Mostrar(armar);
        vistaFormada.Mostrar(formada);
        vistaMensaje.Mostrar(!armar && !formada);
    }

    void Mensaje(Estado e, string titulo, string texto)
    {
        Poner(e);
        if (claveMensaje == titulo) return;
        claveMensaje = titulo;
        mensajeTitulo.text = titulo; mensajeTexto.text = texto;
    }

    void PintarArmar(bool huboCambio)
    {
        ElementoData a = gA.t.datos, b = gB.t.datos;
        int na = gA.atomos.Count, nb = gB.atomos.Count;
        puntoA.color = a.color; nombreA.text = a.nombre; cuentaA.text = na.ToString();
        puntoB.color = b.color; nombreB.text = b.nombre; cuentaB.text = nb.ToString();
        Atenuar(menosA, na <= 1); Atenuar(masA, na >= maximoAtomos);
        Atenuar(menosB, nb <= 1); Atenuar(masB, nb >= maximoAtomos);

        // Pistas: primero lo que falta, luego lo que sobra
        if (na == receta.cantidadA && nb == receta.cantidadB) pista.text = "¡Esa es la cantidad! Mira cómo se unen.";
        else if (!huboCambio) pista.text = "Con uno de cada uno no alcanza. Agrega átomos con el botón +.";
        else if (na < receta.cantidadA) pista.text = $"Casi. Todavía le falta {a.nombre.ToLower()}.";
        else if (nb < receta.cantidadB) pista.text = $"Casi. Todavía le falta {b.nombre.ToLower()}.";
        else if (na > receta.cantidadA) pista.text = $"Ahí sobra {a.nombre.ToLower()}. Quita uno con el botón −.";
        else pista.text = $"Ahí sobra {b.nombre.ToLower()}. Quita uno con el botón −.";
    }

    static void Atenuar(Button b, bool apagado)
    {
        b.interactable = !apagado;
        if (b.TryGetComponent(out CanvasGroup g)) g.alpha = apagado ? 0.35f : 1f;
    }

    void PintarFormada()
    {
        ElementoData a = gA.t.datos, b = gB.t.datos;
        formadaNombre.text = receta.nombre;
        formadaFormula.text = receta.formula;
        formadaFormula.color = Color.Lerp(receta.color, Color.white, 0.35f);
        string atomos = receta.cantidadA == 1 ? "átomo" : "átomos";
        formadaReceta.text = $"{receta.cantidadA} {atomos} de {a.nombre.ToLower()} y {receta.cantidadB} de {b.nombre.ToLower()}.";
        formadaEcuacion.text = "En una reacción real:  " + receta.ecuacionReal;
        formadaDato.text = receta.dato;
    }

    void RefrescarProgreso()
    {
        for (int i = 0; i < chipFondo.Length && i < moleculas.Length; i++)
        {
            bool hecha = Coleccion.MoleculaFormada(moleculas[i].id);
            chipFondo[i].color = hecha ? moleculas[i].color : Velo;
            chipTexto[i].text = moleculas[i].formula;
            chipTexto[i].color = hecha ? AppAtomAR.Tinta(moleculas[i].color) : Tenue;
        }
        if (moleculasTexto) moleculasTexto.text = $"{Coleccion.Moleculas} de {Coleccion.TotalMoleculas}";
    }

    // ───────── Átomos sobre las tarjetas ─────────
    void Tomar(MoleculaData m, ElementoTarget a, ElementoTarget b)
    {
        receta = m; gA = Crear(a); gB = Crear(b);
        invalidoDesde = -1f;
        estado = Estado.Ninguno;
    }

    static Grupo Crear(ElementoTarget t)
    {
        var g = new Grupo { t = t, original = t.Modelo };
        if (g.original)
        {
            g.basePos = g.original.localPosition; g.baseEscala = g.original.localScale;
            g.atomos.Add(g.original);
        }
        return g;
    }

    void Cambiar(Grupo g, int delta)
    {
        if (estado != Estado.Armar || g == null || !g.original) return;
        int n = Mathf.Clamp(g.atomos.Count + delta, 1, maximoAtomos);
        if (n == g.atomos.Count) return;
        while (g.atomos.Count < n)
        {
            // el átomo nuevo nace pequeño junto al original y se acomoda en su lugar
            var copia = Instantiate(g.original, g.t.transform);
            copia.name = g.original.name + "_extra";
            copia.localPosition = g.original.localPosition;
            copia.localScale = g.original.localScale * 0.5f;
            g.atomos.Add(copia);
        }
        while (g.atomos.Count > n)
        {
            var ultimo = g.atomos[^1];
            g.atomos.RemoveAt(g.atomos.Count - 1);
            if (ultimo) Destroy(ultimo.gameObject);
        }
        tEstado = 0f;
        PintarArmar(true);
    }

    void Acomodar(Grupo g, float dt)
    {
        int n = g.atomos.Count; if (n == 0) return;
        float f = Factor[Mathf.Clamp(n - 1, 0, Factor.Length - 1)];
        float paso = 0.66f * f, k = 1f - Mathf.Exp(-dt * 12f);
        for (int i = 0; i < n; i++)
        {
            var a = g.atomos[i]; if (!a) continue;
            var pos = new Vector3(g.basePos.x + (i - (n - 1) * 0.5f) * paso, g.basePos.y * f, g.basePos.z);
            a.localPosition = Vector3.Lerp(a.localPosition, pos, k);
            a.localScale = Vector3.Lerp(a.localScale, g.baseEscala * f, k);
        }
    }

    // ───────── Unión y molécula ─────────
    void EmpezarUnion()
    {
        foreach (var g in new[] { gA, gB })
        {
            g.inicioPos = g.atomos.Select(a => a ? a.position : Vector3.zero).ToArray();
            g.inicioEsc = g.atomos.Select(a => a ? a.localScale : Vector3.one).ToArray();
        }
        Poner(Estado.Formando);
    }

    static void Unir(Grupo g, Vector3 centro, float e)
    {
        for (int i = 0; i < g.atomos.Count; i++)
        {
            var a = g.atomos[i]; if (!a) continue;
            a.position = Vector3.Lerp(g.inicioPos[i], centro, e);
            a.localScale = Vector3.Lerp(g.inicioEsc[i], g.inicioEsc[i] * 0.35f, e);
        }
    }

    void TerminarUnion()
    {
        foreach (var g in new[] { gA, gB })
            foreach (var a in g.atomos) if (a) a.gameObject.SetActive(false);
        CrearMolecula();
        Coleccion.RegistrarMolecula(receta.id);
        PintarFormada();
        RefrescarProgreso();
        Poner(Estado.Formada);
    }

    void CrearMolecula()
    {
        if (molecula) Destroy(molecula.gameObject);
        molecula = new GameObject("Molecula_" + receta.id).transform;
        if (!receta.modelo) return;
        var m = Instantiate(receta.modelo, molecula).transform;
        m.localPosition = Vector3.zero; m.localRotation = Quaternion.identity;
        // se ajusta para que mida 0.9 anchos de tarjeta y quede centrada en su ancla
        var rends = m.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max > 0f) { float s = 0.9f / max; m.localScale *= s; m.localPosition = -b.center * s; }
        }
        molecula.gameObject.SetActive(false);   // se enciende ya colocada, en el siguiente cuadro
    }

    /// Vuelve a 1 átomo por tarjeta, sin soltar la pareja.
    void Reiniciar()
    {
        if (molecula) { Destroy(molecula.gameObject); molecula = null; }
        Restaurar(gA); Restaurar(gB);
        if (gA?.original) gA.atomos.Add(gA.original);
        if (gB?.original) gB.atomos.Add(gB.original);
    }

    /// Suelta la pareja y deja las tarjetas como estaban.
    void Soltar()
    {
        if (molecula) { Destroy(molecula.gameObject); molecula = null; }
        Restaurar(gA); Restaurar(gB);
        gA = gB = null; receta = null; invalidoDesde = -1f;
    }

    static void Restaurar(Grupo g)
    {
        if (g == null) return;
        foreach (var a in g.atomos) if (a && a != g.original) Destroy(a.gameObject);
        g.atomos.Clear();
        if (g.original)
        {
            g.original.gameObject.SetActive(true);
            g.original.localPosition = g.basePos; g.original.localScale = g.baseEscala;
        }
    }
}
