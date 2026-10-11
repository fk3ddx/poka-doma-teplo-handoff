using System.Linq;
using UnityEngine;
using UnityEditor;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void Finishing(){
   M("BathPaint",C(.70f,.75f,.70f),.05f);M("BathGrout",C(.34f,.39f,.37f),.08f);M("FloorTileCream",C(.57f,.60f,.53f),.14f);M("FloorTileGreen",C(.25f,.34f,.29f),.14f);
   Box("Bath painted west",new Vector3(2.906f,1.25f,5.25f),new Vector3(.012f,2.48f,1.49f),"BathPaint",false);
   Box("Bath painted rear",new Vector3(3.75f,1.25f,5.99f),new Vector3(1.69f,2.48f,.012f),"BathPaint",false);
   Box("Bath painted front",new Vector3(3.75f,1.25f,4.51f),new Vector3(1.69f,2.48f,.012f),"BathPaint",false);
   foreach(float z in new[]{4.70f,5.80f})Box("Bath painted door side",new Vector3(4.594f,1.25f,z),new Vector3(.012f,2.48f,.39f),"BathPaint",false);
   Box("Bath painted lintel",new Vector3(4.594f,2.25f,5.25f),new Vector3(.012f,.48f,.72f),"BathPaint",false);
   for(int row=0;row<10;row++)for(int col=0;col<11;col++){
    float x=2.985f+col*.15f,y=.08f+row*.15f;
    foreach(float z in new[]{4.52f,5.98f})Box("Old glazed bathroom tile",new Vector3(x,y,z),new Vector3(.147f,.147f,.012f),"Tile",false);
   }
   for(int row=0;row<10;row++)for(int col=0;col<10;col++)Box("West bathroom tile",new Vector3(2.917f,.08f+row*.15f,4.58f+col*.15f),new Vector3(.012f,.147f,.147f),"Tile",false);
   Box("Bath grout floor",new Vector3(3.75f,.005f,5.25f),new Vector3(1.7f,.01f,1.5f),"BathGrout",false);
   for(int i=0;i<8;i++)for(int j=0;j<7;j++)Box("Bathroom ceramic floor tile",new Vector3(3.006f+i*.212f,.013f,4.607f+j*.214f),new Vector3(.208f,.009f,.21f),(i+j)%2==0?"FloorTileCream":"FloorTileGreen",false);
   for(int i=0;i<14;i++){
    var title=GameObject.CreatePrimitive(PrimitiveType.Quad);title.name="Printed book spine "+i;title.transform.SetParent(art,false);title.transform.localPosition=new Vector3(6.138f,1.825f,1.49f+i*.061f);title.transform.localRotation=Quaternion.Euler(0,90,0);title.transform.localScale=new Vector3(.032f,.14f,1);Object.DestroyImmediate(title.GetComponent<Collider>());
    var mat=M("BookTitle"+i,Color.white,.08f);mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/BookTitle"+i+".png");mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.4f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;title.GetComponent<Renderer>().sharedMaterial=mat;title.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   // Thin collision planes follow both flights, filling the spaces between balusters.
   StairBarrier(new Vector3(7.60f,.32f,8.03f),new Vector3(7.60f,-1.04f,10.81f));
   StairBarrier(new Vector3(7.79f,-2.40f,8.03f),new Vector3(7.79f,-1.04f,10.81f));
   foreach(var lamp in Object.FindObjectsByType<Ataka.LampSwitch>(FindObjectsSortMode.None)){
    if(lamp.persistentId=="lamp-bath"){lamp.controlledLight.color=C(.86f,.91f,1);lamp.intensity=.76f;lamp.controlledLight.intensity=.76f;}
    if(lamp.persistentId=="lamp-bedroom"){lamp.controlledLight.color=C(1,.84f,.64f);lamp.intensity=.92f;lamp.controlledLight.intensity=.92f;}
   }
   // Scanned CC0 props are secondary silhouettes seen from the fifth floor.
   AddModel("covered_car",new Vector3(-5,-Floor,-17),85,1.42f);
   AddModel("covered_car",new Vector3(-12,-Floor,-20),78,1.38f);
   AddModel("painted_wooden_bench",new Vector3(4,-Floor,-9),160,.82f);
   AddModel("painted_wooden_bench",new Vector3(17,-Floor,-24),180,.82f);
   CourtyardFinish();
   // Use cooler distance light while retaining the warm interior refuges.
   var moon=new GameObject("Cloud filtered night light").AddComponent<Light>();moon.type=LightType.Directional;moon.color=C(.39f,.48f,.62f);moon.intensity=.11f;moon.shadows=LightShadows.Soft;moon.shadowStrength=.7f;moon.transform.rotation=Quaternion.Euler(35,-30,0);
   foreach(var r in art.GetComponentsInChildren<MeshRenderer>().Where(x=>x.name=="Panel block"||x.name=="Dirty snowbank"))Reproject(r);
  }
  static void StairBarrier(Vector3 a,Vector3 b){
   var go=new GameObject("Stair rail collision");go.transform.SetParent(art,false);go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,b-a);go.AddComponent<BoxCollider>().size=new Vector3(.04f,1.0f,Vector3.Distance(a,b));
  }
 }
}
