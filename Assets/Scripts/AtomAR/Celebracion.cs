// AtomAR - Pantalla de logro al completar un modo: las 10 tarjetas (o las 4 moléculas) giran en órbita
// y un botón iluminado invita a continuar con el siguiente modo. Nunca obliga: también se puede seguir donde se estaba.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Celebracion : MonoBehaviour
{
    public FundidoUI fundido;
    public RectTransform anilloElementos, anilloMoleculas, halo;
    public CanvasGroup haloGrupo;
    public Image haloImagen, continuarFondo;
    public TMP_Text cuenta, titulo, texto, continuarTexto, quedarseTexto;
    public Button continuar, quedarse;
    public float radio = 330f;

    RectTransform anillo; ModoApp destino; float t;

    public bool Abierta => fundido.Visible;

    void Awake()
    {
        continuar.onClick.AddListener(() => { Cerrar(); AppAtomAR.Ir(destino); });
        quedarse.onClick.AddListener(Cerrar);
    }

    public void Mostrar(bool elementos)
    {
        destino = elementos ? ModoApp.Combinar : ModoApp.Reto;
        anilloElementos.gameObject.SetActive(elementos); anilloMoleculas.gameObject.SetActive(!elementos);
        anillo = elementos ? anilloElementos : anilloMoleculas;
        Color c = AppAtomAR.ColorModo(destino);
        cuenta.text = elementos ? "10 de 10" : "4 de 4";
        titulo.text = elementos ? "¡Descubriste todos los elementos!" : "¡Formaste todas las moléculas!";
        texto.text = elementos ? "Ya conoces los 10. Ahora júntalos para formar moléculas."
                               : "Agua, sal, dióxido de carbono y óxido de hierro. Ahora pon a prueba lo que sabes.";
        continuarTexto.text = elementos ? "Continuar a Combinar" : "Continuar al Reto";
        quedarseTexto.text = elementos ? "Seguir explorando" : "Seguir combinando";
        continuarFondo.color = c; continuarTexto.color = AppAtomAR.Tinta(c); haloImagen.color = c;
        t = 0f;
        fundido.Mostrar(true);
        Update();
    }

    public void Cerrar() => fundido.Mostrar(false);

    void Update()
    {
        if (!anillo || !fundido.Visible) return;
        t += Time.unscaledDeltaTime;
        int n = anillo.childCount;
        for (int i = 0; i < n; i++)
        {
            var hijo = (RectTransform)anillo.GetChild(i);
            float a = (90f - i * 360f / n + t * 7f) * Mathf.Deg2Rad;            // giran despacio alrededor del centro
            hijo.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio;
            float k = Mathf.Clamp01((t - 0.12f - i * 0.06f) / 0.38f);           // entran una tras otra
            hijo.gameObject.SetActive(k > 0f);
            const float c = 1.70158f; float y = k - 1f;
            hijo.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, 1f + (c + 1f) * y * y * y + c * y * y);
        }
        float pulso = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);                        // el botón de continuar "respira" con luz
        halo.localScale = Vector3.one * (1f + 0.05f * pulso);
        haloGrupo.alpha = 0.35f + 0.4f * pulso;
    }
}
