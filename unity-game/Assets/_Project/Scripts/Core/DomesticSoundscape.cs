using UnityEngine;
namespace Ataka {
 // Authored original clips, spatially placed. AudioListener.pause follows the game menu.
 public sealed class DomesticSoundscape : MonoBehaviour {
  public AudioClip radio,weather,tick;
  public AudioClip[] steps;
  public FirstPersonController player;
  public LampSwitch kitchenPower;
  AudioSource radioSource,clockSource,footSource;Vector3 previous;float distance,clockTime;
  AudioSource Source(string label,Vector3 p,AudioClip clip,float volume,bool loop,float maxDistance){
   var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=p;
   var source=go.AddComponent<AudioSource>();source.clip=clip;source.volume=volume;source.loop=loop;source.spatialBlend=1;source.minDistance=.65f;source.maxDistance=maxDistance;source.rolloffMode=AudioRolloffMode.Linear;source.playOnAwake=false;
   if(loop)source.Play();return source;
  }
  void Start(){
   radioSource=Source("Radio point",new Vector3(8.6f,1.78f,2.42f),radio,.24f,true,8);
   Source("Wet snow behind window",new Vector3(4.2f,1.1f,-1.7f),weather,.45f,true,10);
   clockSource=Source("Pendulum clock",new Vector3(5.03f,1.6f,4.36f),tick,.11f,false,6);
   footSource=player.gameObject.AddComponent<AudioSource>();footSource.spatialBlend=0;footSource.volume=.18f;previous=player.transform.position;
  }
  void Update(){
   if(Time.timeScale==0){previous=player.transform.position;return;}
   clockTime+=Time.deltaTime;if(clockTime>=1){clockTime-=1;clockSource.pitch=1+(Mathf.FloorToInt(Time.time)%2)*.13f;clockSource.PlayOneShot(tick);}
   if(kitchenPower!=null)radioSource.volume=Mathf.MoveTowards(radioSource.volume,kitchenPower.powered?.24f:0,Time.deltaTime*.7f);
   var delta=player.transform.position-previous;previous=player.transform.position;delta.y=0;
   if(delta.magnitude>1){distance=0;return;}distance+=delta.magnitude;
   if(distance>.62f&&steps.Length>0){distance=0;footSource.pitch=Random.Range(.94f,1.06f);footSource.PlayOneShot(steps[Random.Range(0,steps.Length)]);}
  }
 }
}
