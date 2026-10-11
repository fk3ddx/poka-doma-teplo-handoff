using UnityEngine;
namespace Ataka.Scenario {
 // Claude review04: pre-existing wet mark, a falling drop from t=2, residue after t=5.
 // Stage VFX only. No replacement audio has been added.
 public sealed class HangingFloorCue:MonoBehaviour {
  public ScenarioTimeline scene;
  GameObject pool,drop;Material wet,rippleMaterial;Mesh mesh;LineRenderer ripple;Vector3 origin;bool anchored;
  void Awake(){
   wet=new Material(Shader.Find("Universal Render Pipeline/Lit"));wet.SetColor("_BaseColor",new Color(.025f,.022f,.02f));wet.SetFloat("_Smoothness",.9f);
   pool=new GameObject("Hanging floor wet mark");pool.transform.SetParent(transform,false);
   mesh=new Mesh{name="Small irregular wet decal"};const int n=32;var v=new Vector3[n+1];var tri=new int[n*3];
   for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,r=.11f*(1+.08f*Mathf.Sin(a*5));v[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);tri[i*3]=0;tri[i*3+1]=(i+1)%n+1;tri[i*3+2]=i+1;}
   mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();pool.AddComponent<MeshFilter>().sharedMesh=mesh;pool.AddComponent<MeshRenderer>().sharedMaterial=wet;
   drop=GameObject.CreatePrimitive(PrimitiveType.Sphere);drop.name="Hanging falling drop";drop.transform.SetParent(transform,false);Destroy(drop.GetComponent<Collider>());drop.GetComponent<Renderer>().sharedMaterial=wet;
   rippleMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));rippleMaterial.SetFloat("_Surface",1);rippleMaterial.SetFloat("_ZWrite",0);rippleMaterial.SetFloat("_SrcBlend",5);rippleMaterial.SetFloat("_DstBlend",10);rippleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");rippleMaterial.renderQueue=3000;
   ripple=new GameObject("Wet impact ripple").AddComponent<LineRenderer>();ripple.transform.SetParent(transform,false);ripple.sharedMaterial=rippleMaterial;ripple.positionCount=32;ripple.loop=true;ripple.useWorldSpace=true;ripple.startWidth=ripple.endWidth=.0015f;ripple.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   ResetCue();
  }
  public void ResetCue(){anchored=false;if(pool)pool.SetActive(false);if(drop)drop.SetActive(false);if(ripple)ripple.enabled=false;}
  public void Prepare(){var toe=scene.flat.grandmother.GetComponent<NinaHangingPose>().RightToePosition;origin=new Vector3(toe.x,.004f,toe.z);anchored=true;pool.transform.position=origin;pool.SetActive(true);}
  void LateUpdate(){
   bool playing=scene&&scene.PlayingScene=="HangingVision";float t=playing?scene.FrameTime:0;
   if(playing&&!anchored)Prepare();
   pool.SetActive(anchored);drop.SetActive(false);ripple.enabled=false;
   if(!playing||t<2||t>=5)return;
   var toe=scene.flat.grandmother.GetComponent<NinaHangingPose>().RightToePosition;
   float phase=Mathf.Repeat(t-2,1.2f),fall=Mathf.Max(0,phase-.5f);float y=toe.y-4.905f*fall*fall;
   drop.SetActive(y>.006f);drop.transform.position=new Vector3(toe.x,Mathf.Max(.006f,y),toe.z);drop.transform.localScale=new Vector3(.012f,.024f,.012f)*Mathf.Clamp01((phase+.1f)/.5f);
   float impact=.5f+Mathf.Sqrt(Mathf.Max(0,toe.y-.006f)/4.905f),age=phase-impact;
   if(age>=0&&age<.3f){ripple.enabled=true;float u=age/.3f,r=Mathf.Lerp(.02f,.06f,u);rippleMaterial.SetColor("_BaseColor",new Color(.24f,.24f,.23f,(1-u)*.5f));for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;ripple.SetPosition(i,new Vector3(toe.x+Mathf.Cos(a)*r,.006f,toe.z+Mathf.Sin(a)*r));}}
  }
  void OnDestroy(){if(wet)Destroy(wet);if(rippleMaterial)Destroy(rippleMaterial);if(mesh)Destroy(mesh);}
 }
}
