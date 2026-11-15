// AtomAR - Une una tarjeta (Image Target) con su ficha de datos y avisa cuando la cámara la ve o la pierde.
// Tocar el átomo en la cámara lo selecciona en el panel (necesita un Collider y un PhysicsRaycaster en la AR Camera).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

[RequireComponent(typeof(ObserverBehaviour))]
public class ElementoTarget : MonoBehaviour, IPointerClickHandler
{
    /// Todas las tarjetas de la escena (las usa el modo Combinar).
    public static readonly List<ElementoTarget> Todos = new();

    public ElementoData datos;

    /// La cámara está viendo la tarjeta ahora mismo.
    public bool Rastreado { get; private set; }
    /// Su átomo se está mostrando (visto ahora o seguido por rastreo extendido).
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

    ObserverBehaviour observador;
    Transform modelo;

    void Awake()
    {
        observador = GetComponent<ObserverBehaviour>();
        observador.OnTargetStatusChanged += AlCambiarEstado;
    }

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() => Todos.Remove(this);

    void OnDestroy()
    {
        if (observador) observador.OnTargetStatusChanged -= AlCambiarEstado;
    }

    void AlCambiarEstado(ObserverBehaviour _, TargetStatus estado)
    {
        bool rastreado = estado.Status == Status.TRACKED;
        Aplicar(rastreado, rastreado || estado.Status == Status.EXTENDED_TRACKED);   // igual que el modelo 3D
    }

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
    /// Solo para pruebas en el editor: finge que la cámara ve (o pierde) esta tarjeta.
    public void Simular(bool visible) => Aplicar(visible, visible);
#endif
}
