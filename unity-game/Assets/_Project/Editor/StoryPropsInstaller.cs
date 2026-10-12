using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 // Idempotent: places the ready story props prepared by tools/art/prepare_story_props_open.py (direct downloads)
 // and tools/art/prepare_story_props.py (Sketchfab: JamJar, Shawl) as "STORY <id>" under the flat.
 // StoryDirector shows, hides and moves them by name. Missing props are skipped with a warning.
 // Ataka.Editor.StoryPropsInstaller.Install  (or InstallAndBuild)
 public static class StoryPropsInstaller {
  const string Root="Assets/_Project/Art/ReadyModels/Story/";
  // Initial placements (bottom-centre pivots, metres, yaw degrees). Hidden ones are revealed by the story.
  static readonly (string id,Vector3 at,float yaw,bool visible)[] Places={
   ("Bag",new Vector3(8.35f,0,5.45f),90,false),
   ("Corvalol",new Vector3(7.25f,.745f,.75f),0,false),
   ("Kettle",new Vector3(8.48f,.86f,1.85f),90,true),   // back burner of the stove (4.3)
   ("Matchbox",new Vector3(8.24f,1.552f,2.40f),20,true),// on top of the ZiL (4.3)
   ("JamJar",new Vector3(5.60f,.905f,-1.26f),0,true),  // on the balcony parapet (4.3)
   ("Belt",new Vector3(2.32f,1.05f,1.96f),90,true),    // inside the wardrobe, left leaf hook (4.5 / 8.2)
   ("Bread",new Vector3(7.45f,.745f,.45f),15,false),   // out of the bag on the table (4.2)
   ("Cans",new Vector3(7.75f,.745f,.42f),-20,false),
   ("Bulb",new Vector3(5.50f,1.95f,5.92f),0,false),    // new bulb in the sconce (4.4)
   ("Shawl",new Vector3(2.50f,1.15f,1.80f),90,false),  // hung inside the wardrobe (4.5); carcass x2.27–2.76 z1.35–2.25 (story15 export)
   ("TeaSet",new Vector3(7.66f,.745f,.50f),0,false)}; // teapot, sugar bowl, two cups — set on the table when the tea is poured (4.3, epilogue)
  public static void Install(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
   foreach(var (id,at,yaw,visible) in Places){
    string fbx=Root+id+"/"+id+".fbx";var old=flat.apartment.Find("STORY "+id);if(old)Object.DestroyImmediate(old.gameObject);
    if(!File.Exists(fbx)){Debug.LogWarning("STORY_PROP missing "+fbx+" — run tools/art/prepare_story_props.py");continue;}
    var m=JsonUtility.FromJson<GuestInstaller.Manifest>(File.ReadAllText(Root+id+"/manifest.json"));
    var root=new GameObject("STORY "+id);root.transform.SetParent(flat.apartment,false);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));model.transform.SetParent(root.transform,false);
    GuestInstaller.MaterialiseFolder(Root+id+"/",model,m);
    root.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));root.SetActive(visible);Debug.Log("STORY_PROP "+id+" at "+at);
   }
   EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  }
  public static void InstallAndBuild(){Install();ScenarioBuilder.Build();}
  public static void InstallReadyAssetsAndBuild(){Install();AntresolInstaller.Install();GuestInstaller.Install();ScenarioBuilder.Build();}
 }
}
