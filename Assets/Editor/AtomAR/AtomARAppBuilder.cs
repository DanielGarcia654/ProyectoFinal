// AtomAR - Arma la app completa sobre la interfaz del Modo Elemento:
// inicio en forma de recorrido, barra superior, guía animada sobre la cámara, modo Combinar, Reto (quiz) y celebraciones.
// Menú: AtomAR > Montar interfaz   (vuelve a generar todo; las fichas de datos se regeneran con los textos de aquí)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class AtomARUIBuilder
{
    const string MolDir = "Assets/AtomAR_Datos/Moleculas";
    const string MolModelos = "Assets/AtomAR_Modelos/Moleculas";

    static readonly Color OroC = Hex("#FFC83D");
    static readonly Color Tarjeta = Hex("#1F2349");
    static readonly Color Velo = new(1f, 1f, 1f, 0.08f);
    static readonly Color TintaOsc = new(0.08f, 0.09f, 0.20f);

    // Pregunta del Reto cuya respuesta es cada elemento
    static readonly Dictionary<string, string> PreguntasUso = new()
    {
        ["H"] = "¿De qué elemento están hechas sobre todo las estrellas?",
        ["He"] = "¿Qué gas hace flotar los globos?",
        ["C"] = "¿De qué elemento están hechos el grafito y el diamante?",
        ["N"] = "¿Qué elemento forma el 78 % del aire?",
        ["O"] = "¿Qué elemento usan tus células para sacar energía de la comida?",
        ["Na"] = "¿Qué metal forma la sal de mesa junto con el cloro?",
        ["Cl"] = "¿Qué elemento se usa para desinfectar el agua de las albercas?",
        ["Ca"] = "¿Qué elemento hace fuertes tus huesos y dientes?",
        ["Fe"] = "¿Qué metal usa tu sangre para llevar oxígeno?",
        ["Au"] = "¿Qué metal no se oxida y se usa en joyería?",
    };

    // id, nombre, fórmula, elemento A y cantidad, elemento B y cantidad, ecuación real balanceada, dato, color, modelo FBX
    static readonly (string id, string nombre, string formula, string a, int na, string b, int nb, string ecuacion, string dato, string color, string modelo)[] Moleculas =
    {
        ("sal", "Sal de mesa", "NaCl", "Na", 1, "Cl", 1, "2 Na + Cl<sub>2</sub> → 2 NaCl",
         "El sodio es un metal que reacciona con fuerza y el cloro es un gas tóxico, pero juntos forman la sal que le pones a tu comida.", "#E4E9F7", "Mol_NaCl_Sal"),
        ("agua", "Agua", "H<sub>2</sub>O", "H", 2, "O", 1, "2 H<sub>2</sub> + O<sub>2</sub> → 2 H<sub>2</sub>O",
         "Cubre casi tres cuartas partes de la Tierra y forma más de la mitad de tu cuerpo.", "#2B8CE6", "Mol_H2O_Agua"),
        ("co2", "Dióxido de carbono", "CO<sub>2</sub>", "C", 1, "O", 2, "C + O<sub>2</sub> → CO<sub>2</sub>",
         "Lo sacas cada vez que exhalas y las plantas lo usan para fabricar su alimento. También es el gas de las burbujas del refresco.", "#7A8299", "Mol_CO2_DioxidoCarbono"),
        ("herrumbre", "Óxido de hierro", "Fe<sub>2</sub>O<sub>3</sub>", "Fe", 2, "O", 3, "4 Fe + 3 O<sub>2</sub> → 2 Fe<sub>2</sub>O<sub>3</sub>",
         "Es la herrumbre: la capa rojiza que le sale al hierro cuando pasa tiempo con aire y humedad.", "#B5451B", "Mol_Fe2O3_Herrumbre"),
    };

    [MenuItem("AtomAR/Montar interfaz")]
    public static void MontarApp()
    {
        MontarInterfaz();                                   // cámara + carta del elemento (Modo Explorar)
        var fichas = CompletarFichas();
        var mols = CrearMoleculas();

        var sp = SpriteRedondeado(); var marcoSp = SpriteMarco(); var brillo = SpriteBrillo();
        var cartas = SpritesTarjetas();
        var syne = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Syne SDF.asset");
        var inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Inter SDF.asset");
        var canvasGO = GameObject.Find("UI_AtomAR");
        var canvas = canvasGO.transform;
        var segura = (RectTransform)canvas.Find("AreaSegura");
        var marca = segura.Find("Marca"); if (marca) Object.DestroyImmediate(marca.gameObject);
        var coleccion = (RectTransform)segura.Find("Coleccion");

        var combinar = MontarCombinar(segura, sp, syne, inter, mols, fichas);
        var guia = MontarGuia(canvas, sp, marcoSp, syne, inter, cartas);
        var quiz = MontarReto(canvas, sp, marcoSp, syne, inter, fichas, cartas, out var pantallaReto);

        // ── Barra superior: atrás, título, salto al siguiente modo, ayuda y avance
        var barra = Nuevo("BarraSuperior", canvas); Estirar(barra); barra.gameObject.AddComponent<AreaSegura>();
        var barraF = barra.gameObject.AddComponent<FundidoUI>();
        var atras = Circulo("Atras", barra, sp, inter, "←", V(0, 1), V(40, -36));
        var tituloBarra = Txt("Titulo", barra, syne, "Explorar", 50, Papel, TextAlignmentOptions.MidlineLeft);
        Anclar(tituloBarra.rectTransform, V(0, 1), V(0, 1), V(0, 1), V(144, -36), V(320, 80)); tituloBarra.characterSpacing = -2f; tituloBarra.enableWordWrapping = false;
        var sig = Caja("Siguiente", barra, marcoSp, Electron, Pil(64));
        Anclar(sig, V(0, 1), V(0, 1), V(0, 1), V(420, -44), V(260, 64));
        var sigHalo = Caja("Halo", sig, brillo, Electron, 1f); Estirar(sigHalo); sigHalo.offsetMin = V(-36, -36); sigHalo.offsetMax = V(36, 36);
        var sigHaloG = sigHalo.gameObject.AddComponent<CanvasGroup>(); sigHaloG.alpha = 0f; sigHaloG.blocksRaycasts = false;
        var sigT = Txt("Texto", sig, inter, "Ir a Combinar", 30, Electron, TextAlignmentOptions.Center, bold: true); Estirar(sigT.rectTransform); sigT.enableWordWrapping = false;
        var bSig = Boton(sig);
        var ayuda = Circulo("Ayuda", barra, sp, inter, "?", V(1, 1), V(-292, -36));
        coleccion.SetParent(barra, false);
        var pastillaMol = Caja("Moleculas", barra, sp, new Color(Noche.r, Noche.g, Noche.b, 0.72f), 1.2f);
        Anclar(pastillaMol, V(1, 1), V(1, 1), V(1, 1), V(-40, -36), V(232, 80));
        var puntoMol = Caja("Punto", pastillaMol, sp, Proton, 48f / 10f);
        Anclar(puntoMol, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(28, 0), V(20, 20));
        var molTxt = Txt("Texto", pastillaMol, inter, "0 de 4", 32, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(molTxt.rectTransform, Vector2.zero, Vector2.one, V(0, 0.5f), V(64, 0), V(-72, 0));
        combinar.moleculasTexto = molTxt;

        // ── Pantalla de inicio: un recorrido de tres paradas en lugar de tres cajas
        var inicio = Pantalla("PantallaInicio", canvas, Noche, out var ini);
        var inicioF = inicio.gameObject.AddComponent<FundidoUI>(); inicioF.visibleAlInicio = true;
        // átomo grande de fondo, asomando por la esquina (la firma visual de la app)
        var decor = Nuevo("AtomoDeFondo", inicio); decor.SetSiblingIndex(0);
        Anclar(decor, V(1, 1), V(1, 1), V(0.5f, 0.5f), V(-90, -250), V(1180, 1180));
        var bohrFondo = decor.gameObject.AddComponent<DiagramaBohr>();
        bohrFondo.punto = sp; bohrFondo.capasIniciales = new[] { 2, 3, 4, 3 };
        bohrFondo.colorOrbita = new Color(1f, 1f, 1f, 0.07f); bohrFondo.colorElectron = new Color(Electron.r, Electron.g, Electron.b, 0.75f);
        bohrFondo.colorNucleo = new Color(Proton.r, Proton.g, Proton.b, 0.85f);
        bohrFondo.radioNucleo = 70f; bohrFondo.tamElectron = 22f; bohrFondo.velocidadCapa1 = 12f;

        var logo = Txt("Marca", ini, syne, "AtomAR", 136, Papel, TextAlignmentOptions.TopLeft);
        Arriba(logo.rectTransform, V(44, -130), V(760, 170)); logo.characterSpacing = -5f;
        var lema = Txt("Lema", ini, inter, "Descubre de qué está hecho todo lo que te rodea.", 40, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(lema.rectTransform, V(50, -306), V(640, 120)); lema.lineSpacing = 6f;

        var botones = new Button[3]; var progresos = new TMP_Text[3]; var anillos = new AnilloUI[3]; var halos = new CanvasGroup[3]; var sigue = new GameObject[3];
        string[] titulos = { "Explorar", "Combinar", "Reto" };
        string[] descripciones = { "Apunta a una tarjeta y conoce su átomo.", "Junta dos tarjetas y forma una molécula.", "Responde preguntas y gana insignias." };
        Color[] colores = { Electron, Proton, OroC };
        for (int i = 0; i < 3; i++)
            botones[i] = FilaModo(ini, sp, brillo, syne, inter, i, titulos[i], descripciones[i], colores[i], out progresos[i], out anillos[i], out halos[i], out sigue[i]);

        var celebracion = MontarCelebracion(canvas, sp, brillo, syne, inter, fichas, mols, cartas);

        // ── Controlador de la app
        canvasGO.AddComponent<GestosAtomo>();                // girar y acercar el átomo en Explorar
        var app = canvasGO.AddComponent<AppAtomAR>();
        app.pantallaInicio = inicioF; app.barra = barraF; app.pantallaReto = pantallaReto; app.guia = guia; app.celebracion = celebracion; app.quiz = quiz;
        app.botonesModo = botones; app.progresos = progresos; app.anillos = anillos; app.halos = halos; app.etiquetasSigue = sigue;
        app.botonAtras = atras; app.botonAyuda = ayuda; app.botonSiguiente = bSig; app.tituloBarra = tituloBarra; app.siguienteTexto = sigT;
        app.siguienteMarco = sig.GetComponent<Image>(); app.siguienteHalo = sigHaloG; app.siguienteHaloImagen = sigHalo.GetComponent<Image>();
        app.pastillaColeccion = coleccion.gameObject; app.pastillaMoleculas = pastillaMol.gameObject;

        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[AtomAR] App montada: inicio, Explorar, Combinar, Reto, guía animada y celebraciones.");
    }

    [MenuItem("AtomAR/Reiniciar progreso completo (pruebas)")]
    public static void ReiniciarTodo()
    {
        Coleccion.Reiniciar(); ProgresoQuiz.Reiniciar(); GuiaVisual.Olvidar(); AppAtomAR.OlvidarLogros(); PlayerPrefs.Save();
        Debug.Log("[AtomAR] Progreso reiniciado: colección, moléculas, insignias y tutoriales.");
    }

    // ───────────────────────── Datos ─────────────────────────
    static ElementoData[] CompletarFichas()
    {
        var fichas = AssetDatabase.FindAssets("t:ElementoData", new[] { DatosDir })
            .Select(g => AssetDatabase.LoadAssetAtPath<ElementoData>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(f => f.numeroAtomico).ToArray();
        foreach (var f in fichas)
            if (PreguntasUso.TryGetValue(f.simbolo, out var p)) { f.preguntaUso = p; EditorUtility.SetDirty(f); }
        AssetDatabase.SaveAssets();
        return fichas;
    }

    static MoleculaData[] CrearMoleculas()
    {
        Directory.CreateDirectory(MolDir);
        var lista = new List<MoleculaData>();
        foreach (var m in Moleculas)
        {
            string path = $"{MolDir}/{m.id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<MoleculaData>(path);
            bool nuevo = d == null;
            if (nuevo) d = ScriptableObject.CreateInstance<MoleculaData>();
            d.id = m.id; d.nombre = m.nombre; d.formula = m.formula;
            d.simboloA = m.a; d.cantidadA = m.na; d.simboloB = m.b; d.cantidadB = m.nb;
            d.ecuacionReal = m.ecuacion; d.dato = m.dato; d.color = Hex(m.color);
            d.modelo = AssetDatabase.LoadAssetAtPath<GameObject>($"{MolModelos}/{m.modelo}.fbx");
            if (!d.modelo) Debug.LogWarning($"[AtomAR] No encontré el modelo {m.modelo}.fbx");
            if (nuevo) AssetDatabase.CreateAsset(d, path); else EditorUtility.SetDirty(d);
            lista.Add(d);
        }
        AssetDatabase.SaveAssets();
        return lista.ToArray();
    }

    // ───────────────────────── Modo Combinar ─────────────────────────
    static ModoCombinar MontarCombinar(RectTransform segura, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter, MoleculaData[] mols, ElementoData[] fichas)
    {
        const float W = 1032f, H = 700f;
        var hoja = Caja("PanelCombinar", segura, sp, new Color(Noche.r, Noche.g, Noche.b, 0.95f), 1f);
        Anclar(hoja, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 24), V(W, H));
        hoja.GetComponent<Image>().raycastTarget = true;
        var hojaF = hoja.gameObject.AddComponent<FundidoUI>(); hojaF.desplazamiento = 60f;

        // Vista: guía + tabla de las moléculas que se pueden formar
        var vm = Nuevo("VistaMensaje", hoja); Estirar(vm); var vmF = vm.gameObject.AddComponent<FundidoUI>(); vmF.desplazamiento = 14f;
        var mTitulo = Txt("Titulo", vm, syne, "Junta dos tarjetas", 54, Papel, TextAlignmentOptions.TopLeft);
        Arriba(mTitulo.rectTransform, V(40, -38), V(W - 80, 128)); mTitulo.characterSpacing = -2f; mTitulo.lineSpacing = -10f;
        mTitulo.enableAutoSizing = true; mTitulo.fontSizeMin = 38; mTitulo.fontSizeMax = 54;
        var mTexto = Txt("Texto", vm, inter, "", 32, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(mTexto.rectTransform, V(40, -172), V(W - 80, 90)); mTexto.lineSpacing = 4f;
        var checkF = new Image[mols.Length]; var formulaT = new TMP_Text[mols.Length];
        for (int i = 0; i < mols.Length; i++)
        {
            var m = mols[i];
            var fila = Nuevo($"Receta_{m.id}", vm); Arriba(fila, V(0, -272 - i * 104), V(W, 104));
            var linea = Caja("Linea", fila, null, new Color(1f, 1f, 1f, 0.07f), 1f);
            Anclar(linea, V(0, 1), V(1, 1), V(0.5f, 1), Vector2.zero, V(-80, 2));
            var dib = Nuevo("Dibujo", fila); Anclar(dib, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(36, 0), V(88, 88));
            dib.gameObject.AddComponent<DibujoUI>().figura = m.id; dib.GetComponent<DibujoUI>().raycastTarget = false;
            var nom = Txt("Nombre", fila, syne, m.nombre, 38, Papel, TextAlignmentOptions.BottomLeft);
            Anclar(nom.rectTransform, V(0, 0.5f), V(0, 0.5f), V(0, 0), V(144, -2), V(560, 50)); nom.characterSpacing = -1.5f;
            string a = fichas.First(f => f.simbolo == m.simboloA).nombre.ToLower(), b = fichas.First(f => f.simbolo == m.simboloB).nombre.ToLower();
            var par = Txt("Pareja", fila, inter, $"{a} + {b}", 28, Tenue, TextAlignmentOptions.TopLeft);
            Anclar(par.rectTransform, V(0, 0.5f), V(0, 0.5f), V(0, 1), V(146, -6), V(560, 40));
            formulaT[i] = Txt("Formula", fila, syne, m.formula, 40, Tenue, TextAlignmentOptions.MidlineRight);
            Anclar(formulaT[i].rectTransform, V(1, 0), V(1, 1), V(1, 0.5f), V(-92, 0), V(260, 0));
            var check = Caja("Lograda", fila, sp, Velo, Pil(28));
            Anclar(check, V(1, 0.5f), V(1, 0.5f), V(1, 0.5f), V(-40, 0), V(28, 28)); checkF[i] = check.GetComponent<Image>();
        }

        // Vista: armar con contadores
        var va = Nuevo("VistaArmar", hoja); Estirar(va); var vaF = va.gameObject.AddComponent<FundidoUI>(); vaF.desplazamiento = 14f;
        var aTitulo = Txt("Titulo", va, syne, "Arma la molécula", 56, Papel, TextAlignmentOptions.TopLeft);
        Arriba(aTitulo.rectTransform, V(40, -40), V(W - 80, 80)); aTitulo.characterSpacing = -2f;
        var aSub = Txt("Subtitulo", va, inter, "¿Cuántos átomos de cada uno hacen falta?", 32, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(aSub.rectTransform, V(42, -116), V(W - 80, 50));
        FilaContador(va, sp, syne, inter, "FilaA", -200, W, out var puntoA, out var nomA, out var cuentaA, out var menosA, out var masA);
        FilaContador(va, sp, syne, inter, "FilaB", -340, W, out var puntoB, out var nomB, out var cuentaB, out var menosB, out var masB);
        var pista = Txt("Pista", va, inter, "", 36, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pista.rectTransform, V(40, -500), V(W - 80, 130)); pista.lineSpacing = 6f;

        // Vista: molécula formada, con una ilustración de dónde la encuentras
        var vf = Nuevo("VistaFormada", hoja); Estirar(vf); var vfF = vf.gameObject.AddComponent<FundidoUI>(); vfF.desplazamiento = 14f;
        var fDib = Nuevo("Dibujo", vf); Anclar(fDib, V(1, 1), V(1, 1), V(1, 1), V(-36, -36), V(250, 250));
        var fDibujo = fDib.gameObject.AddComponent<DibujoUI>(); fDibujo.raycastTarget = false;
        var fNombre = Txt("Nombre", vf, syne, "Agua", 64, Papel, TextAlignmentOptions.TopLeft);
        Arriba(fNombre.rectTransform, V(40, -40), V(W - 350, 90)); fNombre.enableWordWrapping = false; fNombre.characterSpacing = -2f; fNombre.lineSpacing = -12f;
        fNombre.enableAutoSizing = true; fNombre.fontSizeMin = 44; fNombre.fontSizeMax = 64;
        var fFormula = Txt("Formula", vf, syne, "H<sub>2</sub>O", 76, Papel, TextAlignmentOptions.TopLeft);
        Arriba(fFormula.rectTransform, V(40, -132), V(W - 350, 100)); fFormula.enableWordWrapping = false;
        var fReceta = Txt("Receta", vf, inter, "", 32, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(fReceta.rectTransform, V(40, -256), V(W - 350, 46));
        var fEcuacion = Txt("Ecuacion", vf, inter, "", 32, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(fEcuacion.rectTransform, V(40, -304), V(W - 80, 52));
        var fDato = Txt("Dato", vf, inter, "", 36, Papel, TextAlignmentOptions.TopLeft);
        Arriba(fDato.rectTransform, V(40, -384), V(W - 80, 230)); fDato.lineSpacing = 8f;
        fDato.enableAutoSizing = true; fDato.fontSizeMin = 28; fDato.fontSizeMax = 36;
        var fNota = Txt("Nota", vf, inter, "Separa las tarjetas para armar otra.", 28, Tenue, TextAlignmentOptions.BottomLeft);
        Anclar(fNota.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, V(40, 32), V(W - 80, 40));

        var c2 = hoja.gameObject.AddComponent<ModoCombinar>();
        c2.moleculas = mols; c2.hoja = hojaF; c2.vistaMensaje = vmF; c2.vistaArmar = vaF; c2.vistaFormada = vfF;
        c2.mensajeTitulo = mTitulo; c2.mensajeTexto = mTexto; c2.chipFondo = checkF; c2.chipTexto = formulaT;
        c2.puntoA = puntoA; c2.puntoB = puntoB; c2.nombreA = nomA; c2.nombreB = nomB; c2.cuentaA = cuentaA; c2.cuentaB = cuentaB; c2.pista = pista;
        c2.masA = masA; c2.menosA = menosA; c2.masB = masB; c2.menosB = menosB;
        c2.formadaNombre = fNombre; c2.formadaFormula = fFormula; c2.formadaReceta = fReceta; c2.formadaEcuacion = fEcuacion; c2.formadaDato = fDato;
        c2.formadaDibujo = fDibujo;
        return c2;
    }

    static void FilaContador(RectTransform padre, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter, string nombre, float y, float W,
        out Image punto, out TMP_Text nom, out TMP_Text cuenta, out Button menos, out Button mas)
    {
        var f = Nuevo(nombre, padre); Arriba(f, V(0, y), V(W, 120));
        var p = Caja("Punto", f, sp, Color.white, Pil(32));
        Anclar(p, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(40, 0), V(32, 32)); punto = p.GetComponent<Image>();
        nom = Txt("Nombre", f, syne, "Hidrógeno", 52, Papel, TextAlignmentOptions.MidlineLeft);
        Anclar(nom.rectTransform, V(0, 0), V(0, 1), V(0, 0.5f), V(96, 0), V(540, 0)); nom.characterSpacing = -2f;
        mas = BotonContador("Mas", f, sp, inter, "+", -40);
        cuenta = Txt("Cuenta", f, syne, "1", 68, Papel, TextAlignmentOptions.Center);
        Anclar(cuenta.rectTransform, V(1, 0), V(1, 1), V(1, 0.5f), V(-144, 0), V(130, 0));
        menos = BotonContador("Menos", f, sp, inter, "−", -274);
    }

    static Button BotonContador(string nombre, RectTransform padre, Sprite sp, TMP_FontAsset fuente, string glifo, float x)
    {
        var c = Caja(nombre, padre, sp, new Color(1f, 1f, 1f, 0.14f), Pil(104));
        Anclar(c, V(1, 0.5f), V(1, 0.5f), V(1, 0.5f), V(x, 0), V(104, 104));
        var t = Txt("Glifo", c, fuente, glifo, 60, Papel, TextAlignmentOptions.Center, bold: true); Estirar(t.rectTransform);
        c.gameObject.AddComponent<CanvasGroup>();
        return Boton(c);
    }

    // ───────────────────────── Reto ─────────────────────────
    static Quiz MontarReto(Transform canvas, Sprite sp, Sprite marcoSp, TMP_FontAsset syne, TMP_FontAsset inter, ElementoData[] fichas,
        Dictionary<string, Sprite> cartas, out FundidoUI pantallaF)
    {
        var pantalla = Pantalla("PantallaReto", canvas, Noche, out var s);
        pantallaF = pantalla.gameObject.AddComponent<FundidoUI>();
        const float W = 992f, X = 44f;
        var c = V(0.5f, 0.5f);
        var verde = new Color(0.21f, 0.82f, 0.50f);

        // Portada con insignias
        var vp = Nuevo("Portada", s); Estirar(vp); var vpF = vp.gameObject.AddComponent<FundidoUI>(); vpF.desplazamiento = 16f;
        var pTitulo = Txt("Titulo", vp, syne, "¿Cuánto sabes de los elementos?", 80, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pTitulo.rectTransform, V(X, -176), V(W, 200)); pTitulo.characterSpacing = -3f; pTitulo.lineSpacing = -14f;
        var pTexto = Txt("Texto", vp, inter, "Cada ronda trae 10 preguntas. Por cada elemento ganas una insignia: bronce con 1 acierto, plata con 2 y oro con 3.", 34, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(pTexto.rectTransform, V(X + 4, -400), V(W - 8, 150)); pTexto.lineSpacing = 6f;
        var puntos = Txt("Puntos", vp, syne, "Aún sin puntos", 56, OroC, TextAlignmentOptions.TopLeft);
        Arriba(puntos.rectTransform, V(X, -574), V(W, 80)); puntos.characterSpacing = -2f;
        var iFondo = new Image[fichas.Length]; var iAro = new AnilloUI[fichas.Length]; var iSim = new TMP_Text[fichas.Length];
        for (int i = 0; i < fichas.Length; i++)
        {
            var celda = Nuevo($"Insignia_{fichas[i].simbolo}", vp);
            Arriba(celda, V(X + (i % 5) * 198.4f, -690 - (i / 5) * 214), V(198.4f, 200));
            var aro = Nuevo("Aro", celda); Anclar(aro, c, c, c, Vector2.zero, V(172, 172));
            iAro[i] = aro.gameObject.AddComponent<AnilloUI>(); iAro[i].grosor = 9f; iAro[i].raycastTarget = false;
            var fondo = Caja("Fondo", celda, sp, fichas[i].color, Pil(140));
            Anclar(fondo, c, c, c, Vector2.zero, V(140, 140)); iFondo[i] = fondo.GetComponent<Image>();
            iSim[i] = Txt("Simbolo", fondo, syne, fichas[i].simbolo, 56, Papel, TextAlignmentOptions.Center); Estirar(iSim[i].rectTransform);
        }
        var bEmpezar = BotonAncho("Empezar", vp, sp, syne, "Empezar ronda", 80, OroC, out _);

        // Pregunta
        var vq = Nuevo("Pregunta", s); Estirar(vq); var vqF = vq.gameObject.AddComponent<FundidoUI>(); vqF.desplazamiento = 16f;
        var progreso = Txt("Progreso", vq, inter, "Pregunta 1 de 10", 32, Tenue, TextAlignmentOptions.TopLeft, bold: true);
        Arriba(progreso.rectTransform, V(X, -170), V(500, 44));
        var racha = Caja("Racha", vq, sp, OroC, Pil(56)); Anclar(racha, V(1, 1), V(1, 1), V(1, 0.5f), V(-X, -190), V(230, 56));
        var rachaT = Txt("Texto", racha, inter, "Racha de 2", 28, TintaOsc, TextAlignmentOptions.Center, bold: true); Estirar(rachaT.rectTransform);
        var rachaG = racha.gameObject.AddComponent<CanvasGroup>(); rachaG.alpha = 0f; rachaG.blocksRaycasts = false;
        var barraFondo = Caja("Barra", vq, sp, Velo, Pil(12)); Arriba(barraFondo, V(X, -226), V(W, 12));
        var relleno = Caja("Relleno", barraFondo, sp, OroC, Pil(12)); Estirar(relleno); relleno.anchorMax = V(0.001f, 1);

        // tarjeta misteriosa (se voltea al contestar) o átomo animado como pista
        var visual = Nuevo("Visual", vq); Anclar(visual, V(0.5f, 1), V(0.5f, 1), c, V(0, -400), V(264, 264));
        var carta = TarjetaMini("Carta", visual, sp, null, Tarjeta, 250);
        var cartaImg = carta.Find("Imagen").GetComponent<Image>();
        var tapa = Caja("Tapa", visual, sp, Tarjeta, 48f / (250 * 0.13f)); Anclar(tapa, c, c, c, Vector2.zero, V(250, 250));
        var tapaBorde = Caja("Borde", tapa, marcoSp, OroC, 48f / (250 * 0.13f)); Estirar(tapaBorde);
        var tapaT = Txt("Texto", tapa, syne, "?", 130, OroC, TextAlignmentOptions.Center); Estirar(tapaT.rectTransform); tapaT.characterSpacing = -4f;
        var atomo = Nuevo("Atomo", visual); Estirar(atomo);
        var bohr = atomo.gameObject.AddComponent<DiagramaBohr>(); bohr.punto = sp; bohr.colorNucleo = Proton; bohr.colorElectron = Electron;
        bohr.colorOrbita = new Color(1f, 1f, 1f, 0.2f); bohr.radioNucleo = 20f; bohr.tamElectron = 12f;

        var pregunta = Txt("Texto", vq, syne, "", 54, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pregunta.rectTransform, V(X, -556), V(W, 190)); pregunta.characterSpacing = -2f; pregunta.lineSpacing = -10f;
        pregunta.enableAutoSizing = true; pregunta.fontSizeMin = 38; pregunta.fontSizeMax = 54;
        var ops = new Button[4]; var opF = new Image[4]; var opT = new TMP_Text[4]; var opG = new CanvasGroup[4];
        for (int i = 0; i < 4; i++)
        {
            var o = Caja($"Opcion{i + 1}", vq, sp, Velo, 48f / 36f); Arriba(o, V(X, -770 - i * 140), V(W, 124));
            opF[i] = o.GetComponent<Image>(); opG[i] = o.gameObject.AddComponent<CanvasGroup>();
            opT[i] = Txt("Texto", o, inter, "", 42, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
            Estirar(opT[i].rectTransform); opT[i].rectTransform.offsetMin = V(44, 0); opT[i].rectTransform.offsetMax = V(-44, 0);
            ops[i] = Boton(o);
        }
        var flot = Txt("Flotante", vq, syne, "+10", 64, Papel, TextAlignmentOptions.Center);
        Arriba(flot.rectTransform, Vector2.zero, V(220, 80)); var flotG = flot.gameObject.AddComponent<CanvasGroup>(); flotG.alpha = 0f; flotG.blocksRaycasts = false;
        var retro = Nuevo("Retro", vq); Arriba(retro, V(X, -1340), V(W, 220)); var retroF = retro.gameObject.AddComponent<FundidoUI>(); retroF.desplazamiento = 12f;
        var rTitulo = Txt("Titulo", retro, syne, "¡Correcto!", 48, Papel, TextAlignmentOptions.TopLeft); Arriba(rTitulo.rectTransform, V(0, 0), V(W, 64));
        var rTexto = Txt("Texto", retro, inter, "", 32, Papel, TextAlignmentOptions.TopLeft); Arriba(rTexto.rectTransform, V(2, -70), V(W - 4, 150));
        rTexto.lineSpacing = 6f; rTexto.enableAutoSizing = true; rTexto.fontSizeMin = 24; rTexto.fontSizeMax = 32;
        var bSig = BotonAncho("Siguiente", vq, sp, syne, "Siguiente", 80, OroC, out var sigT);

        // Resultado
        var vr = Nuevo("Resultado", s); Estirar(vr); var vrF = vr.gameObject.AddComponent<FundidoUI>(); vrF.desplazamiento = 16f;
        var rNum = Txt("Numero", vr, syne, "7 de 10", 190, OroC, TextAlignmentOptions.TopLeft);
        Arriba(rNum.rectTransform, V(X, -300), V(W, 250)); rNum.characterSpacing = -6f; rNum.enableWordWrapping = false;
        var puntosR = new Image[10];
        for (int i = 0; i < 10; i++)
        {
            var p = Caja($"Punto{i + 1}", vr, sp, verde, Pil(40)); Anclar(p, V(0, 1), V(0, 1), c, V(X + 20 + i * 56, -590), V(40, 40));
            puntosR[i] = p.GetComponent<Image>();
        }
        var rMsg = Txt("Mensaje", vr, syne, "", 64, Papel, TextAlignmentOptions.TopLeft);
        Arriba(rMsg.rectTransform, V(X, -660), V(W, 170)); rMsg.characterSpacing = -2f; rMsg.lineSpacing = -10f;
        var rIns = Txt("Insignias", vr, inter, "", 38, Tenue, TextAlignmentOptions.TopLeft); Arriba(rIns.rectTransform, V(X + 4, -850), V(W, 60));
        var bOtra = BotonAncho("OtraRonda", vr, sp, syne, "Otra ronda", 230, OroC, out _);
        var bVer = BotonAncho("VerInsignias", vr, sp, syne, "Ver mis insignias", 80, Velo, out _);

        // Aviso al ganar una insignia (encima de la pregunta)
        var aviso = Caja("AvisoInsignia", s, sp, Tarjeta, Pil(92)); Anclar(aviso, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 240), V(700, 92));     // abajo, arriba del botón Siguiente
        var avisoF = aviso.gameObject.AddComponent<FundidoUI>(); avisoF.desplazamiento = 18f;
        var avisoAro = Nuevo("Aro", aviso); Anclar(avisoAro, V(0, 0.5f), V(0, 0.5f), c, V(52, 0), V(50, 50));
        var avisoAnillo = avisoAro.gameObject.AddComponent<AnilloUI>(); avisoAnillo.grosor = 8f; avisoAnillo.raycastTarget = false;
        var avisoT = Txt("Texto", aviso, inter, "Insignia de bronce: Hierro", 34, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(avisoT.rectTransform, Vector2.zero, Vector2.one, V(0, 0.5f), V(96, 0), V(-120, 0));

        var q = pantalla.gameObject.AddComponent<Quiz>();
        q.elementos = fichas;
        q.cartas = fichas.Select(f => cartas.TryGetValue(f.targetName, out var sprite) ? sprite : null).ToArray();
        q.portada = vpF; q.puntosTexto = puntos; q.insigniaFondo = iFondo; q.insigniaAro = iAro; q.insigniaSimbolo = iSim; q.empezar = bEmpezar;
        q.vistaPregunta = vqF; q.progresoTexto = progreso; q.preguntaTexto = pregunta; q.barraRelleno = relleno;
        q.opciones = ops; q.opcionFondo = opF; q.opcionTexto = opT; q.opcionGrupo = opG; q.retro = retroF; q.retroTitulo = rTitulo; q.retroTexto = rTexto;
        q.siguiente = bSig; q.siguienteTexto = sigT;
        q.visual = visual; q.carta = carta.gameObject; q.tapa = tapa.gameObject; q.cartaImagen = cartaImg; q.tapaTexto = tapaT; q.atomo = bohr;
        q.rachaRT = racha; q.rachaGrupo = rachaG; q.rachaTexto = rachaT; q.flotante = flot; q.flotanteGrupo = flotG;
        q.avisoInsignia = avisoF; q.avisoAro = avisoAnillo; q.avisoTexto = avisoT;
        q.vistaResultado = vrF; q.resultadoNumero = rNum; q.resultadoMensaje = rMsg; q.resultadoInsignias = rIns; q.puntosResultado = puntosR;
        q.otraRonda = bOtra; q.verInsignias = bVer;
        return q;
    }

    // ───────────────────────── Ayudantes ─────────────────────────
    static Vector2 V(float x, float y) => new(x, y);
    static float Pil(float alto) => 48f / (alto * 0.5f);                    // esquinas totalmente redondas para ese alto

    static Color TintaDe(Color fondo)
    {
        float lum = 0.2126f * fondo.r + 0.7152f * fondo.g + 0.0722f * fondo.b;
        return lum > 0.5f ? TintaOsc : Color.white;
    }

    static Button Boton(RectTransform rt)
    {
        var img = rt.GetComponent<Image>(); img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = img;
        rt.gameObject.AddComponent<BotonPresionable>();
        return b;
    }

    static Button Circulo(string nombre, RectTransform padre, Sprite sp, TMP_FontAsset fuente, string glifo, Vector2 ancla, Vector2 pos)
    {
        var c = Caja(nombre, padre, sp, new Color(Noche.r, Noche.g, Noche.b, 0.72f), Pil(80));
        Anclar(c, ancla, ancla, ancla, pos, V(80, 80));
        var t = Txt("Glifo", c, fuente, glifo, 40, Papel, TextAlignmentOptions.Center, bold: true); Estirar(t.rectTransform);
        return Boton(c);
    }

    /// Botón ancho anclado abajo (y = distancia al borde inferior).
    static Button BotonAncho(string nombre, RectTransform padre, Sprite sp, TMP_FontAsset fuente, string texto, float y, Color fondo, out TMP_Text txt)
    {
        var b = Caja(nombre, padre, sp, fondo, Pil(130));
        Anclar(b, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, y), V(992, 130));
        txt = Txt("Texto", b, fuente, texto, 48, fondo.a < 0.5f ? Papel : TintaDe(fondo), TextAlignmentOptions.Center);
        Estirar(txt.rectTransform); txt.characterSpacing = -1.5f;
        return Boton(b);
    }

    /// Pantalla completa con fondo (cubre la cámara) y un área segura para el contenido.
    static RectTransform Pantalla(string nombre, Transform canvas, Color fondo, out RectTransform segura)
    {
        var p = Nuevo(nombre, canvas); Estirar(p);
        var img = p.gameObject.AddComponent<Image>(); img.color = fondo; img.raycastTarget = true;
        segura = Nuevo("Segura", p); Estirar(segura); segura.gameObject.AddComponent<AreaSegura>();
        return p;
    }

    /// Una parada del recorrido de inicio: emblema animado dentro de su anillo de avance + nombre del modo.
    static Button FilaModo(RectTransform padre, Sprite sp, Sprite brillo, TMP_FontAsset syne, TMP_FontAsset inter, int i, string titulo, string desc, Color color,
        out TMP_Text progreso, out AnilloUI anillo, out CanvasGroup halo, out GameObject sigue)
    {
        var centro = V(0, 0.5f); const float cx = 164f;
        var fila = Nuevo("Modo" + titulo, padre); Anclar(fila, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 150 - i * 400), V(1080, 300));   // repartidas alrededor del centro de la pantalla
        var toque = fila.gameObject.AddComponent<Image>(); toque.color = new Color(0, 0, 0, 0);           // área de toque invisible

        var h = Caja("Luz", fila, brillo, color, 1f); Anclar(h, centro, centro, V(0.5f, 0.5f), V(cx, 0), V(400, 400));
        h.GetComponent<Image>().type = Image.Type.Simple;                 // resplandor redondo
        halo = h.gameObject.AddComponent<CanvasGroup>(); halo.alpha = 0f; halo.blocksRaycasts = false;
        var pista = Nuevo("AnilloBase", fila); Anclar(pista, centro, centro, V(0.5f, 0.5f), V(cx, 0), V(232, 232));
        var pistaA = pista.gameObject.AddComponent<AnilloUI>(); pistaA.grosor = 9f; pistaA.color = new Color(1f, 1f, 1f, 0.10f); pistaA.raycastTarget = false;
        var av = Nuevo("AnilloAvance", fila); Anclar(av, centro, centro, V(0.5f, 0.5f), V(cx, 0), V(232, 232));
        anillo = av.gameObject.AddComponent<AnilloUI>(); anillo.grosor = 9f; anillo.color = color; anillo.relleno = 0f; anillo.raycastTarget = false;

        var emblema = Nuevo("Emblema", fila); Anclar(emblema, centro, centro, V(0.5f, 0.5f), V(cx, 0), V(164, 164));
        if (i == 0)
        {
            var bohr = emblema.gameObject.AddComponent<DiagramaBohr>();
            bohr.punto = sp; bohr.capasIniciales = new[] { 2, 4 }; bohr.colorNucleo = Proton; bohr.colorElectron = Electron;
            bohr.colorOrbita = new Color(1f, 1f, 1f, 0.22f); bohr.radioNucleo = 17f; bohr.tamElectron = 13f; bohr.velocidadCapa1 = 60f;
        }
        else { var d = emblema.gameObject.AddComponent<DibujoUI>(); d.figura = i == 1 ? "molecula" : "medalla"; d.raycastTarget = false; }

        // puntitos que unen esta parada con la siguiente
        if (i < 2)
            for (int k = 0; k < 3; k++)
            {
                var p = Caja($"Camino{k}", fila, sp, new Color(1f, 1f, 1f, 0.22f), Pil(10));
                Anclar(p, centro, centro, V(0.5f, 0.5f), V(cx, -158 - k * 42), V(10, 10));
            }

        var s = Txt("Sigue", fila, inter, "Sigue aquí", 28, color, TextAlignmentOptions.BottomLeft, bold: true);
        Anclar(s.rectTransform, centro, centro, V(0, 0), V(324, 92), V(400, 40)); sigue = s.gameObject;
        var t = Txt("Titulo", fila, syne, titulo, 88, color, TextAlignmentOptions.MidlineLeft);
        Anclar(t.rectTransform, centro, centro, V(0, 0.5f), V(318, 36), V(700, 110)); t.characterSpacing = -3.5f; t.enableWordWrapping = false;
        var d2 = Txt("Descripcion", fila, inter, desc, 34, Tenue, TextAlignmentOptions.MidlineLeft);
        Anclar(d2.rectTransform, centro, centro, V(0, 0.5f), V(324, -42), V(720, 50)); d2.enableWordWrapping = false;
        progreso = Txt("Progreso", fila, inter, "", 30, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(progreso.rectTransform, centro, centro, V(0, 0.5f), V(324, -96), V(700, 44));

        var b = fila.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = toque;
        fila.gameObject.AddComponent<BotonPresionable>().escalaPresionado = 0.98f;
        return b;
    }

    // ───────────────────────── Guía animada ─────────────────────────
    /// Vuelve a armar solo la guía (sin tocar el resto de la interfaz ni las fichas de datos).
    [MenuItem("AtomAR/Actualizar guía animada")]
    public static void ReemplazarGuia()
    {
        var canvas = GameObject.Find("UI_AtomAR").transform;
        var vieja = canvas.Find("GuiaVisual"); int idx = vieja ? vieja.GetSiblingIndex() : 2;
        if (vieja) Object.DestroyImmediate(vieja.gameObject);
        var pistaVieja = canvas.Find("PistaCarta"); if (pistaVieja) Object.DestroyImmediate(pistaVieja.gameObject);
        var syne = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Syne SDF.asset");
        var inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Inter SDF.asset");
        var guia = MontarGuia(canvas, SpriteRedondeado(), SpriteMarco(), syne, inter, SpritesTarjetas());
        guia.transform.SetSiblingIndex(idx);
        canvas.Find("PistaCarta").SetSiblingIndex(idx + 1);
        var app = canvas.GetComponent<AppAtomAR>(); app.guia = guia; EditorUtility.SetDirty(app);
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena);
        Debug.Log("[AtomAR] Guía animada actualizada (ejemplo en 3D).");
    }

    static GuiaVisual MontarGuia(Transform canvas, Sprite sp, Sprite marcoSp, TMP_FontAsset syne, TMP_FontAsset inter, Dictionary<string, Sprite> cartas)
    {
        var brillo = SpriteBrillo();
        var g = Nuevo("GuiaVisual", canvas); Estirar(g);
        var gF = g.gameObject.AddComponent<FundidoUI>(); gF.entrada = 0.12f;
        var oscuro = new Color(Noche.r * 0.5f, Noche.g * 0.5f, Noche.b * 0.5f, 0.72f);
        RectTransform CrearVelo(string n, Vector2 min, Vector2 max) { var v = Caja(n, g, null, oscuro, 1f); v.anchorMin = min; v.anchorMax = max; v.pivot = V(0.5f, 0.5f); v.offsetMin = v.offsetMax = Vector2.zero; return v; }
        var vArriba = CrearVelo("VeloArriba", V(0, 0.5f), V(1, 1));
        var vAbajo = CrearVelo("VeloAbajo", V(0, 0), V(1, 0.5f));
        var vIzq = CrearVelo("VeloIzq", V(0, 0.5f), V(0.5f, 0.5f));
        var vDer = CrearVelo("VeloDer", V(0.5f, 0.5f), V(1, 0.5f));

        var c = V(0.5f, 0.5f);
        var marco = Nuevo("Marco", g); Anclar(marco, c, c, c, V(0, 110), V(560, 620));
        var esquinas = Caja("Esquinas", marco, SpriteHueco(), oscuro, 1f); Estirar(esquinas);       // redondea las esquinas del hueco

        // el ejemplo 3D se dibuja aquí; encima van el botón +1 y los dedos
        var escena = Nuevo("Escena", g); Anclar(escena, c, c, c, V(0, 110), Vector2.zero);
        var vistaRT = Nuevo("Vista3D", escena); Anclar(vistaRT, c, c, c, Vector2.zero, V(560, 620));
        var vista = vistaRT.gameObject.AddComponent<RawImage>(); vista.raycastTarget = false;
        var mas = Nuevo("Mas", vistaRT); Anclar(mas, c, c, c, Vector2.zero, V(150, 76));
        var masG = mas.gameObject.AddComponent<CanvasGroup>(); masG.alpha = 0f; masG.blocksRaycasts = false;
        var masSombra = Caja("Sombra", mas, brillo, new Color(0.01f, 0.02f, 0.06f, 0.55f), 1f); masSombra.GetComponent<Image>().type = Image.Type.Simple;
        Anclar(masSombra, c, c, c, V(4, -16), V(230, 140));
        var masFondo = Caja("Fondo", mas, sp, Electron, Pil(76)); Estirar(masFondo);
        var masT = Txt("Texto", masFondo, syne, "+1", 46, TintaOsc, TextAlignmentOptions.Center); Estirar(masT.rectTransform);
        var dedos = new[] { Dedo("Dedo1", vistaRT, sp, brillo), Dedo("Dedo2", vistaRT, sp, brillo) };
        var borde = Caja("Borde", g, marcoSp, Electron, 1f); Anclar(borde, c, c, c, V(0, 110), V(560, 620));

        var leyenda = Txt("Leyenda", g, syne, "", 50, Papel, TextAlignmentOptions.Top);
        Anclar(leyenda.rectTransform, c, c, V(0.5f, 1), V(0, -286), V(960, 130)); leyenda.characterSpacing = -2f; leyenda.lineSpacing = -10f;
        var ent = Caja("Entendido", g, marcoSp, Papel, Pil(92)); Anclar(ent, c, c, c, V(0, -414), V(330, 92));
        var entT = Txt("Texto", ent, inter, "Entendido", 34, Papel, TextAlignmentOptions.Center, bold: true); Estirar(entT.rectTransform);
        var bEnt = Boton(ent);

        // Pista de una sola vez (vive fuera del fundido de la guía)
        var pista = Caja("PistaCarta", canvas, sp, new Color(Noche.r, Noche.g, Noche.b, 0.92f), Pil(84));
        Anclar(pista, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 372), V(600, 84));
        var pistaF = pista.gameObject.AddComponent<FundidoUI>(); pistaF.desplazamiento = -16f;
        pista.GetComponent<CanvasGroup>().blocksRaycasts = false;
        var pPunto = Caja("Punto", pista, sp, Electron, Pil(20)); Anclar(pPunto, V(0, 0.5f), V(0, 0.5f), V(0.5f, 0.5f), V(48, 0), V(20, 20));
        var pT = Txt("Texto", pista, inter, "Toca la carta para ver más", 34, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(pT.rectTransform, Vector2.zero, Vector2.one, V(0, 0.5f), V(84, 0), V(-100, 0));

        var dio = g.gameObject.AddComponent<DioramaGuia>();
        dio.vista = vista; dio.dedos = dedos; dio.botonMas = mas; dio.sombra = brillo.texture;
        dio.cartaC = TarjetaNitida("03_c_carbono"); dio.cartaH = TarjetaNitida("01_h_hidrogeno"); dio.cartaO = TarjetaNitida("05_o_oxigeno");
        dio.atomoC = Modelo3D("Elementos/C_Carbono"); dio.atomoH = Modelo3D("Elementos/H_Hidrogeno"); dio.atomoO = Modelo3D("Elementos/O_Oxigeno");
        dio.moleculaAgua = Modelo3D("Moleculas/Mol_H2O_Agua");

        var guia = g.gameObject.AddComponent<GuiaVisual>();
        guia.fundido = gF; guia.veloArriba = vArriba; guia.veloAbajo = vAbajo; guia.veloIzq = vIzq; guia.veloDer = vDer;
        guia.marco = marco; guia.escena = escena; guia.marcoImagen = borde.GetComponent<Image>();
        guia.diorama = dio; guia.leyenda = leyenda; guia.entendido = bEnt; guia.pistaCarta = pistaF; guia.pistaPunto = pPunto;
        return guia;
    }

    /// Yema de dedo translúcida con sombra y onda, como los indicadores de toque.
    static RectTransform Dedo(string nombre, RectTransform padre, Sprite sp, Sprite brillo)
    {
        var c = V(0.5f, 0.5f);
        var d = Nuevo(nombre, padre); Anclar(d, c, c, c, Vector2.zero, V(92, 92));
        var g = d.gameObject.AddComponent<CanvasGroup>(); g.alpha = 0f; g.blocksRaycasts = false;
        var sombra = Caja("Sombra", d, brillo, new Color(0.01f, 0.02f, 0.06f, 0.6f), 1f); sombra.GetComponent<Image>().type = Image.Type.Simple;
        Anclar(sombra, c, c, c, V(10, -18), V(170, 170));
        var onda = Nuevo("Onda", d); Anclar(onda, c, c, c, Vector2.zero, V(96, 96));
        var anillo = onda.gameObject.AddComponent<AnilloUI>(); anillo.grosor = 5f; anillo.color = new Color(1, 1, 1, 0); anillo.raycastTarget = false;
        var yema = Caja("Yema", d, sp, new Color(1f, 1f, 1f, 0.62f), Pil(92)); Anclar(yema, c, c, c, Vector2.zero, V(92, 92));
        var borde = Nuevo("Borde", yema); Estirar(borde);
        var b = borde.gameObject.AddComponent<AnilloUI>(); b.grosor = 3f; b.color = new Color(1f, 1f, 1f, 0.95f); b.raycastTarget = false;
        var reflejo = Caja("Reflejo", yema, sp, new Color(1f, 1f, 1f, 0.95f), Pil(26)); Anclar(reflejo, c, c, c, V(-17, 19), V(26, 26));
        return d;
    }

    /// Las tarjetas de la guía se ven grandes: se importan con más resolución.
    static Texture2D TarjetaNitida(string nombre)
    {
        string p = $"{UIDir}/Tarjetas/{nombre}.jpg";
        if (AssetImporter.GetAtPath(p) is TextureImporter imp && imp.maxTextureSize < 1024) { imp.maxTextureSize = 1024; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static GameObject Modelo3D(string ruta) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/AtomAR_Modelos/{ruta}.fbx");

    // ───────────────────────── Celebración ─────────────────────────
    static Celebracion MontarCelebracion(Transform canvas, Sprite sp, Sprite brillo, TMP_FontAsset syne, TMP_FontAsset inter,
        ElementoData[] fichas, MoleculaData[] mols, Dictionary<string, Sprite> cartas)
    {
        var raiz = Pantalla("Celebracion", canvas, new Color(Noche.r * 0.75f, Noche.g * 0.75f, Noche.b * 0.75f, 1f), out var s);
        var f = raiz.gameObject.AddComponent<FundidoUI>(); f.entrada = 0.1f;
        var c = V(0.5f, 0.5f);
        var centro = Nuevo("Orbita", s); Anclar(centro, c, c, c, V(0, 360), Vector2.zero);
        var luz = Caja("Luz", centro, brillo, OroC, 1f); Anclar(luz, c, c, c, Vector2.zero, V(620, 620)); luz.GetComponent<Image>().color = new Color(OroC.r, OroC.g, OroC.b, 0.30f); luz.GetComponent<Image>().type = Image.Type.Simple;
        var aE = Nuevo("Elementos", centro); Anclar(aE, c, c, c, Vector2.zero, Vector2.zero);
        foreach (var e in fichas) { cartas.TryGetValue(e.targetName, out var carta); TarjetaMini("Carta_" + e.simbolo, aE, sp, carta, e.color, 138); }
        var aM = Nuevo("Moleculas", centro); Anclar(aM, c, c, c, Vector2.zero, Vector2.zero);
        foreach (var m in mols) { var d = Nuevo("Dibujo_" + m.id, aM); Anclar(d, c, c, c, Vector2.zero, V(200, 200)); d.gameObject.AddComponent<DibujoUI>().figura = m.id; }
        var cuenta = Txt("Cuenta", centro, syne, "10 de 10", 104, OroC, TextAlignmentOptions.Center);
        Anclar(cuenta.rectTransform, c, c, c, Vector2.zero, V(560, 150)); cuenta.characterSpacing = -4f; cuenta.enableWordWrapping = false;

        var titulo = Txt("Titulo", s, syne, "", 72, Papel, TextAlignmentOptions.Top);
        Anclar(titulo.rectTransform, c, c, V(0.5f, 1), V(0, -120), V(960, 190)); titulo.characterSpacing = -3f; titulo.lineSpacing = -14f;
        var texto = Txt("Texto", s, inter, "", 38, Tenue, TextAlignmentOptions.Top);
        Anclar(texto.rectTransform, c, c, V(0.5f, 1), V(0, -330), V(860, 150)); texto.lineSpacing = 6f;

        var halo = Caja("LuzBoton", s, brillo, Electron, 1f); Anclar(halo, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0.5f), V(0, 295), V(1100, 240));
        var haloG = halo.gameObject.AddComponent<CanvasGroup>(); haloG.blocksRaycasts = false;
        var bCont = BotonAncho("Continuar", s, sp, syne, "Continuar", 230, Electron, out var contT);
        var bQue = BotonAncho("Quedarse", s, sp, inter, "Seguir explorando", 84, new Color(1, 1, 1, 0f), out var queT); queT.fontSize = 36; queT.color = Tenue;

        var cel = raiz.gameObject.AddComponent<Celebracion>();
        cel.fundido = f; cel.anilloElementos = aE; cel.anilloMoleculas = aM; cel.halo = halo; cel.haloGrupo = haloG; cel.haloImagen = halo.GetComponent<Image>();
        cel.continuarFondo = bCont.GetComponent<Image>(); cel.cuenta = cuenta; cel.titulo = titulo; cel.texto = texto; cel.continuarTexto = contT; cel.quedarseTexto = queT;
        cel.continuar = bCont; cel.quedarse = bQue;
        return cel;
    }

    /// Miniatura de una tarjeta real (con esquinas redondeadas); si no hay imagen usa el color del elemento.
    static RectTransform TarjetaMini(string nombre, RectTransform padre, Sprite sp, Sprite carta, Color respaldo, float tam)
    {
        var c = V(0.5f, 0.5f);
        var t = Caja(nombre, padre, sp, Color.white, 48f / (tam * 0.13f)); Anclar(t, c, c, c, Vector2.zero, V(tam, tam));
        t.gameObject.AddComponent<Mask>().showMaskGraphic = true; t.gameObject.AddComponent<CanvasGroup>();
        var img = Nuevo("Imagen", t); Estirar(img);
        var im = img.gameObject.AddComponent<Image>(); im.raycastTarget = false;
        if (carta) im.sprite = carta; else im.color = respaldo;
        return t;
    }

    // ───────────────────────── Sprites generados ─────────────────────────
    /// Las imágenes de las tarjetas (targets de Vuforia), copiadas en chico para usarlas en la interfaz.
    static Dictionary<string, Sprite> SpritesTarjetas()
    {
        const string origen = "Assets/Editor/Vuforia/ImageTargetTextures/AtomAR";
        string destino = UIDir + "/Tarjetas";
        var d = new Dictionary<string, Sprite>();
        if (!Directory.Exists(origen)) return d;
        Directory.CreateDirectory(destino);
        foreach (var archivo in Directory.GetFiles(origen, "*_scaled.jpg"))
        {
            string nombre = Path.GetFileNameWithoutExtension(archivo).Replace("_scaled", "");
            string dst = $"{destino}/{nombre}.jpg";
            if (!File.Exists(dst))
            {
                File.Copy(archivo, dst);
                AssetDatabase.ImportAsset(dst);
                var imp = (TextureImporter)AssetImporter.GetAtPath(dst);
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.maxTextureSize = 256; imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(dst);
            if (s) d[nombre] = s;
        }
        return d;
    }

    /// Contorno redondeado (solo la línea), para marcos y botones sin relleno.
    static Sprite SpriteMarco()
    {
        string path = UIDir + "/Marco.png";
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existente) return existente;
        const int N = 128; const float R = 48f, G = 6f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            // distancia hacia adentro desde el borde del rectángulo redondeado
            float px = Mathf.Min(x + 0.5f, N - x - 0.5f), py = Mathf.Min(y + 0.5f, N - y - 0.5f);
            float dentro = px < R && py < R ? R - Vector2.Distance(new Vector2(px, py), new Vector2(R, R)) : Mathf.Min(px, py);
            float a = Mathf.Clamp01(dentro + 0.5f) * Mathf.Clamp01(G - dentro + 0.5f);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        return GuardarSprite(tex, path, new Vector4(R, R, R, R));
    }

    /// Lo contrario de un rectángulo redondeado: solo las cuatro esquinas de afuera (para el hueco de la guía).
    static Sprite SpriteHueco()
    {
        string path = UIDir + "/Hueco.png";
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existente) return existente;
        const int N = 128; const float R = 48f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float px = Mathf.Min(x + 0.5f, N - x - 0.5f), py = Mathf.Min(y + 0.5f, N - y - 0.5f);
            float dentro = px < R && py < R ? R - Vector2.Distance(new Vector2(px, py), new Vector2(R, R)) : Mathf.Min(px, py);
            tex.SetPixel(x, y, new Color(1, 1, 1, 1f - Mathf.Clamp01(dentro + 0.5f)));
        }
        return GuardarSprite(tex, path, new Vector4(R, R, R, R));
    }

    /// Resplandor suave para iluminar botones y emblemas.
    static Sprite SpriteBrillo()
    {
        string path = UIDir + "/Brillo.png";
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existente) return existente;
        const int N = 128; const float B = 60f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, B, N - B), cy = Mathf.Clamp(y + 0.5f, B, N - B);
            float a = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) / B);
            tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
        }
        return GuardarSprite(tex, path, new Vector4(B, B, B, B));
    }
}
