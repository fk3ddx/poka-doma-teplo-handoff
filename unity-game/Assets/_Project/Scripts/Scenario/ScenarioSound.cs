using UnityEngine;
namespace Ataka.Scenario {
 // Temporary authored synthesis, explicitly listed as a placeholder. No licensed recordings embedded.
 public sealed class ScenarioSound:MonoBehaviour {
  AudioClip knock,creak,phone,breath,tick,impact,shriek;AudioSource ambient;float clockTimer;
  public Transform clock,listener;public bool paused,quiet;public float menace;
  public AudioClip phoneWarning,grandmotherKnock;AudioClip reversedPhone;
  public float PhoneDuration=>phoneWarning?phoneWarning.length:4;
  public float Speak(bool reversed,Vector3 position){var clip=reversed?reversedPhone:phoneWarning;if(!clip)return 0;PlayClip(clip,position,.65f);return clip.length;}
  public void Grandmother(Vector3 position){if(grandmotherKnock)PlayClip(grandmotherKnock,position,.5f);}
  AudioClip Tone(string name,float seconds,float frequency,float noise,float decay){int rate=22050;var data=new float[(int)(rate*seconds)];var rng=new System.Random(173);for(int i=0;i<data.Length;i++){float t=(float)i/rate;float envelope=Mathf.Min(1,t*100)*Mathf.Exp(-t*decay)*(1-Mathf.Pow(t/seconds,8));data[i]=Mathf.Clamp((Mathf.Sin(t*frequency*6.28318f)*.4f+((float)rng.NextDouble()*2-1)*noise)*envelope,-.8f,.8f);}var c=AudioClip.Create(name,data.Length,1,rate,false);c.SetData(data,0);return c;}
  void Awake(){knock=Tone("knock",.6f,72,.3f,8);creak=Tone("creak",1.3f,145,.1f,1);phone=Tone("phone",1,780,.07f,.3f);breath=Tone("breath",2,60,.45f,.8f);tick=Tone("tick",.15f,1200,.1f,25);impact=Tone("impact",1,55,.5f,5);shriek=Tone("shriek",1.2f,790,.3f,2);}
  public void Play(string id,Vector3 position,float volume=.4f){var clip=id=="knock"?knock:id=="phone"?phone:id=="breath"?breath:id=="tick"?tick:id=="impact"?impact:id=="shriek"?shriek:creak;var g=new GameObject("SFX "+id);g.transform.position=position;var s=g.AddComponent<AudioSource>();s.spatialBlend=1;s.minDistance=.5f;s.maxDistance=12;s.volume=volume;s.clip=clip;s.Play();Destroy(g,clip.length+1);}
  void Start(){if(phoneWarning){var data=new float[phoneWarning.samples*phoneWarning.channels];phoneWarning.GetData(data,0);int ch=phoneWarning.channels;for(int i=0;i<phoneWarning.samples/2;i++)for(int c=0;c<ch;c++){int a=i*ch+c,b=(phoneWarning.samples-1-i)*ch+c;float v=data[a];data[a]=data[b];data[b]=v;}reversedPhone=AudioClip.Create("Phone reversed",phoneWarning.samples,ch,phoneWarning.frequency,false);reversedPhone.SetData(data,0);}}
  void PlayClip(AudioClip clip,Vector3 p,float volume){var g=new GameObject("Temporary voiced line");g.transform.position=p;var s=g.AddComponent<AudioSource>();s.spatialBlend=1;s.minDistance=.5f;s.maxDistance=10;s.volume=volume;s.clip=clip;s.Play();Destroy(g,clip.length+1);}
  void Update(){if(paused||quiet)return;clockTimer+=Time.deltaTime;if(clockTimer>1){clockTimer-=1;if(clock)Play("tick",clock.position,.14f);}}
 }
}
