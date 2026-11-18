// AtomAR - Gestos sobre el átomo en el modo Explorar.
// • Un dedo: gira el átomo como una bola, siempre respecto a la pantalla. Al soltar sigue girando un poco por inercia.
// • Dos dedos: pellizcar para acercar o alejar (entre 0.6x y 2.5x). El átomo sigue apoyado sobre su tarjeta.
// • Doble toque: el átomo vuelve suavemente a como estaba.
// • En el editor: arrastrar con el mouse lo gira, la rueda lo acerca y el doble clic lo regresa.
// Los toques que empiezan sobre la interfaz (carta, botones) no mueven el átomo.
// Cuando la tarjeta se pierde o se sale del modo, el átomo vuelve a su posición original.
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

    class Estado { public ElementoTarget tarjeta; public Quaternion rot; public Vector3 esc, pos; public float zoom = 1f; public bool regresando; }

    readonly Dictionary<Transform, Estado> estados = new();
    readonly List<Transform> quitar = new();
    readonly HashSet<int> dedos = new();
    readonly List<TouchControl> activos = new();
    readonly List<RaycastResult> golpes = new();
    Transform ultimo;
    Vector2 vGiro;                       // grados por segundo
    float distPrevia = -1f, ultimoToque = -1f;
    Vector2 posUltimoToque;
    bool arrastreMouse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciar() => Usado = false;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        var tarjeta = AppAtomAR.Modo == ModoApp.Explorar && !GuiaVisual.Activa ? PanelElemento.Elegido : null;
        var modelo = tarjeta && tarjeta.Visible ? tarjeta.Modelo : null;
        Limpiar();

        var cam = Camera.main;
        if (!modelo || !cam) { dedos.Clear(); vGiro = Vector2.zero; distPrevia = -1f; arrastreMouse = false; return; }
        if (modelo != ultimo) { ultimo = modelo; vGiro = Vector2.zero; }
        var e = Obtener(modelo, tarjeta);

        Vector2 giro = Vector2.zero; float factorZoom = 1f; bool tocando = false, reset = false;
        LeerToques(ref giro, ref factorZoom, ref tocando, ref reset);
        LeerMouse(ref giro, ref factorZoom, ref tocando, ref reset);

        if (reset) { e.regresando = true; vGiro = Vector2.zero; }
        else if (giro != Vector2.zero || factorZoom != 1f) { e.regresando = false; Usado = true; }

        // Giro: mientras se toca sigue al dedo; al soltar continúa con la velocidad que llevaba y se frena
        Vector2 grados = giro * (gradosPorPantalla / Mathf.Max(1, Screen.width));
        if (tocando) { if (dt > 0f) vGiro = Vector2.Lerp(vGiro, grados / dt, 0.4f); }
        else { grados = vGiro * dt; vGiro *= Mathf.Exp(-friccion * dt); }

        var padre = modelo.parent;
        if (!e.regresando && grados.sqrMagnitude > 1e-6f)
        {
            Vector3 arriba = padre.InverseTransformDirection(cam.transform.up);
            Vector3 derecha = padre.InverseTransformDirection(cam.transform.right);
            modelo.localRotation = Quaternion.AngleAxis(-grados.x, arriba) * Quaternion.AngleAxis(grados.y, derecha) * modelo.localRotation;
        }

        e.zoom = Mathf.Clamp(e.zoom * factorZoom, zoomMin, zoomMax);
        if (e.regresando)
        {
            float a = 1f - Mathf.Exp(-12f * dt);
            modelo.localRotation = Quaternion.Slerp(modelo.localRotation, e.rot, a);
            e.zoom = Mathf.Lerp(e.zoom, 1f, a);
            if (Quaternion.Angle(modelo.localRotation, e.rot) < 0.2f && Mathf.Abs(e.zoom - 1f) < 0.002f)
            { modelo.localRotation = e.rot; e.zoom = 1f; e.regresando = false; }
        }
        modelo.localScale = e.esc * e.zoom;
        modelo.localPosition = new Vector3(e.pos.x, e.pos.y * e.zoom, e.pos.z);    // al crecer sigue apoyado sobre la tarjeta
    }

    Estado Obtener(Transform modelo, ElementoTarget tarjeta)
    {
        if (!estados.TryGetValue(modelo, out var e))
        {
            e = new Estado { tarjeta = tarjeta, rot = modelo.localRotation, esc = modelo.localScale, pos = modelo.localPosition };
            estados[modelo] = e;
        }
        return e;
    }

    // Los átomos que ya no están a la vista, o todos si se sale de Explorar, vuelven a como estaban
    void Limpiar()
    {
        bool enExplorar = AppAtomAR.Modo == ModoApp.Explorar;
        quitar.Clear();
        foreach (var kv in estados)
        {
            var t = kv.Key; var e = kv.Value;
            if (t && e.tarjeta && e.tarjeta.Visible && enExplorar) continue;
            if (t) { t.localRotation = e.rot; t.localScale = e.esc; t.localPosition = e.pos; }
            quitar.Add(t);
        }
        foreach (var t in quitar) estados.Remove(t);
    }

    void LeerToques(ref Vector2 giro, ref float zoom, ref bool tocando, ref bool reset)
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
            if (distPrevia > 0f && d > 0f) zoom *= d / distPrevia;
            distPrevia = d; tocando = true;
        }
        else distPrevia = -1f;
    }

    void LeerMouse(ref Vector2 giro, ref float zoom, ref bool tocando, ref bool reset)
    {
        var m = Mouse.current;
        if (m == null) return;
        var p = m.position.ReadValue();
        if (m.leftButton.wasPressedThisFrame)
        {
            arrastreMouse = !SobreUI(p);
            if (arrastreMouse && DobleToque(p)) reset = true;
        }
        if (!m.leftButton.isPressed) arrastreMouse = false;
        if (arrastreMouse) { giro += m.delta.ReadValue(); tocando = true; }
        float rueda = m.scroll.ReadValue().y;
        if (rueda != 0f && !SobreUI(p)) zoom *= Mathf.Pow(1.0015f, rueda);
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
        golpes.Clear();
        es.RaycastAll(new PointerEventData(es) { position = p }, golpes);
        foreach (var g in golpes) if (g.module is GraphicRaycaster) return true;
        return false;
    }
}
