// AtomAR - Progreso del Reto: qué preguntas se han contestado bien (se guarda en el teléfono).
// Cada elemento tiene 3 preguntas con id "Fe:0", "Fe:1", "Fe:2". Insignia: 1 acierto bronce, 2 plata, 3 oro.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class ProgresoQuiz
{
    const string Clave = "AtomAR.quiz";
    static HashSet<string> aciertos;
    static HashSet<string> Set => aciertos ??=
        new HashSet<string>(PlayerPrefs.GetString(Clave, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));

    public static bool Acertada(string id) => Set.Contains(id);
    public static int Puntos => Set.Count * 10;

    public static void Registrar(string id)
    {
        if (!Set.Add(id)) return;
        PlayerPrefs.SetString(Clave, string.Join("|", Set));
        PlayerPrefs.Save();
    }

    /// 0 = sin insignia, 1 = bronce, 2 = plata, 3 = oro.
    public static int Aciertos(string simbolo)
    {
        int n = 0;
        for (int i = 0; i < 3; i++) if (Set.Contains(simbolo + ":" + i)) n++;
        return n;
    }

    public static void Reiniciar() { Set.Clear(); PlayerPrefs.DeleteKey(Clave); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciar() => aciertos = null;
}
