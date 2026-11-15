// AtomAR - Ficha de un elemento (un archivo de datos por elemento).
// Cambiar un dato aquí se refleja en la app sin tocar código.
using UnityEngine;

[CreateAssetMenu(menuName = "AtomAR/Elemento", fileName = "Elemento")]
public class ElementoData : ScriptableObject
{
    [Tooltip("Nombre del Image Target en la base de Vuforia, p. ej. 01_h_hidrogeno")]
    public string targetName;
    public string simbolo;
    public string nombre;
    [Tooltip("Frase corta con la personalidad del elemento, p. ej. \"El metal que viaja en tu sangre\"")]
    public string apodo;
    public int numeroAtomico;
    public int neutrones;
    [Tooltip("Masa atómica estándar (IUPAC), como texto para respetar sus cifras")]
    public string masaAtomica;
    public string categoria;
    [Tooltip("Electrones por capa según el modelo de Bohr")]
    public int[] electronesPorCapa;
    [TextArea(2, 5)] public string dato;
    [Tooltip("Pregunta del Reto cuya respuesta es este elemento")]
    public string preguntaUso;
    [Tooltip("Color del elemento (tomado de su tarjeta)")]
    public Color color = Color.white;
}
