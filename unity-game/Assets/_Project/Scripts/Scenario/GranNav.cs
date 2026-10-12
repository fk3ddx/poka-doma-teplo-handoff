using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Ataka.Scenario {
 // Grandmother walking: an occupancy grid swept with HER OWN capsule (same radius/height as the runtime collider),
 // A* around the real furniture, and closed doors as crossings she has to open first. An open leaf is an obstacle
 // in its actual pose. No teleports: when no route exists she stays and the failure is reported.
 public sealed class GranNav {
  public const float Radius=.22f,Height=1.5f,Lift=.08f,Cell=.05f;
  // Cells are classified with exactly her body radius at the cell centre (a margin would close the real 0.54 m
  // gap between the hall dresser and the kitchen jamb). Straight shortcuts are re-checked with real capsule sweeps.
  const float X0=1.3f,Z0=-1.4f,X1=9.0f,Z1=6.95f; // z down to the balcony slab (−1.3…−0.35)
  readonly int nx=Mathf.CeilToInt((X1-X0)/Cell),nz=Mathf.CeilToInt((Z1-Z0)/Cell);
  byte[] cells; // 0 free, 1 blocked, 2+i crossing of closed door i
  readonly List<ScenarioDoor> doors=new List<ScenarioDoor>();
  readonly Func<Transform[]> ignore;readonly Func<string> state;string signature;
  readonly Collider[] hits=new Collider[24];
  public int Builds{get;private set;}
  // A standing player she has to walk around (set by the walker for one plan + smoothing; null for QA).
  public Vector3? Avoid;public float AvoidRadius;
  bool Avoided(Vector3 p){if(Avoid==null)return false;var d=p-Avoid.Value;d.y=0;return d.magnitude<AvoidRadius;}
  public GranNav(Func<Transform[]> ignore,Func<string> state){this.ignore=ignore;this.state=state;}

  public static Vector3 Bottom(Vector3 feet)=>feet+Vector3.up*(Lift+Radius);
  public static Vector3 Top(Vector3 feet)=>feet+Vector3.up*(Height-Radius);
  bool Ignored(Collider c,Transform[] skip){foreach(var t in skip)if(t&&c.transform.IsChildOf(t))return true;return false;}
  public void Ensure(){var s=state();if(cells!=null&&s==signature)return;Build();signature=s;}
  public void Invalidate(){signature=null;}
  void Build(){
   Physics.SyncTransforms();Builds++;cells=new byte[nx*nz];doors.Clear();var skip=ignore();
   for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){
    var p=Center(x,z);int n=Physics.OverlapCapsuleNonAlloc(Bottom(p),Top(p),Radius,hits,~(1<<31),QueryTriggerInteraction.Ignore);
    // A cell needs floor under it (the grid now reaches past the outer wall to the balcony: no walking on air).
    byte code=HasFloor(p,skip)?(byte)0:(byte)1;
    for(int i=0;i<n&&code!=1;i++){var c=hits[i];if(Ignored(c,skip))continue;var d=c.GetComponentInParent<ScenarioDoor>();
     if(d&&!d.open&&d.leaf&&c.transform.IsChildOf(d.leaf)){int k=doors.IndexOf(d);if(k<0){doors.Add(d);k=doors.Count-1;}byte dc=(byte)(2+k);if(code==0)code=dc;else if(code!=dc)code=1;}
     else{code=1;break;}}
    cells[z*nx+x]=code;}
  }
  readonly RaycastHit[] down=new RaycastHit[8];
  bool HasFloor(Vector3 p,Transform[] skip){int n=Physics.RaycastNonAlloc(p+Vector3.up*.3f,Vector3.down,down,.6f,~(1<<31),QueryTriggerInteraction.Ignore);for(int i=0;i<n;i++)if(!Ignored(down[i].collider,skip))return true;return false;}
  Vector3 Center(int x,int z)=>new Vector3(X0+(x+.5f)*Cell,0,Z0+(z+.5f)*Cell);
  bool Cell2(Vector3 p,out int x,out int z){x=Mathf.FloorToInt((p.x-X0)/Cell);z=Mathf.FloorToInt((p.z-Z0)/Cell);return x>=0&&z>=0&&x<nx&&z<nz;}
  public bool IsFree(Vector3 p)=>Cell2(p,out int x,out int z)&&cells[z*nx+x]==0;
  ScenarioDoor DoorAt(int i)=>cells[i]>=2?doors[cells[i]-2]:null;

  // Nearest free floor cell to a named spot (the spot itself may sit in furniture after re-dressing).
  public bool Snap(Vector3 p,float max,out Vector3 q){
   q=p;if(!Cell2(p,out int cx,out int cz))return false;int r=Mathf.CeilToInt(max/Cell);float best=float.MaxValue;
   for(int z=Mathf.Max(0,cz-r);z<=Mathf.Min(nz-1,cz+r);z++)for(int x=Mathf.Max(0,cx-r);x<=Mathf.Min(nx-1,cx+r);x++){
    if(cells[z*nx+x]!=0)continue;var c=Center(x,z);float d=(c.x-p.x)*(c.x-p.x)+(c.z-p.z)*(c.z-p.z);if(d<best&&d<=max*max){best=d;q=c;}}
   return best<float.MaxValue;}

  public sealed class Step {public Vector3 at;public ScenarioDoor door;}
  public sealed class Result {public List<Step> steps;public Vector3 closest;public float closestToGoal;public List<ScenarioDoor> crossed=new List<ScenarioDoor>();}
  // A* on the 8-connected grid; door cells are passable only if canOpen(door).
  public Result Find(Vector3 from,Vector3 to,Func<ScenarioDoor,bool> canOpen){
   var res=new Result();
   if(!Snap(from,.45f,out var a)||!Snap(to,.6f,out var b)){res.closest=from;res.closestToGoal=Vector3.Distance(from,to);return res;}
   Cell2(a,out int ax,out int az);Cell2(b,out int bx,out int bz);int start=az*nx+ax,goal=bz*nx+bx,n=nx*nz;
   var g=new float[n];var parent=new int[n];var closed=new bool[n];for(int i=0;i<n;i++){g[i]=float.MaxValue;parent[i]=-1;}
   var heap=new Heap();g[start]=0;heap.Push(start,H(ax,az,bx,bz));int bestCell=start;float bestH=H(ax,az,bx,bz);
   while(heap.Count>0){int cur=heap.Pop();if(closed[cur])continue;closed[cur]=true;if(cur==goal)break;int cx=cur%nx,cz=cur/nx;
    float h=H(cx,cz,bx,bz);if(h<bestH){bestH=h;bestCell=cur;}
    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){if(dx==0&&dz==0)continue;int x=cx+dx,z=cz+dz;if(x<0||z<0||x>=nx||z>=nz)continue;int k=z*nx+x;if(closed[k]||!Walkable(k,canOpen))continue;
     if(dx!=0&&dz!=0&&(!Walkable(cz*nx+x,canOpen)||!Walkable(z*nx+cx,canOpen)))continue; // no corner cutting
     float ng=g[cur]+(dx!=0&&dz!=0?1.4142f:1f);if(ng<g[k]){g[k]=ng;parent[k]=cur;heap.Push(k,ng+H(x,z,bx,bz));}}}
   res.closest=Center(bestCell%nx,bestCell/nx);res.closestToGoal=bestH*Cell;
   if(!closed[goal])return res;
   var path=new List<Step>();for(int c=goal;c>=0;c=parent[c]){var d=DoorAt(c);path.Insert(0,new Step{at=Center(c%nx,c/nx),door=d});if(d&&!res.crossed.Contains(d))res.crossed.Insert(0,d);}
   res.steps=path;res.closest=b;res.closestToGoal=0;return res;}
  bool Walkable(int k,Func<ScenarioDoor,bool> canOpen){var c=cells[k];if(Avoid!=null&&Avoided(Center(k%nx,k/nx)))return false;return c==0||c>=2&&canOpen!=null&&canOpen(doors[c-2]);}
  static float H(int x,int z,int bx,int bz){int dx=Mathf.Abs(x-bx),dz=Mathf.Abs(z-bz);return Mathf.Max(dx,dz)+.4142f*Mathf.Min(dx,dz);}

  // String-pulling over strictly free cells, so a shortcut never cuts furniture or a door leaf.
  public List<Vector3> Smooth(List<Vector3> pts){
   if(pts.Count<3)return new List<Vector3>(pts);var o=new List<Vector3>{pts[0]};int i=0;
   while(i<pts.Count-1){int j=pts.Count-1;while(j>i+1&&!LineFree(pts[i],pts[j]))j--;o.Add(pts[j]);i=j;}
   return o;}
  bool LineFree(Vector3 a,Vector3 b){float len=Vector3.Distance(a,b);int s=Mathf.CeilToInt(len/(Cell*.5f));var skip=ignore();
   for(int k=0;k<=s;k++){var p=Vector3.Lerp(a,b,s==0?0:(float)k/s);if(!IsFree(p)||Avoided(p))return false;int n=Physics.OverlapCapsuleNonAlloc(Bottom(p),Top(p),Radius,hits,~(1<<31),QueryTriggerInteraction.Ignore);for(int i=0;i<n;i++)if(!Ignored(hits[i],skip))return false;}
   return true;}

  public static float DoorWidth(ScenarioDoor d){float w=.8f;if(d&&d.leaf)foreach(var b in d.leaf.GetComponentsInChildren<BoxCollider>(true)){var s=Vector3.Scale(b.size,b.transform.lossyScale);w=Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.z));}return w;}

  // QA: everything her capsule touches near a point, as full transform paths with world bounds.
  public string Blockers(Vector3 at,float around){
   var skip=ignore();var seen=new HashSet<Collider>();var sb=new StringBuilder();
   for(float dz=-around;dz<=around+1e-4f;dz+=Cell*2)for(float dx=-around;dx<=around+1e-4f;dx+=Cell*2){var p=at+new Vector3(dx,0,dz);
    int n=Physics.OverlapCapsuleNonAlloc(Bottom(p),Top(p),Radius,hits,~(1<<31),QueryTriggerInteraction.Ignore);
    for(int i=0;i<n;i++){var c=hits[i];if(Ignored(c,skip)||!seen.Add(c))continue;var d=c.GetComponentInParent<ScenarioDoor>();
     if(sb.Length>0)sb.Append("; ");sb.Append(PathOf(c.transform)).Append(" [").Append(c.GetType().Name).Append("] bounds c=").Append(c.bounds.center.ToString("F2")).Append(" size=").Append(c.bounds.size.ToString("F2"));
     if(d)sb.Append(d.open?" (OPEN leaf of ":" (closed leaf of ").Append(d.name).Append(d.locked?", locked)":")");}}
   return sb.Length>0?sb.ToString():"nothing within "+around.ToString("F2")+" m";}
  public static string PathOf(Transform t){var sb=new StringBuilder(t.name);for(var p=t.parent;p;p=p.parent)sb.Insert(0,p.name+"/");return sb.ToString();}

  sealed class Heap {readonly List<(int k,float f)> a=new List<(int,float)>();public int Count=>a.Count;
   public void Push(int k,float f){a.Add((k,f));int i=a.Count-1;while(i>0){int p=(i-1)/2;if(a[p].f<=a[i].f)break;(a[p],a[i])=(a[i],a[p]);i=p;}}
   public int Pop(){var top=a[0].k;var last=a[a.Count-1];a.RemoveAt(a.Count-1);if(a.Count>0){a[0]=last;int i=0;while(true){int l=2*i+1,r=l+1,m=i;if(l<a.Count&&a[l].f<a[m].f)m=l;if(r<a.Count&&a[r].f<a[m].f)m=r;if(m==i)break;(a[m],a[i])=(a[i],a[m]);i=m;}}return top;}}
 }
}
