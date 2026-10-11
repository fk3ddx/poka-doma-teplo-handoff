using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Unity.Cinemachine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class ScenarioBuilder {
  const string Root="Assets/_Project/Scenario",Art="Assets/_Project/Art",Lab="Assets/_Project/LightLab";
  static Transform shell;static Material wall,wood,floor,fabric,metal;static FlatBindings flat;
  static T Save<T>(T value,string name) where T:UnityEngine.Object{string path=Root+"/"+name;var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;}AssetDatabase.CreateAsset(value,path);return value;}
  static GameObject Box(string name,Vector3 position,Vector3 size,Material mat,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent?parent:shell,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
  static Material Tint(string name,Material basis,Color color){var m=new Material(basis);m.SetColor("_BaseColor",color);return Save(m,name+".mat");}
  static Transform Point(string name,Vector3 p,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:shell,false);t.localPosition=p;return t;}
  static void WallZ(string name,float x0,float x1,float z,float thick,params Vector4[] openings){
   float cursor=x0;foreach(var gap in openings.OrderBy(x=>x.x)){
    if(gap.x>cursor)Box(name,new Vector3((cursor+gap.x)/2,1.25f,z),new Vector3(gap.x-cursor,2.5f,thick),wall);
    if(gap.z>0)Box(name+" below",new Vector3((gap.x+gap.y)/2,gap.z/2,z),new Vector3(gap.y-gap.x,gap.z,thick),wall);
    if(gap.w<2.5f)Box(name+" lintel",new Vector3((gap.x+gap.y)/2,(gap.w+2.5f)/2,z),new Vector3(gap.y-gap.x,2.5f-gap.w,thick),wall);cursor=gap.y;
   }if(cursor<x1)Box(name,new Vector3((cursor+x1)/2,1.25f,z),new Vector3(x1-cursor,2.5f,thick),wall);
  }
  static void WallX(string name,float x,float z0,float z1,float thick,params Vector4[] openings){
   float cursor=z0;foreach(var gap in openings.OrderBy(x=>x.x)){
    if(gap.x>cursor)Box(name,new Vector3(x,1.25f,(cursor+gap.x)/2),new Vector3(thick,2.5f,gap.x-cursor),wall);
    if(gap.w<2.5f)Box(name+" lintel",new Vector3(x,(gap.w+2.5f)/2,(gap.x+gap.y)/2),new Vector3(thick,2.5f-gap.w,gap.y-gap.x),wall);cursor=gap.y;
   }if(cursor<z1)Box(name,new Vector3(x,1.25f,(cursor+z1)/2),new Vector3(thick,2.5f,z1-cursor),wall);
  }
  static ScenarioDoor Door(string name,Vector3 pos,float width,float yaw){var t=Point(name,pos);t.localRotation=Quaternion.Euler(0,yaw,0);var door=t.gameObject.AddComponent<ScenarioDoor>();var hinge=Point("Hinge",new Vector3(-width/2,-1,0),t);Box("Architectural door leaf",new Vector3(width/2,1,0),new Vector3(width,2,.045f),wood,hinge);door.leaf=hinge;return door;}
  static Light Lamp(string name,Vector3 p,Color c,float intensity,float range,Transform parent=null){var t=Point(name,p,parent);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;l.shadowBias=.035f;l.shadowNormalBias=.15f;return l;}
  static GameObject Prop(string id,Vector3 p,Vector3 size,float yaw=0){
   var ready=ScenarioReadyArt.TryPlace(id,p,size,yaw,shell);if(ready)return ready;
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/ScenarioProxies/Kenney/"+id+".fbx");if(!source)throw new Exception("Missing ready-made proxy: "+id);
   var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.name="PROXY Kenney "+id;g.transform.SetParent(shell,false);g.transform.rotation=Quaternion.Euler(0,yaw,0)*g.transform.rotation;
   var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);
   // Author's FBX materials use Standard: map their existing texture/color to URP, don't flatten everything to one color.
   foreach(var rr in rs){var mats=rr.sharedMaterials;for(int i=0;i<mats.Length;i++){var src=mats[i];string matPath=Root+"/Kenney_"+src.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));if(src.HasProperty("_Color"))m.color=src.color;if(src.mainTexture)m.mainTexture=src.mainTexture;else m.mainTexture=wood.mainTexture;m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,matPath);}mats[i]=m;}rr.sharedMaterials=mats;}
   g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z)));
   b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);g.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);
   b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);
   var collider=g.AddComponent<BoxCollider>();var local=new Bounds(g.transform.InverseTransformPoint(b.center),Vector3.zero);foreach(var rr in rs){var mb=rr.GetComponent<MeshFilter>();if(mb)local.Encapsulate(g.transform.InverseTransformPoint(rr.bounds.min));local.Encapsulate(g.transform.InverseTransformPoint(rr.bounds.max));}collider.center=local.center;collider.size=local.size;
   return g;
  }
  static TextMesh Label(string text,Vector3 pos,float scale,Transform parent=null){var t=Point("Text "+text,pos,parent);var l=t.gameObject.AddComponent<TextMesh>();l.text=text;l.fontSize=72;l.characterSize=scale;l.anchor=TextAnchor.MiddleCenter;l.color=new Color(.25f,.015f,.01f);l.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.gameObject.AddComponent<ScenarioWallWriting>().shader=Shader.Find("Ataka/DepthText");return l;}
  static GameObject Haven(string id,Vector3 position,float height,Transform parent=null){
   string folder=Art+"/ScenarioProxies/PolyHaven/"+id;var src=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+id+".fbx");if(!src)throw new Exception("Missing CC0 model "+id);
   var g=(GameObject)PrefabUtility.InstantiatePrefab(src);g.name="Poly Haven "+id;g.transform.SetParent(parent?parent:shell,false);
   var rs=g.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);g.transform.localScale*=height/bounds.size.y;
   bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);g.transform.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   var texture=AssetDatabase.FindAssets("t:Texture2D",new[]{folder}).Select(x=>AssetDatabase.GUIDToAssetPath(x)).First(x=>x.Contains("_diff_"));
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);mat.SetFloat("_Smoothness",.2f);mat=Save(mat,id+"_URP.mat");
   var glass=new Material(Shader.Find("Universal Render Pipeline/Lit"));glass.SetColor("_BaseColor",new Color(.5f,.6f,.62f,.05f));glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetShaderPassEnabled("ShadowCaster",false);glass.renderQueue=3000;glass=Save(glass,id+"_Glass.mat");
   foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.ToLowerInvariant().Contains("glass")?glass:mat).ToArray();return g;
  }
  static ClockFace Clock(Vector3 p,Transform parent=null){
   var holder=Point("Grandfather clock",p,parent);var ready=Haven("vintage_grandfather_clock_01",holder.TransformPoint(new Vector3(0,-.8f,0)),1.6f,holder);
   var clock=holder.gameObject.AddComponent<ClockFace>();var all=ready.GetComponentsInChildren<Transform>();
   var minute=all.First(x=>x.name.EndsWith("MinuteHand"));var hour=all.First(x=>x.name.EndsWith("HourHand"));
   // The author's dial defines the front. Orient it into the room, not into the wall.
   var bounds=ready.GetComponentsInChildren<Renderer>().First().bounds;foreach(var r in ready.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
   if(holder.InverseTransformPoint(minute.position).z>holder.InverseTransformPoint(bounds.center).z)ready.transform.Rotate(0,180,0,Space.World);
   Transform Pivot(Transform model,string name){
    var pivot=Point(name,holder.InverseTransformPoint(model.position),holder);model.SetParent(pivot,true);
    var mf=model.GetComponent<MeshFilter>();if(mf){Vector3 tip=Vector3.zero;foreach(var v in mf.sharedMesh.vertices){var point=pivot.InverseTransformPoint(model.TransformPoint(v));if(point.x*point.x+point.y*point.y>tip.x*tip.x+tip.y*tip.y)tip=point;}model.RotateAround(pivot.position,pivot.forward,Mathf.Atan2(tip.x,tip.y)*Mathf.Rad2Deg);}
    return pivot;
   }
   clock.hour=Pivot(hour,"Hour axis");clock.minute=Pivot(minute,"Minute axis");
   clock.second=Point("Second axis",holder.InverseTransformPoint(minute.position)+Vector3.back*.002f,holder);Stroke(clock.second,Vector3.down*.02f,Vector3.up*.085f,.002f);
   clock.pendulum=Point("Pendulum pivot",new Vector3(0,.05f,-.16f),holder);Stroke(clock.pendulum,Vector3.zero,Vector3.down*.6f,.009f);Stroke(Point("Pendulum bob",Vector3.down*.6f,clock.pendulum),Vector3.left*.05f,Vector3.right*.05f,.04f);
   var key=Point("Winding key",new Vector3(.08f,.27f,-.17f),holder);Stroke(key,Vector3.down*.025f,Vector3.up*.025f,.008f);Stroke(key,Vector3.left*.02f,Vector3.right*.02f,.008f);
   return clock;
  }
  static void Stroke(Transform root,Vector3 a,Vector3 b,float width){var t=Point("Mechanism stroke",Vector3.zero,root);var line=t.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.SetPositions(new[]{a,b});line.startWidth=line.endWidth=width;line.numCapVertices=3;line.sharedMaterial=metal;line.shadowCastingMode=ShadowCastingMode.Off;}
  static TimelineAsset Timeline(string name,string kind,float duration){
   var path=Root+"/"+name+".playable";var t=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
   if(!t){t=ScriptableObject.CreateInstance<TimelineAsset>();AssetDatabase.CreateAsset(t,path);}
   foreach(var old in t.GetRootTracks().ToArray())if(old)t.DeleteTrack(old);
   // Persist the owning asset before creating tracks so Timeline embeds all sub-assets.
   var track=t.CreateTrack<ScenarioCueTrack>(null,kind);var clip=track.CreateClip<ScenarioCue>();clip.start=0;clip.duration=duration;((ScenarioCue)clip.asset).scene=kind;
   if(!AssetDatabase.Contains(track))AssetDatabase.AddObjectToAsset(track,t);if(!AssetDatabase.Contains(clip.asset))AssetDatabase.AddObjectToAsset(clip.asset,t);
   EditorUtility.SetDirty(track);EditorUtility.SetDirty(clip.asset);EditorUtility.SetDirty(t);return t;
  }
  static void Models(){
   foreach(string name in new[]{"Superhero_Female_FullBody","Superhero_Male_FullBody"}){var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/ScenarioProxies/Quaternius/"+name+".fbx");if(importer.animationType!=ModelImporterAnimationType.Human){importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();}}
  }
  static GameObject Person(string label,Vector3 pos,float height){var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/ScenarioProxies/Quaternius/Superhero_Female_FullBody.fbx");var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.name=label;var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);g.transform.localScale*=height/b.size.y;g.transform.position=pos;var mat=Tint("ProxyCharacter",fabric,new Color(.38f,.36f,.3f));foreach(var rr in rs)rr.sharedMaterials=rr.sharedMaterials.Select(_=>mat).ToArray();var a=g.GetComponent<Animator>();if(a)a.enabled=false;g.AddComponent<ProxyPose>();return g;}
  static CloseupHand Hand(bool left){
   var g=Person("TEMP GuestHands_Closeup "+(left?"L":"R"),Vector3.zero,2.3f);UnityEngine.Object.DestroyImmediate(g.GetComponent<ProxyPose>());
   var animator=g.GetComponent<Animator>();animator.enabled=true;animator.Rebind();animator.Update(0);
   var hand=g.AddComponent<CloseupHand>();hand.left=left;hand.palm=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);hand.forearm=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);animator.enabled=false;
   foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);var weights=mesh.boneWeights;var keep=r.bones.Select(b=>b==hand.forearm||b==hand.palm||b.IsChildOf(hand.palm)).ToArray();
    bool Selected(int vertex){var w=weights[vertex];return (w.weight0>.2f&&keep[w.boneIndex0])||(w.weight1>.2f&&keep[w.boneIndex1])||(w.weight2>.2f&&keep[w.boneIndex2])||(w.weight3>.2f&&keep[w.boneIndex3]);}
    var triangles=mesh.triangles;var clipped=new List<int>();for(int i=0;i<triangles.Length;i+=3)if(Selected(triangles[i])&&Selected(triangles[i+1])&&Selected(triangles[i+2])){clipped.Add(triangles[i]);clipped.Add(triangles[i+1]);clipped.Add(triangles[i+2]);}
    mesh.subMeshCount=1;mesh.SetTriangles(clipped,0);r.sharedMesh=Save(mesh,"Hand_"+(left?"L_":"R_")+r.name+".asset");r.sharedMaterials=new[]{r.sharedMaterial};r.updateWhenOffscreen=true;
   }
   g.SetActive(false);return hand;
  }
  public static void Generate(){
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();Models();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   wall=AssetDatabase.LoadAssetAtPath<Material>(Lab+"/Wall_plaster.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(Lab+"/Wood.mat");floor=AssetDatabase.LoadAssetAtPath<Material>(Lab+"/Parquet.mat");fabric=AssetDatabase.LoadAssetAtPath<Material>(Lab+"/Linen.mat");metal=Tint("Metal",wall,new Color(.22f,.23f,.24f));
   shell=new GameObject("Flat geometry — SCENARIO section 1").transform;flat=new GameObject("Flat bindings").AddComponent<FlatBindings>();flat.apartment=shell;
   // Split the west service-room band so the endless hall can replace it without coplanar floors.
   foreach(var band in new[]{new Vector4(-.35f,9.15f,-.35f,4.5f),new Vector4(4.7f,9.15f,4.5f,6.35f),new Vector4(-.35f,4.7f,4.5f,6.35f)}){
    var center=new Vector3((band.x+band.y)/2,0,(band.z+band.w)/2);var size=new Vector3(band.y-band.x,.24f,band.w-band.z);
    Box("Continuous subfloor section",center+Vector3.down*.12f,size,floor);size.y=.2f;Box("Ceiling section 2.5m",center+Vector3.up*2.6f,size,wall);
   }
   WallX("Exterior west north",-.175f,-.35f,4.5f,.35f);WallX("Exterior west chase overlap",-.175f,4.5f,6.35f,.35f);WallX("Exterior east",8.975f,-.35f,6.35f,.35f);
   WallZ("Facade",0,8.8f,-.175f,.35f,new Vector4(.75f,2.05f,.85f,2.3f),new Vector4(3.25f,4.65f,.85f,2.3f),new Vector4(4.8f,5.55f,0,2),new Vector4(7.1f,8.3f,.85f,2.3f));
   WallZ("South",0,8.8f,6.175f,.35f,new Vector4(7.6f,8.5f,0,2.05f));
   WallX("Bedroom partition",2.85f,0,4.5f,.1f,new Vector4(3.35f,4.15f,0,2));WallX("Living east",6.55f,0,4.5f,.1f);
   WallZ("Bedroom south",0,2.9f,4.45f,.1f,new Vector4(.45f,1.35f,0,2.05f));WallZ("Living south",2.9f,6.5f,4.45f,.1f,new Vector4(5.25f,6.05f,0,2));
   WallZ("Kitchen south",6.6f,8.8f,2.95f,.1f,new Vector4(6.9f,7.6f,0,2));WallX("Bathroom east",4.65f,4.5f,6,.1f,new Vector4(4.9f,5.6f,0,2));WallX("Store east",2.85f,4.5f,6,.1f);
   flat.entrance=Door("Entrance60",new Vector3(8.05f,1,6.03f),.9f,180);flat.kitchen=Door("Kitchen door",new Vector3(7.25f,1,2.95f),.7f,0);flat.bath=Door("Bathroom door",new Vector3(4.65f,1,5.25f),.7f,90);flat.bedroom=Door("Bedroom door",new Vector3(2.85f,1,3.75f),.8f,90);flat.bedroom.open=true;
   flat.entrance.direction=-1;flat.living=Door("Living frosted doorway — temporary leaf",new Vector3(5.65f,1,4.45f),.8f,0);flat.living.direction=1;flat.living.open=true;
   flat.carpet=Door("Carpet door",new Vector3(.9f,1,4.45f),.9f,0);flat.carpet.direction=1;
   flat.vestibuleInner=Door("Vestibule second closed door",new Vector3(.9f,1,5.5f),.9f,0);flat.vestibuleInner.locked=true;
   flat.sourceVestibule=Point("Source local frame",new Vector3(.9f,0,4.975f));flat.destinationVestibule=Point("Destination local frame",new Vector3(8.05f,0,6.555f));flat.destinationVestibule.rotation=Quaternion.Euler(0,180,0);
   foreach(var frame in new[]{flat.sourceVestibule,flat.destinationVestibule}){
    Box("Vestibule floor",new Vector3(0,-.045f,0),new Vector3(1.1f,.1f,1.05f),floor,frame);
    Box("Vestibule ceiling",new Vector3(0,2.55f,0),new Vector3(1.1f,.1f,1.05f),wood,frame);
    foreach(float x in new[]{-.5f,.5f})Box("Vestibule lining",new Vector3(x,1.25f,0),new Vector3(.1f,2.5f,1.05f),wood,frame);
    foreach(float z in new[]{-.525f,.525f})Box("Vestibule lintel",new Vector3(0,2.275f,z),new Vector3(1.1f,.45f,.05f),wood,frame);
   }
   var outerDoor=Door("Entry vestibule closed outer door",new Vector3(8.05f,1,7.08f),.9f,180);outerDoor.locked=true;
   foreach(var frame in new[]{flat.sourceVestibule,flat.destinationVestibule,flat.carpet.transform,flat.vestibuleInner.transform,flat.entrance.transform,outerDoor.transform})foreach(var surface in frame.GetComponentsInChildren<Renderer>())surface.renderingLayerMask=2;
   foreach(var win in new[]{new Vector2(.75f,2.05f),new Vector2(3.25f,4.65f),new Vector2(7.1f,8.3f)}){float mid=(win.x+win.y)/2;foreach(float x in new[]{win.x,mid,win.y})Box("Window frame",new Vector3(x,1.575f,-.18f),new Vector3(.045f,1.45f,.16f),wood);foreach(float y in new[]{.85f,1.85f,2.3f})Box("Window transom",new Vector3(mid,y,-.18f),new Vector3(win.y-win.x,.045f,.16f),wood);Box("Window sill",new Vector3(mid,.82f,-.02f),new Vector3(win.y-win.x+.15f,.06f,.4f),wood);}
   flat.balcony=Point("Balcony interaction",ScenarioLayout.BalconyDoor);flat.balconyDoor=Door("Bound balcony door",ScenarioLayout.BalconyDoor,.75f,0);flat.balconyDoor.locked=true;
   flat.balconyDoor.leaf.GetComponentInChildren<Renderer>().enabled=false;
   foreach(float x in new[]{.025f,.725f})Box("Balcony stile",new Vector3(x,1,0),new Vector3(.05f,2,.06f),wood,flat.balconyDoor.leaf);
   foreach(float y in new[]{.025f,.8f,1.975f})Box("Balcony rail",new Vector3(.375f,y,0),new Vector3(.7f,.05f,.06f),wood,flat.balconyDoor.leaf);
   Box("Balcony lower panel",new Vector3(.375f,.4f,0),new Vector3(.7f,.75f,.04f),wood,flat.balconyDoor.leaf);
   var glass=new Material(Shader.Find("Universal Render Pipeline/Lit"));glass.SetColor("_BaseColor",new Color(.35f,.46f,.52f,.14f));glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_Cull",0);glass.renderQueue=3000;glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass=Save(glass,"BalconyGlass.mat");var pane=Box("Balcony glass",new Vector3(.375f,1.4f,0),new Vector3(.65f,1.13f,.008f),glass,flat.balconyDoor.leaf);pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   Box("Balcony slab",new Vector3(4.7f,-.1f,-.83f),new Vector3(3.5f,.2f,.95f),wall);Box("Balcony parapet",new Vector3(4.7f,.45f,-1.3f),new Vector3(3.5f,.9f,.15f),wall);
   Furnish();CreateLook();CreatePlayer();CreateEvents();CreateCorridor();
   Physics.SyncTransforms();var overlap=new Bounds(new Vector3(2.1725f,1.25f,5.25f),new Vector3(5.045f,3,1.49f));
   flat.chaseOverlapRenderers=flat.apartment.GetComponentsInChildren<Renderer>(true).Where(r=>r.bounds.Intersects(overlap)).ToArray();flat.chaseOverlapColliders=flat.apartment.GetComponentsInChildren<Collider>(true).Where(c=>c.bounds.Intersects(overlap)).ToArray();
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Flat.unity");Debug.Log("SCENARIO_FLAT_GENERATED");
  }
  static void Furnish(){
   Prop("coatRack",new Vector3(8.62f,1.2f,5.1f),new Vector3(.12f,.75f,1));Prop("bench",new Vector3(8.48f,0,5.1f),new Vector3(.45f,.42f,1));
   Prop("cabinetBedDrawer",ScenarioLayout.Dresser,new Vector3(.55f,.8f,.85f),90);Prop("bathroomMirror",new Vector3(6.66f,.85f,3.7f),new Vector3(.05f,1.2f,.6f),90);
   flat.phone=Point("Phone interaction",new Vector3(7.05f,1,3.7f));Prop("radio",new Vector3(7.03f,.8f,3.7f),new Vector3(.25f,.15f,.2f));flat.torchPickup=Haven("signal_flashlight",new Vector3(7.15f,.86f,3.85f),.14f).transform;
   flat.clock=Clock(ScenarioLayout.Clock);Prop("lampWall",new Vector3(5.5f,1.95f,5.85f),new Vector3(.25f,.25f,.22f));
   Prop("table",new Vector3(7.65f,0,.75f),new Vector3(1,.74f,.65f));Prop("stoolBar",new Vector3(7.15f,0,1.2f),new Vector3(.32f,.45f,.32f));flat.stool=BedroomReadyProps.Create(flat.apartment,new Vector3(5.5f,0,5.8f));
   Prop("kitchenStove",new Vector3(8.5f,0,1.7f),new Vector3(.55f,.85f,.6f),90);Prop("kitchenFridge",new Vector3(8.25f,0,2.55f),new Vector3(.6f,1.55f,.6f));Prop("kitchenSink",new Vector3(6.9f,0,1.6f),new Vector3(.6f,.85f,.9f),90);Prop("kitchenCabinetUpper",new Vector3(6.82f,1.5f,1.6f),new Vector3(.35f,.65f,.9f),90);
   Prop("radio",new Vector3(8.73f,1.45f,2),new Vector3(.1f,.28f,.35f),90);Prop("plantSmall1",new Vector3(7.35f,.85f,0),new Vector3(.2f,.35f,.2f));
   Prop("loungeSofa",new Vector3(3.42f,0,1.9f),new Vector3(.8f,.85f,1.9f),90);Prop("rugRectangle",new Vector3(4.65f,.005f,2.3f),new Vector3(2.4f,.02f,2.6f));
   Prop("bookcaseOpen",new Vector3(6.15f,0,1.55f),new Vector3(.55f,1.9f,.7f),-90);Prop("cabinetTelevision",new Vector3(6.15f,0,2.5f),new Vector3(.55f,.65f,1.1f),-90);Prop("bookcaseClosedDoors",new Vector3(6.15f,0,3.5f),new Vector3(.55f,1.9f,.6f),-90);
   flat.television=Point("TV interaction",ScenarioLayout.Television);Prop("televisionVintage",new Vector3(6.1f,.65f,2.5f),new Vector3(.45f,.45f,.6f),-90);
   flat.televisionScreen=Box("TV temporary screen",new Vector3(5.84f,.9f,2.5f),new Vector3(.015f,.3f,.4f),metal).GetComponent<Renderer>();
   Prop("tableCoffee",new Vector3(4.7f,0,2.15f),new Vector3(.6f,.45f,.85f),90);Prop("books",new Vector3(4.7f,.45f,2.15f),new Vector3(.2f,.08f,.2f));Prop("loungeChair",new Vector3(3.5f,0,.6f),new Vector3(.75f,.85f,.75f),150);Prop("lampRoundFloor",new Vector3(3.1f,0,.55f),new Vector3(.35f,1.6f,.35f));
   flat.bed=Point("Under bed interaction",new Vector3(1.05f,.25f,2.2f));Prop("bedSingle",ScenarioLayout.Bed,new Vector3(.95f,.65f,1.9f));Prop("pillow",new Vector3(.6f,.65f,1.1f),new Vector3(.6f,.25f,.45f));Prop("cabinetBed",new Vector3(.4f,0,3.1f),new Vector3(.5f,.55f,.4f));
   flat.wardrobe=Prop("bookcaseClosedDoors",BedroomLayoutRevision.OldCenter,new Vector3(1,1.9f,.55f)).transform;
   flat.wardrobeLeaves=new[]{Door("Wardrobe left temporary leaf",new Vector3(1.8f,.95f,3.78f),.48f,0).leaf,Door("Wardrobe right temporary leaf",new Vector3(2.3f,.95f,3.78f),.48f,0).leaf};
   flat.slipper=Prop("pillow",new Vector3(1.85f,.01f,3.4f),new Vector3(.11f,.045f,.23f));
   Prop("bathtub",new Vector3(3.3f,0,5.4f),new Vector3(.7f,.65f,1.3f));Prop("bathroomSink",new Vector3(4.22f,0,4.8f),new Vector3(.5f,.75f,.42f));Prop("washer",new Vector3(4.2f,0,5.7f),new Vector3(.45f,.65f,.45f));
   flat.conditionGroups=new GameObject[6];for(int i=0;i<6;i++)flat.conditionGroups[i]=new GameObject("Lap "+(i+1)+" condition props");
   var snow=new Material(wall);snow.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/ScenarioProxies/PolyHaven/snow_01/snow_01.jpg"));snow.SetColor("_BaseColor",new Color(.55f,.65f,.72f));snow=Save(snow,"ColdWindowEdges.mat");
   for(int lap=1;lap<=2;lap++)foreach(var window in new[]{new Vector2(.75f,2.05f),new Vector2(3.25f,4.65f),new Vector2(7.1f,8.3f)}){float width=lap==1?.045f:.095f;foreach(float x in new[]{window.x+width/2,window.y-width/2})Box("Frost at window edge",new Vector3(x,1.575f,-.07f),new Vector3(width,1.43f,.003f),snow,flat.conditionGroups[lap].transform);Box("Frost at window sill",new Vector3((window.x+window.y)/2,.87f,-.07f),new Vector3(window.y-window.x,width,.003f),snow,flat.conditionGroups[lap].transform);}
   flat.dressing=flat.gameObject.AddComponent<ScenarioDressing>();flat.dressing.walls=shell.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==wall).ToArray();flat.dressing.normal=wall.GetTexture("_BaseMap");flat.dressing.original=wall.GetColor("_BaseColor");flat.dressing.decay=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/ScenarioProxies/PolyHaven/worn_mossy_plasterwall/worn_mossy_plasterwall.jpg");
   var red=Tint("RedWriting",wall,new Color(.25f,.015f,.01f));for(int i=0;i<3;i++){var text=Label("ПОМНИ",new Vector3(8.7f,1.5f,3.5f+i*.8f),.045f,flat.conditionGroups[5].transform);text.transform.rotation=Quaternion.Euler(0,90,0);}
   flat.guest=Person("TEMP Guest — ready-made CC0 rig",new Vector3(0,-20,0),2.3f);flat.grandmother=NinaScenarioInstaller.CreateActor();BedroomLayoutRevision.Relocate(flat);
  }
  static void CreateLook(){
   var renderer=Save(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Lab+"/LightLab_Renderer.asset")),"Scenario_Renderer.asset");var material=Save(new Material(Shader.Find("Ataka/ScenarioVHS")),"ScenarioVHS.mat");var features=renderer.rendererFeatures.ToArray();renderer.rendererFeatures.Clear();foreach(var source in features){var f=UnityEngine.Object.Instantiate(source);AssetDatabase.AddObjectToAsset(f,renderer);if(f is FullScreenPassRendererFeature full)full.passMaterial=material;renderer.rendererFeatures.Add(f);f.Create();}EditorUtility.SetDirty(renderer);
   var pipe=Save(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Lab+"/LightLab_URP.asset")),"Scenario_URP.asset");var serialized=new SerializedObject(pipe);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.FindProperty("m_SupportsLightLayers").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipe);
   var volume=new GameObject("Scenario grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Lab+"/LightLabVolume.asset");
   var lighting=new GameObject("Scenario lighting always active").transform;var look=lighting.gameObject.AddComponent<ScenarioLook>();look.pipeline=pipe;look.vhs=material;look.volume=volume;flat.look=look;
   var warm=new Color(1,.706f,.42f);look.warm=new[]{Lamp("HallA ceiling",new Vector3(7.7f,2.3f,4.5f),warm,.08f,3,lighting),Lamp("HallB sconce",new Vector3(5.5f,2,5.7f),warm,.08f,3,lighting),Lamp("Kitchen ceiling",new Vector3(7.7f,2.3f,1.5f),warm,0,3,lighting),Lamp("Living ceiling",new Vector3(4.7f,2.3f,2.1f),warm,0,4,lighting),Lamp("Floor lamp",new Vector3(3.1f,1.45f,.55f),warm,0,3.4f,lighting)};
   look.cold=new Light[3];float[] centers={1.4f,3.95f,7.7f};for(int i=0;i<3;i++){var l=Lamp("Window cold cookie "+i,new Vector3(centers[i]-.45f,2.25f,-.8f),new Color(.43f,.60f,.85f),3.24f,10,lighting);l.type=LightType.Spot;l.spotAngle=96;l.innerSpotAngle=75;l.cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(Lab+"/WindowCookie.asset");l.transform.LookAt(new Vector3(centers[i]+.5f,.8f,3));look.cold[i]=l;}
   look.icon=Lamp("Icon flame",new Vector3(.15f,1.6f,.15f),new Color(1,.05f,.015f),.14f,2,lighting);flat.televisionLight=Lamp("TV glow",new Vector3(5.8f,1,2.5f),new Color(.4f,.6f,1),0,2,lighting);
   RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(12/255f,14/255f,20/255f);RenderSettings.reflectionIntensity=.1f;RenderSettings.fog=false;
  }
  static void CreatePlayer(){
   var actor=new GameObject("Misha — eye1.55m");actor.transform.position=ScenarioLayout.Spawn;actor.transform.rotation=Quaternion.Euler(0,180,0);actor.layer=2;var body=actor.AddComponent<CharacterController>();body.height=1.7f;body.radius=.22f;body.center=Vector3.up*.85f;body.skinWidth=.015f;body.stepOffset=.1f;
   var player=actor.AddComponent<ScenarioPlayer>();flat.player=player;var head=new GameObject("Head").transform;head.SetParent(actor.transform,false);head.localPosition=Vector3.up*1.55f;player.head=head;
   var cine=head.gameObject.AddComponent<CinemachineCamera>();var lens=cine.Lens;lens.FieldOfView=65;lens.NearClipPlane=.03f;lens.FarClipPlane=80;cine.Lens=lens;
   var cameraObject=new GameObject("Camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=~(1<<31);camera.allowHDR=true;camera.backgroundColor=new Color(.012f,.02f,.03f);camera.clearFlags=CameraClearFlags.SolidColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;cameraObject.AddComponent<CinemachineBrain>();cameraObject.AddComponent<AudioListener>();player.view=camera;
   player.flashlight=Lamp("Dynamo flashlight",Vector3.zero,new Color(1,.89f,.72f),0,11,head);player.flashlight.type=LightType.Spot;player.flashlight.spotAngle=50;player.flashlight.innerSpotAngle=24;player.flashlight.GetUniversalAdditionalLightData().renderingLayers=(RenderingLayerMask)3u;
   var canvasObject=new GameObject("Subtitles before VHS");var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;canvasObject.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
   var loop=new GameObject("LoopDirector").AddComponent<LoopDirector>();loop.flat=flat;loop.laps=new LapData[6];for(int i=0;i<6;i++){var lap=ScriptableObject.CreateInstance<LapData>();lap.number=i+1;lap.minute=7-i;lap.gate=(LapGate)i;lap.condition=(FlatCondition)i;loop.laps[i]=Save(lap,"Lap"+(i+1)+".asset");}
   Text TextLine(string name,float y){var g=new GameObject(name);g.transform.SetParent(canvas.transform,false);var text=g.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;var rect=text.rectTransform;rect.anchorMin=new Vector2(0,y);rect.anchorMax=new Vector2(1,y+.1f);rect.offsetMin=rect.offsetMax=Vector2.zero;return text;}
   loop.subtitle=TextLine("Thought",.06f);loop.prompt=TextLine("Interaction",.38f);
   var sound=loop.gameObject.AddComponent<ScenarioSound>();sound.bank=AssetDatabase.LoadAssetAtPath<ScenarioSoundBank>("Assets/_Project/Audio/Recorded/ScenarioBank.asset");sound.clock=flat.clock.transform;sound.listener=head;sound.phoneWarning=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/ScenarioTemp/phone-warning.wav");sound.grandmotherKnock=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/ScenarioTemp/knock-grandmother.wav");loop.sound=sound;
   var scenes=loop.gameObject.AddComponent<ScenarioTimeline>();scenes.flat=flat;scenes.loop=loop;scenes.sound=sound;scenes.cine=cine;scenes.director=loop.gameObject.AddComponent<PlayableDirector>();scenes.director.playOnAwake=false;scenes.director.extrapolationMode=DirectorWrapMode.None;scenes.leftHand=Hand(true);scenes.rightHand=Hand(false);loop.scenes=scenes;
   scenes.rope=new GameObject("Hanging scene rope").AddComponent<LineRenderer>();scenes.rope.useWorldSpace=true;scenes.rope.positionCount=2;scenes.rope.startWidth=scenes.rope.endWidth=.018f;scenes.rope.sharedMaterial=wood;scenes.rope.enabled=false;
   scenes.landingLight=Lamp("Landing overhead",new Vector3(8.05f,2.35f,6.7f),new Color(1,.9f,.74f),0,2.5f);
   scenes.veil=loop.gameObject.AddComponent<DeathVeil>();scenes.veil.head=head;scenes.veil.skin=Save(new Material(Shader.Find("Ataka/TemporaryVeil")),"TemporaryVeil.mat");
   var chase=loop.gameObject.AddComponent<ChaseCorridor>();chase.flat=flat;chase.loop=loop;chase.scenes=scenes;chase.sound=sound;loop.chase=chase;
   var tv=loop.gameObject.AddComponent<ScenarioTelevision>();tv.loop=loop;tv.screen=flat.televisionScreen;tv.screenMaterial=Save(new Material(Shader.Find("Universal Render Pipeline/Unlit")),"RubinScreen.mat");tv.misha=Person("TEMP Misha in TV",Vector3.zero,1.65f).transform;tv.visitor=Person("TEMP Guest in TV",Vector3.zero,2.3f).transform;
   foreach(var root in new[]{tv.misha,tv.visitor})foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=31;
   tv.feed=new GameObject("Rubin camera").AddComponent<Camera>();tv.feed.depth=-10;tv.feed.nearClipPlane=.05f;tv.feed.farClipPlane=15;tv.feed.fieldOfView=65;tv.feed.backgroundColor=Color.black;tv.feed.clearFlags=CameraClearFlags.SolidColor;tv.feed.GetUniversalAdditionalCameraData();tv.feed.enabled=false;
  }
  static void CreateEvents(){var s=UnityEngine.Object.FindFirstObjectByType<ScenarioTimeline>();s.underBed=Timeline("UnderBed","UnderBed",1.4f);s.balcony=Timeline("Balcony","BalconyPalm",1.2f);s.television=Timeline("Television","Television",.1f);s.hanging=Timeline("Hanging","HangingVision",6.1f);s.peephole=Timeline("Peephole","Peephole",7);s.death=Timeline("DeathShort","DeathShort",3.5f);s.chaseStart=Timeline("ChaseStart","ChaseStart",3f);}
  static void CreateCorridor(){
   var chase=UnityEngine.Object.FindFirstObjectByType<ChaseCorridor>();chase.pool=new CorridorModule[6];var old=shell;
   for(int i=0;i<6;i++){shell=new GameObject("Pooled corridor "+i).transform;shell.position=new Vector3(i*10,0,30);var module=shell.gameObject.AddComponent<CorridorModule>();
    Box("Floor",new Vector3(0,-.1f,3),new Vector3(1.59f,.2f,6),floor);Box("Ceiling",new Vector3(0,2.6f,3),new Vector3(1.69f,.2f,6),wall);Box("Right",new Vector3(.795f,1.25f,3),new Vector3(.1f,2.5f,6),wood);Box("Left",new Vector3(-.795f,1.25f,2.255f),new Vector3(.1f,2.5f,4.51f),wood);
    module.cornerCap=Box("Turn end cap",new Vector3(0,1.25f,6.05f),new Vector3(1.69f,2.5f,.1f),wood);module.straightLeftEnd=Box("Straight left end",new Vector3(-.795f,1.25f,5.255f),new Vector3(.1f,2.5f,1.49f),wood);
    module.clock=Clock(new Vector3(-.63f,1.05f,3),shell);module.clock.transform.localRotation=Quaternion.Euler(0,-90,0);module.lamp=Lamp("Red sconce",new Vector3(-.4f,2.1f,3),Color.red,0,3.8f,shell);
    var door=Door("Locked side door",new Vector3(.735f,1,3),.8f,90);door.locked=true;var text=Label("ПОМНИ",new Vector3(.72f,1.5f,4.5f),.04f,shell);text.transform.localRotation=Quaternion.Euler(0,90,0);
    module.surfaces=shell.GetComponentsInChildren<Renderer>();module.solids=shell.GetComponentsInChildren<Collider>();chase.pool[i]=module;
   }shell=old;
  }
  public static void Build(){
   var old=GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;var name=PlayerSettings.productName;var window=PlayerSettings.fullScreenMode;int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;var apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);bool defaultApis=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
   try{var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Scenario_URP.asset");GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;PlayerSettings.productName="Атака — тест петли";PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});Directory.CreateDirectory("Builds/ScenarioCore");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Flat.unity"},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/ScenarioCore/Ataka-Loop.exe",options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Scenario build failed");Debug.Log("SCENARIO_BUILD_SUCCESS");}
   finally{GraphicsSettings.defaultRenderPipeline=old;QualitySettings.renderPipeline=quality;PlayerSettings.productName=name;PlayerSettings.fullScreenMode=window;PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,defaultApis);AssetDatabase.SaveAssets();}
  }
  public static void GenerateAndBuild(){Generate();Build();}
 }
}
