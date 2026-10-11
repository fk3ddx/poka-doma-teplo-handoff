using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void RoomLightingProbes(){
   var points=new List<Vector3>();
   void Room(string name,Vector3 center,Vector3 size){
    var probe=new GameObject(name+" room reflection").AddComponent<ReflectionProbe>();
    probe.transform.position=center+Vector3.up*Floor;probe.size=size;probe.mode=ReflectionProbeMode.Baked;
    probe.resolution=128;probe.hdr=true;probe.boxProjection=true;probe.intensity=.7f;probe.blendDistance=.15f;
    probe.nearClipPlane=.08f;probe.farClipPlane=100;probe.cullingMask=~(1<<30);
    // Distributed probes stay clear of the wall surface, at floor/body/head heights.
    for(int x=0;x<3;x++)for(int z=0;z<3;z++)foreach(float y in new[]{.25f,1.15f,2.1f}){
     var v=new Vector3(center.x+(x-1)*(size.x*.5f-.25f),y,center.z+(z-1)*(size.z*.5f-.25f));
     points.Add(v+Vector3.up*Floor);
    }
   }
   Room("living",new Vector3(4.7f,1.25f,2.2f),new Vector3(3.5f,2.5f,4.3f));
   Room("bedroom",new Vector3(1.4f,1.25f,2.2f),new Vector3(2.7f,2.5f,4.3f));
   Room("kitchen",new Vector3(7.7f,1.25f,1.45f),new Vector3(2.1f,2.5f,2.8f));
   Room("bath",new Vector3(3.75f,1.25f,5.25f),new Vector3(1.6f,2.5f,1.4f));
   Room("hall",new Vector3(7.7f,1.25f,4.5f),new Vector3(2.1f,2.5f,2.9f));
   Room("hall-link",new Vector3(5.65f,1.25f,5.25f),new Vector3(1.8f,2.5f,1.4f));
   // Exclude samples inside authored furniture while retaining a connected room lattice.
   Physics.SyncTransforms();
   points.RemoveAll(v=>Physics.CheckSphere(v,.10f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));
   var group=new GameObject("Apartment dynamic light probes").AddComponent<LightProbeGroup>();group.probePositions=points.ToArray();
  }
 }
}
