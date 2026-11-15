// AtomAR - Ficha de una molécula: qué dos elementos la forman y cuántos átomos de cada uno.
using UnityEngine;

[CreateAssetMenu(menuName = "AtomAR/Molécula", fileName = "Molecula")]
public class MoleculaData : ScriptableObject
{
    public string id;
    public string nombre;
    [Tooltip("Fórmula con subíndices de TextMeshPro, p. ej. H<sub>2</sub>O")]
    public string formula;
    public string simboloA; public int cantidadA = 1;
    public string simboloB; public int cantidadB = 1;
    [Tooltip("Ecuación química balanceada de la reacción real")]
    public string ecuacionReal;
    [TextArea(2, 5)] public string dato;
    public Color color = Color.white;
    public GameObject modelo;
}
