using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ataka.LightLab {
 public sealed class LightLabSession:MonoBehaviour {
  public UniversalRenderPipelineAsset pipeline;
  public Material vhs;
  public VhsProfile[] profiles;
  public FirstPersonController player;
  public Light[] chandelier, floorLamp;
  public Light windowLight,tvLight;
  public Renderer chandelierShade,lampShade;
  public Volume volume;
  public Transform[] viewpoints;
  public ReflectionProbe[] probes;
  public ShaderVariantCollection variants;
  [Serializable] public sealed class Tune {public float exposure=-1.7f, main=1.8f, lamp=3.5f, window=.9f,bounce=.10f, grain=.032f,lumaBlur=1.5f, colorBlur=8, ambient=1;}
  Tune tune=new Tune();
  bool candidate=true; Light windowBounce; Quaternion windowRotation; Vector3 windowPosition;
  bool paused=true,capturing,measureTransitions,hasTune; int mode,profile; float lightBlend, lampBlend=1;
  float[] current=new float[11]; float[] targets=new float[11];
  static readonly string[] names={"_Lens","_LumaBlur","_ChromaBlur","_ChromaShift","_GhostPixels","_GhostWeight","_Aberration","_Grain","_Jitter","_Scanlines","_Disruption"};
  readonly List<float> frameMs=new List<float>(); int errors; string output;
  RenderPipelineAsset previous; Material runtimeVhs;
  public int Mode=>mode;
  void Awake(){previous=QualitySettings.renderPipeline;QualitySettings.renderPipeline=pipeline;}
  IEnumerator Start(){
   Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
   // Shared feature material is restored on exit; assets on disk are never edited at runtime.
   runtimeVhs=new Material(vhs);
   var args=Environment.GetCommandLineArgs();candidate=Array.IndexOf(args,"-lab-baseline")<0;int c=Array.IndexOf(args,"-lab-config");
   if(c>=0&&c+1<args.Length){tune=JsonUtility.FromJson<Tune>(File.ReadAllText(args[c+1]));hasTune=true;}
   volume.profile.TryGet<ColorAdjustments>(out var ca);ca.postExposure.value=tune.exposure;
   RenderSettings.ambientLight=new Color(12/255f,14/255f,20/255f)*tune.ambient;
   windowPosition=windowLight.transform.position;windowRotation=windowLight.transform.rotation;
   windowBounce=new GameObject("Window diffuse bounce — always active").AddComponent<Light>();
   windowBounce.type=LightType.Point;windowBounce.transform.position=new Vector3(2,1.9f,4.65f);windowBounce.range=4;
   windowBounce.color=new Color(.43f,.59f,.84f);windowBounce.shadows=LightShadows.Soft;windowBounce.shadowBias=.035f;windowBounce.shadowNormalBias=.15f;
   ApplyWindow();
   if(variants)variants.WarmUp();
   SelectProfile(0,true);SetLight(0);Pause(true);
   Application.logMessageReceived+=Log;
   // Warm every state before user interaction; only intensity changes thereafter.
   for(int i=0;i<3;i++){SetLight(i);ApplyLights(true);yield return null;}
   SetLight(0);ApplyLights(true);yield return null;
   foreach(var probe in probes)probe.RenderProbe();
   int a=Array.IndexOf(args,"-lightlab-capture");
   if(a>=0&&a+1<args.Length){output=Path.GetFullPath(args[a+1]);Directory.CreateDirectory(output);capturing=true;StartCoroutine(CaptureAll());}
  }
  void Log(string s,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors++;}
  public void SetLight(int value){mode=Mathf.Clamp(value,0,2);}
  public void SelectProfile(int value,bool snap=false){
   profile=Mathf.Clamp(value,0,profiles.Length-1);var p=profiles[profile];
   targets=new[]{p.lens,profile==0&&hasTune?tune.lumaBlur:p.lumaBlur,profile==0&&hasTune?tune.colorBlur:p.chromaBlur,p.chromaShift,p.ghostPixels,p.ghostWeight,p.aberration,profile==0&&hasTune?tune.grain:p.grain,p.jitter,p.scanlines,p.disruption};
   if(candidate && profile==0){targets[1]=2.0f;targets[2]=10;targets[7]=.036f;}
   if(snap)Array.Copy(targets,current,11);
  }
  void Update(){
   if(vhs==null||targets==null)return;
   float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*7);
   for(int i=0;i<11;i++){current[i]=Mathf.Lerp(current[i],targets[i],blend);vhs.SetFloat(names[i],current[i]);}
   vhs.SetFloat("_Wear",candidate?1:0);
   vhs.SetVector("_VhsOutputSize",new Vector4(Screen.width,Screen.height,0,0));
   ApplyLights(false);
   if(measureTransitions)frameMs.Add(Time.unscaledDeltaTime*1000);
   var k=Keyboard.current;if(k==null||capturing)return;
   if(k.escapeKey.wasPressedThisFrame)Pause(!paused);
   if(k.fKey.wasPressedThisFrame){candidate=!candidate;ApplyWindow();SelectProfile(profile);}
   if(paused)return;
   if(k.digit1Key.wasPressedThisFrame)SetLight(0);if(k.digit2Key.wasPressedThisFrame)SetLight(1);if(k.digit3Key.wasPressedThisFrame)SetLight(2);
   if(k.vKey.wasPressedThisFrame)SelectProfile(0);if(k.bKey.wasPressedThisFrame)SelectProfile(1);if(k.nKey.wasPressedThisFrame)SelectProfile(2);
  }
  void ApplyWindow(){
   windowLight.intensity=tune.window*(candidate?3.6f:1);
   windowLight.color=candidate?new Color(.43f,.60f,.85f):new Color(.56f,.65f,.78f);
   windowLight.transform.position=candidate?new Vector3(3.1f,2.25f,6.1f):windowPosition;
   if(candidate)windowLight.transform.LookAt(new Vector3(1.1f,.8f,2));else windowLight.transform.rotation=windowRotation;
   windowBounce.intensity=candidate?.16f:0;
   floorLamp[0].range=candidate?3.4f:4.5f;floorLamp[2].range=candidate?3.4f:6;
  }
  void ApplyLights(bool instant){
   float b=instant?1:1-Mathf.Exp(-Time.unscaledDeltaTime*9);
   lightBlend=Mathf.Lerp(lightBlend,mode==0?1:0,b);lampBlend=Mathf.Lerp(lampBlend,mode<2?1:0,b);
   for(int i=0;i<chandelier.Length;i++)chandelier[i].intensity=(i==0?tune.main:tune.main*tune.bounce)*lightBlend;
   for(int i=0;i<floorLamp.Length;i++)floorLamp[i].intensity=(i==0?tune.lamp*(candidate?.85f:1):i==1?tune.lamp*.06f*(candidate?.55f:1):tune.lamp*tune.bounce*(candidate?.4f:1))*lampBlend;
   chandelierShade.material.SetColor("_EmissionColor",new Color(1,.5f,.18f)*6*lightBlend);
   lampShade.material.SetColor("_EmissionColor",new Color(1,.5f,.18f)*4*lampBlend);
   tvLight.intensity=0; // The borrowed TV has an unlit screen; do not fake a source it does not show.
   foreach(var p in probes)p.intensity=Mathf.Lerp(.03f,.25f,Mathf.Max(lightBlend,lampBlend*(candidate?.55f:1)));
  }
  public void Pause(bool value){paused=value;player.inputEnabled=!value;Cursor.visible=value;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;player.SuppressLook();}
  void OnGUI(){
   if(!paused)return;
   float w=580,h=480;GUI.Box(new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h),"");
   GUILayout.BeginArea(new Rect((Screen.width-w)/2+30,(Screen.height-h)/2+22,w-60,h-44));
   var title=new GUIStyle(GUI.skin.label){fontSize=28};var text=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};
   GUILayout.Label("LIGHTLAB 0.1.1 · сравнение",title);GUILayout.Space(18);
   GUILayout.Label(candidate?"Вариант B: холодное окно + мягкий износ сигнала":"Вариант A: исходный баланс света и VHS",text);GUILayout.Space(18);
   GUILayout.Label("WASD — движение · мышь — взгляд\n1 — свет есть · 2 — только торшер · 3 — блэкаут\nV — Норма · B — Тревога · N — Скример\nF — A / B (исходный / новый)\nEsc — меню",text);GUILayout.Space(22);
   if(GUILayout.Button("Войти / продолжить",GUILayout.Height(45)))Pause(false);
   GUILayout.Space(12);if(GUILayout.Button("Выйти",GUILayout.Height(36)))Application.Quit();GUILayout.EndArea();
  }
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var img=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),img.EncodeToPNG());Destroy(img);}
  IEnumerator CaptureAll(){
   yield return new WaitForSecondsRealtime(2);yield return Shot("menu-start");Pause(false);player.inputEnabled=false;
   var report=new Report{gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,width=Screen.width,height=Screen.height,renderScale=pipeline.renderScale,upscale=pipeline.upscalingFilter.ToString(),tuning=tune,candidate=candidate};
   frameMs.Clear();
   for(int m=0;m<3;m++){
    yield return null;yield return null;measureTransitions=true;SetLight(m);yield return new WaitForSecondsRealtime(1.2f);measureTransitions=false;
    for(int i=0;i<viewpoints.Length;i++){
     var pose=viewpoints[i];player.Restore(pose.position-Vector3.up*1.55f,pose.eulerAngles.y,pose.eulerAngles.x,false);player.inputEnabled=false;
     yield return new WaitForSecondsRealtime(.35f);yield return Shot("view"+(i+1)+"-light"+(m+1));
    }
   }
   SetLight(0);var first=viewpoints[0];player.Restore(first.position-Vector3.up*1.55f,first.eulerAngles.y,first.eulerAngles.x,false);player.inputEnabled=false;
   SelectProfile(1);yield return new WaitForSecondsRealtime(.8f);yield return Shot("profile-tension");
   SelectProfile(2);yield return new WaitForSecondsRealtime(.8f);yield return Shot("profile-scare");
   SelectProfile(0);SetLight(0);yield return new WaitForSecondsRealtime(.8f);Pause(true);yield return Shot("menu-return");
   report.errors=errors;report.frameMs=frameMs.ToArray();report.activeLightObjects=true;
   foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))report.activeLightObjects&=l.gameObject.activeInHierarchy&&l.enabled;
   File.WriteAllText(Path.Combine(output,"capture-report.json"),JsonUtility.ToJson(report,true));Application.Quit(errors==0?0:1);
  }
  void OnDestroy(){Application.logMessageReceived-=Log;QualitySettings.renderPipeline=previous;if(runtimeVhs){vhs.CopyPropertiesFromMaterial(runtimeVhs);Destroy(runtimeVhs);}}
  [Serializable] sealed class Report {public string gpu,graphicsApi,unity,upscale;public int width,height,errors;public bool activeLightObjects,candidate;public float renderScale;public Tune tuning;public float[] frameMs;}
 }
}
