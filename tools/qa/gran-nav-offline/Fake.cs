using System;using System.Collections.Generic;using System.Globalization;
namespace UnityEngine{
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>new Vector3(0,0,0);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);public static Vector3 operator*(float s,Vector3 a)=>a*s;
  public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
  public string ToString(string f)=>"("+x.ToString(f,CultureInfo.InvariantCulture)+", "+y.ToString(f,CultureInfo.InvariantCulture)+", "+z.ToString(f,CultureInfo.InvariantCulture)+")";}
 public static class Mathf{public static int CeilToInt(float f)=>(int)Math.Ceiling(f);public static int FloorToInt(float f)=>(int)Math.Floor(f);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Abs(float a)=>Math.Abs(a);public static int Abs(int a)=>Math.Abs(a);}
 public struct Bounds{public Vector3 center,size;}
 public enum QueryTriggerInteraction{Ignore}
 public class Object{public string name="";public bool alive=true;public static implicit operator bool(Object o)=>o!=null&&o.alive;}
 public class Transform:Component{public Transform(){transform=this;}public Transform parent;public Vector3 position;public Vector3 lossyScale=new Vector3(1,1,1);public List<Component> comps=new List<Component>();
  public bool IsChildOf(Transform t){for(var p=this;p!=null;p=p.parent)if(p==t)return true;return false;}}
 public class Component:Object{public Transform transform;
  public T GetComponentInParent<T>() where T:class{for(var p=transform;p!=null;p=p.parent)foreach(var c in p.comps)if(c is T t)return t;return null;}
  public T[] GetComponentsInChildren<T>(bool b) where T:class{var l=new List<T>();foreach(var c in Physics.all)if(c is T t&&c.transform.IsChildOf(transform))l.Add(t);return l.ToArray();}}
 public class Collider:Component{public Vector3 min,max;public Bounds bounds=>new Bounds{center=(min+max)*.5f,size=max-min};}
 public class BoxCollider:Collider{public Vector3 size;}
 public static class Physics{public static List<Collider> all=new List<Collider>();public static void SyncTransforms(){}
  public static int OverlapCapsuleNonAlloc(Vector3 a,Vector3 b,float r,Collider[] res,int mask,QueryTriggerInteraction q){int n=0;
   foreach(var c in all){if(!c.alive)continue;if(b.y+r<c.min.y||a.y-r>c.max.y)continue;float dx=Math.Max(0,Math.Max(c.min.x-a.x,a.x-c.max.x)),dz=Math.Max(0,Math.Max(c.min.z-a.z,a.z-c.max.z));
    if(dx*dx+dz*dz<r*r&&n<res.Length)res[n++]=c;}return n;}}
}
namespace Ataka.Scenario{public class ScenarioDoor:UnityEngine.Component{public UnityEngine.Transform leaf;public bool open,locked;public float direction=1;}}
