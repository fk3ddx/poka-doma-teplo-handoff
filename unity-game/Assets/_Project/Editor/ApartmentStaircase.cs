using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void Staircase(){
   foreach(string name in new[]{"Landing west","Landing east","Landing ceiling"}){var surface=GameObject.Find(name);if(surface!=null)surface.GetComponent<Renderer>().sharedMaterial=mats["Plaster"];}
   var landing=GameObject.Find("Fifth floor landing");if(landing!=null)landing.GetComponent<Renderer>().sharedMaterial=mats["Concrete"];
   var old=GameObject.Find("Landing rear");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
   M("StairGreen",C(.19f,.29f,.24f),.16f);Pbr(mats["StairGreen"],"plastered_wall_02",.7f);
   Box("Stairwell rear",new Vector3(7.7f,-.1f,11.65f),new Vector3(2.5f,5.4f,.2f),"Plaster");
   foreach(float x in new[]{6.55f,8.85f}){
    Box("Stairwell side",new Vector3(x,-.1f,9.9f),new Vector3(.2f,5.4f,3.7f),"Plaster");
    Box("Landing green paint",new Vector3(x+(x<7?.105f:-.105f),.70f,7.2f),new Vector3(.009f,1.4f,1.8f),"StairGreen",false);
    Box("Stairwell green paint",new Vector3(x+(x<7?.105f:-.105f),-.70f,9.8f),new Vector3(.009f,2.8f,3.5f),"StairGreen",false);
   }
   Box("Stairwell ceiling",new Vector3(7.7f,2.6f,9.9f),new Vector3(2.5f,.2f,3.7f),"Plaster");
   Box("Fourth floor landing",new Vector3(7.7f,-2.9f,7.3f),new Vector3(2.3f,.2f,2.2f),"Concrete");
   Box("Half landing",new Vector3(7.7f,-1.5f,11.1f),new Vector3(2.3f,.2f,.9f),"Concrete");
   for(int i=0;i<12;i++){
    float z=8.13f+i*.235f,top=-(i+1)*1.4f/12;
    Box("Descending concrete step",new Vector3(7.12f,top-.12f,z),new Vector3(.94f,.24f,.238f),"Concrete");
    Box("Returning concrete step",new Vector3(8.27f,-2.8f+(i+1)*1.4f/12-.12f,z),new Vector3(.94f,.24f,.238f),"Concrete");
    if(i%2==0){Rod("Stair baluster",new Vector3(7.60f,top,z),new Vector3(7.60f,top+.93f,z),.015f,"Metal");Rod("Stair baluster",new Vector3(7.79f,-2.8f+(i+1)*1.4f/12,z),new Vector3(7.79f,-1.87f+(i+1)*1.4f/12,z),.015f,"Metal");}
   }
   Rod("Wooden handrail",new Vector3(7.60f,.82f,8.13f),new Vector3(7.60f,-.47f,10.72f),.029f,"Wood");Rod("Wooden handrail",new Vector3(7.79f,-1.75f,8.13f),new Vector3(7.79f,-.47f,10.72f),.029f,"Wood");
   Box("Fourth floor end wall",new Vector3(7.7f,-1.45f,6.20f),new Vector3(2.5f,2.7f,.18f),"StairGreen");
   Box("Fourth floor closed door",new Vector3(8.06f,-1.8f,6.305f),new Vector3(.88f,2.0f,.04f),"Wood");
   Label("48",new Vector3(8.06f,-1.22f,6.332f),Quaternion.Euler(0,180,0),.035f,C(.74f,.69f,.5f));
   foreach(float x in new[]{6.666f,8.734f}){
    Box("Neighbour upholstered door",new Vector3(x,1,7.22f),new Vector3(.04f,2,.81f),"TVBrown");
    Label(x<7?"59":"61",new Vector3(x+(x<7?.026f:-.026f),1.65f,7.22f),Quaternion.Euler(0,x<7?-90:90,0),.035f,C(.73f,.68f,.52f));
   }
   Box("Attic hatch",new Vector3(7.64f,2.485f,7.4f),new Vector3(.73f,.028f,.88f),"Wood");
   var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name="Landing bare bulb";bulb.transform.SetParent(art,false);bulb.transform.localPosition=new Vector3(8.25f,2.26f,7.45f);bulb.transform.localScale=new Vector3(.08f,.12f,.08f);bulb.GetComponent<Renderer>().sharedMaterial=mats["Bulb"];UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
   Rod("Bulb wire",new Vector3(8.25f,2.31f,7.45f),new Vector3(8.25f,2.49f,7.45f),.004f,"Black");
   Point("Landing weak bulb",new Vector3(8.25f,2.17f,7.45f),C(1,.78f,.52f),.43f,4,true);
   Label("ЦОЙ ЖИВ",new Vector3(6.659f,1.65f,8.77f),Quaternion.Euler(0,-90,0),.033f,C(.13f,.14f,.12f));
   var session=UnityEngine.Object.FindFirstObjectByType<Ataka.GameSession>();session.checkpointMin=new Vector3(-.1f,8.65f,-1.5f);session.checkpointMax=new Vector3(9,14.3f,11.6f);
  }
 }
}
