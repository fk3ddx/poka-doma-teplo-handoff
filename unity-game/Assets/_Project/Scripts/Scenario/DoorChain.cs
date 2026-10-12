using UnityEngine;
namespace Ataka.Scenario {
 // The entrance door chain for the finale (11.6), made from a READY asset: a runtime copy of the ball chain of the
 // Poly Haven "pull_chain_light_socket" (Josh Dean, CC0) already in the bedroom. There is no door-chain model in the
 // project; this stands in for it (14.6 cm ball chain). From a slot on the wall beside the opening to the inside face of
 // the leaf near its free edge; held = its leaf end follows her nail; hanging = straight down from the wall slot.
 public sealed class DoorChain:MonoBehaviour {
  public enum State {Engaged,Held,Hanging}
  public State Is{get;private set;}=State.Engaged;
  public ScenarioDoor door;public Vector3 anchorLocal=new Vector3(.52f,.45f,.07f);   // entrance-local: wall face beside the opening, 1.45 m
  public Vector3 leafLocal=new Vector3(.84f,1.45f,.03f);                            // hinge-local: inside face of the leaf near its free edge
  Transform pivot,model;float length=.146f;System.Func<Vector3> holder;
  public bool Ready=>model;
  public Vector3 Anchor=>door.transform.TransformPoint(anchorLocal);
  public Vector3 LeafEnd=>door.leaf.TransformPoint(leafLocal);
  public Vector3 FreeEnd{get;private set;}
  public static DoorChain Create(ScenarioDoor door){
   MeshFilter source=null;foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(f.name.Contains("pull_chain_light_socket_chain")&&f.sharedMesh){source=f;break;}
   var chain=new GameObject("STORY door chain (Poly Haven pull chain, CC0)").AddComponent<DoorChain>();chain.door=door;
   if(!source){Debug.LogWarning("DoorChain: ready ball chain (pull_chain_light_socket_chain) not found in the scene");return chain;}
   chain.pivot=new GameObject("pivot").transform;chain.pivot.SetParent(chain.transform,false);
   var copy=Instantiate(source.gameObject,chain.pivot,false);copy.name="ball chain";foreach(var c in copy.GetComponentsInChildren<Collider>(true))Destroy(c);foreach(Transform child in copy.transform)Destroy(child.gameObject);
   var scale=source.transform.lossyScale;copy.transform.localScale=scale;
   // Long axis of the mesh → pivot +Z, its upper end at the pivot.
   var b=source.sharedMesh.bounds;var size=Vector3.Scale(b.size,scale);int axis=size.x>size.y&&size.x>size.z?0:size.y>size.z?1:2;var dir=Vector3.zero;dir[axis]=1;
   chain.length=size[axis];var end=b.center+Vector3.Scale(dir,b.extents)*(axis==1?1:-1);
   copy.transform.localRotation=Quaternion.FromToRotation(axis==1?Vector3.down:dir,Vector3.forward);copy.transform.localPosition=-(copy.transform.localRotation*Vector3.Scale(end,scale));
   chain.model=copy.transform;var r=copy.GetComponent<Renderer>();if(r)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   return chain;
  }
  public void Engage(){Is=State.Engaged;holder=null;}
  public void Hold(System.Func<Vector3> nail){Is=State.Held;holder=nail;}
  public void Hang(){Is=State.Hanging;holder=null;}
  void LateUpdate(){
   if(!pivot||!door)return;var a=Anchor;
   var to=Is==State.Engaged?LeafEnd:Is==State.Held&&holder!=null?holder():a+Vector3.down*length;FreeEnd=to;
   var d=to-a;if(d.sqrMagnitude<1e-6f)d=Vector3.down*length;
   pivot.SetPositionAndRotation(a,Quaternion.LookRotation(d.normalized,Mathf.Abs(Vector3.Dot(d.normalized,Vector3.up))>.95f?door.transform.forward:Vector3.up));
   pivot.localScale=Vector3.one*Mathf.Clamp(d.magnitude/Mathf.Max(.01f,length),.6f,1.5f);
  }
 }
}
