// AtomAR - Arma la escena principal con AR Camera y los 10 Image Targets de la base "AtomAR".
// Uso: menú AtomAR > Montar escena principal
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AtomARSceneBuilder
{
    const string ScenePath   = "Assets/Scenes/AtomAR_Main.unity";
    const string DataSetPath = "Vuforia/AtomAR.xml";              // relativo a StreamingAssets
    const string ModelsDir   = "Assets/AtomAR_Modelos/Elementos";
    const float  ModelSize   = 0.6f;                               // tamaño del átomo respecto al ancho de la tarjeta

    // nombre del target en Vuforia -> nombre del modelo FBX
    static readonly (string target, string model)[] Elements =
    {
        ("01_h_hidrogeno", "H_Hidrogeno"), ("02_he_helio", "He_Helio"),   ("03_c_carbono", "C_Carbono"),
        ("04_n_nitrogeno", "N_Nitrogeno"), ("05_o_oxigeno", "O_Oxigeno"), ("06_na_sodio", "Na_Sodio"),
        ("07_cl_cloro", "Cl_Cloro"),       ("08_ca_calcio", "Ca_Calcio"), ("09_fe_hierro", "Fe_Hierro"),
        ("10_au_oro", "Au_Oro"),
    };

    [MenuItem("AtomAR/Montar escena principal")]
    public static void Build()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("AtomAR", $"{ScenePath} ya existe. ¿Reemplazarla?", "Reemplazar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var mainCam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault();
        if (mainCam) Object.DestroyImmediate(mainCam.gameObject);

        var arCam = CreateFromMenu("GameObject/Vuforia Engine/AR Camera");
        if (arCam == null) { Debug.LogError("[AtomAR] No se pudo crear la AR Camera (¿está instalado Vuforia?)."); return; }
        arCam.name = "AR Camera";

        var root = new GameObject("ImageTargets");
        var log = new List<string>();
        for (int i = 0; i < Elements.Length; i++)
        {
            var (target, model) = Elements[i];
            var it = CreateFromMenu("GameObject/Vuforia Engine/Image Target");
            if (it == null) { Debug.LogError("[AtomAR] No se pudo crear un Image Target."); return; }
            it.name = "IT_" + model;
            it.transform.SetParent(root.transform, false);
            it.transform.position = new Vector3((i % 5) * 1.5f - 3f, 0f, (i / 5) * -1.5f);
            it.transform.localScale = Vector3.one;              // los targets miden 1 x 1 en la base

            ConfigureTarget(it, target, log);
            AttachModel(it, model, log);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Selection.activeGameObject = root;

        Debug.Log("[AtomAR] Escena creada en " + ScenePath + "\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog("AtomAR", "Escena AtomAR_Main creada con 10 Image Targets.\nRevisa la consola para el detalle.", "OK");
    }

    static GameObject CreateFromMenu(string menu)
    {
        Selection.activeObject = null;
        if (!EditorApplication.ExecuteMenuItem(menu)) return null;
        return Selection.activeGameObject;
    }

    static void ConfigureTarget(GameObject it, string target, List<string> log)
    {
        var behaviour = it.GetComponents<MonoBehaviour>().FirstOrDefault(c => c && c.GetType().Name == "ImageTargetBehaviour");
        if (behaviour == null) { log.Add($"✗ {it.name}: no tiene ImageTargetBehaviour"); return; }

        var so = new SerializedObject(behaviour);
        // Vuforia 11 guarda el nombre en mTrackableName (mTargetName era de versiones anteriores)
        bool nameOk = SetString(so, "mTrackableName", target) | SetString(so, "mTargetName", target);
        bool ok = SetString(so, "mDataSetPath", DataSetPath) & nameOk;
        var type = so.FindProperty("mImageTargetType"); if (type != null) type.intValue = 0;   // Predefinido (de base)
        foreach (var p in new[] { "mAspectRatio", "mWidth", "mHeight" })
        { var sp = so.FindProperty(p); if (sp != null && sp.propertyType == SerializedPropertyType.Float) sp.floatValue = 1f; }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(behaviour);

        // Verifica lo que realmente quedó guardado
        so.Update();
        var saved = so.FindProperty("mTrackableName")?.stringValue ?? so.FindProperty("mTargetName")?.stringValue;
        ok &= saved == target;
        log.Add((ok ? "✓ " : "✗ ") + $"{it.name} -> {DataSetPath} / {saved}");
    }

    static bool SetString(SerializedObject so, string prop, string value)
    {
        var sp = so.FindProperty(prop);
        if (sp == null || sp.propertyType != SerializedPropertyType.String) return false;
        sp.stringValue = value; return true;
    }

    static void AttachModel(GameObject it, string model, List<string> log)
    {
        var path = $"{ModelsDir}/{model}.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.Add($"   ✗ modelo no encontrado: {path}"); return; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, it.transform);
        go.name = model;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.AddComponent<AtomoAnimado>();                 // electrones girando y núcleo con pulso

        // Escala el átomo para que mida ModelSize del ancho de la tarjeta y lo apoya encima
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max > 0f)
            {
                float s = ModelSize / max * it.transform.lossyScale.x;
                go.transform.localScale = Vector3.one * (s / it.transform.lossyScale.x);
                b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                go.transform.position += new Vector3(it.transform.position.x - b.center.x,
                                                     it.transform.position.y - b.min.y + 0.02f,
                                                     it.transform.position.z - b.center.z);
            }
        }
        log.Add($"   ✓ modelo {model} (escala {go.transform.localScale.x:0.###})");
    }

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
