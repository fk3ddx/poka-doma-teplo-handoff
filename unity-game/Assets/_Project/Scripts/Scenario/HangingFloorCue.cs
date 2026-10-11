using UnityEngine;
namespace Ataka.Scenario {
 // Claude review04: pre-existing wet mark, a falling drop from t=2, residue after t=5.
 // Stage VFX only. No replacement audio has been added.
 public sealed class HangingFloorCue:MonoBehaviour {
  public ScenarioTimeline scene;
  GameObject pool,drop;Material wet,dropMaterial,rippleMaterial;Mesh mesh;LineRenderer ripple;Vector3 origin;bool anchored;
  void Awake(){
   // Resources reference guarantees that the shader is included in a player build.
   var wetShader=Resources.Load<Shader>("ScenarioWetMark");
   if(!wetShader)throw new System.InvalidOperationException("Missing Resources/ScenarioWetMark.shader");
   wet=new Material(wetShader);wet.SetFloat("_WetFactor",.55f);
   pool=new GameObject("Hanging floor wet mark");pool.transform.SetParent(transform,false);
   mesh=new Mesh{name="Soft wet footprint quad"};
   mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
   mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
   mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
   WetPatch("Main soft wet patch",Vector3.zero,.30f,0);
   // Diameters after the procedural mask: approximately 1–2 cm.
   WetPatch("Satellite drop 1",new Vector3(.128f,0,.022f),.022f,17);
   WetPatch("Satellite drop 2",new Vector3(-.116f,0,-.061f),.018f,81);
   WetPatch("Satellite drop 3",new Vector3(.027f,0,-.132f),.027f,143);
   dropMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));dropMaterial.SetColor("_BaseColor",new Color(.12f,.11f,.1f));dropMaterial.SetFloat("_Smoothness",.9f);
   drop=GameObject.CreatePrimitive(PrimitiveType.Sphere);drop.name="Hanging falling drop";drop.transform.SetParent(transform,false);Destroy(drop.GetComponent<Collider>());drop.GetComponent<Renderer>().sharedMaterial=dropMaterial;
   rippleMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));rippleMaterial.SetFloat("_Surface",1);rippleMaterial.SetFloat("_ZWrite",0);rippleMaterial.SetFloat("_SrcBlend",5);rippleMaterial.SetFloat("_DstBlend",10);rippleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");rippleMaterial.renderQueue=3000;
   ripple=new GameObject("Wet impact ripple").AddComponent<LineRenderer>();ripple.transform.SetParent(transform,false);ripple.sharedMaterial=rippleMaterial;ripple.positionCount=32;ripple.loop=true;ripple.useWorldSpace=true;ripple.startWidth=ripple.endWidth=.0015f;ripple.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   ResetCue();
  }
  void WetPatch(string name,Vector3 position,float width,float angle){
   var part=new GameObject(name);part.transform.SetParent(pool.transform,false);part.transform.localPosition=position;
   part.transform.localScale=new Vector3(width,1,width);part.transform.localRotation=Quaternion.Euler(0,angle,0);
   part.AddComponent<MeshFilter>().sharedMesh=mesh;var r=part.AddComponent<MeshRenderer>();r.sharedMaterial=wet;
   r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
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
  void OnDestroy(){if(wet)Destroy(wet);if(dropMaterial)Destroy(dropMaterial);if(rippleMaterial)Destroy(rippleMaterial);if(mesh)Destroy(mesh);}
 }
}
