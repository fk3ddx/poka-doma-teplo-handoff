using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 [RequireComponent(typeof(CharacterController))]
 public sealed class ScenarioPlayer:MonoBehaviour {
  public Transform head; public Camera view; public Light flashlight;
  public bool lookDuringScene;
  public bool control=true,paused=true,chasing,hasFlashlight,crouched,automating;
  public float sensitivity=.085f;public float pitch;public bool LookingBack{get;private set;}
  public float ActualSpeed{get;private set;}
  // The dynamo torch fades in ~10 s; until the player has pumped it once, the director shows how.
  public bool TorchNeedsPump=>hasFlashlight&&torch&&charge<2.5f&&!pumpedOnce;bool pumpedOnce;
  CharacterController body; float vy,charge=10,pressDuration; bool torch=true;int suppress;
  public Vector3 routeForward=Vector3.forward;
  void Awake(){body=GetComponent<CharacterController>();}
  void Update(){
   if(paused||!control){ActualSpeed=0;
    if(!paused&&lookDuringScene&&Mouse.current!=null&&!automating){var delta=Mouse.current.delta.ReadValue()*sensitivity;if(delta.sqrMagnitude<1e6){transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-80,80);}head.localRotation=Quaternion.Euler(pitch,0,0);}
    return;
   }var k=Keyboard.current;var m=Mouse.current;if(k==null||m==null)return;
   if(k.leftCtrlKey.wasPressedThisFrame&&!chasing)crouched=!crouched;
   LookingBack=chasing&&(k.cKey.isPressed||Vector3.Dot(transform.forward,routeForward)<-.2f);
   if(suppress>0)suppress--;else if(!automating){var delta=m.delta.ReadValue()*sensitivity;if(delta.sqrMagnitude<1e6){transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-80,80);}}
   head.localRotation=Quaternion.Euler(pitch,chasing&&k.cKey.isPressed?180:0,0);
   float h=crouched?1.05f:1.7f;body.height=h;body.center=Vector3.up*h/2;head.localPosition=Vector3.up*(crouched?.94f:1.55f);
   var axis=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));axis=Vector2.ClampMagnitude(axis,1);
   float speed=chasing?4.5f*1.3f:crouched?1.15f:k.leftShiftKey.isPressed?4.5f:2;
   if(LookingBack)speed*=.5f;
   var before=transform.position;vy=body.isGrounded?-2:Mathf.Max(-20,vy-20*Time.deltaTime);body.Move(((transform.right*axis.x+transform.forward*axis.y)*speed+Vector3.up*vy)*Time.deltaTime);
   var displacement=transform.position-before;displacement.y=0;ActualSpeed=displacement.magnitude/Mathf.Max(.0001f,Time.deltaTime);
   if(k.fKey.wasPressedThisFrame)pressDuration=0;
   if(k.fKey.isPressed){pressDuration+=Time.deltaTime;if(pressDuration>.25f){torch=true;pumpedOnce=true;charge=Mathf.Min(10,charge+Time.deltaTime*6);}}
   else {if(k.fKey.wasReleasedThisFrame&&pressDuration<=.25f)torch=!torch;if(torch)charge=Mathf.Max(0,charge-Time.deltaTime);}
   flashlight.intensity=hasFlashlight&&torch?Mathf.Lerp(.3f,3,charge/10):0;
  }
  public void Place(Vector3 feet,float yaw,float look=0){body.enabled=false;transform.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));pitch=look;head.localPosition=Vector3.up*(crouched?.94f:1.55f);head.localRotation=Quaternion.Euler(pitch,0,0);body.enabled=true;vy=0;suppress=2;}
  public void Pause(bool value){paused=value;if(!Application.isBatchMode){Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}suppress=2;}
 }
}
