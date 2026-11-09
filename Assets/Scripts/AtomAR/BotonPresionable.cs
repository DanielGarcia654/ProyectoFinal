// AtomAR - Respuesta táctil inmediata: el botón se encoge un poco al presionarlo (no al soltarlo).
using UnityEngine;
using UnityEngine.EventSystems;

public class BotonPresionable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Range(0.9f, 1f)] public float escalaPresionado = 0.96f;
    public float respuesta = 0.05f;

    float objetivo = 1f, velocidad;

    public void OnPointerDown(PointerEventData _) => objetivo = escalaPresionado;
    public void OnPointerUp(PointerEventData _) => objetivo = 1f;
    public void OnPointerExit(PointerEventData _) => objetivo = 1f;

    void OnDisable() { objetivo = 1f; transform.localScale = Vector3.one; }

    void Update()
    {
        float s = Mathf.SmoothDamp(transform.localScale.x, objetivo, ref velocidad, respuesta, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.localScale = new Vector3(s, s, 1f);
    }
}
