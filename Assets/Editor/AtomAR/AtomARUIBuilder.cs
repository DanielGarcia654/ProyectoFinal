// AtomAR - Genera las fichas de datos de los 10 elementos y arma la interfaz del Modo Elemento.
// Menús: AtomAR > Crear datos de elementos   |   AtomAR > Montar interfaz
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class AtomARUIBuilder
{
    const string DatosDir = "Assets/AtomAR_Datos/Elementos";
    const string UIDir = "Assets/AtomAR_UI";
    const string SpritePath = UIDir + "/Redondeado.png";
    const string DegradadoPath = UIDir + "/Degradado.png";

    // ── Paleta AtomAR ──
    static readonly Color Noche = Hex("#151833");          // fondo de la hoja (índigo profundo, no negro)
    static readonly Color Papel = Hex("#F3F5FF");          // texto principal
    static readonly Color Tenue = Hex("#98A1C8");          // texto secundario
    static readonly Color Proton = Hex("#F2464F");         // mismos colores que el modelo 3D
    static readonly Color Neutron = Hex("#9AA6C2");
    static readonly Color Electron = Hex("#2EE6FF");

    // target, símbolo, nombre, apodo, Z, neutrones, masa (IUPAC), categoría, capas, color (tomado de su tarjeta), dato
    static readonly (string t, string s, string n, string apodo, int z, int nn, string m, string cat, int[] capas, string col, string dato)[] Elementos =
    {
        ("01_h_hidrogeno", "H", "Hidrógeno", "El más ligero y el más abundante del universo", 1, 0, "1.008", "No metal", new[]{1}, "#2663EA",
         "Las estrellas, como el Sol, están hechas sobre todo de hidrógeno. En la Tierra lo encuentras en el agua: H<sub>2</sub>O."),
        ("02_he_helio", "He", "Helio", "El gas que hace flotar los globos", 2, 2, "4.0026", "Gas noble", new[]{2}, "#D72876",
         "Es tan ligero que escapa de la atmósfera al espacio. También se usa para enfriar los imanes de las máquinas de resonancia magnética."),
        ("03_c_carbono", "C", "Carbono", "El ladrillo de la vida", 6, 6, "12.011", "No metal", new[]{2,4}, "#575B68",
         "Todos los seres vivos están hechos de carbono. El grafito de tu lápiz y un diamante son el mismo elemento, solo acomodado distinto."),
        ("04_n_nitrogeno", "N", "Nitrógeno", "Casi todo el aire que respiras", 7, 7, "14.007", "No metal", new[]{2,5}, "#14823B",
         "Forma el 78 % del aire. Las plantas lo necesitan para crecer, por eso está en los fertilizantes."),
        ("05_o_oxigeno", "O", "Oxígeno", "Tu combustible para vivir", 8, 8, "15.999", "No metal", new[]{2,6}, "#0990B2",
         "Forma el 21 % del aire. Tus células lo usan para sacar energía de la comida, y sin él nada puede arder."),
        ("06_na_sodio", "Na", "Sodio", "Un metal tan blando que se corta con cuchillo", 11, 12, "22.990", "Metal alcalino", new[]{2,8,1}, "#EA570B",
         "Unido al cloro forma la sal de mesa (NaCl). Tus nervios lo usan para mandar mensajes a tus músculos."),
        ("07_cl_cloro", "Cl", "Cloro", "El guardián del agua limpia", 17, 18, "35.45", "Halógeno", new[]{2,8,7}, "#6BAA12",
         "Se usa para potabilizar el agua y desinfectar albercas. Unido al sodio forma la sal de mesa."),
        ("08_ca_calcio", "Ca", "Calcio", "El constructor de tus huesos", 20, 20, "40.078", "Metal alcalinotérreo", new[]{2,8,8,2}, "#DCC6A0",
         "Hace fuertes tus huesos y dientes. Está en la leche, en las tortillas de maíz nixtamalizado y en el cemento."),
        ("09_fe_hierro", "Fe", "Hierro", "El metal que viaja en tu sangre", 26, 30, "55.845", "Metal de transición", new[]{2,8,14,2}, "#943813",
         "Es el ingrediente principal del acero. En tu sangre, la hemoglobina usa hierro para llevar oxígeno a todo tu cuerpo."),
        ("10_au_oro", "Au", "Oro", "El metal que nunca se oxida", 79, 118, "196.97", "Metal de transición", new[]{2,8,18,32,18,1}, "#C8860F",
         "No pierde su brillo con el tiempo, por eso se usa en joyería y en los contactos de celulares y computadoras."),
    };

    [MenuItem("AtomAR/Crear datos de elementos")]
    public static void CrearDatos()
    {
        Directory.CreateDirectory(DatosDir);
        foreach (var e in Elementos)
        {
            string path = $"{DatosDir}/{e.s}_{Sinacentos(e.n)}.asset";
            var d = AssetDatabase.LoadAssetAtPath<ElementoData>(path);
            bool nuevo = d == null;
            if (nuevo) d = ScriptableObject.CreateInstance<ElementoData>();
            d.targetName = e.t; d.simbolo = e.s; d.nombre = e.n; d.apodo = e.apodo; d.numeroAtomico = e.z; d.neutrones = e.nn;
            d.masaAtomica = e.m; d.categoria = e.cat; d.electronesPorCapa = e.capas; d.dato = e.dato;
            d.color = Hex(e.col);
            if (nuevo) AssetDatabase.CreateAsset(d, path); else EditorUtility.SetDirty(d);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[AtomAR] {Elementos.Length} fichas de elementos en {DatosDir}");
    }

    // ───────────────────────── Interfaz ─────────────────────────
    // Base del Modo Explorar; el menú "AtomAR > Montar interfaz" está en AtomARAppBuilder.cs y llama a este método
    public static void MontarInterfaz()
    {
        CrearDatos();
        var sp = SpriteRedondeado();
        var deg = SpriteDegradado();
        var syne = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Syne SDF.asset");
        var inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Inter SDF.asset");

        var viejo = GameObject.Find("UI_AtomAR");
        if (viejo) Object.DestroyImmediate(viejo);

        var canvasGO = new GameObject("UI_AtomAR", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;

        // Sombra superior para que la barra se lea sobre cualquier fondo de cámara
        var sombra = Nuevo("SombraSuperior", canvasGO.transform);
        Anclar(sombra, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 300));
        var sombraImg = sombra.gameObject.AddComponent<Image>(); sombraImg.sprite = deg; sombraImg.color = new Color(Noche.r, Noche.g, Noche.b, 0.7f); sombraImg.raycastTarget = false;

        var segura = Nuevo("AreaSegura", canvasGO.transform); Estirar(segura); segura.gameObject.AddComponent<AreaSegura>();

        // ── Barra superior: marca + colección
        var marca = Txt("Marca", segura, syne, "AtomAR", 54, Papel, TextAlignmentOptions.TopLeft);
        Anclar(marca.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -36), new Vector2(400, 80));
        marca.characterSpacing = -2f;

        var pastilla = Caja("Coleccion", segura, sp, new Color(Noche.r, Noche.g, Noche.b, 0.72f), 1.2f);
        Anclar(pastilla, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -36), new Vector2(232, 80));
        var punto = Caja("Punto", pastilla, sp, Electron, 48f / 10f);
        Anclar(punto, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(20, 20));
        var colTxt = Txt("Texto", pastilla, inter, "0 de 10", 32, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(colTxt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(64, 0), new Vector2(-72, 0));

        // ── Escáner (cuando no hay tarjeta)
        var esc = Nuevo("Escaner", segura); Estirar(esc);
        var escGrupo = esc.gameObject.AddComponent<CanvasGroup>(); escGrupo.blocksRaycasts = false; escGrupo.interactable = false;
        var marco = Nuevo("Marco", esc);
        Anclar(marco, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(480, 480));
        foreach (var (ax, ay) in new[] { (0, 1), (1, 1), (0, 0), (1, 0) })
        {
            var a = new Vector2(ax, ay);
            var h = Caja($"H{ax}{ay}", marco, sp, Color.white, 48f / 5f); Anclar(h, a, a, a, Vector2.zero, new Vector2(96, 10));
            var v = Caja($"V{ax}{ay}", marco, sp, Color.white, 48f / 5f); Anclar(v, a, a, a, Vector2.zero, new Vector2(10, 96));
        }
        var escPill = Caja("Indicacion", esc, sp, new Color(Noche.r, Noche.g, Noche.b, 0.82f), 1f);
        Anclar(escPill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -210), new Vector2(760, 100));
        var escTxt = Txt("Texto", escPill, inter, "Apunta la cámara a una tarjeta", 36, Papel, TextAlignmentOptions.Center);
        Estirar(escTxt.rectTransform);

        // ── Aviso de descubrimiento
        var aviso = Caja("AvisoDescubrimiento", segura, sp, new Color(Noche.r, Noche.g, Noche.b, 0.94f), 1f);
        Anclar(aviso, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(780, 108));
        var avisoGrupo = aviso.gameObject.AddComponent<CanvasGroup>(); avisoGrupo.blocksRaycasts = false; avisoGrupo.alpha = 0;
        var avisoPunto = Caja("Punto", aviso, sp, Color.white, 48f / 15f);
        Anclar(avisoPunto, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(30, 30));
        var avisoTxt = Txt("Texto", aviso, syne, "¡Descubriste el Hierro!", 40, Papel, TextAlignmentOptions.MidlineLeft);
        Anclar(avisoTxt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(88, 0), new Vector2(-112, 0));
        avisoTxt.enableAutoSizing = true; avisoTxt.fontSizeMin = 28; avisoTxt.fontSizeMax = 40;

        // ── Hoja / carta del elemento
        const float W = 1032f, H = 1010f, IN = 14f, BH = 272f;
        var hoja = Caja("Hoja", segura, sp, new Color(Noche.r, Noche.g, Noche.b, 0.95f), 1f);
        Anclar(hoja, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(W, H));
        var grupoHoja = hoja.gameObject.AddComponent<CanvasGroup>();
        var contenido = Nuevo("Contenido", hoja); Estirar(contenido);
        var grupoContenido = contenido.gameObject.AddComponent<CanvasGroup>();

        // Bloque 0 · Carta (banda de color del elemento)
        var carta = Nuevo("Carta", contenido); Arriba(carta, new Vector2(IN, -IN), new Vector2(W - 2 * IN, BH));
        var banda = Caja("Banda", carta, sp, Hex("#943813"), 48f / 38f); Estirar(banda);
        banda.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        var fantasma = Txt("SimboloFantasma", banda, syne, "Fe", 360, Color.white, TextAlignmentOptions.MidlineRight);
        Anclar(fantasma.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(46, -34), new Vector2(700, 420));
        fantasma.characterSpacing = -6f; fantasma.enableWordWrapping = false;

        var chip = Caja("ChipZ", banda, sp, new Color(0, 0, 0, 0.14f), 48f / 28f);
        Arriba(chip, new Vector2(28, -28), new Vector2(92, 56));
        var chipTxt = Txt("Z", chip, inter, "26", 32, Papel, TextAlignmentOptions.Center, bold: true); Estirar(chipTxt.rectTransform);
        var masa = Txt("Masa", banda, inter, "55.845 u", 28, Papel, TextAlignmentOptions.MidlineLeft);
        Arriba(masa.rectTransform, new Vector2(136, -28), new Vector2(320, 56));

        var nombre = Txt("Nombre", banda, syne, "Hierro", 84, Papel, TextAlignmentOptions.BottomLeft);
        Arriba(nombre.rectTransform, new Vector2(28, -92), new Vector2(760, 104)); nombre.characterSpacing = -3f;
        nombre.enableAutoSizing = true; nombre.fontSizeMin = 56; nombre.fontSizeMax = 84; nombre.enableWordWrapping = false;
        var apodo = Txt("Apodo", banda, inter, "El metal que viaja en tu sangre", 34, Papel, TextAlignmentOptions.TopLeft);
        Arriba(apodo.rectTransform, new Vector2(30, -198), new Vector2(760, 60));
        apodo.enableAutoSizing = true; apodo.fontSizeMin = 24; apodo.fontSizeMax = 34;

        var botonMas = Caja("BotonMas", banda, sp, new Color(0, 0, 0, 0.16f), 48f / 44f);
        Anclar(botonMas, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(88, 88));
        var icono = Nuevo("Icono", botonMas); Anclar(icono, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36));
        var barraH = Caja("H", icono, sp, Papel, 48f / 3f); Anclar(barraH, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 6));
        var barraV = Caja("V", icono, sp, Papel, 48f / 3f); Anclar(barraV, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 36));
        var boton = botonMas.gameObject.AddComponent<Button>(); boton.transition = Selectable.Transition.None;
        botonMas.gameObject.AddComponent<BotonPresionable>();

        // Bloque 1 · Átomo + conteo de partículas
        var bloqueAtomo = Nuevo("BloqueAtomo", contenido); Arriba(bloqueAtomo, new Vector2(0, -(IN + BH + 30)), new Vector2(W, 300));
        var diag = Nuevo("DiagramaBohr", bloqueAtomo); Arriba(diag, new Vector2(40, 0), new Vector2(292, 292));
        var diagrama = diag.gameObject.AddComponent<DiagramaBohr>();
        diagrama.punto = sp; diagrama.colorNucleo = Proton; diagrama.colorElectron = Electron;
        TMP_Text cP, cN, cE, eP, eN, eE;
        Conteo(bloqueAtomo, "Protones", 0, Proton, syne, inter, sp, out cP, out eP);
        Conteo(bloqueAtomo, "Neutrones", 1, Neutron, syne, inter, sp, out cN, out eN);
        Conteo(bloqueAtomo, "Electrones", 2, Electron, syne, inter, sp, out cE, out eE);

        // Bloque 2 · Frase (categoría y capas en lenguaje natural)
        var bloqueFrase = Nuevo("BloqueFrase", contenido); Arriba(bloqueFrase, new Vector2(0, -(IN + BH + 346)), new Vector2(W, 110));
        var frase = Txt("Frase", bloqueFrase, inter, "", 32, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(frase.rectTransform, new Vector2(40, 0), new Vector2(W - 80, 110)); frase.lineSpacing = 6f;

        // Bloque 3 · ¿Sabías que…?
        var bloqueDato = Nuevo("BloqueDato", contenido); Arriba(bloqueDato, new Vector2(0, -(IN + BH + 470)), new Vector2(W, 240));
        var sabias = Txt("Titulo", bloqueDato, syne, "¿Sabías que…?", 44, Papel, TextAlignmentOptions.TopLeft);
        Arriba(sabias.rectTransform, new Vector2(40, 0), new Vector2(W - 80, 60)); sabias.characterSpacing = -1.5f;
        var dato = Txt("Texto", bloqueDato, inter, "", 36, Papel, TextAlignmentOptions.TopLeft);
        Arriba(dato.rectTransform, new Vector2(40, -66), new Vector2(W - 80, 170)); dato.lineSpacing = 8f;

        // Selector de tarjetas: aparece justo arriba de la carta cuando hay 2 o más a la vista
        var selector = Nuevo("Selector", hoja);
        Anclar(selector, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 16), new Vector2(W, 84));
        var selGrupo = selector.gameObject.AddComponent<CanvasGroup>();
        var pastillas = new Button[4]; var pastFondo = new Image[4]; var pastTxt = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var pa = Caja($"Pastilla{i + 1}", selector, sp, Color.white, 48f / 42f);
            Anclar(pa, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(76 + i * 140, 42), new Vector2(124, 84));
            pastFondo[i] = pa.GetComponent<Image>(); pastFondo[i].raycastTarget = true;
            pastillas[i] = pa.gameObject.AddComponent<Button>(); pastillas[i].transition = Selectable.Transition.None;
            pastTxt[i] = Txt("Simbolo", pa, syne, "H", 44, Papel, TextAlignmentOptions.Center); Estirar(pastTxt[i].rectTransform);
        }

        // Controlador
        var panel = hoja.gameObject.AddComponent<PanelElemento>();
        panel.hoja = hoja; panel.grupoHoja = grupoHoja; panel.grupoContenido = grupoContenido;
        panel.filas = new[] { carta, bloqueAtomo, bloqueFrase, bloqueDato };
        panel.alturaCompacta = IN + BH + IN; panel.margenInferior = 24f;
        panel.banda = banda.GetComponent<Image>(); panel.simboloFantasma = fantasma; panel.nombre = nombre; panel.apodo = apodo;
        panel.chipZ = chipTxt; panel.chipZFondo = chip.GetComponent<Image>(); panel.masa = masa;
        panel.iconoMas = icono; panel.botonMasFondo = botonMas.GetComponent<Image>();
        panel.diagrama = diagrama;
        panel.cuentaP = cP; panel.cuentaN = cN; panel.cuentaE = cE; panel.etiquetaP = eP; panel.etiquetaN = eN; panel.etiquetaE = eE;
        panel.frase = frase; panel.sabiasTitulo = sabias; panel.dato = dato;
        panel.coleccionTexto = colTxt; panel.coleccionPastilla = pastilla;
        panel.aviso = avisoGrupo; panel.avisoRT = aviso; panel.avisoPunto = avisoPunto.GetComponent<Image>(); panel.avisoTexto = avisoTxt;
        panel.escaner = escGrupo; panel.escanerMarco = marco;
        panel.selector = selGrupo; panel.pastillas = pastillas; panel.pastillasFondo = pastFondo; panel.pastillasTexto = pastTxt;
        UnityEventTools.AddPersistentListener(boton.onClick, panel.AlternarDetalle);
        // Reciben toques: la hoja (arrastre), la banda (tocar para desplegar) y el botón +
        hoja.GetComponent<Image>().raycastTarget = true; banda.GetComponent<Image>().raycastTarget = true; botonMas.GetComponent<Image>().raycastTarget = true;

        if (!Object.FindAnyObjectByType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var modulo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (modulo != null) es.AddComponent(modulo); else es.AddComponent<StandaloneInputModule>();
        }

        var fichas = AssetDatabase.FindAssets("t:ElementoData", new[] { DatosDir })
            .Select(g => AssetDatabase.LoadAssetAtPath<ElementoData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
        int conectadas = 0;
        foreach (var it in Object.FindObjectsByType<Vuforia.ImageTargetBehaviour>(FindObjectsSortMode.None))
        {
            var d = fichas.FirstOrDefault(f => f.targetName == it.TargetName);
            if (!d) { Debug.LogWarning($"[AtomAR] Sin ficha para {it.TargetName}"); continue; }
            if (!it.TryGetComponent(out ElementoTarget et)) et = it.gameObject.AddComponent<ElementoTarget>();
            et.datos = d; EditorUtility.SetDirty(et); conectadas++;
            // Collider para poder tocar el átomo en la cámara y seleccionarlo
            if (!it.TryGetComponent(out BoxCollider bc)) bc = it.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.3f, 0f); bc.size = new Vector3(1f, 0.6f, 1f);
        }

        var arCam = GameObject.Find("AR Camera");
        if (arCam && !arCam.GetComponent<PhysicsRaycaster>()) arCam.AddComponent<PhysicsRaycaster>();

        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeGameObject = hoja.gameObject;
        Debug.Log($"[AtomAR] Interfaz montada; {conectadas} tarjetas conectadas a su ficha.");
    }

    [MenuItem("AtomAR/Reiniciar colección (pruebas)")]
    public static void ReiniciarColeccion() { Coleccion.Reiniciar(); Debug.Log("[AtomAR] Colección reiniciada: 0 de 10."); }

    // ───────────────────────── Ayudantes ─────────────────────────
    static void Conteo(RectTransform padre, string nombre, int i, Color color, TMP_FontAsset syne, TMP_FontAsset inter, Sprite sp, out TMP_Text numero, out TMP_Text etiqueta)
    {
        var f = Nuevo(nombre, padre); Arriba(f, new Vector2(384, -24 - i * 88), new Vector2(620, 84));
        var p = Caja("Punto", f, sp, color, 48f / 12f);
        Anclar(p, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(24, 24));
        numero = Txt("Numero", f, syne, "0", 64, Papel, TextAlignmentOptions.MidlineLeft);
        Anclar(numero.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(44, 0), new Vector2(150, 0));
        numero.characterSpacing = -2f; numero.enableWordWrapping = false;
        etiqueta = Txt("Etiqueta", f, inter, nombre.ToLower(), 34, Tenue, TextAlignmentOptions.MidlineLeft);
        Anclar(etiqueta.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(172, 0), new Vector2(-172, 0));
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static string Sinacentos(string s) =>
        s.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

    static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Caja(string nombre, Transform padre, Sprite sprite, Color color, float multiplicador)
    {
        var rt = Nuevo(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        if (sprite) { img.sprite = sprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = multiplicador; }
        return rt;
    }

    static TMP_Text Txt(string nombre, Transform padre, TMP_FontAsset fuente, string texto, float tam, Color color, TextAlignmentOptions al, bool bold = false)
    {
        var rt = Nuevo(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente) t.font = fuente;
        t.text = texto; t.fontSize = tam; t.color = color; t.alignment = al;
        bool display = fuente && fuente.name.StartsWith("Syne");   // Syne siempre en negrita: es la voz de la marca
        t.fontStyle = bold || display ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false; t.richText = true;
        return t;
    }

    static void Estirar(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    static void Anclar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void Arriba(RectTransform rt, Vector2 pos, Vector2 size)
    {
        var a = new Vector2(0f, 1f);
        Anclar(rt, a, a, a, pos, size);
    }

    static Sprite SpriteRedondeado()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existente) return existente;
        Directory.CreateDirectory(UIDir);
        const int N = 128; const float R = 48f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(R - d + 0.5f)));
        }
        return GuardarSprite(tex, SpritePath, new Vector4(R, R, R, R));
    }

    static Sprite SpriteDegradado()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(DegradadoPath);
        if (existente) return existente;
        Directory.CreateDirectory(UIDir);
        var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            float t = y / 127f;                       // 0 abajo (transparente) → 1 arriba (opaco)
            float a = t * t * (3f - 2f * t);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        return GuardarSprite(tex, DegradadoPath, Vector4.zero);
    }

    static Sprite GuardarSprite(Texture2D tex, string path, Vector4 borde)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = borde;
        imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
