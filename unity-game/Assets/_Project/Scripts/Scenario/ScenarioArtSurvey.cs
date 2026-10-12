using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA-only scene survey. Documents camera positions and overrides; not a gameplay test.
 public sealed class ScenarioArtSurvey:MonoBehaviour {
  string output;
  [Serializable]public sealed class ViewRecord {public string file,mode;public Vector3 feet,target;public float fov=65;}
  [Serializable]public sealed class Report {public string scope="QA camera placement, normal in-game lap1 and warm-return lighting. All internal doors requested open for layout inspection. View17 hides lace only for material diagnosis. Events disabled; not proof of walkability.";public ViewRecord[] views;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-scenario-art-survey");if(i>=0&&i+1<args.Length)new GameObject("Art review survey").AddComponent<ScenarioArtSurvey>().output=args[i+1];}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.runInBackground=true;Application.targetFrameRate=30;yield return new WaitForSecondsRealtime(1);
   var loop=FindFirstObjectByType<LoopDirector>();var flat=loop.flat;var player=flat.player;loop.testing=true;loop.Pause(false);loop.enabled=false;player.automating=true;player.control=false;AudioListener.pause=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   foreach(var door in new[]{flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor}){door.locked=false;door.open=true;}
   var records=new List<ViewRecord>();
   var laceRenderers=new List<Renderer>();foreach(var r in flat.apartment.GetComponentsInChildren<Renderer>())if(r.name=="Textile lace veil")laceRenderers.Add(r);
   var views=new[]{
    A("ref-A-hall",8.05f,5.7f,0,-3),A("ref-B-hallB",6.45f,5.25f,90,-3),
    A("ref-C-living-door",5.65f,4.15f,10,-5),A("ref-D-living-reverse",4.6f,.7f,200,-4),
    A("ref-E-bedroom-door",3.25f,3.75f,90,-5),A("ref-F-bedroom-in",2.4f,3.9f,60,-8),A("ref-G-kitchen",7.25f,3.4f,0,-6),
    V("01-entry-dresser",new Vector3(8.05f,.02f,5.65f),new Vector3(7.1f,1.15f,3.8f)),
    V("02-hall-clock",new Vector3(6.15f,.02f,5.35f),new Vector3(4.8f,1.2f,5.5f)),
    V("03-living-wide",new Vector3(3.25f,.02f,3.65f),new Vector3(5.5f,1.05f,1.45f)),
    V("04-living-reverse",new Vector3(5.8f,.02f,.8f),new Vector3(3.4f,1.1f,3.5f)),
    V("05-bedroom-door",new Vector3(3.25f,.02f,3.6f),new Vector3(2.2f,1.25f,3.85f)),
    V("06-bedroom-bed",new Vector3(2.4f,.02f,3.4f),new Vector3(.7f,.85f,1.6f)),
    V("07-wardrobe-clearance",new Vector3(1.2f,.02f,3.0f),new Vector3(2.2f,1.15f,3.9f)),
    V("08-bedroom-reverse",new Vector3(.9f,.02f,.6f),new Vector3(2.2f,1.15f,3.8f)),
    V("09-kitchen-wide",new Vector3(7.25f,.02f,2.65f),new Vector3(7.8f,1.05f,.65f)),
    V("10-kitchen-reverse",new Vector3(7.1f,.02f,.7f),new Vector3(8.05f,1.05f,2.55f)),
    V("11-bathroom",new Vector3(4.25f,.02f,5.6f),new Vector3(3.1f,.9f,5.1f)),
    V("12-window-courtyard",new Vector3(4.25f,.02f,.6f),new Vector3(4.25f,.7f,-5)),
    V("13-bedroom-wardrobe-front-closed",new Vector3(1.2f,.02f,1.8f),new Vector3(2.4f,1.1f,1.8f)),
    V("14-bedroom-wardrobe-front-open",new Vector3(1.2f,.02f,1.8f),new Vector3(2.4f,1.1f,1.8f)),
    V("15-bedroom-icon-detail",new Vector3(.75f,.02f,1.0f),new Vector3(.23f,1.75f,.23f)),
    V("16-bedroom-bedding-detail",new Vector3(1.25f,.02f,1.8f),new Vector3(.5f,.9f,1.05f)),
    V("17-bedroom-bedding-without-lace",new Vector3(1.25f,.02f,1.8f),new Vector3(.5f,.9f,1.05f)),
    V("18-bedroom-bedside-detail",new Vector3(1.9f,.02f,1.65f),new Vector3(1.22f,.63f,1.0f))
   };
   bool materialProbe=Array.IndexOf(Environment.GetCommandLineArgs(),"-scenario-bed-probe")>=0;
   float originalIconIntensity=flat.look.icon.intensity;Light iconHalo=null;foreach(var l in flat.apartment.GetComponentsInChildren<Light>())if(l.name=="Icon glass halo")iconHalo=l;float originalHaloIntensity=iconHalo?iconHalo.intensity:0;
   var originals=new Dictionary<Renderer,Material[]>();var diagnosticMaterials=new List<Material>();
   foreach(var r in flat.apartment.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Textile upright pillow")||r.name=="Textile mattress")originals.Add(r,r.sharedMaterials);
   foreach(string mode in materialProbe?new[]{"lap1","diagnostic-no-normal","diagnostic-no-icon-light","diagnostic-flashlight"}:new[]{"lap1","warm-return"}){
    foreach(var pair in originals){
     if(mode=="diagnostic-no-normal"){
      var materials=new Material[pair.Value.Length];for(int i=0;i<materials.Length;i++){var m=new Material(pair.Value[i]);m.DisableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",0);materials[i]=m;diagnosticMaterials.Add(m);}pair.Key.sharedMaterials=materials;
     }else pair.Key.sharedMaterials=pair.Value;
    }
    if(mode=="warm-return")flat.look.WarmReturn();else flat.look.Apply(FlatCondition.Familiar);
    flat.look.icon.intensity=mode=="diagnostic-no-icon-light"?0:originalIconIntensity;if(iconHalo)iconHalo.intensity=mode=="diagnostic-no-icon-light"?0:originalHaloIntensity;
    foreach(var view in views){
     if(materialProbe&&!view.file.StartsWith("16-")&&!view.file.StartsWith("17-")&&!view.file.StartsWith("18-"))continue;
     foreach(var r in laceRenderers)r.enabled=!view.file.StartsWith("17-");
     if(view.file.StartsWith("13-"))foreach(var h in flat.wardrobeLeaves)h.localRotation=Quaternion.identity;
     if(view.file.StartsWith("14-"))foreach(var h in flat.wardrobeLeaves)h.localRotation=Quaternion.Euler(0,95,0);
     var lens=loop.scenes.cine.Lens;lens.FieldOfView=view.fov;loop.scenes.cine.Lens=lens;
     var direction=view.target-(view.feet+Vector3.up*1.55f);var angles=Quaternion.LookRotation(direction).eulerAngles;player.Place(view.feet,angles.y,Mathf.DeltaAngle(0,angles.x));player.flashlight.intensity=mode=="diagnostic-flashlight"?2.0f:0;
     yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
     string file=mode+"-"+view.file+".png";var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,file),image.EncodeToPNG());Destroy(image);
     records.Add(new ViewRecord{file=file,mode=mode,feet=view.feet,target=view.target,fov=view.fov});
    }
   }
   flat.look.icon.intensity=originalIconIntensity;if(iconHalo)iconHalo.intensity=originalHaloIntensity;
   foreach(var pair in originals)pair.Key.sharedMaterials=pair.Value;foreach(var m in diagnosticMaterials)Destroy(m);
   var report=new Report{views=records.ToArray()};if(materialProbe)report.scope="Diagnostic ONLY: staged cameras16/17/18, normal lap1; then same lighting with textile normal maps disabled; then original materials with icon spot/halo intensity0 to locate the red band; then original materials/light with flashlight intensity2. Neither override is production art or an approved change. View17 hides lace.";
   File.WriteAllText(Path.Combine(output,"survey.json"),JsonUtility.ToJson(report,true));Application.Quit();
  }
  static ViewRecord A(string id,float x,float z,float yaw,float pitch){var feet=new Vector3(x,.02f,z);return new ViewRecord{file=id,feet=feet,target=feet+Vector3.up*1.55f+Quaternion.Euler(-pitch,yaw+180,0)*Vector3.forward*2,fov=68};}
  static ViewRecord V(string id,Vector3 feet,Vector3 target)=>new ViewRecord{file=id,feet=feet,target=target};
 }
}
