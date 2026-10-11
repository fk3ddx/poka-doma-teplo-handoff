using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ataka {
 // Only visible, nearby mirrors render. No recursive mirrors, no per-frame allocations.
 public sealed class PlanarMirror : MonoBehaviour {
  public Vector3 normal=Vector3.right;
  public Shader reflectionShader;
  Camera source,reflection;
  Renderer surface;
  RenderTexture texture;
  Material material;
  Material original;
  readonly Plane[] frustum=new Plane[6];
  UniversalRenderPipeline.SingleCameraRequest request;
  float nextFrame;
  int resolution=512;float frequency=10;
  public int Resolution=>resolution;public float Frequency=>frequency;
  public RenderTexture ReflectionTexture=>texture;
  public void Configure(int size,float hz){
   size=Mathf.Clamp(size,128,1024);frequency=Mathf.Clamp(hz,1,30);
   if(resolution==size)return;resolution=size;
   if(texture!=null){texture.Release();texture.width=size;texture.height=size;texture.Create();nextFrame=0;}
  }
  void Start(){
   surface=GetComponent<Renderer>();original=surface.sharedMaterial;
   if(reflectionShader==null){enabled=false;return;}
   var session=FindFirstObjectByType<GameSession>();source=session!=null?session.player.view:Camera.main;
   texture=new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGBHalf){name="Mirror reflection",hideFlags=HideFlags.DontSave,antiAliasing=1};texture.Create();
   material=new Material(reflectionShader){hideFlags=HideFlags.DontSave};material.SetTexture("_ReflectionTex",texture);surface.sharedMaterial=material;
   var go=new GameObject("Mirror view"){hideFlags=HideFlags.HideAndDontSave};reflection=go.AddComponent<Camera>();reflection.enabled=false;
   request=new UniversalRenderPipeline.SingleCameraRequest{destination=texture};
  }
  void LateUpdate(){
   if(source==null||reflection==null||Time.unscaledTime<nextFrame)return;
   Vector3 p=surface.bounds.center,n=normal.normalized;
   if(Vector3.Dot(source.transform.position-p,n)<.04f||Vector3.Distance(source.transform.position,p)>5)return;
   GeometryUtility.CalculateFrustumPlanes(source,frustum);if(!GeometryUtility.TestPlanesAABB(frustum,surface.bounds))return;
   nextFrame=Time.unscaledTime+1f/frequency;
   reflection.CopyFrom(source);reflection.enabled=false;reflection.cameraType=CameraType.Reflection;reflection.targetTexture=texture;reflection.cullingMask=source.cullingMask&~(1<<30);reflection.useOcclusionCulling=false;reflection.allowMSAA=false;
   var data=reflection.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.On;data.volumeLayerMask=0;
   float d=-Vector3.Dot(n,p);Matrix4x4 r=Matrix4x4.identity;
   r.m00-=2*n.x*n.x;r.m01=-2*n.x*n.y;r.m02=-2*n.x*n.z;r.m03=-2*d*n.x;
   r.m10=-2*n.y*n.x;r.m11-=2*n.y*n.y;r.m12=-2*n.y*n.z;r.m13=-2*d*n.y;
   r.m20=-2*n.z*n.x;r.m21=-2*n.z*n.y;r.m22-=2*n.z*n.z;r.m23=-2*d*n.z;
   reflection.transform.position=r.MultiplyPoint(source.transform.position);
   reflection.transform.rotation=Quaternion.LookRotation(r.MultiplyVector(source.transform.forward),r.MultiplyVector(source.transform.up));
   reflection.worldToCameraMatrix=source.worldToCameraMatrix*r;
   var view=reflection.worldToCameraMatrix;Vector3 cp=view.MultiplyPoint(p+n*.012f),cn=view.MultiplyVector(n).normalized;
   reflection.projectionMatrix=reflection.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
   material.SetMatrix("_ReflectionVP",GL.GetGPUProjectionMatrix(reflection.projectionMatrix,true)*reflection.worldToCameraMatrix);
   bool inverted=GL.invertCulling;
   try{GL.invertCulling=!inverted;RenderPipeline.SubmitRenderRequest(reflection,request);}
   finally{GL.invertCulling=inverted;}
  }
  void OnDestroy(){
   if(surface!=null&&original!=null)surface.sharedMaterial=original;
   if(reflection!=null)Destroy(reflection.gameObject);
   if(texture!=null){texture.Release();Destroy(texture);}
   if(material!=null)Destroy(material);
  }
 }
}
