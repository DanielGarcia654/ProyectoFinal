// AtomAR - Une una tarjeta (Image Target) con su ficha de datos y avisa al panel cuando la cámara la ve o la pierde.
// Tocar el átomo en la cámara lo selecciona en el panel (necesita un Collider y un PhysicsRaycaster en la AR Camera).
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

[RequireComponent(typeof(ObserverBehaviour))]
public class ElementoTarget : MonoBehaviour, IPointerClickHandler
{
    public ElementoData datos;

    /// La cámara está viendo la tarjeta ahora mismo.
    public bool Rastreado { get; private set; }
    /// Su átomo se está mostrando (visto ahora o seguido por rastreo extendido).
    public bool Visible { get; private set; }

    ObserverBehaviour observador;

    void Awake()
    {
        observador = GetComponent<ObserverBehaviour>();
        observador.OnTargetStatusChanged += AlCambiarEstado;
    }

    void OnDestroy()
    {
        if (observador) observador.OnTargetStatusChanged -= AlCambiarEstado;
    }

    void AlCambiarEstado(ObserverBehaviour _, TargetStatus estado)
    {
        bool ahoraRastreado = estado.Status == Status.TRACKED;
        bool ahoraVisible = ahoraRastreado || estado.Status == Status.EXTENDED_TRACKED;   // igual que el modelo 3D
        bool eraVisible = Visible;
        Rastreado = ahoraRastreado;
        Visible = ahoraVisible;

        if (ahoraVisible && !eraVisible) PanelElemento.Encontrado(this);
        if (!ahoraVisible && eraVisible) PanelElemento.Perdido(this);
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (Visible) PanelElemento.Seleccionar(this);
    }
}
