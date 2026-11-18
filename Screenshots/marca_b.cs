using UnityEngine; using UnityEngine.Rendering; using System.Collections.Generic; using System.IO; using TMPro;
const int L = 29;
Color H(string h){ ColorUtility.TryParseHtmlString(h, out var c); return c; }
var Noche=H("#151833"); var Proton=H("#F2464F"); var Neutron=H("#9AA6C2"); var Electron=H("#2EE6FF"); var Papel=H("#F3F5FF");
var basura = new List<Object>();
var raiz = new GameObject("_Logo3D"); raiz.transform.position = new Vector3(0,-2000,0); basura.Add(raiz);
Material Mat(Color c, float gloss, float metal=0f, Color? em=null){ var m=new Material(Shader.Find("Standard")); m.color=c; m.SetFloat("_Glossiness",gloss); m.SetFloat("_Metallic",metal); if(em!=null){ m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", em.Value);} basura.Add(m); return m; }
GameObject Obj(string n, Mesh me, Material m, Vector3 p, Quaternion r, Vector3 s){ var g=new GameObject(n); g.transform.SetParent(raiz.transform,false); g.transform.localPosition=p; g.transform.localRotation=r; g.transform.localScale=s; g.AddComponent<MeshFilter>().sharedMesh=me; var mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterial=m; mr.shadowCastingMode=ShadowCastingMode.Off; g.layer=L; return g; }
Mesh Losa(float w, float d, float rad, float h){
  var pts=new List<Vector2>(); int seg=12; var cs=new[]{new Vector2(w/2-rad,d/2-rad),new Vector2(-w/2+rad,d/2-rad),new Vector2(-w/2+rad,-d/2+rad),new Vector2(w/2-rad,-d/2+rad)};
  for(int k=0;k<4;k++) for(int i=0;i<=seg;i++){ float a=(k*90+i*90f/seg)*Mathf.Deg2Rad; pts.Add(cs[k]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad); }
  var v=new List<Vector3>(); var t=new List<int>(); int n=pts.Count;
  v.Add(new Vector3(0,h,0)); foreach(var p in pts) v.Add(new Vector3(p.x,h,p.y)); for(int i=0;i<n;i++){ t.Add(0); t.Add(1+(i+1)%n); t.Add(1+i); }
  for(int i=0;i<n;i++){ var p=pts[i]; var q=pts[(i+1)%n]; int s=v.Count; v.Add(new Vector3(p.x,h,p.y)); v.Add(new Vector3(q.x,h,q.y)); v.Add(new Vector3(p.x,0,p.y)); v.Add(new Vector3(q.x,0,q.y)); t.AddRange(new[]{s,s+1,s+2,s+1,s+3,s+2}); }
  var m=new Mesh(); m.SetVertices(v); m.SetTriangles(t,0); m.RecalculateNormals(); basura.Add(m); return m; }
Mesh Toro(float R, float r){ int U=180,V=20; var v=new List<Vector3>(); var t=new List<int>();
  for(int i=0;i<=U;i++) for(int j=0;j<=V;j++){ float u=i*2*Mathf.PI/U, w=j*2*Mathf.PI/V; v.Add(new Vector3((R+r*Mathf.Cos(w))*Mathf.Cos(u), r*Mathf.Sin(w), (R+r*Mathf.Cos(w))*Mathf.Sin(u))); }
  for(int i=0;i<U;i++) for(int j=0;j<V;j++){ int a=i*(V+1)+j, b=a+V+1; t.AddRange(new[]{a,a+1,b, b,a+1,b+1}); }
  var m=new Mesh(); m.indexFormat=IndexFormat.UInt32; m.SetVertices(v); m.SetTriangles(t,0); m.RecalculateNormals(); basura.Add(m); return m; }
Texture2D Radial(int n, float pw){ var tx=new Texture2D(n,n,TextureFormat.RGBA32,false); var px=new Color[n*n]; for(int y=0;y<n;y++)for(int x=0;x<n;x++){ float dx=(x+.5f)/n*2-1, dy=(y+.5f)/n*2-1; float a=Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy)),pw); px[y*n+x]=new Color(1,1,1,a);} tx.SetPixels(px); tx.Apply(); tx.wrapMode=TextureWrapMode.Clamp; basura.Add(tx); return tx; }
var esf = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"); var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); var cubo=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
var rad = Radial(256, 2.2f);
var addSh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
Material Brillo(Color c){ var m=new Material(addSh); m.mainTexture=rad; if(m.HasProperty("_TintColor")) m.SetColor("_TintColor",c); else m.color=c; basura.Add(m); return m; }
Material Sombra(float a){ var m=new Material(Shader.Find("Sprites/Default")); m.mainTexture=rad; m.color=new Color(0.02f,0.02f,0.07f,a); basura.Add(m); return m; }
var plano = Quaternion.Euler(90,0,0);

