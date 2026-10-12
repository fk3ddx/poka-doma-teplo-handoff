using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ScenarioExterior:MonoBehaviour {
  public Renderer[] surfaces;public Light[] practicals;public float[] intensities;
  MaterialPropertyBlock block;
  public void Blackout(bool value){if(block==null)block=new MaterialPropertyBlock();block.Clear();block.SetFloat("_Power",value?0:1);foreach(var r in surfaces)if(r)r.SetPropertyBlock(block);for(int i=0;i<practicals.Length;i++)if(practicals[i])practicals[i].intensity=value?0:intensities[i];}
 }
}
