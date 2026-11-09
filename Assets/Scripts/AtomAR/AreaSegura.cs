// AtomAR - Ajusta este RectTransform al área segura de la pantalla (muesca, barra de gestos).
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AreaSegura : MonoBehaviour
{
    RectTransform rt;
    Rect ultima;
    Vector2Int pantalla;

    void Awake() { rt = GetComponent<RectTransform>(); Aplicar(); }
    void Update() { if (Screen.safeArea != ultima || pantalla.x != Screen.width || pantalla.y != Screen.height) Aplicar(); }

    void Aplicar()
    {
        ultima = Screen.safeArea; pantalla = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;
        var min = ultima.position; var max = ultima.position + ultima.size;
        min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
