// AtomAR - Anima un átomo modelo de Bohr: cada capa de electrones gira sobre su órbita
// (las capas internas más rápido) y el núcleo "respira" suavemente.
// Se agrega automáticamente a la raíz de cada modelo de elemento.
using System.Collections.Generic;
using UnityEngine;

public class AtomoAnimado : MonoBehaviour
{
    [Tooltip("Grados por segundo de la capa 1; cada capa exterior va más lenta.")]
    public float velocidadCapa1 = 120f;
    [Range(0.3f, 1f)] public float factorPorCapa = 0.7f;
    [Tooltip("Amplitud del pulso del núcleo (0 = sin pulso).")]
    public float pulsoNucleo = 0.04f;
    public float frecuenciaPulso = 1.2f;

    readonly List<(Transform t, float vel)> capas = new();
    Transform nucleo;
    Vector3 escalaNucleo;
    float fase;

    void Awake()
    {
        // Desactiva el Animator/clip importado del FBX para que no compita con este script
        var anim = GetComponent<Animator>(); if (anim) anim.enabled = false;
        var legacy = GetComponent<Animation>(); if (legacy) legacy.enabled = false;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.EndsWith("_Pivote"))
            {
                int capa = ExtraerCapa(t.name);
                float vel = velocidadCapa1 * Mathf.Pow(factorPorCapa, capa - 1);
                if (capa % 2 == 0) vel = -vel;               // capas alternas en sentido contrario
                capas.Add((t, vel));
            }
            else if (t.name.EndsWith("_Nucleo")) nucleo = t;
        }
        if (nucleo) escalaNucleo = nucleo.localScale;
        fase = Random.value * 10f;                           // que no todos los átomos vayan sincronizados
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var (t, vel) in capas) t.Rotate(0f, 0f, vel * dt, Space.Self);

        if (nucleo && pulsoNucleo > 0f)
        {
            float s = 1f + Mathf.Sin((Time.time + fase) * frecuenciaPulso * Mathf.PI * 2f) * pulsoNucleo;
            nucleo.localScale = escalaNucleo * s;
            nucleo.Rotate(0f, 15f * dt, 0f, Space.Self);
        }
    }

    static int ExtraerCapa(string nombre)
    {
        // "Na_Capa_2_Pivote" -> 2
        var partes = nombre.Split('_');
        for (int i = 0; i < partes.Length - 1; i++)
            if (partes[i] == "Capa" && int.TryParse(partes[i + 1], out int n)) return n;
        return 1;
    }
}
