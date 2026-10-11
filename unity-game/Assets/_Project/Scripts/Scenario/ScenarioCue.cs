using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace Ataka.Scenario {
 public sealed class ScenarioCue:PlayableAsset,ITimelineClipAsset {
  public string scene;public ClipCaps clipCaps=>ClipCaps.None;
  public override Playable CreatePlayable(PlayableGraph graph,GameObject owner){var p=ScriptPlayable<ScenarioCueBehaviour>.Create(graph);p.GetBehaviour().scene=scene;return p;}
 }
 public sealed class ScenarioCueBehaviour:PlayableBehaviour {
  public string scene;
  public override void ProcessFrame(Playable playable,FrameData info,object playerData){if(playerData is ScenarioTimeline receiver)receiver.Evaluate(scene,(float)playable.GetTime());}
 }
}
