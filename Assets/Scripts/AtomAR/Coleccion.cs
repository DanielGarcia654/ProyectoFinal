// AtomAR - Lo que el alumno ya descubrió (elementos) y formó (moléculas). Se guarda en el teléfono.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Coleccion
{
    const string Clave = "AtomAR.descubiertos", ClaveMol = "AtomAR.moleculas";
    public const int TotalElementos = 10, TotalMoleculas = 4;

    static HashSet<string> descubiertos, moleculas;
    static HashSet<string> Cargar(string clave) =>
        new HashSet<string>(PlayerPrefs.GetString(clave, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
    static HashSet<string> Set => descubiertos ??= Cargar(Clave);
    static HashSet<string> SetMol => moleculas ??= Cargar(ClaveMol);

    public static int Cantidad => Set.Count;
    public static int Moleculas => SetMol.Count;
    public static bool Descubierto(string targetName) => Set.Contains(targetName);
    public static bool MoleculaFormada(string id) => SetMol.Contains(id);

    /// Marca el elemento como descubierto. Devuelve true solo la primera vez.
    public static bool Registrar(string targetName) => Agregar(Set, Clave, targetName);
    /// Marca la molécula como formada. Devuelve true solo la primera vez.
    public static bool RegistrarMolecula(string id) => Agregar(SetMol, ClaveMol, id);

    static bool Agregar(HashSet<string> set, string clave, string valor)
    {
        if (string.IsNullOrEmpty(valor) || !set.Add(valor)) return false;
        PlayerPrefs.SetString(clave, string.Join("|", set));
        PlayerPrefs.Save();
        return true;
    }

    public static void Reiniciar()
    {
        Set.Clear(); SetMol.Clear();
        PlayerPrefs.DeleteKey(Clave); PlayerPrefs.DeleteKey(ClaveMol);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciar() { descubiertos = null; moleculas = null; }
}
