// AtomAR - Mini tutorial por pasos. Sale la primera vez que se entra a cada modo
// y se puede volver a abrir con el botón "?" de la barra superior.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public FundidoUI fundido;
    public FundidoUI contenido;
    public Image circulo, siguienteFondo;
    public TMP_Text numero, paso, titulo, cuerpo, siguienteTexto;
    public Button siguiente, saltar;

    // Los textos de ayuda de cada modo: (título, explicación)
    static readonly Dictionary<ModoApp, (string t, string c)[]> Pasos = new()
    {
        [ModoApp.Inicio] = new[]
        {
            ("Explora los átomos", "En Explorar apuntas la cámara a una tarjeta y aparece su átomo en 3D, con sus datos. Empieza por aquí."),
            ("Combina y forma moléculas", "En Combinar juntas dos tarjetas y decides cuántos átomos de cada una usar para formar agua, sal y más."),
            ("Ponte a prueba", "En Reto contestas preguntas sobre lo que viste y ganas insignias. Para este modo no necesitas las tarjetas."),
        },
        [ModoApp.Explorar] = new[]
        {
            ("Pon una tarjeta frente a la cámara", "Déjala sobre la mesa, con buena luz, a unos 30 cm del teléfono. Espera un segundo sin moverlo y el átomo aparecerá encima."),
            ("Toca la carta para saber más", "Abajo sale la carta del elemento. Tócala o arrástrala hacia arriba para ver sus protones, neutrones y electrones."),
            ("Colecciona los 10 elementos", "Cada tarjeta nueva se suma a tu colección. Si hay varias a la vista, toca el átomo que quieras ver."),
        },
        [ModoApp.Combinar] = new[]
        {
            ("Junta dos tarjetas", "Ponlas una al lado de la otra y apunta la cámara para que se vean las dos. Prueba primero con sodio y cloro."),
            ("Agrega los átomos que faltan", "Con los botones + y − decides cuántos átomos de cada elemento usar. Las pistas te dicen si falta o sobra alguno."),
            ("Mira cómo se forma", "Cuando la cantidad es la correcta, los átomos se unen y la molécula aparece entre las dos tarjetas."),
        },
        [ModoApp.Reto] = new[]
        {
            ("Responde 10 preguntas", "Cada ronda tiene 10 preguntas sobre los elementos. Toca la respuesta que creas correcta y verás por qué."),
            ("Gana insignias", "Cada elemento tiene 3 preguntas. Con 1 acierto ganas bronce, con 2 plata y con 3 oro."),
        },
    };

    (string t, string c)[] actuales;
    int indice; ModoApp modo;

    public bool Abierto => fundido.Visible;

    public static bool Visto(ModoApp m) => PlayerPrefs.GetInt("AtomAR.tutorial." + m, 0) == 1;

    public static void OlvidarVistos()
    {
        foreach (ModoApp m in System.Enum.GetValues(typeof(ModoApp))) PlayerPrefs.DeleteKey("AtomAR.tutorial." + m);
    }

    void Awake()
    {
        siguiente.onClick.AddListener(Avanzar);
        saltar.onClick.AddListener(Cerrar);
    }

    public void Mostrar(ModoApp m)
    {
        modo = m; actuales = Pasos[m]; indice = 0;
        Pintar();
        fundido.Mostrar(true);
    }

    public void Cerrar()
    {
        PlayerPrefs.SetInt("AtomAR.tutorial." + modo, 1);
        PlayerPrefs.Save();
        fundido.Mostrar(false);
    }

    void Avanzar()
    {
        if (indice < actuales.Length - 1) { indice++; Pintar(); }
        else Cerrar();
    }

    void Pintar()
    {
        bool ultimo = indice == actuales.Length - 1;
        Color c = AppAtomAR.ColorModo(modo), tinta = AppAtomAR.Tinta(c);
        circulo.color = c; numero.color = tinta; numero.text = (indice + 1).ToString();
        siguienteFondo.color = c; siguienteTexto.color = tinta;
        siguienteTexto.text = ultimo ? "¡Listo!" : "Siguiente";
        paso.text = $"Paso {indice + 1} de {actuales.Length}";
        titulo.text = actuales[indice].t;
        cuerpo.text = actuales[indice].c;
        saltar.gameObject.SetActive(!ultimo);
        // el texto del paso entra con un fundido corto
        contenido.Mostrar(false, true);
        contenido.Mostrar(true);
    }
}
