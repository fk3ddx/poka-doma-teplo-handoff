using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // Physical aim wins; a nearby off-axis door cannot steal E from a phone or cupboard.
 public sealed class ScenarioFocus {
  struct Target {public Transform key,root;public float range,cosine;public bool fallback;}
  readonly List<Target> targets=new List<Target>(9);readonly RaycastHit[] hits=new RaycastHit[64];
  void Add(Transform key,Transform root,float range,float cosine,bool fallback){if(key&&root)targets.Add(new Target{key=key,root=root,range=range,cosine=cosine,fallback=fallback});}
  public Transform Select(FlatBindings flat){
   targets.Clear();var p=flat.player;var eye=p.head.position;var forward=p.head.forward;
   Add(flat.phone,flat.phoneModel,1.65f,.86f,true);
   if(!p.hasFlashlight&&flat.hallCupboard)Add(flat.hallCupboard.interaction,flat.hallCupboard.transform,1.7f,.86f,true);
   foreach(var d in new[]{flat.kitchen,flat.bath,flat.bedroom,flat.living})Add(d.transform,d.transform,1.65f,.86f,!d.locked);
   Add(flat.carpet.transform,flat.carpet.transform,1.6f,.7f,!flat.carpet.locked);
   Add(flat.entrance.transform,flat.entrance.transform,1.8f,.6f,true);
   int count=Physics.RaycastNonAlloc(eye,forward,hits,3,~(1<<31),QueryTriggerInteraction.Ignore);Transform hit=null;float nearest=float.MaxValue;
   for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(p.transform)&&hits[i].distance<nearest){nearest=hits[i].distance;hit=hits[i].transform;}
   Transform selected=null;float best=-1;
   foreach(var t in targets){var delta=t.key.position-eye;if(delta.magnitude>t.range)continue;
    if(hit){if(hit.IsChildOf(t.root))return t.key;continue;}
    float angle=Vector3.Dot(forward,delta.normalized);if(t.fallback&&angle>t.cosine&&angle>best){best=angle;selected=t.key;}
   }
   return selected;
  }
 }
}
