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
   AssetDatabase.Refresh();foreach(string file in Directory.GetFiles(Root,"*.wav")){var importer=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));if(importer==null)continue;importer.forceToMono=false;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
   string path=Root+"/ScenarioBank.asset";var bank=AssetDatabase.LoadAssetAtPath<ScenarioSoundBank>(path);if(!bank){bank=ScriptableObject.CreateInstance<ScenarioSoundBank>();AssetDatabase.CreateAsset(bank,path);}
   bank.cues=new[]{
    Cue("phone","phone_",ScenarioBus.World,-2,max:1,pitch:0,range:15),Cue("clock","clock_loop",ScenarioBus.World,-8,true,max:8,pitch:0,range:8),Cue("chime","chime",ScenarioBus.World,-2,max:3,pitch:0,range:15),
    Cue("phone_shriek","shriek",ScenarioBus.Voice,-4,max:1,pitch:0),Cue("phone_breath","guest_breath_",ScenarioBus.Voice,-7,max:1,pitch:0,range:5),Cue("breath","guest_breath_",ScenarioBus.Monster,-5,max:1,pitch:0,range:5),Cue("guest_breath","guest_breath_",ScenarioBus.Monster,-5,true,max:1,pitch:0),Cue("player_breath","player_breath_loop",ScenarioBus.Foley,-7,true,false,1,0),Cue("heartbeat","heartbeat_loop",ScenarioBus.Foley,-18,true,false,1,0),
    Cue("knock","knock_",ScenarioBus.World,-5,max:3,pitch:.015f),Cue("creak","door_creak_",ScenarioBus.World,-9,max:2),Cue("door_open","door_open_",ScenarioBus.World,-10,max:2),Cue("door_close","door_close_",ScenarioBus.World,-10,max:2),Cue("latch","latch_",ScenarioBus.World,-13,max:2),
    Cue("choked_cry","choked_cry",ScenarioBus.Foley,-6,max:1,pitch:0),Cue("tinnitus","tinnitus",ScenarioBus.Stinger,-22,max:1,pitch:0),Cue("finger_crack","finger_crack",ScenarioBus.Foley,-8,max:1,pitch:0),Cue("wet_tear","wet_tear",ScenarioBus.Stinger,-4,max:1,pitch:0),Cue("wardrobe","wood_impact_",ScenarioBus.World,-6),Cue("balcony","glass_impact_",ScenarioBus.World,-6),Cue("door_burst","wood_impact_",ScenarioBus.World,-4),Cue("palms","palms_",ScenarioBus.Monster,-4,max:2),Cue("shriek","shriek",ScenarioBus.Stinger,0,max:1,pitch:0),
    Cue("step","step_",ScenarioBus.Foley,-10,max:3),Cue("carpet_step","carpet_step_",ScenarioBus.Foley,-13,max:3),Cue("wind","wind_loop",ScenarioBus.Ambience,-20,true,max:1,pitch:0,range:8),Cue("rope","rope_loop",ScenarioBus.World,-10,true,max:1,pitch:0,range:5),Cue("scratch","scratch",ScenarioBus.World,-9,max:1),Cue("fridge","fridge_loop",ScenarioBus.Ambience,-18,true,max:1,pitch:0,range:6),
    // Household pass (SCENARIO 4 / 6 / 11 / 14), CC0 candidates from tools/audio/prepare_household.py; radio_loop is the earlier original waltz.
    Cue("match","match_strike",ScenarioBus.Foley,-8,max:1),Cue("gas_ignite","gas_ignite",ScenarioBus.World,-9,max:1),Cue("gas","gas_hiss_loop",ScenarioBus.Ambience,-26,true,max:1,pitch:0,range:4),
    Cue("tap","tap_fill",ScenarioBus.World,-10,max:1,pitch:0,range:6),Cue("kettle_rise","kettle_rise",ScenarioBus.World,-10,max:1,pitch:0,range:12),Cue("kettle","kettle_whistle_loop",ScenarioBus.World,-11,true,max:1,pitch:0,range:12),
    Cue("spoon","spoon_clink",ScenarioBus.Foley,-12,max:1),Cue("drip","drip_",ScenarioBus.World,-22,max:2,range:5),Cue("clock_wind","clock_wind",ScenarioBus.Foley,-9,max:1,pitch:0),
    Cue("bulb","bulb_screw",ScenarioBus.Foley,-14,max:1),Cue("switch","switch_click",ScenarioBus.World,-12,max:2),Cue("breaker","breaker",ScenarioBus.World,-6,max:1,pitch:0,range:14),
    Cue("tv_on","tv_on",ScenarioBus.World,-10,max:1,pitch:0,range:10),Cue("crt_whine","crt_whine_loop",ScenarioBus.World,-34,true,max:1,pitch:0,range:5),
    Cue("doorbell","doorbell",ScenarioBus.World,-8,max:2,pitch:0,range:14),Cue("car","car_pass",ScenarioBus.Ambience,-24,max:1,range:30),Cue("radio","radio_loop",ScenarioBus.Ambience,-24,true,max:1,pitch:0,range:5)
   };
   foreach(var cue in bank.cues){if(cue.clips.Length==0)throw new Exception("No recorded clips for "+cue.id);if(cue.id=="step"||cue.id=="carpet_step")cue.minDistance=.3f;else if(cue.id=="phone")cue.minDistance=.7f;}
   bank.mixer=CreateMixer();bank.groups=Enum.GetNames(typeof(ScenarioBus)).Select(n=>bank.mixer.FindMatchingGroups(n).First(g=>g.name==n)).ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();return bank;
  }
  public static void InstallAndBuild(){var bank=MakeBank();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var sound=UnityEngine.Object.FindFirstObjectByType<ScenarioSound>();sound.bank=bank;EditorUtility.SetDirty(sound);EditorSceneManager.SaveScene(scene);ScenarioBuilder.Build();}
 }
}