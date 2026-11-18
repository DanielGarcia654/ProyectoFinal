// AtomAR - Genera la marca de la app y configura la pantalla de carga y el ícono de Android.
// Menú: AtomAR > Generar marca, ícono y pantalla de carga
//
// La marca se dibuja con los mismos componentes de la interfaz (DibujoUI y la tipografía Syne), así que coincide
// con la app y se puede regenerar si algo cambia. Archivos que produce en Assets/AtomAR_UI/Marca:
//   AtomAR_Simbolo.png          símbolo solo, fondo transparente
//   AtomAR_Logo.png             símbolo + nombre, para fondos oscuros
//   AtomAR_Logo_SobreClaro.png  símbolo + nombre, para fondos claros (documentos, registro)
//   AtomAR_Logo_Vertical.png    versión apilada (la usa la pantalla de carga)
//   AtomAR_Icono.png            ícono cuadrado de la app
//   AtomAR_Icono_Frente/Fondo   capas del ícono adaptativo de Android
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public static class AtomARMarca
{
    const string Dir = "Assets/AtomAR_UI/Marca";
    const int Capa = 29;
    static readonly Color Noche = new Color32(0x15, 0x18, 0x33, 255);
    static readonly Color Papel = new Color32(0xF3, 0xF5, 0xFF, 255);
    static readonly Color Tinta = new Color32(0x15, 0x18, 0x33, 255);

    [MenuItem("AtomAR/Generar marca, ícono y pantalla de carga")]
    public static void Generar()
    {
        Directory.CreateDirectory(Dir);
        var syne = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AtomAR_UI/Fuentes/Syne SDF.asset");

        Guardar("AtomAR_Simbolo", Recortar(Transparente(1024, 1024, r => Simbolo(r, Vector2.zero, 1000f, false)), 24));
        Guardar("AtomAR_Logo", Recortar(Transparente(2600, 900, r => Horizontal(r, syne, Papel, false)), 48));
        Guardar("AtomAR_Logo_SobreClaro", Recortar(Transparente(2600, 900, r => Horizontal(r, syne, Tinta, true)), 48));
        Guardar("AtomAR_Logo_Vertical", Recortar(Transparente(1400, 1400, r => Vertical(r, syne)), 48));
        Guardar("AtomAR_Icono", Render(1024, 1024, Noche, r => Simbolo(r, new Vector2(0f, 8f), 780f, false)));
        Guardar("AtomAR_Icono_Frente", Transparente(1024, 1024, r => Simbolo(r, new Vector2(0f, 6f), 560f, false)));   // zona segura del ícono adaptativo
        Guardar("AtomAR_Icono_Fondo", Liso(432, 432, Noche));

        AssetDatabase.Refresh();
        Importar();
        PantallaDeCarga();
        Icono();
        AssetDatabase.SaveAssets();
        Debug.Log("[AtomAR] Marca generada en " + Dir + "; pantalla de carga e ícono configurados.");
    }

    // ───────── Composiciones ─────────
    static void Simbolo(RectTransform padre, Vector2 pos, float tam, bool sobreClaro)
    {
        var go = new GameObject("Simbolo", typeof(RectTransform));
        var rt = (RectTransform)go.transform; rt.SetParent(padre, false);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(tam, tam);
        var d = go.AddComponent<DibujoUI>(); d.figura = "logo"; d.animado = false; d.sobreClaro = sobreClaro; d.raycastTarget = false;
    }

    static void Nombre(RectTransform padre, TMP_FontAsset fuente, Color color, Vector2 pos, Vector2 tam, float tamLetra, TextAlignmentOptions al)
    {
        var go = new GameObject("Nombre", typeof(RectTransform));
        var rt = (RectTransform)go.transform; rt.SetParent(padre, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (fuente) t.font = fuente;
        t.text = "AtomAR"; t.fontSize = tamLetra; t.fontStyle = FontStyles.Bold; t.color = color;
        t.alignment = al; t.characterSpacing = -4f; t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
    }

    static void Horizontal(RectTransform r, TMP_FontAsset fuente, Color color, bool sobreClaro)
    {
        Simbolo(r, new Vector2(-880f, 0f), 720f, sobreClaro);
        Nombre(r, fuente, color, new Vector2(330f, -20f), new Vector2(1700f, 700f), 400f, TextAlignmentOptions.MidlineLeft);
    }

    static void Vertical(RectTransform r, TMP_FontAsset fuente)
    {
        Simbolo(r, new Vector2(0f, 190f), 760f, false);
        Nombre(r, fuente, Papel, new Vector2(0f, -400f), new Vector2(1300f, 340f), 250f, TextAlignmentOptions.Center);
    }

    // ───────── Render ─────────
    /// Renderiza sobre negro y sobre blanco y de la diferencia saca la transparencia exacta (bordes limpios en cualquier fondo).
    static Texture2D Transparente(int w, int h, System.Action<RectTransform> armar)
    {
        var negro = Render(w, h, Color.black, armar);
        var blanco = Render(w, h, Color.white, armar);
        var a = negro.GetPixels(); var b = blanco.GetPixels();
        for (int i = 0; i < a.Length; i++)
        {
            float alfa = Mathf.Clamp01(1f - ((b[i].r - a[i].r) + (b[i].g - a[i].g) + (b[i].b - a[i].b)) / 3f);
            a[i] = alfa < 0.002f ? new Color(0, 0, 0, 0)
                 : new Color(Mathf.Clamp01(a[i].r / alfa), Mathf.Clamp01(a[i].g / alfa), Mathf.Clamp01(a[i].b / alfa), alfa);
        }
        negro.SetPixels(a); negro.Apply();
        Object.DestroyImmediate(blanco);
        return negro;
    }

    static Texture2D Render(int w, int h, Color fondo, System.Action<RectTransform> armar)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        var camGO = new GameObject("_MarcaCamara") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGO.AddComponent<Camera>();
        cam.enabled = false; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fondo;
        cam.cullingMask = 1 << Capa; cam.orthographic = true; cam.targetTexture = rt; cam.nearClipPlane = 0.1f; cam.farClipPlane = 10f;
        var lienzo = new GameObject("_MarcaLienzo", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
        try
        {
            armar((RectTransform)lienzo.transform);
            foreach (var t in lienzo.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = Capa; t.gameObject.hideFlags = HideFlags.HideAndDontSave; }
            Canvas.ForceUpdateCanvases();
            foreach (var t in lienzo.GetComponentsInChildren<TMP_Text>()) t.ForceMeshUpdate(true, true);
            foreach (var g in lienzo.GetComponentsInChildren<Graphic>()) g.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var previo = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = previo;
            return tex;
        }
        finally
        {
            Object.DestroyImmediate(lienzo); Object.DestroyImmediate(camGO);
            rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// Quita el espacio transparente sobrante y deja un margen.
    static Texture2D Recortar(Texture2D tex, int margen)
    {
        int w = tex.width, h = tex.height, x0 = w, y0 = h, x1 = -1, y1 = -1;
        var px = tex.GetPixels32();
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            if (px[y * w + x].a > 3) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
        if (x1 < 0) return tex;
        x0 = Mathf.Max(0, x0 - margen); y0 = Mathf.Max(0, y0 - margen); x1 = Mathf.Min(w - 1, x1 + margen); y1 = Mathf.Min(h - 1, y1 + margen);
        var r = new Texture2D(x1 - x0 + 1, y1 - y0 + 1, TextureFormat.RGBA32, false);
        r.SetPixels(tex.GetPixels(x0, y0, r.width, r.height)); r.Apply();
        Object.DestroyImmediate(tex);
        return r;
    }

    static Texture2D Liso(int w, int h, Color c)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(Enumerable.Repeat(c, w * h).ToArray()); t.Apply();
        return t;
    }

    static void Guardar(string nombre, Texture2D tex)
    {
        File.WriteAllBytes($"{Dir}/{nombre}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void Importar()
    {
        foreach (var path in Directory.GetFiles(Dir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            AssetDatabase.ImportAsset(p);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    // ───────── Configuración del proyecto ─────────
    /// Pantalla de carga nativa: logo de AtomAR arriba, "Made with Unity" abajo, sobre el índigo de la app.
    static void PantallaDeCarga()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/AtomAR_Logo_Vertical.png");
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
        PlayerSettings.SplashScreen.backgroundColor = Noche;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
    }

    /// Ícono de la app: el mismo en todas partes y, en Android, también la versión adaptativa (fondo + frente).
    static void Icono()
    {
        var icono = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/AtomAR_Icono.png");
        var frente = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/AtomAR_Icono_Frente.png");
        var fondo = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/AtomAR_Icono_Fondo.png");
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);
        foreach (var tipo in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            var iconos = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, tipo);
            bool adaptativo = tipo.ToString().ToLower().Contains("adaptive");
            foreach (var i in iconos)
            {
                if (adaptativo) { i.SetTexture(fondo, 0); i.SetTexture(frente, 1); }
                else i.SetTexture(icono);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, tipo, iconos);
        }
    }
}
