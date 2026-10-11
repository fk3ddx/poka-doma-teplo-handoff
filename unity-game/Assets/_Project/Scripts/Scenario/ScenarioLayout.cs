using UnityEngine;
namespace Ataka.Scenario {
 public static class ScenarioLayout {
  public const float Ceiling=2.5f,OuterWall=.35f,InnerWall=.1f,Eye=1.55f;
  public readonly struct Room {
   public readonly string id; public readonly Rect bounds;
   public Room(string id,float x0,float x1,float z0,float z1){this.id=id;bounds=Rect.MinMaxRect(x0,z0,x1,z1);}
  }
  public static readonly Room[] Rooms={
   new Room("Bedroom",0,2.8f,0,4.4f),new Room("Living",2.9f,6.5f,0,4.4f),
   new Room("Kitchen",6.6f,8.8f,0,2.9f),new Room("HallA",6.6f,8.8f,3,6),
   new Room("HallB",4.7f,6.6f,4.5f,6),new Room("Bathroom",2.9f,4.6f,4.5f,6),
   new Room("Store",0,2.9f,4.5f,6),new Room("Balcony",2.95f,6.45f,-1.3f,-.35f)};
  public static readonly Vector3 Spawn=new Vector3(8.05f,.02f,5.65f);
  public static readonly Vector3 Clock=new Vector3(5.5f,1.05f,5.9f);
  public static readonly Vector3 Dresser=new Vector3(6.9f,0,3.7f);
  public static readonly Vector3 Wardrobe=new Vector3(2.05f,0,4.07f);
  public static readonly Vector3 Bed=new Vector3(.6f,0,1.8f);
  public static readonly Vector3 Television=new Vector3(6.1f,.85f,2.25f);
  public static readonly Vector3 BalconyDoor=new Vector3(5.175f,1,0);
 }
}
