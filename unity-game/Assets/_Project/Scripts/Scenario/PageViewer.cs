using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // Full-screen reading view for paper things (4.7 album, «Теленеделя», the epilogue's last album spread). Each page is
 // an image from Resources (an album spread, 2:1) or, while that image is not supplied yet, a paper page with its
 // caption and the exact resource path it waits for. Only draws — the story reads the keys (so Esc never reaches the
 // pause menu at the same time). Drawn above the story's black card layer, never while the game menu is open.
 public sealed class PageViewer:MonoBehaviour {
  public sealed class Page {public string resource,caption;public System.Action<Rect> draw;public Texture2D image;public bool loaded;}
  public readonly List<Page> pages=new List<Page>();public int Index{get;private set;}public bool IsOpen{get;private set;}public string Title="";
  public ScenarioPlayer player;Texture2D paper;GUIStyle cap,head,note;
  public Page Current=>IsOpen&&Index<pages.Count?pages[Index]:null;
  public static Texture2D Load(string resource){return string.IsNullOrEmpty(resource)?null:Resources.Load<Texture2D>(resource);}
  public void Open(string title,IEnumerable<Page> list){pages.Clear();pages.AddRange(list);foreach(var p in pages){p.image=Load(p.resource);p.loaded=p.image;}Title=title;Index=0;IsOpen=pages.Count>0;}
  public void Next(){if(IsOpen&&Index<pages.Count-1)Index++;}public void Previous(){if(IsOpen&&Index>0)Index--;}
  public void Close(){IsOpen=false;}
  void OnGUI(){
   if(!IsOpen||(player&&player.paused))return;GUI.depth=-2;var p=Current;if(p==null)return;float w=Screen.width,h=Screen.height;
   if(!paper){paper=new Texture2D(1,1);paper.SetPixel(0,0,new Color(.86f,.82f,.72f));paper.Apply();}
   cap??=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.03f),alignment=TextAnchor.MiddleCenter,wordWrap=true};head??=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.025f),alignment=TextAnchor.UpperCenter};note??=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.02f),alignment=TextAnchor.LowerCenter,wordWrap=true};
   GUI.color=new Color(0,0,0,.9f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
   var area=new Rect(w*.06f,h*.08f,w*.88f,h*.74f);
   if(p.image){float a=(float)p.image.width/p.image.height,fw=Mathf.Min(area.width,area.height*a),fh=fw/a;GUI.DrawTexture(new Rect(area.center.x-fw/2,area.center.y-fh/2,fw,fh),p.image,ScaleMode.StretchToFill);}
   else{GUI.DrawTexture(area,paper);GUI.color=new Color(.18f,.15f,.12f);
    if(p.draw!=null)p.draw(area);else{GUI.Label(new Rect(area.x+area.width*.1f,area.y,area.width*.8f,area.height*.8f),p.caption,cap);GUI.Label(new Rect(area.x,area.y,area.width,area.height-8),"(изображение не подключено: Resources/"+p.resource+".png)",note);}
    GUI.color=Color.white;}
   GUI.Label(new Rect(0,h*.015f,w,h*.05f),Title+(pages.Count>1?"   "+(Index+1)+" / "+pages.Count:""),head);
   if(p.image&&!string.IsNullOrEmpty(p.caption))GUI.Label(new Rect(w*.1f,h*.83f,w*.8f,h*.08f),p.caption,cap);
   GUI.Label(new Rect(0,h*.92f,w,h*.06f),(pages.Count>1?"[A/D] листать   ":"")+"[E] / [Esc] закрыть",note);
  }
  void OnDestroy(){if(paper)Destroy(paper);}
 }
}
