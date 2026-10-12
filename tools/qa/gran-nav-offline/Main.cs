using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Text.Json;using UnityEngine;using Ataka.Scenario;
static class P{
 static Vector3 V(JsonElement e)=>new Vector3((float)e.GetProperty("x").GetDouble(),(float)e.GetProperty("y").GetDouble(),(float)e.GetProperty("z").GetDouble());
 static Dictionary<string,ScenarioDoor> doors=new Dictionary<string,ScenarioDoor>();
 static Dictionary<string,Transform> nodes=new Dictionary<string,Transform>();
 static Transform Node(string path){if(nodes.TryGetValue(path,out var t))return t;int i=path.LastIndexOf('/');var parent=i>0?Node(path.Substring(0,i)):null;t=new Transform{name=i>0?path.Substring(i+1):path,parent=parent};nodes[path]=t;return t;}
 static void Main(string[] a){
  var openDoors=new HashSet<string>(a.Length>1?a[1].Split(','):new string[0]); // door names staged open
  var ser=JsonDocument.Parse(File.ReadAllText(Path.Combine(a[0],"colliders-serialized.json"))).RootElement.GetProperty("solids");
  var opn=JsonDocument.Parse(File.ReadAllText(Path.Combine(a[0],"colliders-open.json"))).RootElement.GetProperty("solids");
  var openByPath=new Dictionary<string,JsonElement>();foreach(var s in opn.EnumerateArray())openByPath[s.GetProperty("path").GetString()]=s;
  foreach(var s in ser.EnumerateArray()){
   string path=s.GetProperty("path").GetString(),kind=s.GetProperty("kind").GetString(),door=s.GetProperty("door").GetString();
   if(kind=="CharacterController"||path.StartsWith("Pooled corridor"))continue;
   var src=s;bool isOpen=door!=""&&openDoors.Contains(door.Substring(door.LastIndexOf('/')+1));if(isOpen&&openByPath.ContainsKey(path))src=openByPath[path];
   ScenarioDoor d=null;Transform leafT=null;
   if(door!=""){if(!doors.TryGetValue(door,out d)){var dt=Node(door);d=new ScenarioDoor{name=dt.name,transform=dt};dt.comps.Add(d);doors[door]=d;}d.open=isOpen;
    // the leaf = child chain under the door: treat the collider's own parent chain (all under door) as leaf
    leafT=Node(door+"/LEAF");d.leaf=leafT;}
   var t=door!=""?new Transform{name=path.Substring(path.LastIndexOf('/')+1),parent=leafT}:Node(path);
   var c=new BoxCollider{name=t.name,transform=t,min=V(src.GetProperty("min")),max=V(src.GetProperty("max"))};c.size=c.max-c.min;t.comps.Add(c);Physics.all.Add(c);}
  foreach(var d in doors.Values){var b=Physics.all.Where(c=>c.transform.IsChildOf(d.leaf)).ToList();d.leaf.position=b.Count>0?b[0].bounds.center:new Vector3();}
  var graph=JsonDocument.Parse(File.ReadAllText(Path.Combine(a[0],"graph.json"))).RootElement.GetProperty("nodes");var N=new Dictionary<string,Vector3>();foreach(var n in graph.EnumerateArray())N[n.GetProperty("name").GetString()]=V(n.GetProperty("at"));
  foreach(var extra in a.Skip(2)){var p=extra.Split('=');var xyz=p[1].Split(':').Select(float.Parse).ToArray();N[p[0]]=new Vector3(xyz[0],0,xyz[1]);}
  var nav=new GranNav(()=>new Transform[0],()=>"x");nav.Ensure();
  Func<ScenarioDoor,bool> can=d=>d.name=="Kitchen door"||d.name=="Entrance60";
  foreach(var kv in N){if(!nav.Snap(kv.Value,.6f,out var q))Console.WriteLine("NODE "+kv.Key+" NO FREE FLOOR: "+nav.Blockers(kv.Value,.1f));else if(Vector3.Distance(q,kv.Value)>.06f)Console.WriteLine("NODE "+kv.Key+" "+kv.Value.ToString("F2")+" -> "+q.ToString("F2"));}
  string[][] walks={new[]{"Door","Kitchen"},new[]{"Kitchen","HallA"},new[]{"HallA","Door"},new[]{"Door","HallB"},new[]{"HallB","Clock"},new[]{"Clock","Armchair"},new[]{"Armchair","Door"},new[]{"Door","Landing"},new[]{"Door","Stove"}};
  foreach(var w in walks){var r=nav.Find(N[w[0]],N[w[1]],can);
   if(r.steps==null){Console.WriteLine("FAIL "+w[0]+"→"+w[1]+" closest "+r.closest.ToString("F2")+" short "+r.closestToGoal.ToString("F2")+" :: "+nav.Blockers(r.closest,.12f));continue;}
   var sm=nav.Smooth(r.steps.Select(s=>s.at).ToList());Console.WriteLine("OK   "+w[0]+"→"+w[1]+" doors["+string.Join(",",r.crossed.Select(d=>d.name))+"] "+string.Join(" ",sm.Select(p=>"("+p.x.ToString("F2")+","+p.z.ToString("F2")+")")));}
 }}
