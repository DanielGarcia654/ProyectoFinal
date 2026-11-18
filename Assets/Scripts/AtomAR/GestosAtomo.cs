// AtomAR - Gestos sobre el objeto 3D que se está mirando.
//   Explorar → el átomo del elemento elegido en la carta.
//   Combinar → la molécula ya formada.
// • Un dedo: gira como una bola, siempre respecto a la pantalla. Al soltar sigue girando un poco por inercia.
// • Dos dedos: pellizcar para acercar o alejar (entre 0.6x y 2.5x). El átomo sigue apoyado sobre su tarjeta.
// • Doble toque: vuelve suavemente a como estaba.
// • En el editor: arrastrar con el mouse gira, la rueda acerca y el doble clic regresa.
// Los toques que empiezan sobre la interfaz (carta, botones) no mueven nada.
// Solo hay un objetivo a la vez: si cambia (otro elemento, la molécula se deshace, se sale del modo), el anterior
// regresa a su pose original y la inercia se reinicia, así nunca quedan dos objetos "agarrados".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using FaseToque = UnityEngine.InputSystem.TouchPhase;

public class GestosAtomo : MonoBehaviour
{
    [Tooltip("Grados que gira al deslizar el dedo todo el ancho de la pantalla")]
    public float gradosPorPantalla = 300f;
    public float zoomMin = 0.6f, zoomMax = 2.5f;
    [Tooltip("Qué tan rápido se frena el giro al soltar (mayor = se frena antes)")]
    public float friccion = 4f;

    /// Ya se hizo al menos un gesto (la guía deja de mostrar la pista).
    public static bool Usado { get; private set; }

    // Objetivo actual y su pose original
    Transform objetivo;
    Quaternion baseRot = Quaternion.identity; Vector3 baseEsc = Vector3.one, basePos;
    float zoom = 1f; bool regresando;

    readonly HashSet<int> dedos = new();
    readonly List<TouchControl> activos = new();
    readonly List<RaycastResult> golpes = new();
    PointerEventData evento;
    Vector2 vGiro;                       // grados por segundo
    float distPrevia = -1f, ultimoToque = -1f;
    Vector2 posUltimoToque;
    bool arrastreMouse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciar() => Usado = false;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        var nuevo = BuscarObjetivo(out var tarjeta);
        if (nuevo != objetivo) CambiarObjetivo(nuevo, tarjeta);

        var cam = Camera.main;
        if (!objetivo || !cam) { dedos.Clear(); vGiro = Vector2.zero; distPrevia = -1f; arrastreMouse = false; return; }

        Vector2 giro = Vector2.zero; float factorZoom = 1f; bool tocando = false, reset = false;
        LeerToques(ref giro, ref factorZoom, ref tocando, ref reset);
        LeerMouse(ref giro, ref factorZoom, ref tocando, ref reset);

        if (reset) { regresando = true; vGiro = Vector2.zero; }
        else if (giro != Vector2.zero || factorZoom != 1f) { regresando = false; Usado = true; }

        // Giro: mientras se toca sigue al dedo; al soltar continúa con la velocidad que llevaba y se frena
        Vector2 grados = giro * (gradosPorPantalla / Mathf.Max(1, Screen.width));
        if (tocando) { if (dt > 0f) vGiro = Vector2.Lerp(vGiro, grados / dt, 0.4f); }
        else { grados = vGiro * dt; vGiro *= Mathf.Exp(-friccion * dt); }

        var padre = objetivo.parent;
        if (!regresando && grados.sqrMagnitude > 1e-6f && padre)
        {
            Vector3 arriba = padre.InverseTransformDirection(cam.transform.up);
            Vector3 derecha = padre.InverseTransformDirection(cam.transform.right);
            objetivo.localRotation = Quaternion.AngleAxis(-grados.x, arriba) * Quaternion.AngleAxis(grados.y, derecha) * objetivo.localRotation;
        }

