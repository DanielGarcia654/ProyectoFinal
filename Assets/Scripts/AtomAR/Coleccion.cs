// AtomAR - Elementos que el alumno ya descubrió (se guarda en el teléfono).
// Lo usa el panel ("4 de 10") y lo usará el Quiz para las insignias.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Coleccion
{
    const string Clave = "AtomAR.descubiertos";
    public const int TotalElementos = 10;

    static HashSet<string> descubiertos;
    static HashSet<string> Set => descubiertos ??= new HashSet<string>(
        PlayerPrefs.GetString(Clave, "").Split('|', StringSplitOptions.RemoveEmptyEntries));

    public static int Cantidad => Set.Count;
    public static bool Descubierto(string targetName) => Set.Contains(targetName);

    /// Marca el elemento como descubierto. Devuelve true solo la primera vez.
    public static bool Registrar(string targetName)
    {
        if (string.IsNullOrEmpty(targetName) || !Set.Add(targetName)) return false;
        PlayerPrefs.SetString(Clave, string.Join("|", Set));
        PlayerPrefs.Save();
        return true;
    }

    public static void Reiniciar()
    {
        Set.Clear();
        PlayerPrefs.DeleteKey(Clave);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciar() => descubiertos = null;   // relee al entrar a Play sin recarga de dominio
}
