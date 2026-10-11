using UnityEngine;
namespace Ataka.Scenario {
 [RequireComponent(typeof(TextMesh),typeof(MeshRenderer))]
 public sealed class ScenarioWallWriting:MonoBehaviour {
  public Shader shader;TextMesh text;Material material;
  void OnEnable(){text=GetComponent<TextMesh>();material=new Material(shader);GetComponent<MeshRenderer>().sharedMaterial=material;Font.textureRebuilt+=Rebuilt;text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);Bind();}
  void Rebuilt(Font font){if(text&&font==text.font)Bind();}
  void Bind(){if(material&&text.font)material.mainTexture=text.font.material.mainTexture;}
  void OnDisable(){Font.textureRebuilt-=Rebuilt;if(material)Destroy(material);}
 }
}
