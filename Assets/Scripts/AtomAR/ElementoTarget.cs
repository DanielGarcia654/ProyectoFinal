// AtomAR - Une una tarjeta (Image Target) con su ficha de datos y avisa cuando la cámara la ve o la pierde.
// Tocar el átomo en la cámara lo selecciona en el panel (necesita un Collider y un PhysicsRaycaster en la AR Camera).
//
// Visibilidad: la decide la app, no Vuforia. Cada cuadro se calcula si el átomo debe verse y se aplica a todos
// los dibujos y colisionadores de la tarjeta (también a los átomos extra de Combinar). Regla:
// • TRACKED (la cámara ve la tarjeta)                                         → visible.
// • EXTENDED_TRACKED (ya no la ve; Vuforia solo estima dónde quedó)           → visible solo un momento
//   (GraciaExtendido) para que no parpadee con un movimiento brusco, y solo si ninguna otra tarjeta se ve.
//   Pasado ese momento se oculta: así no quedan átomos flotando en el cuarto ni encimados sobre otra tarjeta.
// Se usa Renderer/Collider.enabled (lo mismo que Vuforia), nunca SetActive, para no chocar con Combinar.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

[RequireComponent(typeof(ObserverBehaviour))]
public class ElementoTarget : MonoBehaviour, IPointerClickHandler
{
    /// Todas las tarjetas de la escena.
    public static readonly List<ElementoTarget> Todos = new();

    public ElementoData datos;

    /// La cámara está viendo la tarjeta ahora mismo.
    public bool Rastreado { get; private set; }
    /// Su átomo se está mostrando.
    public bool Visible { get; private set; }

    /// El modelo 3D del átomo que está sobre la tarjeta.
    public Transform Modelo
    {
        get
        {
            if (!modelo) { var a = GetComponentInChildren<AtomoAnimado>(true); modelo = a ? a.transform : null; }
            return modelo;
        }
    }

    /// Pose original del átomo sobre su tarjeta (gestos y Combinar regresan aquí).
    public Vector3 BasePos { get; private set; }
    public Quaternion BaseRot { get; private set; } = Quaternion.identity;
    public Vector3 BaseEsc { get; private set; } = Vector3.one;

    public void RestaurarModelo()
    {
        var m = Modelo; if (!m) return;
        m.localPosition = BasePos; m.localRotation = BaseRot; m.localScale = BaseEsc;
    }

    const float GraciaExtendido = 1.2f;   // s que se sigue mostrando una tarjeta que la cámara dejó de ver
    const float MemoriaOtra = 0.5f;       // s que se recuerda que otra tarjeta se vio (evita parpadeos)

    ObserverBehaviour observador;
    Transform modelo;
    bool crudoRastreado, crudoExtendido;  // lo que reporta Vuforia
    float ultimoRastreo = -10f;
    bool dibujado, forzar = true;         // lo último que aplicamos a los dibujos / volver a aplicarlo
    readonly List<Renderer> rends = new();
    readonly List<Collider> cols = new();

    void Awake()
    {
        observador = GetComponent<ObserverBehaviour>();
        observador.OnTargetStatusChanged += AlCambiarEstado;
        var m = Modelo;
        if (m) { BasePos = m.localPosition; BaseRot = m.localRotation; BaseEsc = m.localScale; }
    }

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() => Todos.Remove(this);

    void OnDestroy()
    {
        if (observador) observador.OnTargetStatusChanged -= AlCambiarEstado;
    }

    void AlCambiarEstado(ObserverBehaviour _, TargetStatus estado)
    {
        crudoRastreado = estado.Status == Status.TRACKED;
        crudoExtendido = estado.Status == Status.EXTENDED_TRACKED;
        if (crudoRastreado) ultimoRastreo = Time.unscaledTime;
        forzar = true;                    // Vuforia pudo haber cambiado los dibujos: se vuelve a aplicar lo nuestro
        Evaluar();
    }

    // LateUpdate: después de que Vuforia aplicó lo suyo, así nuestra decisión es la última del cuadro
    void LateUpdate()
    {
        if (crudoRastreado) ultimoRastreo = Time.unscaledTime;
        Evaluar();
        if (forzar || Visible != dibujado) Dibujar(Visible);
    }

    bool OtraSeVe()
    {
        float ahora = Time.unscaledTime;
        for (int i = 0; i < Todos.Count; i++)
        {
            var o = Todos[i];
            if (o != this && ahora - o.ultimoRastreo < MemoriaOtra) return true;
        }
        return false;
    }

    void Evaluar()
    {
        bool extendidoValido = crudoExtendido && Time.unscaledTime - ultimoRastreo < GraciaExtendido && !OtraSeVe();
        bool ver = crudoRastreado || extendidoValido;
        if (ver != Visible || crudoRastreado != Rastreado) Aplicar(crudoRastreado, ver);
    }

    // Enciende o apaga los dibujos y colisionadores de la tarjeta y de todos sus átomos
    void Dibujar(bool si)
    {
        GetComponentsInChildren(true, rends);
        GetComponentsInChildren(true, cols);
        foreach (var r in rends) if (r.enabled != si) r.enabled = si;
        foreach (var c in cols) if (c.enabled != si) c.enabled = si;
        dibujado = si; forzar = false;
    }

    /// Combinar crea átomos extra sobre la tarjeta: con esto se les aplica de inmediato la visibilidad actual.
    public void RefrescarDibujo() => forzar = true;

    void Aplicar(bool ahoraRastreado, bool ahoraVisible)
    {
        bool eraVisible = Visible;
        Rastreado = ahoraRastreado;
        Visible = ahoraVisible;
        if (ahoraVisible && !eraVisible) PanelElemento.Encontrado(this);
        if (!ahoraVisible && eraVisible) PanelElemento.Perdido(this);
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (Visible && AppAtomAR.Modo == ModoApp.Explorar) PanelElemento.Seleccionar(this);
    }

#if UNITY_EDITOR
    /// Solo para pruebas en el editor: finge que la cámara ve (o pierde por completo) esta tarjeta.
    public void Simular(bool visible) { crudoRastreado = visible; crudoExtendido = false; if (visible) ultimoRastreo = Time.unscaledTime; forzar = true; Evaluar(); }
    /// Pruebas: finge que la tarjeta pasó a rastreo extendido (la cámara ya no la ve, pero Vuforia la sigue estimando).
    public void SimularExtendido() { crudoRastreado = false; crudoExtendido = true; forzar = true; Evaluar(); }
#endif
}
