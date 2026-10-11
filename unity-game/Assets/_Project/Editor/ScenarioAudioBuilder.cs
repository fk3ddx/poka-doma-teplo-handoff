using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class ScenarioAudioBuilder {
  const string Root="Assets/_Project/Audio/Recorded";
  static AudioClip[] Clips(string prefix){return Directory.GetFiles(Root,prefix+"*.wav").OrderBy(p=>p).Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p.Replace('\\','/'))).Where(c=>c).ToArray();}
  static ScenarioSoundCue Cue(string id,string prefix,ScenarioBus bus,float gain=-6,bool loop=false,bool spatial=true,int max=3,float pitch=.025f,float range=12){return new ScenarioSoundCue{id=id,clips=Clips(prefix),bus=bus,gainDb=gain,loop=loop,spatial=spatial,maxInstances=max,pitchVariation=pitch,maxDistance=range,priority=bus==ScenarioBus.Voice?20:bus==ScenarioBus.Monster?40:100};}
  static object Call(object o,string name,params object[] args){var m=o.GetType().GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).First(x=>x.Name==name&&x.GetParameters().Length==args.Length);return m.Invoke(o,args);}
  static AudioMixer CreateMixer(){
   const string path=Root+"/Scenario.mixer";var existing=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);if(existing)return existing;
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).First(t=>t!=null);
   var mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{path});
   var master=type.GetProperty("masterGroup").GetValue(mixer);foreach(string name in Enum.GetNames(typeof(ScenarioBus))){var group=Call(mixer,"CreateNewGroup",name,false);Call(mixer,"AddChildToParent",group,master);}
   var snapshot=type.GetProperty("TargetSnapshot").GetValue(mixer);((UnityEngine.Object)snapshot).name="Normal";Call(snapshot,"SetValue",Call(master,"GetGUIDForVolume"),-3f);
   EditorUtility.SetDirty(mixer);AssetDatabase.SaveAssets();return mixer;
  }
  public static ScenarioSoundBank MakeBank(){
   AssetDatabase.Refresh();foreach(string file in Directory.GetFiles(Root,"*.wav")){var importer=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));if(importer==null)continue;importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
   string path=Root+"/ScenarioBank.asset";var bank=AssetDatabase.LoadAssetAtPath<ScenarioSoundBank>(path);if(!bank){bank=ScriptableObject.CreateInstance<ScenarioSoundBank>();AssetDatabase.CreateAsset(bank,path);}
   bank.cues=new[]{
    Cue("phone","phone_",ScenarioBus.World,-3,max:1,pitch:0,range:15),Cue("clock","clock_loop",ScenarioBus.World,-9,true,max:8,pitch:0,range:8),Cue("chime","chime",ScenarioBus.World,-10,max:14,pitch:0,range:15),
    Cue("phone_shriek","shriek",ScenarioBus.Voice,-8,max:1,pitch:0),Cue("phone_breath","guest_breath_",ScenarioBus.Voice,-10,max:1,pitch:0,range:5),Cue("breath","guest_breath_",ScenarioBus.Monster,-7,max:1,pitch:0,range:5),Cue("guest_breath","guest_breath_",ScenarioBus.Monster,-7,true,max:1,pitch:0),Cue("player_breath","player_breath_loop",ScenarioBus.Foley,-11,true,false,1,0),Cue("heartbeat","heartbeat_loop",ScenarioBus.Foley,-18,true,false,1,0),
    Cue("knock","knock_",ScenarioBus.World,-5,max:3,pitch:.015f),Cue("creak","door_creak_",ScenarioBus.World,-9,max:2),Cue("door_open","door_open_",ScenarioBus.World,-10,max:2),Cue("door_close","door_close_",ScenarioBus.World,-10,max:2),Cue("latch","latch_",ScenarioBus.World,-13,max:2),
    Cue("wardrobe","wood_impact_",ScenarioBus.World,-6),Cue("balcony","glass_impact_",ScenarioBus.World,-6),Cue("door_burst","wood_impact_",ScenarioBus.World,-4),Cue("palms","palms_",ScenarioBus.Monster,-8,max:2),Cue("shriek","shriek",ScenarioBus.Stinger,-8,max:1,pitch:0),
    Cue("step","step_",ScenarioBus.Foley,-7,max:3),Cue("carpet_step","carpet_step_",ScenarioBus.Foley,-10,max:3),Cue("wind","wind_loop",ScenarioBus.Ambience,-15,true,max:1,pitch:0,range:8),Cue("rope","rope_loop",ScenarioBus.World,-9,true,max:1,pitch:0,range:5),Cue("scratch","scratch",ScenarioBus.World,-9,max:1),Cue("fridge","fridge_loop",ScenarioBus.Ambience,-18,true,max:1,pitch:0,range:6)
   };
   foreach(var cue in bank.cues)if(cue.clips.Length==0)throw new Exception("No recorded clips for "+cue.id);
   bank.mixer=CreateMixer();bank.groups=Enum.GetNames(typeof(ScenarioBus)).Select(n=>bank.mixer.FindMatchingGroups(n).First(g=>g.name==n)).ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();return bank;
  }
  public static void InstallAndBuild(){var bank=MakeBank();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var sound=UnityEngine.Object.FindFirstObjectByType<ScenarioSound>();sound.bank=bank;EditorUtility.SetDirty(sound);EditorSceneManager.SaveScene(scene);ScenarioBuilder.Build();}
 }
}