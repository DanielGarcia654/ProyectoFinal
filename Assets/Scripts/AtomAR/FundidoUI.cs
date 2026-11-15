// AtomAR - Muestra u oculta un bloque de interfaz con un fundido suave (y, si se quiere, un pequeño desplazamiento).
// La salida es más rápida que la entrada y siempre parte del valor actual, así que se puede interrumpir.
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FundidoUI : MonoBehaviour
{
    public bool visibleAlInicio;
    public float entrada = 0.07f, salida = 0.04f;
    [Tooltip("Píxeles que sube al aparecer (0 = solo fundido)")]
    public float desplazamiento;

    CanvasGroup grupo; RectTransform rt; Vector2 basePos;
    float alfa, vel; bool visible, listo;

    public bool Visible => visible;

    void Preparar()
    {
        if (listo) return;
        listo = true;
        grupo = GetComponent<CanvasGroup>(); rt = (RectTransform)transform; basePos = rt.anchoredPosition;
        visible = visibleAlInicio; alfa = visible ? 1f : 0f;
        Aplicar();
    }

    void Awake() => Preparar();

    public void Mostrar(bool si, bool inmediato = false)
    {
        Preparar();
        visible = si;
        if (inmediato) { alfa = si ? 1f : 0f; vel = 0f; }
        Aplicar();
    }

    void Update()
    {
        alfa = Mathf.SmoothDamp(alfa, visible ? 1f : 0f, ref vel, visible ? entrada : salida, Mathf.Infinity, Time.unscaledDeltaTime);
        Aplicar();
    }

    void Aplicar()
    {
        grupo.alpha = alfa;
        grupo.blocksRaycasts = visible;
        grupo.interactable = visible;
        if (desplazamiento != 0f) rt.anchoredPosition = basePos + new Vector2(0f, -desplazamiento * (1f - alfa));
    }
}
