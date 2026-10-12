using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 // Ataka.Editor.NeighboursInstaller.Install  (batch: -executeMethod Ataka.Editor.NeighboursInstaller.Install)
 // pc29c: the 4.9 / 8.1 neighbours from geometry already in the project, nothing new modelled:
 //  - Tuzik: the Quaternius Animal Pack Dog (CC0, art-source/prologue/QuaterniusDog: rigged, Idle/Walking, no UVs) given
 //    UVs and an original ginger-mongrel texture in Blender (art-source/prologue/Tuzik: Tuzik.blend/.fbx, tuzik.png);
 //  - Valya: the Katcho granny model with an original recolour of its atlas (art-source/prologue/Valya/valya.png:
 //    raspberry quilted dressing gown, blue polka-dot kerchief over the hair, raspberry slippers). Her FACE IS THE SAME
 //    Katcho face as the grandmother's — a labelled stand-in until Valya has her own face.
 // Both become Resources/Characters/Tuzik.prefab and Valya.prefab (feet at the pivot, facing +Z, legacy idle looping),
 // which StoryDirector.Neighbours instantiates on the landing in 4.9 and 8.1.
 public static class NeighboursInstaller {
  const string Chars="Assets/_Project/Art/Characters/",Res="Assets/_Project/Resources/Characters/";
  [MenuItem("Ataka/Prologue/Install neighbours (Valya, Tuzik)")]
  public static void Install(){
   Directory.CreateDirectory(Res);AssetDatabase.Refresh();
   var granny=AssetDatabase.LoadAssetAtPath<Material>(Chars+"KatchoGranny/Granny.mat");if(!granny)throw new System.Exception("Granny.mat missing");
   // ---- Tuzik
   string fbx=Chars+"Tuzik/Tuzik.fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fbx);mi.animationType=ModelImporterAnimationType.Legacy;mi.importAnimation=true;mi.isReadable=true;mi.animationWrapMode=WrapMode.Loop;mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.SaveAndReimport();
   var tuzikMat=Mat(Chars+"Tuzik/Tuzik.mat",granny,Texture(Chars+"Tuzik/tuzik.png"));
   string tuzikReport=Build("Tuzik",AssetDatabase.LoadAssetAtPath<GameObject>(fbx),tuzikMat,.42f,"Idle",true,Vector3.one);
   // ---- Valya: «крупная» — a little wider than the grandmother, 1.60 m
   var valyaMat=Mat(Chars+"Valya/Valya.mat",granny,Texture(Chars+"Valya/valya.png"));
   string valyaReport=Build("Valya",AssetDatabase.LoadAssetAtPath<GameObject>(Chars+"KatchoGranny/Granny.fbx"),valyaMat,1.60f,"Oidle",false,new Vector3(1.1f,1,1.08f));
   AssetDatabase.SaveAssets();
   Directory.CreateDirectory("QA/Prologue");File.WriteAllText("QA/Prologue/neighbours.txt",tuzikReport+"\n"+valyaReport+"\nValya's face is the grandmother's Katcho face (stand-in)\n");
   Debug.Log("NEIGHBOURS_INSTALLED\n"+File.ReadAllText("QA/Prologue/neighbours.txt"));
  }
  static Texture2D Texture(string path){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=true;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=true;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  static Material Mat(string path,Material template,Texture2D tex){var m=new Material(template){name=Path.GetFileNameWithoutExtension(path)};m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(m,path);return m;}
  // An instance of the model under an empty root: its materials replaced, a legacy idle looping, scaled to `height`
  // (and `shape` on top), feet on the pivot; a four-legged model is turned so its head end points along +Z.
  static string Build(string name,GameObject model,Material mat,float height,string idleSuffix,bool quadruped,Vector3 shape){
   if(!model)throw new System.Exception(name+": model missing");
   var root=new GameObject(name);var calibration=new GameObject("Model calibration").transform;calibration.SetParent(root.transform,false);var inst=(GameObject)PrefabUtility.InstantiatePrefab(model);inst.transform.SetParent(calibration,false);
   foreach(var r in inst.GetComponentsInChildren<Renderer>(true)){var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++)ms[i]=mat;r.sharedMaterials=ms;}
   var anim=inst.GetComponentInChildren<Animation>(true);AnimationClip idle=null;
   if(anim){foreach(AnimationState s in anim)if(s.name.EndsWith(idleSuffix)){idle=s.clip;break;}if(!idle)idle=AnimationUtility.GetAnimationClips(anim.gameObject).FirstOrDefault(c=>c.name.EndsWith(idleSuffix));}
   if(!idle)idle=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<AnimationClip>().FirstOrDefault(c=>c.name.EndsWith(idleSuffix)&&!c.name.StartsWith("__preview"));
   if(!anim)anim=inst.AddComponent<Animation>();
   if(idle){if(!anim.GetClip(idle.name))anim.AddClip(idle,idle.name);anim.clip=idle;anim.playAutomatically=true;anim.wrapMode=WrapMode.Loop;anim.cullingType=AnimationCullingType.AlwaysAnimate;idle.SampleAnimation(anim.gameObject,0);}
   var pts=Points(inst,root.transform);
   if(quadruped){
    // the head end of the longest horizontal axis is the one that rises higher
    var b=Bounds(pts);bool alongX=b.size.x>b.size.z;float lo=alongX?b.min.x:b.min.z,hi=alongX?b.max.x:b.max.z,cut=(hi-lo)*.25f;
    float topLo=pts.Where(p=>(alongX?p.x:p.z)<lo+cut).Select(p=>p.y).DefaultIfEmpty(0).Max(),topHi=pts.Where(p=>(alongX?p.x:p.z)>hi-cut).Select(p=>p.y).DefaultIfEmpty(0).Max();
    var head=alongX?(topHi>topLo?Vector3.right:Vector3.left):(topHi>topLo?Vector3.forward:Vector3.back);
    calibration.localRotation=Quaternion.FromToRotation(head,Vector3.forward);pts=Points(inst,root.transform);}
   var bb=Bounds(pts);float k=height/Mathf.Max(.01f,bb.size.y);calibration.localScale=Vector3.one*k;
   pts=Points(inst,root.transform);bb=Bounds(pts);calibration.localPosition-=new Vector3(bb.center.x,bb.min.y,bb.center.z);
   pts=Points(inst,root.transform);bb=Bounds(pts);root.transform.localScale=shape;bb.size=Vector3.Scale(bb.size,shape);
   string path=Res+name+".prefab";AssetDatabase.DeleteAsset(path);PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
   return name+": "+path+" size "+bb.size.ToString("F2")+" idle "+(idle?idle.name:"NONE (static)")+" renderers "+model.GetComponentsInChildren<Renderer>(true).Length+" animated root curves "+(idle?string.Join(",",AnimationUtility.GetCurveBindings(idle).Where(b=>b.path=="").Select(b=>b.propertyName)):"none");}
  // Exact weighted bind-pose skinning in root space, avoiding BakeMesh scale compensation ambiguity.
  static System.Collections.Generic.List<Vector3> Points(GameObject inst,Transform root){var list=new System.Collections.Generic.List<Vector3>();
   foreach(var s in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)){if(!s.sharedMesh)continue;var vs=s.sharedMesh.vertices;var ws=s.sharedMesh.boneWeights;var binds=s.sharedMesh.bindposes;var bones=s.bones;var matrices=new Matrix4x4[bones.Length];for(int j=0;j<bones.Length;j++)matrices[j]=bones[j].localToWorldMatrix*binds[j];for(int j=0;j<vs.Length;j++){var w=ws[j];var v=matrices[w.boneIndex0].MultiplyPoint3x4(vs[j])*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(vs[j])*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(vs[j])*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(vs[j])*w.weight3;list.Add(root.InverseTransformPoint(v));}}
   foreach(var f in inst.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh)foreach(var v in f.sharedMesh.vertices)list.Add(root.InverseTransformPoint(f.transform.TransformPoint(v)));
   return list;}
  static Bounds Bounds(System.Collections.Generic.List<Vector3> pts){var b=new Bounds(pts.Count>0?pts[0]:Vector3.zero,Vector3.zero);foreach(var p in pts)b.Encapsulate(p);return b;}
 }
}
