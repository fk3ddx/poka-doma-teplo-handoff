using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ScenarioDressing:MonoBehaviour {
  public Renderer[] walls;public Texture normal,decay;public Color original=Color.white;
  public Renderer[] bedroomWalls;public Texture bedroomNormal;
  MaterialPropertyBlock properties;
  public void Apply(int lap){if(properties==null)properties=new MaterialPropertyBlock();properties.Clear();properties.SetTexture("_BaseMap",lap>=5?decay:normal);properties.SetColor("_BaseColor",lap>=5?new Color(.66f,.65f,.59f):original);foreach(var wall in walls)wall.SetPropertyBlock(properties);if(bedroomWalls!=null&&bedroomNormal){properties.SetTexture("_BaseMap",lap>=5?decay:bedroomNormal);properties.SetColor("_BaseColor",lap>=5?new Color(.66f,.65f,.59f):Color.white);foreach(var wall in bedroomWalls)if(wall)wall.SetPropertyBlock(properties);}}
 }
}
