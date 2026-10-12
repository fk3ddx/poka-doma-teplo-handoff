using System;using System.Collections;using System.Collections.Generic;using System.IO;using UnityEngine;
namespace Ataka.Scenario {
 // QA only. Equal diffuse targets under the unchanged Familiar profile, never saved into Flat.
 public static class HallLightProbe {
  [Serializable]class Sample {public string file;public Vector3 position,feet;public Vector2 pixelCenter;public float radiusPixels,meanDisplayLuma;}
  [Serializable]class Report {public string scope="QA ONLY: 8cm diffuse grey sphere, albedo .7, metallic0, smoothness0; no player flashlight or new light. Luma from central 55% radius, display-referred screenshot; also inspect silhouettes and exposure.";public Sample[] samples;}
  public static IEnumerator Run(FlatBindings flat,string output,Action<Vector3> aim){
   var player=flat.player;var leaf=flat.hallCupboard.door.leaf;
   var points=new[]{new Vector3(7.04f,.284f,4.09f),leaf.position+leaf.rotation*new Vector3(-.05f,.35f-leaf.position.y,-.29f),new Vector3(7.045f,.59f,4.03f)};
   var names=new[]{"shelf","open-leaf","tabletop"};var records=new List<Sample>();
   var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="QA grey sphere (not production)";UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());sphere.transform.localScale=Vector3.one*.08f;
   var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.7f,.7f,.7f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",0);sphere.GetComponent<Renderer>().sharedMaterial=material;
   bool oldControl=player.control;player.control=false;player.flashlight.intensity=0;flat.look.Apply(FlatCondition.Familiar);
   for(int i=0;i<points.Length;i++){
    sphere.transform.position=points[i];aim(points[i]);yield return new WaitForSeconds(.7f);yield return new WaitForEndOfFrame();
    var screen=ScreenCapture.CaptureScreenshotAsTexture();string file="grey-"+names[i]+".png";File.WriteAllBytes(Path.Combine(output,file),screen.EncodeToPNG());
    var centre=player.view.WorldToScreenPoint(points[i]);var edge=player.view.WorldToScreenPoint(points[i]+player.view.transform.right*.04f);float radius=Vector2.Distance(centre,edge);double sum=0;int count=0;
    int crop=Mathf.Max(1,Mathf.FloorToInt(radius*.55f));for(int y=-crop;y<=crop;y++)for(int x=-crop;x<=crop;x++)if(x*x+y*y<=crop*crop){var c=screen.GetPixel(Mathf.Clamp((int)centre.x+x,0,screen.width-1),Mathf.Clamp((int)centre.y+y,0,screen.height-1));sum+=.2126*c.r+.7152*c.g+.0722*c.b;count++;}
    records.Add(new Sample{file=file,position=points[i],feet=player.transform.position,pixelCenter=new Vector2(centre.x,screen.height-centre.y),radiusPixels=radius,meanDisplayLuma=(float)(sum/count)});UnityEngine.Object.Destroy(screen);
   }
   UnityEngine.Object.Destroy(sphere);UnityEngine.Object.Destroy(material);player.control=oldControl;File.WriteAllText(Path.Combine(output,"grey-probe.json"),JsonUtility.ToJson(new Report{samples=records.ToArray()},true));
  }
 }
}
