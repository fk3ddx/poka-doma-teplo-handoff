using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
namespace Ataka.Scenario {
 // Opt-in QA capture of the listener mix, before the OS device. No microphone access.
 // Capture mode is always silent: copy the mix, then zero the outgoing listener buffer.
 // Disk I/O runs on a worker; normal game launches never install this component.
 public sealed class ScenarioAudioCapture : MonoBehaviour {
  const int Capacity=1<<20;
  readonly float[] ring=new float[Capacity];
  long written,read; int channels,rate,dropped,formatChanges; volatile bool stopping;
  Thread worker; string path,failure; readonly List<Marker> markers=new List<Marker>();
  LoopDirector loop; string lastState; bool saved; long clearedCallbacks;
  static bool captureRequested; static float previousVolume;
  [Serializable] public sealed class Marker { public string state; public long sample; public double seconds;public string scene;public float timelineSeconds=-1; }
  [Serializable] public sealed class Report { public string scope="Unity listener mix, pre-OS output; not microphone or loopback",error; public bool captureModeMutesOutput=true; public long clearedOutputCallbacks; public int sampleRate,channels,droppedSamples,formatChanges,peakActiveVoices,suppressedOverlaps; public long samples; public Marker[] markers;public ScenarioSound.CueDiagnostic[] cues; }
  // The listener filter is not installed until the scene exists. Hold this game's
  // master volume at zero before any scene Awake/OnEnable/playOnAwake can run.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void GuardStartup(){
   captureRequested=ReadOutputArgument(Environment.GetCommandLineArgs(),out _,out _);
   if(captureRequested){previousVolume=AudioListener.volume;AudioListener.volume=0;}
  }
  public static bool ReadOutputArgument(string[] args,out string output,out string error){
   output=null;error=null;int i=Array.IndexOf(args,"-audio-capture");if(i<0)return false;
   if(i+1>=args.Length||string.IsNullOrWhiteSpace(args[i+1])||args[i+1].StartsWith("-")){error="-audio-capture requires a WAV output path";return true;}
   output=args[i+1];return true;
  }
  static void FailClosed(string error){AudioListener.volume=0;AudioListener.pause=true;Debug.LogError("Audio capture startup failed: "+error);Application.Quit(1);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
   if(!ReadOutputArgument(Environment.GetCommandLineArgs(),out var output,out var error))return;
   if(error!=null){FailClosed(error);return;}
   try{
    var listeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
    if(listeners.Length!=1||!listeners[0].enabled){FailClosed("Expected exactly one enabled AudioListener");return;}
    var capture=listeners[0].gameObject.AddComponent<ScenarioAudioCapture>();capture.Begin(output);
    // Source samples are retained for the WAV; only the outgoing buffer is cleared.
    AudioListener.volume=previousVolume;
   }catch(Exception e){FailClosed(e.Message);}
  }
  void Begin(string output){path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));rate=AudioSettings.outputSampleRate;loop=FindFirstObjectByType<LoopDirector>();if(loop&&loop.sound)loop.sound.CueStarted+=OnCue;worker=new Thread(WriteLoop){IsBackground=true,Name="Scenario QA audio writer"};worker.Start();}
  void OnAudioFilterRead(float[] data,int count){
   try {
   if(worker==null||stopping)return;
   if(channels==0)Volatile.Write(ref channels,count);
   else if(channels!=count){Interlocked.Add(ref formatChanges,1);return;}
   long w=written,r=Volatile.Read(ref read);if(w-r+data.Length>Capacity){Interlocked.Add(ref dropped,data.Length);return;}
   int at=(int)(w&(Capacity-1)),first=Math.Min(data.Length,Capacity-at);Array.Copy(data,0,ring,at,first);if(first<data.Length)Array.Copy(data,first,ring,0,data.Length-first);Volatile.Write(ref written,w+data.Length);
   } finally {Array.Clear(data,0,data.Length);Interlocked.Increment(ref clearedCallbacks);}
  }
  void OnCue(string id){long n=Volatile.Read(ref written);markers.Add(new Marker{state="cue:"+id,sample=n,seconds=channels>0?(double)n/(rate*channels):0,scene=loop.scenes.PlayingScene,timelineSeconds=string.IsNullOrEmpty(loop.scenes.PlayingScene)?-1:loop.scenes.FrameTime});}
  public void Mark(string state){long n=Volatile.Read(ref written);markers.Add(new Marker{state=state,sample=n,seconds=channels>0?(double)n/(rate*channels):0});}
  void Update(){if(loop){string state=loop.Finished?"finished":loop.flat.player.paused?"pause":loop.scenes.PlayingScene??(loop.sound.DeathRecovery?"death-recovery":loop.chase.Active?"chase":"lap-"+loop.Progress.Lap);if(state!=lastState){lastState=state;long n=Volatile.Read(ref written);markers.Add(new Marker{state=state,sample=n,seconds=channels>0?(double)n/(rate*channels):0});}}}
  void WriteLoop(){
   try{using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read,65536)){stream.Write(new byte[44],0,44);var chunk=new float[16384];var bytes=new byte[chunk.Length*4];
    while(!stopping||Volatile.Read(ref read)<Volatile.Read(ref written)){
     long r=read,w=Volatile.Read(ref written);int length=(int)Math.Min(chunk.Length,w-r);if(length==0){Thread.Sleep(5);continue;}
     int at=(int)(r&(Capacity-1)),first=Math.Min(length,Capacity-at);Array.Copy(ring,at,chunk,0,first);if(first<length)Array.Copy(ring,0,chunk,first,length-first);Volatile.Write(ref read,r+length);Buffer.BlockCopy(chunk,0,bytes,0,length*4);stream.Write(bytes,0,length*4);
    }
    int ch=channels>0?channels:2;int size=checked((int)(stream.Length-44));stream.Position=0;using(var writer=new BinaryWriter(stream,Encoding.ASCII,true)){writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(size+36);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)3);writer.Write((short)ch);writer.Write(rate);writer.Write(rate*ch*4);writer.Write((short)(ch*4));writer.Write((short)32);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(size);}
   }}catch(Exception e){failure=e.ToString();}
  }
  public void Save(){if(saved||worker==null)return;if(loop&&loop.sound)loop.sound.CueStarted-=OnCue;stopping=true;worker.Join();saved=true;var report=new Report{clearedOutputCallbacks=Interlocked.Read(ref clearedCallbacks),sampleRate=rate,channels=channels,samples=read,droppedSamples=dropped,formatChanges=formatChanges,error=failure,peakActiveVoices=loop&&loop.sound?loop.sound.PeakVoiceCount:0,suppressedOverlaps=loop&&loop.sound?loop.sound.SuppressedOverlaps:0,markers=markers.ToArray(),cues=loop&&loop.sound?loop.sound.Diagnostics:null};File.WriteAllText(Path.ChangeExtension(path,"json"),JsonUtility.ToJson(report,true));if(failure!=null||dropped>0||formatChanges>0||read==0)Debug.LogError("Audio capture failed/incomplete: "+failure+" dropped="+dropped+" samples="+read);else Debug.Log("AUDIO_CAPTURE_SAVED "+path+" samples="+read);}
  void OnApplicationQuit(){if(captureRequested)AudioListener.volume=0;Save();} void OnDestroy(){if(captureRequested)AudioListener.volume=0;Save();}
 }
}