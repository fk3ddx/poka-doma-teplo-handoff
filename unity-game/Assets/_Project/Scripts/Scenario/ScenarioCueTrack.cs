using UnityEngine.Timeline;
namespace Ataka.Scenario {
 [TrackBindingType(typeof(ScenarioTimeline)),TrackClipType(typeof(ScenarioCue))]
 public sealed class ScenarioCueTrack:TrackAsset{}
}
