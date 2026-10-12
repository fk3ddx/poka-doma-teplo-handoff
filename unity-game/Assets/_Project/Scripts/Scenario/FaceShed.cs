using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // 11.9, second by second, from geometry that is already in the game (no new model):
 //  - her head is rebuilt from her OWN Katcho triangles (bone Head, her 256 px atlas, same UV): the front ones become a
 //    loose FACE SKIN, the rest stays as the scalp/bun; her skinned head is collapsed while they stand in for it;
 //  - inside it sits the Guest's own head (its Head-weighted triangles, its materials, scaled to fit) and its JAW
 //    (Jaw-weighted triangles, hinged at the Jaw joint) — the grey skin and eye hollows under her face.
 // At(t): 1.0 one eye of the skin slides down · 1.3 the skin splits at the temple · 1.5–2.2 it slides off the chin like
 // wet cloth and hangs at the neck · 2.0 the jaw drops · 2.6 the skin falls off. All follows the RENDERED head (after
 // GranRig, order 560) and her growth. End() removes everything and gives her skinned head back.
 // pc28d (story28-key-d / face-d: black face at 1.2, a small toothy head floating over a spike of neck at 2.8):
 //  - everything is built in the Head bone's BIND space (bindposes), not from the pose she happens to stand in: the face
 //    slice and the Guest's fit were taken along her body's forward while her real head was bowed/tilted, so the upright
 //    Guest stuck out through her face (the black) and the slice cut the wrong triangles;
 //  - her Head bone is no longer collapsed to 0.001: that pulled every neck vertex with some Head weight into a spike at
 //    the pivot (the "floating" head). Her skinned mesh is swapped for a copy without exactly the head triangles we
 //    rebuild, so the neck stays whole and the rebuilt head (head bone × bind, = her skinning) closes onto it;
 //  - the Guest's head appears only from the temple seam (1.3); normals come from her mesh (no flat re-shading).
 // pc28e (story28-face-h / key-i: still a dark mask at 1.2, a toothy head over a hole at 2.3):
 //  - only her PAINTED FACE is cut, chosen by its UV rectangle in her atlas (forehead to chin, 22 polygons); scalp, hair,
 //    ears and throat stay in her own skinned body — the 55 % depth slice also took the throat, so a hole opened under
 //    the head once the skin slid;
 //  - the face skin is drawn by a SECOND SkinnedMeshRenderer that copies her body renderer (bones, bindposes, weights,
 //    bounds, layer, rendering layers, probes, shadows, her tint block): it is lit and skinned exactly as she is;
 //  - her face at the stove is lit from above (hair bright, face dark even on her own model): a warm scenic key on her
 //    head during 11.9 only, like the «Death fill light» of 11.10, so the face, the slide and the mouth read.
 // pc28g (story28-face-i: the bit-5 key never reached her — at 0.9 her own face was as black as without it): the key
 //    is no longer limited by a rendering layer but by geometry — a narrow 36° spot 0.8 m in front of her face whose range
 //    ends 35 cm behind it, so the fridge (beside his camera, behind the light) and the walls stay as they were; QA
 //    proves it with a key-on / key-off pair at 1.2.
 // pc28f (story28-face-audit-a/b/c: her ORIGINAL Lit face is dark too; a camera fill reads but burns the fridge):
 //  - the key is the kitchen's light bounced off the floor/table in front of her — low, warm, soft — and it lights ONLY
 //    her (and the Guest's head inside hers) through its own rendering layer (bit 5): the fridge, walls and the accepted
 //    kitchen light are untouched;
 //  - Top is her RENDERED head top, measured in LateUpdate after GranRig's layer (straighten, bow) — the story keeps her
 //    growth under the ceiling with it.
 [DefaultExecutionOrder(560)]
 public sealed class FaceShed:MonoBehaviour {
  GranRig rig;Transform root,follow,guestT,jawT;Quaternion offset;Matrix4x4 meshFromFrame;SkinnedMeshRenderer body,face;Mesh bodyMesh,faceless,faceMesh,guestMesh,jawMesh,fallMesh;
  Vector3[] mesh0,meshNow;int[] faceIdx,faceTriangles,liningTriangles;Vector3[] face0;int[] eye,temple;float skinTop,skinBottom;Vector3 jawPivot;Vector3 headScale0;
  Transform fallT;Vector3 fallVelocity;bool fallen;float fallDrop;Light key;MaterialPropertyBlock block;uint bodyLayers0;List<Vector3> headPts=new List<Vector3>();float renderedTop=float.NaN;
  public bool Ready{get;private set;}public bool Shown{get;private set;}public bool Fallen=>fallen;
  public int SkinTris,ScalpTris,GuestTris,JawTris;
  public float EyeDrop{get;private set;}public float SeamGap{get;private set;}public float Slide{get;private set;}public float JawAngle{get;private set;}
  public Transform QaGuestHead=>guestT;public Transform QaJaw=>jawT;public Renderer QaFace=>face;public Light QaKey=>key;
  public bool Mirrored{get;private set;}public bool GuestInside{get;private set;}public float GuestScale{get;private set;}Vector3 herMin,herMax;
  // the top of what is rendered as her head: her own (skinned) head and, inside it, the Guest's
  public float Top=>float.IsNaN(renderedTop)?(rig?rig.Top:float.NaN):renderedTop;
  void MeasureTop(){if(!rig||!rig.head)return;float top=float.MinValue;foreach(var p in headPts)top=Mathf.Max(top,rig.head.TransformPoint(p).y);if(guestT&&guestT.gameObject.activeInHierarchy&&guestMesh)foreach(var v in guestMesh.vertices)top=Mathf.Max(top,guestT.TransformPoint(v).y);if(top>float.MinValue)renderedTop=top;}
  static int Dominant(BoneWeight w){int b=w.boneIndex0;float m=w.weight0;if(w.weight1>m){m=w.weight1;b=w.boneIndex1;}if(w.weight2>m){m=w.weight2;b=w.boneIndex2;}if(w.weight3>m){b=w.boneIndex3;}return b;}
  static Mesh Build(string name,List<Vector3> v,List<Vector2> uv,List<int>[] subs){var m=new Mesh{name=name};m.SetVertices(v);if(uv.Count==v.Count)m.SetUVs(0,uv);m.subMeshCount=subs.Length;for(int i=0;i<subs.Length;i++)m.SetTriangles(subs[i],i);m.RecalculateNormals();m.RecalculateBounds();return m;}
  Transform Part(string name,Mesh mesh,Material[] mats){var g=new GameObject(name);g.transform.SetParent(follow,false);g.layer=body.gameObject.layer;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();g.SetActive(false);r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.renderingLayerMask=body.renderingLayerMask;r.lightProbeUsage=body.lightProbeUsage;r.probeAnchor=body.probeAnchor?body.probeAnchor:body.transform;r.reflectionProbeUsage=body.reflectionProbeUsage;return g.transform;}
  // her face in the atlas (granny.png 256 px): the photo at the top left, x 0–76, y 0–66 px; her painted eyes at (30,33)/(53,33)
  static bool InFace(Vector2 uv)=>uv.x<=76/256f&&uv.y>=1-66/256f;static readonly Vector2 EyeUV=new Vector2(53/256f,1-33/256f);

  public static FaceShed Create(GranRig rig,GameObject guest){var f=new GameObject("Finale face shed (11.9)").AddComponent<FaceShed>();f.rig=rig;f.Extract(guest);return f;}
  void Extract(GameObject guest){
   rig.Init();var g=rig.transform;root=g;var head=rig.head;var skin=g.GetComponentInChildren<SkinnedMeshRenderer>(true);if(!skin||!head)return;
   body=skin;bodyMesh=skin.sharedMesh;headScale0=head.localScale;bodyLayers0=skin.renderingLayerMask;
   if(!bodyMesh.isReadable){Debug.LogError("FaceShed: her mesh must be readable: "+bodyMesh.name);return;}
   int hi=System.Array.IndexOf(skin.bones,head);if(hi<0||hi>=bodyMesh.bindposes.Length){Debug.LogWarning("FaceShed: Head is not one of her skin bones");return;}
   // her face frame inside the Head bone (bind pose): forward/up of her body expressed in head-bone space
   var bind=bodyMesh.bindposes[hi];var faceB=bind.MultiplyVector(skin.transform.InverseTransformDirection(g.forward)).normalized;var upB=bind.MultiplyVector(skin.transform.InverseTransformDirection(g.up));upB=Vector3.ProjectOnPlane(upB,faceB).normalized;
   offset=Quaternion.LookRotation(faceB,upB);var inv=Quaternion.Inverse(offset);meshFromFrame=bind.inverse*Matrix4x4.Rotate(offset);
   follow=new GameObject("follow").transform;follow.SetParent(transform,false);Mirrored=head.localToWorldMatrix.determinant<0;
   var bv=bodyMesh.vertices;var buv=bodyMesh.uv;var bw=bodyMesh.boneWeights;var tris=bodyMesh.triangles;
   var local=new Vector3[bv.Length];for(int i=0;i<bv.Length;i++)local[i]=inv*bind.MultiplyPoint3x4(bv[i]);
   for(int i=0;i<bv.Length&&i<bw.Length;i++){var w=bw[i];float hw=(w.boneIndex0==hi?w.weight0:0)+(w.boneIndex1==hi?w.weight1:0)+(w.boneIndex2==hi?w.weight2:0)+(w.boneIndex3==hi?w.weight3:0);if(hw>.5f)headPts.Add(bind.MultiplyPoint3x4(bv[i]));} // as GranRig.Top
   // her whole head (Head-dominant triangles) bounds the Guest; her painted face (UV rectangle) is the skin that slides
   herMin=Vector3.positiveInfinity;herMax=Vector3.negativeInfinity;int headTris=0;
   for(int i=0;i<tris.Length;i+=3)if(Dominant(bw[tris[i]])==hi&&Dominant(bw[tris[i+1]])==hi&&Dominant(bw[tris[i+2]])==hi){headTris++;for(int k=0;k<3;k++){herMin=Vector3.Min(herMin,local[tris[i+k]]);herMax=Vector3.Max(herMax,local[tris[i+k]]);}}
   if(headTris==0){Debug.LogWarning("FaceShed: no Head-weighted triangles in her mesh");return;}
   faceless=Instantiate(bodyMesh);faceless.name=bodyMesh.name+" (face cut, 11.9)";
   var faceTris=new List<int>();var used=new HashSet<int>();Material faceMat=skin.sharedMaterial;
   for(int s=0;s<bodyMesh.subMeshCount;s++){var t=bodyMesh.GetTriangles(s);var keep=new List<int>(t.Length);
    for(int i=0;i<t.Length;i+=3){bool f=t[i]<buv.Length&&t[i+1]<buv.Length&&t[i+2]<buv.Length&&InFace(buv[t[i]])&&InFace(buv[t[i+1]])&&InFace(buv[t[i+2]]);
     if(f){faceTris.Add(t[i]);faceTris.Add(t[i+1]);faceTris.Add(t[i+2]);used.Add(t[i]);used.Add(t[i+1]);used.Add(t[i+2]);if(s<skin.sharedMaterials.Length)faceMat=skin.sharedMaterials[s];}else{keep.Add(t[i]);keep.Add(t[i+1]);keep.Add(t[i+2]);}}
    faceless.SetTriangles(keep,s);}
   SkinTris=faceTris.Count/3;ScalpTris=headTris-SkinTris;faceTriangles=faceTris.ToArray();
   if(SkinTris==0){Debug.LogWarning("FaceShed: no triangles inside her face UV rectangle");return;}
   // the skinned copy: all her vertices (indices kept) + an inner LINING of the rest of her head (its triangles reversed,
   // normals inward, same weights): culled from outside, it shows only through the opening once her face is gone, so the
   // Guest sits inside her opened head instead of floating in a hollow one
   var bn=bodyMesh.normals;var LV=new List<Vector3>(bv);var LN=new List<Vector3>(bn.Length==bv.Length?bn:new Vector3[bv.Length]);var LU=new List<Vector2>(buv.Length==bv.Length?buv:new Vector2[bv.Length]);var LW=new List<BoneWeight>(bw);var LT=new List<int>(faceTris);
   for(int i=0;i<tris.Length;i+=3){bool hd=Dominant(bw[tris[i]])==hi&&Dominant(bw[tris[i+1]])==hi&&Dominant(bw[tris[i+2]])==hi;bool f=InFace(buv[tris[i]])&&InFace(buv[tris[i+1]])&&InFace(buv[tris[i+2]]);if(!hd||f)continue;
    foreach(int k in new[]{0,2,1}){int src=tris[i+k];LV.Add(bv[src]);LN.Add(-LN[src]);LU.Add(buv[src]);LW.Add(bw[src]);LT.Add(LV.Count-1);}}
   faceMesh=new Mesh{name=bodyMesh.name+" (face skin + head lining, 11.9)"};faceMesh.SetVertices(LV);faceMesh.SetNormals(LN);faceMesh.SetUVs(0,LU);faceMesh.boneWeights=LW.ToArray();faceMesh.bindposes=bodyMesh.bindposes;faceMesh.subMeshCount=1;faceMesh.SetTriangles(LT,0);faceMesh.RecalculateBounds();liningTriangles=LT.GetRange(faceTris.Count,LT.Count-faceTris.Count).ToArray();
   faceIdx=new List<int>(used).ToArray();face0=new Vector3[faceIdx.Length];for(int i=0;i<faceIdx.Length;i++)face0[i]=local[faceIdx[i]];mesh0=faceMesh.vertices;meshNow=(Vector3[])mesh0.Clone();
   skinTop=float.MinValue;skinBottom=float.MaxValue;foreach(var v in face0){skinTop=Mathf.Max(skinTop,v.y);skinBottom=Mathf.Min(skinBottom,v.y);}
   // one painted eye (UV) and the face's edge at her temple (left edge of the photo, upper half)
   var eyes=new List<int>();float best=float.MaxValue;foreach(int i in faceIdx)best=Mathf.Min(best,(buv[i]-EyeUV).magnitude);for(int k=0;k<faceIdx.Length;k++)if((buv[faceIdx[k]]-EyeUV).magnitude<=best+8/256f)eyes.Add(k);eye=eyes.ToArray();
   var side=new List<int>();for(int k=0;k<faceIdx.Length;k++){var u=buv[faceIdx[k]];if(u.x<10/256f&&u.y>1-36/256f)side.Add(k);}temple=side.ToArray();
   // the face skin: her own skinning, a second renderer with every setting of hers
   var fg=new GameObject("Her face skin (11.9, skinned like her)");fg.layer=skin.gameObject.layer;fg.transform.SetParent(skin.transform.parent,false);fg.transform.localPosition=skin.transform.localPosition;fg.transform.localRotation=skin.transform.localRotation;fg.transform.localScale=skin.transform.localScale;
   face=fg.AddComponent<SkinnedMeshRenderer>();face.sharedMesh=faceMesh;face.bones=skin.bones;face.rootBone=skin.rootBone;face.sharedMaterials=new[]{faceMat};face.localBounds=skin.localBounds;face.updateWhenOffscreen=skin.updateWhenOffscreen;face.quality=skin.quality;face.skinnedMotionVectors=skin.skinnedMotionVectors;
   face.shadowCastingMode=skin.shadowCastingMode;face.receiveShadows=skin.receiveShadows;face.lightProbeUsage=skin.lightProbeUsage;face.probeAnchor=skin.probeAnchor;face.reflectionProbeUsage=skin.reflectionProbeUsage;face.renderingLayerMask=skin.renderingLayerMask;face.motionVectorGenerationMode=skin.motionVectorGenerationMode;face.allowOcclusionWhenDynamic=skin.allowOcclusionWhenDynamic;
   fg.SetActive(false);block=new MaterialPropertyBlock();
   // the Guest's head and jaw, in bind pose, into the same frame, scaled to sit inside her head
   var look=guest?guest.GetComponent<GuestLookAt>():null;SkinnedMeshRenderer gs=null;
   // The actor also retains inactive proxy renderers. Select the mesh bound to the actual Guest head,
   // never the first child (which can be an unreadable Quaternius Eyebrows mesh).
   if(guest&&look&&look.headBone)foreach(var candidate in guest.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    if(candidate.sharedMesh&&System.Array.IndexOf(candidate.bones,look.headBone)>=0){gs=candidate;break;}
   if(gs&&!gs.sharedMesh.isReadable){Debug.LogError("FaceShed: the actual Guest mesh must be readable: "+gs.sharedMesh.name);return;}
   if(gs&&look&&look.headBone){
    var gm=gs.sharedMesh;var gb=gm.bindposes;var gvs=gm.vertices;var gw=gm.boneWeights;int gh=System.Array.IndexOf(gs.bones,look.headBone);int gj=-1;for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].name.Contains("Jaw"))gj=i;
    // Guest rig: the head is joint4 (no weights of its own), the lower jaw is weighted to joint18 under the Jaw bone
    var jawSet=new HashSet<int>();if(gj>=0)for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].IsChildOf(gs.bones[gj]))jawSet.Add(i);
    var under=new HashSet<int>{gh};for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].IsChildOf(look.headBone)&&!jawSet.Contains(i))under.Add(i);
    var gface=gb[gh].inverse.MultiplyVector(look.headFaceLocal).normalized;var gup=gs.transform.InverseTransformDirection(guest.transform.up).normalized;gface=Vector3.ProjectOnPlane(gface,gup).normalized;var grot=Quaternion.Inverse(Quaternion.LookRotation(gface,gup));
    var hv=new List<Vector3>();var huv=new List<Vector2>();var hs=new List<int>[gm.subMeshCount];var jv=new List<Vector3>();var juv=new List<Vector2>();var js=new List<int>[gm.subMeshCount];var guv=gm.uv;
    var centre=Vector3.zero;int n=0;for(int i=0;i<gvs.Length;i++){int d=Dominant(gw[i]);if(under.Contains(d)||jawSet.Contains(d)){centre+=gvs[i];n++;}}if(n>0)centre/=n;
    float gTop=float.MinValue,gBot=float.MaxValue;
    for(int s=0;s<gm.subMeshCount;s++){hs[s]=new List<int>();js[s]=new List<int>();var t=gm.GetTriangles(s);
     for(int i=0;i<t.Length;i+=3){int d0=Dominant(gw[t[i]]),d1=Dominant(gw[t[i+1]]),d2=Dominant(gw[t[i+2]]);int inJaw=(jawSet.Contains(d0)?1:0)+(jawSet.Contains(d1)?1:0)+(jawSet.Contains(d2)?1:0);bool all=(jawSet.Contains(d0)||under.Contains(d0))&&(jawSet.Contains(d1)||under.Contains(d1))&&(jawSet.Contains(d2)||under.Contains(d2));bool isJaw=all&&inJaw>=2;bool isHead=all&&!isJaw;if(!isJaw&&!isHead)continue;
      var V=isJaw?jv:hv;var U=isJaw?juv:huv;var T=isJaw?js[s]:hs[s];for(int k=0;k<3;k++){var p=grot*(gvs[t[i+k]]-centre);V.Add(p);U.Add(t[i+k]<guv.Length?guv[t[i+k]]:Vector2.zero);T.Add(V.Count-1);if(!isJaw){gTop=Mathf.Max(gTop,p.y);gBot=Mathf.Min(gBot,p.y);}}}}
    // pc28c (story28-key-c: it stood over her bun like a cap and floated over the neck): the Guest's head AND jaw are fitted
    // INSIDE her rebuilt head (92 % of the tightest axis) and stand on her chin, so nothing of it shows until her face slides.
    var gMin=Vector3.positiveInfinity;var gMax=Vector3.negativeInfinity;foreach(var p in hv){gMin=Vector3.Min(gMin,p);gMax=Vector3.Max(gMax,p);}foreach(var p in jv){gMin=Vector3.Min(gMin,p);gMax=Vector3.Max(gMax,p);}
    var gSize=gMax-gMin;var herSize=herMax-herMin;float k2=.92f*Mathf.Min(herSize.x/Mathf.Max(.01f,gSize.x),Mathf.Min(herSize.y/Mathf.Max(.01f,gSize.y),herSize.z/Mathf.Max(.01f,gSize.z)));
    var shift=new Vector3((herMin.x+herMax.x)/2-(gMin.x+gMax.x)/2*k2,herMin.y+.01f-gMin.y*k2,(herMin.z+herMax.z)/2-(gMin.z+gMax.z)/2*k2-.005f);
    for(int i=0;i<hv.Count;i++)hv[i]=shift+hv[i]*k2;for(int i=0;i<jv.Count;i++)jv[i]=shift+jv[i]*k2;GuestScale=k2;
    var jawBone=gj>=0?gb[gj].inverse.MultiplyPoint(Vector3.zero):centre;jawPivot=shift+grot*(jawBone-centre)*k2;
    GuestInside=true;foreach(var p in hv)if(p.x<herMin.x-.005f||p.x>herMax.x+.005f||p.y<herMin.y-.005f||p.y>herMax.y+.005f||p.z<herMin.z-.005f||p.z>herMax.z+.005f){GuestInside=false;break;}
    for(int i=0;i<jv.Count;i++)jv[i]-=jawPivot; // the jaw mesh hinges at its joint
    var gmats=gs.sharedMaterials;guestMesh=Build("Guest head",hv,huv,hs);jawMesh=Build("Guest jaw",jv,juv,js);GuestTris=hv.Count/3;JawTris=jv.Count/3;
    guestT=Part("Guest head (under her face)",guestMesh,gmats);jawT=Part("Guest jaw",jawMesh,gmats);jawT.localPosition=jawPivot;}
   else Debug.LogWarning("FaceShed: Guest head bone not found — no grey head under her face");
   foreach(Transform c in follow)c.gameObject.SetActive(false);Ready=SkinTris>0;
  }
  public void Show(){if(!Ready||Shown)return;Shown=true;if(body&&faceless)body.sharedMesh=faceless;if(face)face.gameObject.SetActive(true);Place();}
  public bool HeadlessBody=>body&&faceless&&body.sharedMesh==faceless;public bool GuestShown=>guestT&&guestT.gameObject.activeSelf;
  void LateUpdate(){if(Shown)Place();if(face&&body){body.GetPropertyBlock(block);face.SetPropertyBlock(block);}Aim();MeasureTop();}
  // the follower IS her head bone (position, rotation, scale incl. her growth) turned into the face frame (the Guest's parts)
  void Place(){var head=rig.head;follow.SetPositionAndRotation(head.position,head.rotation*offset);var ls=head.lossyScale;follow.localScale=Vector3.one*((Mathf.Abs(ls.x)+Mathf.Abs(ls.y)+Mathf.Abs(ls.z))/3);}
  // the key: the kitchen light bounced off the floor and the table in front of her — from his side, a little below her
  // face, warm and soft; it lights only her rendering layer (no fridge, no walls), 11.9 only
  public Vector3 KeyTarget{get{float s=follow?follow.lossyScale.y:1;return rig.head.position+rig.head.rotation*offset*Vector3.up*(.1f*s);}}
  void Aim(){if(!key||!rig||!rig.head)return;var cam=Camera.main;var h=KeyTarget;var from=cam?cam.transform.position:h+root.forward;var d=from-h;d.y=0;if(d.sqrMagnitude<1e-4f)d=root.forward;key.transform.position=h+d.normalized*.8f+Vector3.down*.12f;key.transform.rotation=Quaternion.LookRotation(h-key.transform.position);key.range=Vector3.Distance(key.transform.position,h)+.35f;}
  public void KeyLight(float intensity){if(!key){var g=new GameObject("11.9 face key (narrow spot on her head, 11.9 only)");g.transform.SetParent(transform,false);key=g.AddComponent<Light>();key.type=LightType.Spot;key.color=new Color(1,.82f,.66f);key.spotAngle=36;key.innerSpotAngle=20;key.shadows=LightShadows.None;}key.intensity=intensity;Aim();}
  // QA: where the key is and whether a point lies inside its cone and range
  public string KeyReport(Vector3 p){if(!key)return "no key";var d=p-key.transform.position;float ang=Vector3.Angle(key.transform.forward,d);return "key pos "+key.transform.position.ToString("F2")+" I "+key.intensity.ToString("F2")+" range "+key.range.ToString("F2")+" cone "+key.spotAngle+"° → point at "+d.magnitude.ToString("F2")+" m, "+ang.ToString("F1")+"° off axis, inside="+(ang<=key.spotAngle/2&&d.magnitude<key.range)+", layers light "+key.renderingLayerMask+" / her "+(body?body.renderingLayerMask:0);}
  public bool KeyCovers(Bounds b){if(!key)return false;for(int i=0;i<8;i++){var c=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var d=c-key.transform.position;if(d.magnitude<key.range&&Vector3.Angle(key.transform.forward,d)<=key.spotAngle/2)return true;}var dc=b.center-key.transform.position;return dc.magnitude<key.range&&Vector3.Angle(key.transform.forward,dc)<=key.spotAngle/2;}
  // QA: the screen positions of her face skin's front-facing triangles (her own face before Show, the skin after), baked as drawn
  public List<Vector3> FaceSamples(Camera cam){var list=new List<Vector3>();if(!cam||!body||faceIdx==null)return list;var r=Shown&&!fallen&&face?face:body;var m=new Mesh();r.BakeMesh(m,true);var v=m.vertices;Destroy(m);
   var t=faceTriangles;for(int i=0;i<t.Length;i+=3){if(t[i+2]>=v.Length)continue;var a=r.transform.TransformPoint(v[t[i]]);var b=r.transform.TransformPoint(v[t[i+1]]);var c=r.transform.TransformPoint(v[t[i+2]]);var mid=(a+b+c)/3;if(Vector3.Dot(Vector3.Cross(b-a,c-a),cam.transform.position-mid)<=0)continue;var s=cam.WorldToScreenPoint(mid);if(s.z>0)list.Add(s);}return list;}
  public Vector3 SkinCentre{get{if(fallen&&fallT)return fallT.GetComponent<Renderer>().bounds.center;if(!face||!face.gameObject.activeInHierarchy)return Vector3.zero;var m=new Mesh();face.BakeMesh(m,true);var v=m.vertices;Destroy(m);var c=Vector3.zero;foreach(int i in faceIdx)c+=face.transform.TransformPoint(v[i]);return c/faceIdx.Length;}}
  public Vector3 HerChin=>follow?follow.TransformPoint(new Vector3((herMin.x+herMax.x)/2,skinBottom,herMax.z)):Vector3.zero;
  public Bounds ScreenFace(Camera cam){var pts=FaceSamples(cam);var r=new Bounds();if(pts.Count==0)return r;var min=pts[0];var max=pts[0];foreach(var p in pts){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}r.SetMinMax(new Vector3(min.x,min.y,0),new Vector3(max.x,max.y,0));return r;}
  // t = seconds into 11.9
  public void At(float t){
   if(!Ready)return;KeyLight(.8f*Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.8f)));
   if(t>=.95f)Show();if(!Shown)return;if(t>=1.3f&&guestT&&!guestT.gameObject.activeSelf){guestT.gameObject.SetActive(true);if(jawT)jawT.gameObject.SetActive(true);} // the grey head only once her skin splits
   float h=skinTop-skinBottom;EyeDrop=.035f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1f)/.3f));SeamGap=.014f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.3f)/.12f));Slide=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.5f)/.7f));JawAngle=38*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-2f)/.25f));
   if(!fallen){
    var d=new Vector3[face0.Length];
    foreach(int k in eye)d[k]+=Vector3.down*EyeDrop;
    foreach(int k in temple)d[k]+=new Vector3(-SeamGap,0,SeamGap*.3f);
    if(Slide>0)for(int k=0;k<face0.Length;k++){float yn=Mathf.Clamp01((face0[k].y-skinBottom)/Mathf.Max(.01f,h));
     // wet cloth: it slides down past her chin, the upper part more, folding forward and outwards; it ends hanging from
     // her chin over the throat (SCENARIO «висит на шее»): its top edge reaches her chin, not her chest (pc28f)
     d[k]+=Vector3.down*Slide*h*(.55f+.4f*yn)+Vector3.forward*Slide*(.03f+.03f*(1-yn))+new Vector3(face0[k].x*.25f*Slide*yn,0,0);}
    // frame offsets → her mesh space (the face skin is skinned with her own weights)
    for(int k=0;k<faceIdx.Length;k++)meshNow[faceIdx[k]]=mesh0[faceIdx[k]]+meshFromFrame.MultiplyVector(d[k]);faceMesh.vertices=meshNow;}
   if(jawT)jawT.localRotation=Quaternion.AngleAxis(JawAngle,Vector3.right);
   // 2.6 the skin drops off her: the drawn skin is baked where it hangs and falls as a loose piece
   if(t>=2.6f&&!fallen&&face){fallen=true;fallMesh=new Mesh();face.BakeMesh(fallMesh,true);var g=new GameObject("Her face skin (fallen)");g.layer=face.gameObject.layer;fallT=g.transform;fallT.SetParent(transform,false);fallT.SetPositionAndRotation(face.transform.position,face.transform.rotation);
    fallMesh.SetTriangles(faceTriangles,0);g.AddComponent<MeshFilter>().sharedMesh=fallMesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=face.sharedMaterials;r.renderingLayerMask=face.renderingLayerMask;r.lightProbeUsage=face.lightProbeUsage;r.probeAnchor=face.probeAnchor;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    faceMesh.SetTriangles(liningTriangles,0);fallVelocity=Vector3.zero;float low=float.MaxValue;var fv=fallMesh.vertices;foreach(int i in faceTriangles)low=Mathf.Min(low,fallT.TransformPoint(fv[i]).y);fallDrop=Mathf.Max(0,low-.02f);} // the lining of her head stays on her
   if(fallen&&fallT&&fallDrop>0){fallVelocity+=Physics.gravity*Time.deltaTime;float step=Mathf.Min(fallDrop,-fallVelocity.y*Time.deltaTime);fallT.position+=Vector3.down*step;fallDrop-=step;} // until its lowest edge lies on the floor
  }
  void GiveBack(){if(body)body.renderingLayerMask=bodyLayers0;if(body&&bodyMesh&&body.sharedMesh!=bodyMesh)body.sharedMesh=bodyMesh;if(rig&&rig.head)rig.head.localScale=headScale0;}
  public void End(){GiveBack();Shown=false;Destroy(gameObject);}
  void OnDestroy(){GiveBack();if(face)Destroy(face.gameObject);foreach(var m in new[]{guestMesh,jawMesh,faceless,faceMesh,fallMesh})if(m)Destroy(m);}
 }
}
