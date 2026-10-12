using UnityEngine;
namespace Ataka.Scenario {
 // 4.7 seed 1 (story26-evening-a: in the accepted rough probe-128 mirror the figure was invisible — a brown smear).
 // While the seed is on, a real reflection is shown over the dresser mirror: a camera at his eye mirrored through the
 // mirror plane, with an off-axis projection exactly through the mirror rectangle (its near plane IS the mirror, so the
 // wall behind is clipped), renders into a texture that a quad lying on the glass shows (the project's own URP Unlit
 // «Рубин» screen material). It also sees layer 31, where the figure stands, which his own camera never draws.
 // It is switched on before he looks (when he comes near) and off once he has looked away or left, so the mirror
 // never visibly changes under his eyes. The accepted mirror material, probe and lights are not touched.
 [DefaultExecutionOrder(700)]
 public sealed class MirrorGlimpse:MonoBehaviour {
  Renderer mirror;Camera view,cam;RenderTexture rt;MeshRenderer quad;Mesh mesh;Material mat;Vector3 normal,center;Vector3[] corners=new Vector3[4];
  public bool On{get;private set;}
  public RenderTexture QaTexture=>rt;public Renderer QaQuad=>quad;
  public static MirrorGlimpse Create(Renderer mirror,Camera view,Material unlitScreen){
   if(!mirror||!view||!unlitScreen)return null;var g=new GameObject("Mirror glimpse (4.7 seed)").AddComponent<MirrorGlimpse>();g.mirror=mirror;g.view=view;g.Build(unlitScreen);return g;}
  void Build(Material unlitScreen){
   // the glass: the thinnest axis of its bounds is the normal, pointing into the room (towards the viewer)
   var b=mirror.bounds;var s=b.size;int axis=s.x<s.y&&s.x<s.z?0:s.y<s.z?1:2;normal=Vector3.zero;normal[axis]=1;if(Vector3.Dot(view.transform.position-b.center,normal)<0)normal=-normal;
   center=b.center+normal*(b.extents[axis]+.004f);
   var u=axis==1?Vector3.right:Vector3.up;var r=Vector3.Cross(u,normal).normalized;u=Vector3.Cross(normal,r).normalized;
   float hw=Vector3.Scale(b.extents,new Vector3(Mathf.Abs(r.x),Mathf.Abs(r.y),Mathf.Abs(r.z))).magnitude,hh=Vector3.Scale(b.extents,new Vector3(Mathf.Abs(u.x),Mathf.Abs(u.y),Mathf.Abs(u.z))).magnitude;
   corners[0]=center-r*hw-u*hh;corners[1]=center+r*hw-u*hh;corners[2]=center+r*hw+u*hh;corners[3]=center-r*hw+u*hh;
   int w=512,h=Mathf.Clamp(Mathf.RoundToInt(512*hh/Mathf.Max(.05f,hw)),128,1024);rt=new RenderTexture(w,h,24){name="Mirror glimpse",filterMode=FilterMode.Bilinear};
   var cg=new GameObject("mirror camera");cg.transform.SetParent(transform,false);cam=cg.AddComponent<Camera>();cam.CopyFrom(view);cam.targetTexture=rt;cam.cullingMask=view.cullingMask|(1<<31);cam.depth=view.depth-1;cam.enabled=false;
   var data=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(!data)data=cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=false; // his camera's VHS lies over the whole frame anyway
   var qg=new GameObject("mirror glass picture");qg.transform.SetParent(transform,false);mesh=new Mesh{name="Mirror glimpse quad"};mesh.vertices=corners;mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=new Vector2[4];mesh.RecalculateNormals();mesh.RecalculateBounds();
   qg.AddComponent<MeshFilter>().sharedMesh=mesh;quad=qg.AddComponent<MeshRenderer>();quad.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;quad.receiveShadows=false;
   mat=new Material(unlitScreen){name="Mirror glimpse"};mat.SetTexture("_BaseMap",rt);mat.SetColor("_BaseColor",new Color(.8f,.8f,.8f,1));quad.sharedMaterial=mat;quad.enabled=false;
   // facing the room: flip the winding if the normal of the first triangle points into the wall
   if(Vector3.Dot(Vector3.Cross(corners[2]-corners[0],corners[1]-corners[0]),normal)<0)mesh.triangles=new[]{0,1,2,0,2,3};
  }
  public void Show(bool on){On=on;if(cam)cam.enabled=on;if(quad)quad.enabled=on;if(on)Place();}
  void LateUpdate(){if(On)Place();}
  // The eye mirrored through the glass plane, looking along the normal; the frustum through the glass rectangle.
  void Place(){
   var eye=view.transform.position;float d=Vector3.Dot(eye-center,normal);if(d<.05f){quad.enabled=false;return;}quad.enabled=true;
   var mirrored=eye-2*d*normal;var rot=Quaternion.LookRotation(normal,Mathf.Abs(normal.y)>.9f?Vector3.forward:Vector3.up);cam.transform.SetPositionAndRotation(mirrored,rot);
   var inv=Quaternion.Inverse(rot);float l=float.MaxValue,r=float.MinValue,bo=float.MaxValue,t=float.MinValue;var local=new Vector3[4];
   for(int i=0;i<4;i++){local[i]=inv*(corners[i]-mirrored);l=Mathf.Min(l,local[i].x);r=Mathf.Max(r,local[i].x);bo=Mathf.Min(bo,local[i].y);t=Mathf.Max(t,local[i].y);}
   float near=Mathf.Max(.01f,local[0].z);cam.projectionMatrix=Matrix4x4.Frustum(l,r,bo,t,near,40);
   var uv=new Vector2[4];for(int i=0;i<4;i++)uv[i]=new Vector2((local[i].x-l)/(r-l),(local[i].y-bo)/(t-bo));mesh.uv=uv;
  }
  void OnDestroy(){if(rt){rt.Release();Destroy(rt);}if(mesh)Destroy(mesh);if(mat)Destroy(mat);}
 }
}