// ── Tarjeta de RA: cuerpo cian que brilla en el borde, cara oscura y esquinas de escaneo
Obj("Cuerpo", Losa(1.7f,1.7f,0.22f,0.07f), Mat(Electron*0.6f,0.8f,0f,Electron*0.85f), Vector3.zero, Quaternion.identity, Vector3.one);
Obj("Cara", Losa(1.58f,1.58f,0.16f,0.02f), Mat(H("#141836"),0.88f), new Vector3(0,0.065f,0), Quaternion.identity, Vector3.one);
var blanco = Mat(Papel,0.6f,0f,Papel*0.5f);
foreach(var sx in new[]{-1,1}) foreach(var sz in new[]{-1,1}){
  var c=new Vector3(sx*0.6f,0.095f,sz*0.6f);
  Obj("EsqA",cubo,blanco,c+new Vector3(-sx*0.1f,0,0),Quaternion.identity,new Vector3(0.24f,0.018f,0.045f));
  Obj("EsqB",cubo,blanco,c+new Vector3(0,0,-sz*0.1f),Quaternion.identity,new Vector3(0.045f,0.018f,0.24f)); }

// ── Átomo flotando sobre la tarjeta
var A = new Vector3(0,1.08f,0);
Obj("Derrame", quad, Brillo(Electron*0.3f), new Vector3(0,0.09f,0.02f), plano, Vector3.one*1.45f);
Obj("SombraAtomo", quad, Sombra(0.9f), new Vector3(0.06f,0.092f,0.1f), plano, Vector3.one*1.0f);
var pr=Mat(Proton,0.85f); var ne=Mat(Neutron,0.85f);
var nuc = new[]{ new Vector3(0,0,0), new Vector3(0.17f,0.05f,0.02f), new Vector3(-0.15f,0.07f,0.06f), new Vector3(0.03f,0.16f,-0.05f), new Vector3(0.04f,-0.15f,0.07f), new Vector3(-0.07f,-0.06f,-0.15f), new Vector3(0.09f,0.02f,0.16f), new Vector3(-0.1f,0.1f,0.15f), new Vector3(0.12f,-0.06f,-0.13f) };
for(int i=0;i<nuc.Length;i++) Obj("N",esf, i%2==0?pr:ne, A+nuc[i]*0.8f, Quaternion.identity, Vector3.one*0.17f);
var cam = new GameObject("_cam").AddComponent<Camera>(); basura.Add(cam.gameObject); cam.transform.SetParent(raiz.transform,false);
cam.transform.localPosition = new Vector3(0,2.55f,-4.15f); cam.transform.LookAt(raiz.transform.TransformPoint(0,0.62f,0)); cam.fieldOfView=31f; cam.cullingMask=1<<L; cam.enabled=false; cam.clearFlags=CameraClearFlags.SolidColor; cam.nearClipPlane=0.1f; cam.farClipPlane=20f; cam.allowHDR=false;
// órbitas: anillos casi de frente a la cámara, cruzados en X
var hacia = Quaternion.LookRotation(raiz.transform.InverseTransformDirection(-cam.transform.forward));
var orb = Mat(H("#BFF7FF"),0.9f,0f,Electron*1.05f); var el = Mat(Color.white,0.9f,0f,Electron*1.6f);
var anillos = new[]{ hacia*Quaternion.Euler(0,0,32)*Quaternion.Euler(22,0,0), hacia*Quaternion.Euler(0,0,-32)*Quaternion.Euler(22,0,0) };
var angs = new[]{ 335f, 155f };
for(int k=0;k<2;k++){ Obj("Orbita", Toro(0.74f,0.016f), orb, A, anillos[k], Vector3.one);
  var p=A+anillos[k]*(new Vector3(Mathf.Cos(angs[k]*Mathf.Deg2Rad),0,Mathf.Sin(angs[k]*Mathf.Deg2Rad))*0.74f);
  Obj("Electron",esf,el,p,Quaternion.identity,Vector3.one*0.1f);
  Obj("Halo",quad,Brillo(Electron*0.85f),p,Quaternion.LookRotation(p-cam.transform.localPosition),Vector3.one*0.5f); }
Obj("HaloAtomo",quad,Brillo(Electron*0.16f),A+new Vector3(0,0,0.6f),Quaternion.LookRotation(A-cam.transform.localPosition),Vector3.one*2.4f);