        zoom = Mathf.Clamp(zoom * factorZoom, zoomMin, zoomMax);
        if (regresando)
        {
            float a = 1f - Mathf.Exp(-12f * dt);
            objetivo.localRotation = Quaternion.Slerp(objetivo.localRotation, baseRot, a);
            zoom = Mathf.Lerp(zoom, 1f, a);
            if (Quaternion.Angle(objetivo.localRotation, baseRot) < 0.2f && Mathf.Abs(zoom - 1f) < 0.002f)
            { objetivo.localRotation = baseRot; zoom = 1f; regresando = false; }
        }
        objetivo.localScale = baseEsc * zoom;
        objetivo.localPosition = new Vector3(basePos.x, basePos.y * zoom, basePos.z);    // al crecer sigue apoyado sobre la tarjeta
    }

    static Transform BuscarObjetivo(out ElementoTarget tarjeta)
    {
        tarjeta = null;
        if (GuiaVisual.Activa) return null;
        if (AppAtomAR.Modo == ModoApp.Explorar)
        {
            var t = PanelElemento.Elegido;
            if (t && t.Visible && t.Modelo) { tarjeta = t; return t.Modelo; }
            return null;
        }
        if (AppAtomAR.Modo == ModoApp.Combinar) return ModoCombinar.ObjetivoGestos;
        return null;
    }

    void CambiarObjetivo(Transform nuevo, ElementoTarget tarjeta)
    {
        // el anterior vuelve exactamente a como estaba
        if (objetivo) { objetivo.localRotation = baseRot; objetivo.localScale = baseEsc; objetivo.localPosition = basePos; }
        objetivo = nuevo; zoom = 1f; regresando = false; vGiro = Vector2.zero; distPrevia = -1f;
        if (!objetivo) return;
        if (tarjeta) { baseRot = tarjeta.BaseRot; baseEsc = tarjeta.BaseEsc; basePos = tarjeta.BasePos; }
        else { baseRot = objetivo.localRotation; baseEsc = objetivo.localScale; basePos = objetivo.localPosition; }
    }

    void OnDisable() => CambiarObjetivo(null, null);

    void LeerToques(ref Vector2 giro, ref float zoomF, ref bool tocando, ref bool reset)
    {
        var pantalla = Touchscreen.current;
        if (pantalla == null) return;
        activos.Clear();
        foreach (var t in pantalla.touches)
        {
            var fase = t.phase.ReadValue();
            int id = t.touchId.ReadValue();
            if (fase == FaseToque.Began)
            {
                var p = t.position.ReadValue();
                if (!SobreUI(p))
                {
                    dedos.Add(id);
                    if (DobleToque(p)) reset = true;
                }
            }
            bool vivo = fase == FaseToque.Began || fase == FaseToque.Moved || fase == FaseToque.Stationary;
            if (!vivo) { dedos.Remove(id); continue; }
            if (dedos.Contains(id)) activos.Add(t);
        }

        if (activos.Count == 1) { giro += activos[0].delta.ReadValue(); tocando = true; distPrevia = -1f; }
        else if (activos.Count >= 2)
        {
            float d = Vector2.Distance(activos[0].position.ReadValue(), activos[1].position.ReadValue());
            if (distPrevia > 0f && d > 0f) zoomF *= d / distPrevia;
            distPrevia = d; tocando = true;
        }
        else distPrevia = -1f;
    }

    void LeerMouse(ref Vector2 giro, ref float zoomF, ref bool tocando, ref bool reset)
    {
        var m = Mouse.current;
        if (m == null || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return;
        var p = m.position.ReadValue();
        if (m.leftButton.wasPressedThisFrame)
        {
            arrastreMouse = !SobreUI(p);
            if (arrastreMouse && DobleToque(p)) reset = true;
        }
        if (!m.leftButton.isPressed) arrastreMouse = false;
        if (arrastreMouse) { giro += m.delta.ReadValue(); tocando = true; }
        float rueda = m.scroll.ReadValue().y;
        if (rueda != 0f && !SobreUI(p)) zoomF *= Mathf.Pow(1.0015f, rueda);
    }

    bool DobleToque(Vector2 p)
    {
        float ahora = Time.unscaledTime;
        bool doble = ahora - ultimoToque < 0.3f && (p - posUltimoToque).magnitude < Screen.width * 0.08f;
        ultimoToque = doble ? -1f : ahora; posUltimoToque = p;
        return doble;
    }

    bool SobreUI(Vector2 p)
    {
        var es = EventSystem.current;
        if (!es) return false;
        evento ??= new PointerEventData(es);
        evento.Reset(); evento.position = p;
        golpes.Clear();
        es.RaycastAll(evento, golpes);
        foreach (var g in golpes) if (g.module is GraphicRaycaster) return true;
        return false;
    }
}
