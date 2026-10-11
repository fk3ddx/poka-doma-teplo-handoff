using UnityEngine;
namespace Ataka.Scenario {
 // Small stage rope requested by Claude review 02. No character/furniture mesh generation.
 public sealed class HangingRope:MonoBehaviour {
  public ScenarioTimeline scene;
  Mesh[] meshes;MeshRenderer[] renderers;Material material;Texture2D fibres;
  void Awake(){
   fibres=new Texture2D(32,64,TextureFormat.RGB24,false);fibres.wrapMode=TextureWrapMode.Repeat;fibres.filterMode=FilterMode.Bilinear;
   for(int y=0;y<64;y++)for(int x=0;x<32;x++){float v=.8f+.2f*Mathf.PerlinNoise(x*.8f,y*.15f);fibres.SetPixel(x,y,new Color(v,v,v));}fibres.Apply();
   material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.42f,.37f,.29f));material.SetTexture("_BaseMap",fibres);material.SetFloat("_Smoothness",.05f);
   meshes=new Mesh[4];renderers=new MeshRenderer[4];
   for(int i=0;i<4;i++){var g=new GameObject("Hemp rope section "+i);g.transform.SetParent(transform,false);meshes[i]=new Mesh{name="Hemp stage tube"};meshes[i].MarkDynamic();g.AddComponent<MeshFilter>().sharedMesh=meshes[i];renderers[i]=g.AddComponent<MeshRenderer>();renderers[i].sharedMaterial=material;renderers[i].enabled=false;}
  }
  void Tube(int index,Vector3[] path,float radius){
   const int sides=8;int n=path.Length;var vertices=new Vector3[n*sides];var uv=new Vector2[vertices.Length];var tris=new int[(n-1)*sides*6];float length=0;
   for(int i=0;i<n;i++){var tangent=(path[Mathf.Min(i+1,n-1)]-path[Mathf.Max(i-1,0)]).normalized;var axis=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;var other=Vector3.Cross(tangent,axis);if(i>0)length+=Vector3.Distance(path[i-1],path[i]);
    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices[i*sides+j]=transform.InverseTransformPoint(path[i]+radius*(axis*Mathf.Cos(a)+other*Mathf.Sin(a)));uv[i*sides+j]=new Vector2(j/(float)sides,length*8);if(i<n-1){int k=(i*sides+j)*6,a0=i*sides+j,a1=i*sides+(j+1)%sides,b0=a0+sides,b1=a1+sides;tris[k]=a0;tris[k+1]=a1;tris[k+2]=b0;tris[k+3]=a1;tris[k+4]=b1;tris[k+5]=b0;}}
   }
   var mesh=meshes[index];mesh.Clear();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();
  }
  void LateUpdate(){
   if(!scene)return;if(scene.rope)scene.rope.enabled=false;
   bool visible=scene.flat.grandmother.activeSelf&&(scene.loop.HangingWaiting||scene.PlayingScene=="HangingVision");foreach(var r in renderers)r.enabled=visible;if(!visible)return;
   var nina=scene.flat.grandmother.GetComponent<NinaHangingPose>();var rotation=nina.transform.rotation*Quaternion.Euler(25,0,0);var center=nina.NeckPosition+Vector3.up*.025f;
   var ring=new Vector3[25];for(int i=0;i<ring.Length;i++){float a=i*Mathf.PI*2/24;ring[i]=center+rotation*new Vector3(Mathf.Cos(a)*.058f,0,Mathf.Sin(a)*.058f);}
   var knot=center+rotation*new Vector3(-.058f,.035f,0);Tube(0,new[]{new Vector3(1.65f,2.5f,2.3f),knot},.008f);Tube(1,ring,.008f);
   var coil=new Vector3[33];for(int i=0;i<coil.Length;i++){float f=i/32f,a=f*Mathf.PI*8;coil[i]=knot+rotation*new Vector3(Mathf.Cos(a)*.016f,(f-.5f)*.035f,Mathf.Sin(a)*.016f);}Tube(2,coil,.008f);
   Tube(3,new[]{knot,knot+Vector3.down*.08f+nina.transform.right*.015f,knot+Vector3.down*.15f+nina.transform.right*.01f},.008f);
  }
  void OnDestroy(){if(meshes!=null)foreach(var mesh in meshes)Destroy(mesh);if(material)Destroy(material);if(fibres)Destroy(fibres);}
 }
}
