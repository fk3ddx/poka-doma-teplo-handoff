using System;
using UnityEngine;
using UnityEngine.Audio;
namespace Ataka.Scenario {
 public enum ScenarioBus { Ambience,Foley,World,Monster,Voice,Stinger,UI }
 [Serializable] public sealed class ScenarioSoundCue {
  public string id;public AudioClip[] clips;public ScenarioBus bus=ScenarioBus.World;
  public float gainDb=-6,pitchVariation=.025f,minDistance=1,maxDistance=12;
  public int maxInstances=3,priority=100;public bool spatial=true,loop;
 }
 [CreateAssetMenu(menuName="Ataka/Scenario sound bank")]
 public sealed class ScenarioSoundBank:ScriptableObject {
  public ScenarioSoundCue[] cues;public AudioMixer mixer;public AudioMixerGroup[] groups;
 }
}