// ── Nombre (solo para el logo horizontal)
TextMeshPro nombre = null;
if (true) { var go=new GameObject("_nombre"); go.layer=L; go.transform.SetParent(raiz.transform,false); basura.Add(go);
  nombre=go.AddComponent<TextMeshPro>(); nombre.font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AtomAR_UI/Fuentes/Syne SDF.asset");
  nombre.text="Atom<color=#2EE6FF>AR</color>"; nombre.fontStyle=FontStyles.Bold; nombre.color=Papel; nombre.fontSize=7.5f; nombre.characterSpacing=-4; nombre.textWrappingMode=TextWrappingModes.NoWrap; nombre.alignment=TextAlignmentOptions.MidlineLeft;
  nombre.rectTransform.sizeDelta=new Vector2(10,3); nombre.rectTransform.pivot=new Vector2(0,0.5f);
  go.transform.rotation=cam.transform.rotation; go.transform.position=cam.transform.position+cam.transform.forward*4f+cam.transform.right*1.15f+cam.transform.up*-0.08f; nombre.ForceMeshUpdate(); }

// ── Luces: llave cálida, contraluz frío, relleno índigo
void Luz(Vector3 rot, Color c, float i){ var g=new GameObject("_luz"); g.transform.SetParent(raiz.transform,false); g.transform.localRotation=Quaternion.Euler(rot); var l=g.AddComponent<Light>(); l.type=LightType.Directional; l.color=c; l.intensity=i; l.cullingMask=1<<L; }
Luz(new Vector3(52,-38,0), H("#FFF1DE"), 1.2f); Luz(new Vector3(18,155,0), H("#8FD8FF"), 0.95f); Luz(new Vector3(-10,30,0), H("#6F7BFF"), 0.22f);
foreach(var g in raiz.GetComponentsInChildren<Transform>(true)) g.gameObject.layer=L;
var otras = new List<(Light,int)>(); foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(!l.transform.IsChildOf(raiz.transform)){ otras.Add((l,l.cullingMask)); l.cullingMask&=~(1<<L);}
var amb=RenderSettings.ambientMode; var ambC=RenderSettings.ambientLight; RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=H("#262B55");
Texture2D Render(int w,int h,Color bg){ var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32){antiAliasing=8}; cam.targetTexture=rt; cam.backgroundColor=bg; cam.aspect=(float)w/h; cam.Render(); var pr0=RenderTexture.active; RenderTexture.active=rt; var tx=new Texture2D(w,h,TextureFormat.RGBA32,false); tx.ReadPixels(new Rect(0,0,w,h),0,0); tx.Apply(); RenderTexture.active=pr0; cam.targetTexture=null; rt.Release(); Object.DestroyImmediate(rt); return tx; }
Texture2D Transparente(int w,int h){ var a=Render(w,h,Color.black); var b=Render(w,h,Color.white); var pa=a.GetPixels(); var pb=b.GetPixels();
  for(int i=0;i<pa.Length;i++){ float al=Mathf.Clamp01(1f-((pb[i].r-pa[i].r)+(pb[i].g-pa[i].g)+(pb[i].b-pa[i].b))/3f); pa[i]= al<0.003f? new Color(0,0,0,0) : new Color(Mathf.Clamp01(pa[i].r/al),Mathf.Clamp01(pa[i].g/al),Mathf.Clamp01(pa[i].b/al),al);} a.SetPixels(pa); a.Apply(); Object.DestroyImmediate(b); return a; }
// fondo del ícono: índigo con un resplandor suave detrás del átomo
Texture2D Fondo(Texture2D t){ int w=t.width,h=t.height; var px=t.GetPixels(); for(int y=0;y<h;y++)for(int x=0;x<w;x++){ float dx=(x-w*0.5f)/w, dy=(y-h*0.62f)/h; float g=Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy)*1.6f); var bg=Color.Lerp(H("#0E1029"),H("#1F2552"),g*g); var c=px[y*w+x]; px[y*w+x]=new Color(Mathf.Lerp(bg.r,c.r,c.a),Mathf.Lerp(bg.g,c.g,c.a),Mathf.Lerp(bg.b,c.b,c.a),1);} t.SetPixels(px); t.Apply(); return t; }
string salida = "";
try {
  Directory.CreateDirectory("Screenshots");
  var lg=Transparente(3600,1000); File.WriteAllBytes("Screenshots/AtomAR_Logo_3D_raw.png", lg.EncodeToPNG()); Object.DestroyImmediate(lg); salida="ok";
} finally {
  RenderSettings.ambientMode=amb; RenderSettings.ambientLight=ambC; foreach(var (l,m) in otras) l.cullingMask=m;
  foreach(var o in basura) if(o) Object.DestroyImmediate(o);
}
return salida;
