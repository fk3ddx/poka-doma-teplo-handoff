using System;
using System.IO;
using UnityEngine;
namespace Ataka {
 public static class GameSettingsStore {
  public static GameSettings Load(string path){
   if(!File.Exists(path))return new GameSettings();
   try {var settings=JsonUtility.FromJson<GameSettings>(File.ReadAllText(path))??new GameSettings();Validate(settings);return settings;}
   catch(Exception e) when(e is IOException||e is ArgumentException){return new GameSettings();}
  }
  public static void Save(string path,GameSettings settings){
   Validate(settings);var directory=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
   File.WriteAllText(path,JsonUtility.ToJson(settings,true));
  }
  public static void Validate(GameSettings settings){
   settings.quality=(VisualQuality)Mathf.Clamp((int)settings.quality,0,2);
   settings.rasterStyle=(RasterStyle)Mathf.Clamp((int)settings.rasterStyle,0,2);
   if(settings.fpsLimit!=30&&settings.fpsLimit!=60&&settings.fpsLimit!=120&&settings.fpsLimit!=-1)settings.fpsLimit=60;
   settings.fieldOfView=float.IsNaN(settings.fieldOfView)||float.IsInfinity(settings.fieldOfView)?70:Mathf.Clamp(settings.fieldOfView,60,90);
   settings.sensitivity=float.IsNaN(settings.sensitivity)||float.IsInfinity(settings.sensitivity)?.085f:Mathf.Clamp(settings.sensitivity,.025f,.25f);
  }
 }
}
