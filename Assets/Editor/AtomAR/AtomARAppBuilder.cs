// AtomAR - Arma la app completa sobre la interfaz del Modo Elemento:
// pantalla de inicio, barra superior con ayuda, panel del modo Combinar, Reto (quiz) y tutorial.
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

        var sp = SpriteRedondeado();
        var syne = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Syne SDF.asset");
        var inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIDir + "/Fuentes/Inter SDF.asset");
        var canvasGO = GameObject.Find("UI_AtomAR");
        var canvas = canvasGO.transform;
        var segura = (RectTransform)canvas.Find("AreaSegura");
        var marca = segura.Find("Marca"); if (marca) Object.DestroyImmediate(marca.gameObject);
        var coleccion = (RectTransform)segura.Find("Coleccion");

        var combinar = MontarCombinar(segura, sp, syne, inter, mols);
        var quiz = MontarReto(canvas, sp, syne, inter, fichas, out var pantallaReto);

        // ── Barra superior (modos): atrás, título, ayuda y avance
        var barra = Nuevo("BarraSuperior", canvas); Estirar(barra); barra.gameObject.AddComponent<AreaSegura>();
        var barraF = barra.gameObject.AddComponent<FundidoUI>();
        var atras = Circulo("Atras", barra, sp, inter, "←", V(0, 1), V(40, -36));
        var tituloBarra = Txt("Titulo", barra, syne, "Explorar", 50, Papel, TextAlignmentOptions.MidlineLeft);
        Anclar(tituloBarra.rectTransform, V(0, 1), V(0, 1), V(0, 1), V(144, -36), V(460, 80)); tituloBarra.characterSpacing = -2f;
        var ayuda = Circulo("Ayuda", barra, sp, inter, "?", V(1, 1), V(-292, -36));
        coleccion.SetParent(barra, false);
        var pastillaMol = Caja("Moleculas", barra, sp, new Color(Noche.r, Noche.g, Noche.b, 0.72f), 1.2f);
        Anclar(pastillaMol, V(1, 1), V(1, 1), V(1, 1), V(-40, -36), V(232, 80));
        var puntoMol = Caja("Punto", pastillaMol, sp, Proton, 48f / 10f);
        Anclar(puntoMol, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(28, 0), V(20, 20));
        var molTxt = Txt("Texto", pastillaMol, inter, "0 de 4", 32, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
        Anclar(molTxt.rectTransform, Vector2.zero, Vector2.one, V(0, 0.5f), V(64, 0), V(-72, 0));
        combinar.moleculasTexto = molTxt;

        // ── Pantalla de inicio
        var inicio = Pantalla("PantallaInicio", canvas, Noche, out var ini);
        var inicioF = inicio.gameObject.AddComponent<FundidoUI>(); inicioF.visibleAlInicio = true;
        var logo = Txt("Marca", ini, syne, "AtomAR", 136, Papel, TextAlignmentOptions.TopLeft);
        Arriba(logo.rectTransform, V(44, -120), V(960, 170)); logo.characterSpacing = -5f;
        var lema = Txt("Lema", ini, inter, "Descubre de qué está hecho todo lo que te rodea.", 40, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(lema.rectTransform, V(50, -296), V(900, 120)); lema.lineSpacing = 6f;

        var bExplorar = TarjetaModo(ini, sp, syne, inter, 0, "Explorar", "Apunta a una tarjeta y conoce su átomo.", "Fe", Electron, out var pExplorar, out var tExplorar);
        var bCombinar = TarjetaModo(ini, sp, syne, inter, 1, "Combinar", "Junta dos tarjetas y forma una molécula.", "H<sub>2</sub>O", Proton, out var pCombinar, out _);
        var bReto = TarjetaModo(ini, sp, syne, inter, 2, "Reto", "Responde preguntas y gana insignias.", "?", OroC, out var pReto, out _);
        var chip = Caja("EmpiezaAqui", tExplorar, sp, new Color(TintaOsc.r, TintaOsc.g, TintaOsc.b, 0.9f), Pil(60));
        Anclar(chip, V(1, 1), V(1, 1), V(1, 1), V(-28, -28), V(250, 60));
        var chipT = Txt("Texto", chip, inter, "Empieza aquí", 28, Electron, TextAlignmentOptions.Center, bold: true); Estirar(chipT.rectTransform);

        var ayudaIni = Caja("ComoSeUsa", ini, sp, Velo, Pil(92));
        Anclar(ayudaIni, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 56), V(420, 92));
        var ayudaIniT = Txt("Texto", ayudaIni, inter, "¿Cómo se usa?", 34, Papel, TextAlignmentOptions.Center, bold: true); Estirar(ayudaIniT.rectTransform);
        var bAyudaIni = Boton(ayudaIni);

        // ── Tutorial (encima de todo)
        var tutorial = MontarTutorial(canvas, sp, syne, inter);

        // ── Controlador de la app
        var app = canvasGO.AddComponent<AppAtomAR>();
        app.pantallaInicio = inicioF; app.barra = barraF; app.pantallaReto = pantallaReto; app.tutorial = tutorial; app.quiz = quiz;
        app.botonExplorar = bExplorar; app.botonCombinar = bCombinar; app.botonReto = bReto; app.botonAyudaInicio = bAyudaIni;
        app.progresoExplorar = pExplorar; app.progresoCombinar = pCombinar; app.progresoReto = pReto; app.chipEmpieza = chip.gameObject;
        app.botonAtras = atras; app.botonAyuda = ayuda; app.tituloBarra = tituloBarra;
        app.pastillaColeccion = coleccion.gameObject; app.pastillaMoleculas = pastillaMol.gameObject;

        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[AtomAR] App montada: inicio, Explorar, Combinar, Reto y tutorial.");
    }

    [MenuItem("AtomAR/Reiniciar progreso completo (pruebas)")]
    public static void ReiniciarTodo()
    {
        Coleccion.Reiniciar(); ProgresoQuiz.Reiniciar(); Tutorial.OlvidarVistos(); PlayerPrefs.Save();
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
    static ModoCombinar MontarCombinar(RectTransform segura, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter, MoleculaData[] mols)
    {
        const float W = 1032f, H = 600f;
        var hoja = Caja("PanelCombinar", segura, sp, new Color(Noche.r, Noche.g, Noche.b, 0.95f), 1f);
        Anclar(hoja, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 24), V(W, H));
        hoja.GetComponent<Image>().raycastTarget = true;
        var hojaF = hoja.gameObject.AddComponent<FundidoUI>(); hojaF.desplazamiento = 60f;

        // Vista: mensajes de guía + avance de las 4 moléculas
        var vm = Nuevo("VistaMensaje", hoja); Estirar(vm); var vmF = vm.gameObject.AddComponent<FundidoUI>(); vmF.desplazamiento = 14f;
        var mTitulo = Txt("Titulo", vm, syne, "Junta dos tarjetas", 62, Papel, TextAlignmentOptions.TopLeft);
        Arriba(mTitulo.rectTransform, V(40, -44), V(W - 80, 160)); mTitulo.characterSpacing = -2f;
        mTitulo.enableAutoSizing = true; mTitulo.fontSizeMin = 42; mTitulo.fontSizeMax = 62;
        var mTexto = Txt("Texto", vm, inter, "", 36, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(mTexto.rectTransform, V(40, -214), V(W - 80, 190)); mTexto.lineSpacing = 6f;
        var chipF = new Image[4]; var chipT = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var c = Caja($"Molecula{i + 1}", vm, sp, Velo, Pil(92));
            Anclar(c, Vector2.zero, Vector2.zero, Vector2.zero, V(40 + i * 242, 44), V(226, 92));
            chipF[i] = c.GetComponent<Image>();
            chipT[i] = Txt("Formula", c, syne, mols[i].formula, 40, Tenue, TextAlignmentOptions.Center); Estirar(chipT[i].rectTransform);
        }

        // Vista: armar con contadores
        var va = Nuevo("VistaArmar", hoja); Estirar(va); var vaF = va.gameObject.AddComponent<FundidoUI>(); vaF.desplazamiento = 14f;
        var aTitulo = Txt("Titulo", va, syne, "Arma la molécula", 56, Papel, TextAlignmentOptions.TopLeft);
        Arriba(aTitulo.rectTransform, V(40, -40), V(W - 80, 80)); aTitulo.characterSpacing = -2f;
        FilaContador(va, sp, syne, inter, "FilaA", -148, W, out var puntoA, out var nomA, out var cuentaA, out var menosA, out var masA);
        FilaContador(va, sp, syne, inter, "FilaB", -288, W, out var puntoB, out var nomB, out var cuentaB, out var menosB, out var masB);
        var pista = Txt("Pista", va, inter, "", 36, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pista.rectTransform, V(40, -440), V(W - 80, 130)); pista.lineSpacing = 6f;

        // Vista: molécula formada
        var vf = Nuevo("VistaFormada", hoja); Estirar(vf); var vfF = vf.gameObject.AddComponent<FundidoUI>(); vfF.desplazamiento = 14f;
        var fNombre = Txt("Nombre", vf, syne, "Agua", 64, Papel, TextAlignmentOptions.TopLeft);
        Arriba(fNombre.rectTransform, V(40, -40), V(600, 90)); fNombre.characterSpacing = -2f;
        fNombre.enableAutoSizing = true; fNombre.fontSizeMin = 40; fNombre.fontSizeMax = 64; fNombre.enableWordWrapping = false;
        var fFormula = Txt("Formula", vf, syne, "H<sub>2</sub>O", 96, Papel, TextAlignmentOptions.TopRight);
        Anclar(fFormula.rectTransform, V(1, 1), V(1, 1), V(1, 1), V(-40, -28), V(380, 130)); fFormula.enableWordWrapping = false;
        var fReceta = Txt("Receta", vf, inter, "", 34, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(fReceta.rectTransform, V(40, -150), V(W - 80, 50));
        var fEcuacion = Txt("Ecuacion", vf, inter, "", 34, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(fEcuacion.rectTransform, V(40, -206), V(W - 80, 56));
        var fDato = Txt("Dato", vf, inter, "", 36, Papel, TextAlignmentOptions.TopLeft);
        Arriba(fDato.rectTransform, V(40, -290), V(W - 80, 210)); fDato.lineSpacing = 8f;
        var fNota = Txt("Nota", vf, inter, "Separa las tarjetas para armar otra.", 30, Tenue, TextAlignmentOptions.BottomLeft);
        Anclar(fNota.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, V(40, 36), V(W - 80, 44));

        var c2 = hoja.gameObject.AddComponent<ModoCombinar>();
        c2.moleculas = mols; c2.hoja = hojaF; c2.vistaMensaje = vmF; c2.vistaArmar = vaF; c2.vistaFormada = vfF;
        c2.mensajeTitulo = mTitulo; c2.mensajeTexto = mTexto; c2.chipFondo = chipF; c2.chipTexto = chipT;
        c2.puntoA = puntoA; c2.puntoB = puntoB; c2.nombreA = nomA; c2.nombreB = nomB; c2.cuentaA = cuentaA; c2.cuentaB = cuentaB; c2.pista = pista;
        c2.masA = masA; c2.menosA = menosA; c2.masB = masB; c2.menosB = menosB;
        c2.formadaNombre = fNombre; c2.formadaFormula = fFormula; c2.formadaReceta = fReceta; c2.formadaEcuacion = fEcuacion; c2.formadaDato = fDato;
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
    static Quiz MontarReto(Transform canvas, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter, ElementoData[] fichas, out FundidoUI pantallaF)
    {
        var pantalla = Pantalla("PantallaReto", canvas, Noche, out var s);
        pantallaF = pantalla.gameObject.AddComponent<FundidoUI>();
        const float W = 992f, X = 44f;

        // Portada con insignias
        var vp = Nuevo("Portada", s); Estirar(vp); var vpF = vp.gameObject.AddComponent<FundidoUI>(); vpF.desplazamiento = 16f;
        var pTitulo = Txt("Titulo", vp, syne, "¿Cuánto sabes de los elementos?", 80, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pTitulo.rectTransform, V(X, -176), V(W, 200)); pTitulo.characterSpacing = -3f; pTitulo.lineSpacing = -14f;
        var pTexto = Txt("Texto", vp, inter, "Cada ronda trae 10 preguntas. Por cada elemento ganas una insignia: bronce con 1 acierto, plata con 2 y oro con 3.", 34, Tenue, TextAlignmentOptions.TopLeft);
        Arriba(pTexto.rectTransform, V(X + 4, -400), V(W - 8, 150)); pTexto.lineSpacing = 6f;
        var puntos = Txt("Puntos", vp, syne, "0 puntos", 56, OroC, TextAlignmentOptions.TopLeft);
        Arriba(puntos.rectTransform, V(X, -574), V(W, 80)); puntos.characterSpacing = -2f;
        var iFondo = new Image[fichas.Length]; var iAro = new AnilloUI[fichas.Length]; var iSim = new TMP_Text[fichas.Length];
        for (int i = 0; i < fichas.Length; i++)
        {
            var celda = Nuevo($"Insignia_{fichas[i].simbolo}", vp);
            Arriba(celda, V(X + (i % 5) * 198.4f, -690 - (i / 5) * 214), V(198.4f, 200));
            var aro = Nuevo("Aro", celda); Anclar(aro, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), Vector2.zero, V(172, 172));
            iAro[i] = aro.gameObject.AddComponent<AnilloUI>(); iAro[i].grosor = 9f; iAro[i].raycastTarget = false;
            var fondo = Caja("Fondo", celda, sp, fichas[i].color, Pil(140));
            Anclar(fondo, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), Vector2.zero, V(140, 140)); iFondo[i] = fondo.GetComponent<Image>();
            iSim[i] = Txt("Simbolo", fondo, syne, fichas[i].simbolo, 56, Papel, TextAlignmentOptions.Center); Estirar(iSim[i].rectTransform);
        }
        var bEmpezar = BotonAncho("Empezar", vp, sp, syne, "Empezar ronda", 80, OroC, out _);

        // Pregunta
        var vq = Nuevo("Pregunta", s); Estirar(vq); var vqF = vq.gameObject.AddComponent<FundidoUI>(); vqF.desplazamiento = 16f;
        var progreso = Txt("Progreso", vq, inter, "Pregunta 1 de 10", 32, Tenue, TextAlignmentOptions.TopLeft, bold: true);
        Arriba(progreso.rectTransform, V(X, -170), V(W, 44));
        var barraFondo = Caja("Barra", vq, sp, Velo, Pil(12)); Arriba(barraFondo, V(X, -226), V(W, 12));
        var relleno = Caja("Relleno", barraFondo, sp, OroC, Pil(12)); Estirar(relleno); relleno.anchorMax = V(0.001f, 1);
        var pregunta = Txt("Texto", vq, syne, "", 60, Papel, TextAlignmentOptions.TopLeft);
        Arriba(pregunta.rectTransform, V(X, -280), V(W, 310)); pregunta.characterSpacing = -2f; pregunta.lineSpacing = -10f;
        pregunta.enableAutoSizing = true; pregunta.fontSizeMin = 42; pregunta.fontSizeMax = 60;
        var ops = new Button[4]; var opF = new Image[4]; var opT = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var o = Caja($"Opcion{i + 1}", vq, sp, Velo, 48f / 36f); Arriba(o, V(X, -630 - i * 150), V(W, 130));
            opF[i] = o.GetComponent<Image>();
            opT[i] = Txt("Texto", o, inter, "", 42, Papel, TextAlignmentOptions.MidlineLeft, bold: true);
            Estirar(opT[i].rectTransform); opT[i].rectTransform.offsetMin = V(44, 0); opT[i].rectTransform.offsetMax = V(-44, 0);
            ops[i] = Boton(o);
        }
        var retro = Nuevo("Retro", vq); Arriba(retro, V(X, -1250), V(W, 250)); var retroF = retro.gameObject.AddComponent<FundidoUI>(); retroF.desplazamiento = 12f;
        var rTitulo = Txt("Titulo", retro, syne, "¡Correcto!", 48, Papel, TextAlignmentOptions.TopLeft); Arriba(rTitulo.rectTransform, V(0, 0), V(W, 64));
        var rTexto = Txt("Texto", retro, inter, "", 32, Papel, TextAlignmentOptions.TopLeft); Arriba(rTexto.rectTransform, V(2, -70), V(W - 4, 180));
        rTexto.lineSpacing = 6f; rTexto.enableAutoSizing = true; rTexto.fontSizeMin = 26; rTexto.fontSizeMax = 32;
        var bSig = BotonAncho("Siguiente", vq, sp, syne, "Siguiente", 80, OroC, out var sigT);

        // Resultado
        var vr = Nuevo("Resultado", s); Estirar(vr); var vrF = vr.gameObject.AddComponent<FundidoUI>(); vrF.desplazamiento = 16f;
        var rNum = Txt("Numero", vr, syne, "7 de 10", 190, OroC, TextAlignmentOptions.TopLeft);
        Arriba(rNum.rectTransform, V(X, -300), V(W, 250)); rNum.characterSpacing = -6f; rNum.enableWordWrapping = false;
        var rMsg = Txt("Mensaje", vr, syne, "", 64, Papel, TextAlignmentOptions.TopLeft);
        Arriba(rMsg.rectTransform, V(X, -580), V(W, 170)); rMsg.characterSpacing = -2f; rMsg.lineSpacing = -10f;
        var rIns = Txt("Insignias", vr, inter, "", 38, Tenue, TextAlignmentOptions.TopLeft); Arriba(rIns.rectTransform, V(X + 4, -770), V(W, 60));
        var bOtra = BotonAncho("OtraRonda", vr, sp, syne, "Otra ronda", 230, OroC, out _);
        var bVer = BotonAncho("VerInsignias", vr, sp, syne, "Ver mis insignias", 80, Velo, out _);

        var q = pantalla.gameObject.AddComponent<Quiz>();
        q.elementos = fichas;
        q.portada = vpF; q.puntosTexto = puntos; q.insigniaFondo = iFondo; q.insigniaAro = iAro; q.insigniaSimbolo = iSim; q.empezar = bEmpezar;
        q.vistaPregunta = vqF; q.progresoTexto = progreso; q.preguntaTexto = pregunta; q.barraRelleno = relleno;
        q.opciones = ops; q.opcionFondo = opF; q.opcionTexto = opT; q.retro = retroF; q.retroTitulo = rTitulo; q.retroTexto = rTexto;
        q.siguiente = bSig; q.siguienteTexto = sigT;
        q.vistaResultado = vrF; q.resultadoNumero = rNum; q.resultadoMensaje = rMsg; q.resultadoInsignias = rIns; q.otraRonda = bOtra; q.verInsignias = bVer;
        return q;
    }

    // ───────────────────────── Tutorial ─────────────────────────
    static Tutorial MontarTutorial(Transform canvas, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter)
    {
        var raiz = Pantalla("Tutorial", canvas, new Color(Noche.r * 0.6f, Noche.g * 0.6f, Noche.b * 0.6f, 0.86f), out var s);
        var raizF = raiz.gameObject.AddComponent<FundidoUI>();
        var tarjeta = Caja("Tarjeta", s, sp, Tarjeta, 1f);
        Anclar(tarjeta, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), Vector2.zero, V(960, 1000));

        var cont = Nuevo("Contenido", tarjeta); Estirar(cont); var contF = cont.gameObject.AddComponent<FundidoUI>(); contF.desplazamiento = 14f; contF.visibleAlInicio = true;
        var circulo = Caja("Circulo", cont, sp, Electron, Pil(150)); Arriba(circulo, V(56, -56), V(150, 150));
        var numero = Txt("Numero", circulo, syne, "1", 88, TintaOsc, TextAlignmentOptions.Center); Estirar(numero.rectTransform);
        var paso = Txt("Paso", cont, inter, "Paso 1 de 3", 32, Tenue, TextAlignmentOptions.MidlineRight, bold: true);
        Anclar(paso.rectTransform, V(1, 1), V(1, 1), V(1, 1), V(-56, -100), V(400, 60));
        var titulo = Txt("Titulo", cont, syne, "", 64, Papel, TextAlignmentOptions.TopLeft);
        Arriba(titulo.rectTransform, V(56, -256), V(848, 220)); titulo.characterSpacing = -2.5f; titulo.lineSpacing = -12f;
        titulo.enableAutoSizing = true; titulo.fontSizeMin = 46; titulo.fontSizeMax = 64;
        var cuerpo = Txt("Cuerpo", cont, inter, "", 38, Papel, TextAlignmentOptions.TopLeft);
        Arriba(cuerpo.rectTransform, V(58, -496), V(844, 320)); cuerpo.lineSpacing = 10f;

        var saltar = Caja("Saltar", tarjeta, sp, new Color(1, 1, 1, 0f), Pil(110));
        Anclar(saltar, Vector2.zero, Vector2.zero, Vector2.zero, V(40, 48), V(220, 110));
        var saltarT = Txt("Texto", saltar, inter, "Saltar", 36, Tenue, TextAlignmentOptions.Center, bold: true); Estirar(saltarT.rectTransform);
        var bSaltar = Boton(saltar);
        var sig = Caja("Siguiente", tarjeta, sp, Electron, Pil(110));
        Anclar(sig, V(1, 0), V(1, 0), V(1, 0), V(-56, 48), V(400, 110));
        var sigT = Txt("Texto", sig, syne, "Siguiente", 44, TintaOsc, TextAlignmentOptions.Center); Estirar(sigT.rectTransform); sigT.characterSpacing = -1.5f;
        var bSig = Boton(sig);

        var t = raiz.gameObject.AddComponent<Tutorial>();
        t.fundido = raizF; t.contenido = contF; t.circulo = circulo.GetComponent<Image>(); t.siguienteFondo = sig.GetComponent<Image>();
        t.numero = numero; t.paso = paso; t.titulo = titulo; t.cuerpo = cuerpo; t.siguienteTexto = sigT; t.siguiente = bSig; t.saltar = bSaltar;
        return t;
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

    static Button TarjetaModo(RectTransform padre, Sprite sp, TMP_FontAsset syne, TMP_FontAsset inter, int i, string titulo, string desc, string fantasma,
        Color color, out TMP_Text progreso, out RectTransform tarjeta)
    {
        Color tinta = TintaDe(color);
        tarjeta = Caja("Modo" + titulo, padre, sp, color, 48f / 44f);
        Arriba(tarjeta, V(44, -470 - i * 358), V(992, 330));
        tarjeta.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var f = Txt("Fantasma", tarjeta, syne, fantasma, 300, new Color(tinta.r, tinta.g, tinta.b, 0.13f), TextAlignmentOptions.MidlineRight);
        Anclar(f.rectTransform, V(1, 0.5f), V(1, 0.5f), V(1, 0.5f), V(36, -44), V(800, 400)); f.enableWordWrapping = false; f.characterSpacing = -6f;
        var t = Txt("Titulo", tarjeta, syne, titulo, 80, tinta, TextAlignmentOptions.TopLeft);
        Arriba(t.rectTransform, V(40, -34), V(700, 100)); t.characterSpacing = -3f;
        var d = Txt("Descripcion", tarjeta, inter, desc, 34, new Color(tinta.r, tinta.g, tinta.b, 0.82f), TextAlignmentOptions.TopLeft);
        Arriba(d.rectTransform, V(43, -142), V(560, 100)); d.lineSpacing = 4f;
        progreso = Txt("Progreso", tarjeta, inter, "", 32, tinta, TextAlignmentOptions.BottomLeft, bold: true);
        Anclar(progreso.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, V(43, 34), V(700, 50));
        return Boton(tarjeta);
    }
}
