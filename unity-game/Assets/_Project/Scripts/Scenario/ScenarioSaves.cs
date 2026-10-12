using System;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // Where the scenario's two checkpoints live (story chapter: story-checkpoint.json; loop lap/chase: scenario-core-
 // checkpoint.json). QA only: `-qa-save-dir <dir>` redirects BOTH before either director reads or writes anything, so a
 // test never reads, changes or deletes the player's own saves in persistentDataPath. Without the flag: unchanged.
 // A flag with no folder, an unusable folder or the player's own folder is REJECTED: the paths then point to a throw-
 // away folder in the temporary cache (never persistentDataPath) and QaRejected tells the test to fail at once.
 public static class ScenarioSaves {
  static bool resolved;static string qaDirectory;
  public static bool QaRequested{get{Resolve();return requested;}}static bool requested;
  public static bool QaRejected{get{Resolve();return rejected;}}static bool rejected;
  public static string QaProblem{get{Resolve();return problem;}}static string problem="";
  public static string QaDirectory{get{Resolve();return rejected?null:qaDirectory;}}
  public static string File(string name){Resolve();return Path.Combine(qaDirectory??Application.persistentDataPath,name);}
  static void Resolve(){
   if(resolved)return;resolved=true;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-qa-save-dir");if(i<0)return;requested=true;
   try{
    var raw=i+1<args.Length?args[i+1]:null;if(string.IsNullOrWhiteSpace(raw)||raw.StartsWith("-"))throw new ArgumentException("no folder given after -qa-save-dir");
    var dir=Path.GetFullPath(raw).TrimEnd('\\','/');var own=Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\','/');
    if(string.Equals(dir,own,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("-qa-save-dir is the player's own save folder");
    Directory.CreateDirectory(dir);qaDirectory=dir;
   }catch(Exception e){rejected=true;problem=e.Message;qaDirectory=Path.Combine(Application.temporaryCachePath,"qa-save-dir-rejected");Directory.CreateDirectory(qaDirectory);Debug.LogError("-qa-save-dir rejected: "+e.Message);}
  }
 }
}
