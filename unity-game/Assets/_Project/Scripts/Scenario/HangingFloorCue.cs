using UnityEngine;
namespace Ataka.Scenario {
 // SCENARIO 7.5: unspecified liquid dripping from t=2. Stage VFX only.
 public sealed class HangingFloorCue:MonoBehaviour {
  public ScenarioTimeline scene;
  GameObject pool,drop;Material wet;Mesh mesh;Vector3 origin;bool anchored;
  void Awake(){
   wet=new Material(Shader.Find("Universal Render Pipeline/Lit"));wet.SetColor("_BaseColor",new Color(.025f,.022f,.02f));wet.SetFloat("_Smoothness",.88f);
   pool=new GameObject("Hanging floor wet mark");pool.transform.SetParent(transform,false);
   mesh=new Mesh{name="Small irregular wet decal"};const int n=32;var v=new Vector3[n+1];var tri=new int[n*3];
   for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,r=.11f*(1+.08f*Mathf.Sin(a*5));v[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);tri[i*3]=0;tri[i*3+1]=(i+1)%n+1;tri[i*3+2]=i+1;}
   mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();pool.AddComponent<MeshFilter>().sharedMesh=mesh;pool.AddComponent<MeshRenderer>().sharedMaterial=wet;
   drop=GameObject.CreatePrimitive(PrimitiveType.Sphere);drop.name="Hanging falling drop";drop.transform.SetParent(transform,false);Destroy(drop.GetComponent<Collider>());drop.GetComponent<Renderer>().sharedMaterial=wet;pool.SetActive(false);drop.SetActive(false);
  }
  void LateUpdate(){
   bool playing=scene&&scene.PlayingScene=="HangingVision";float t=playing?scene.FrameTime:0;
   if(!playing){pool.SetActive(false);drop.SetActive(false);anchored=false;return;}
   bool visible=t>=2&&t<5;pool.SetActive(visible);drop.SetActive(visible);if(!visible)return;
   var nina=scene.flat.grandmother.GetComponent<NinaHangingPose>();var toe=nina.RightToePosition;
   if(!anchored){origin=new Vector3(toe.x,.004f,toe.z);anchored=true;}pool.transform.position=origin;
   float phase=Mathf.Repeat(t-2,1.2f),fall=Mathf.Max(0,phase-.5f);float y=toe.y-4.905f*fall*fall;
   drop.SetActive(y>.006f);drop.transform.position=new Vector3(toe.x,Mathf.Max(.006f,y),toe.z);drop.transform.localScale=new Vector3(.012f,.022f,.012f)*Mathf.Clamp01((phase+.1f)/.5f);
  }
  void OnDestroy(){if(wet)Destroy(wet);if(mesh)Destroy(mesh);}
 }
}
