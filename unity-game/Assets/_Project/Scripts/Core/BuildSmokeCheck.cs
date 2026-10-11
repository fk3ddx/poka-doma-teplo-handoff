#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ataka
{
    // Opt-in verification of the built player. Never runs during ordinary play.
    public sealed class BuildSmokeCheck : MonoBehaviour
    {
        string output;
        int errors;
        float deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-m1-smoke");
            if (flag < 0) flag = Array.IndexOf(args, "-m0-smoke");
            if (flag < 0 || flag + 1 >= args.Length) return;
            var check = new GameObject("Development build smoke check").AddComponent<BuildSmokeCheck>();
            check.output = Path.GetFullPath(args[flag + 1]);
            DontDestroyOnLoad(check.gameObject);
            Application.logMessageReceived += check.CountErrors;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            deadline = Time.realtimeSinceStartup + 150;
            Application.runInBackground = true;
            yield return null;
            var session = FindFirstObjectByType<GameSession>();
            if (session == null) { Debug.LogError("Smoke: no GameSession"); Finish(); yield break; }
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Apartment_M1"){
                var visibleMaterials=FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct();
                foreach(var material in visibleMaterials.Where(m=>new[]{"WindowWarm","SodiumGlass","Bulb","Shade"}.Contains(m.name)))
                    if(!material.IsKeywordEnabled("_EMISSION"))Debug.LogError("Smoke: visible emissive material lost its shader keyword: "+material.name);
            }
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Apartment_M1"){
                var snow=GameObject.Find("Windblown wet snow");
                if(snow==null||snow.GetComponent<ParticleSystem>()==null)Debug.LogError("Smoke: exterior weather component missing");
            }
            session.settings.quality=VisualQuality.High;session.settings.fpsLimit=60;session.settings.fieldOfView=70;session.ApplySettings();
            yield return Capture("01-menu");
            session.NewGame();
            // Let the ordinary four-second controls notice expire before comparative frames.
            yield return new WaitForSecondsRealtime(4.2f);
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None)) door.Restore(1);
            session.player.Restore(new Vector3(5.8f, 11.72f, 3.7f), 215, 3, false);
            session.player.inputEnabled = false;
            yield return Capture("02-living");
            session.player.Restore(new Vector3(7.3f, 11.72f, 2.5f), 150, 4, false);
            yield return Capture("03-kitchen");
            session.player.Restore(new Vector3(7.70f,11.72f,.85f),180,7,false);
            yield return Capture("18-kitchen-window");
            session.player.Restore(new Vector3(4.1f, 11.72f, .6f), 180, 12, false);
            yield return Capture("04-courtyard");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Apartment_M1")
            {
                session.player.Restore(new Vector3(3.88f,11.72f,3.0f),125,6,false);
                yield return Capture("06-cabinet-tv");
                Directory.CreateDirectory(Path.Combine(output,"props"));
                session.player.Restore(new Vector3(5.10f,11.72f,1.96f),90,36,false);
                yield return Capture("props/television-front");
                session.player.Restore(new Vector3(5.05f,11.72f,2.67f),127,32,false);
                yield return Capture("props/television-side");
                session.player.Restore(new Vector3(2.25f,11.72f,3.50f),205,8,false);
                yield return Capture("07-bedroom");
                session.player.Restore(new Vector3(2.15f,11.72f,1.50f),185,32,false);
                yield return Capture("12-bedside-detail");
                session.player.Restore(new Vector3(2.15f,11.72f,2.65f),310,0,false);
                yield return Capture("13-bedroom-storage");
                session.player.Restore(new Vector3(7.75f,11.72f,3.55f),0,5,false);
                yield return Capture("08-hall");
                var drawer=FindFirstObjectByType<SlidingDrawer>();drawer.Restore(1);
                session.player.Restore(new Vector3(7.82f,11.72f,5.72f),270,46,false);
                yield return Capture("14-hall-drawer");drawer.Restore(0);
                session.player.Restore(new Vector3(7.60f,11.72f,1.22f),245,39,false);
                yield return Capture("15-kitchen-sink");
                session.player.Restore(new Vector3(4.37f,11.72f,5.05f),320,16,false);
                yield return Capture("09-bathroom");
                session.player.Restore(new Vector3(8.05f,11.72f,7.1f),0,18,false);
                yield return Capture("10-staircase");
                session.player.Restore(new Vector3(5.20f,11.72f,-.65f),190,30,false);
                yield return Capture("11-balcony-courtyard");
                var seat=FindFirstObjectByType<ArmchairSeat>();
                session.player.Restore(seat.exitAnchor.position,180,35,false);
                seat.Interact(session);yield return new WaitForSecondsRealtime(.85f);
                yield return Capture("16-seated-living");
                session.player.pitch=45;session.player.view.transform.localRotation=Quaternion.Euler(45,0,0);
                yield return Capture("17-seated-lap");
                seat.Interact(session);yield return new WaitForSecondsRealtime(.85f);
                if(session.player.Seat!=null)Debug.LogError("Smoke: failed to leave chair");
            }
            var pickups = FindObjectsByType<PickUp>(FindObjectsSortMode.None);
            foreach (var pickup in pickups) pickup.Interact(session);
            if (session.inventory.Count != 2) Debug.LogError("Smoke: expected two collected items");
            session.NewGame();
            session.player.inputEnabled = false;
            if (session.inventory.Count != 0 || pickups.Any(x => !x.gameObject.activeSelf))
                Debug.LogError("Smoke: new game did not reset pickups");
            var switches = FindObjectsByType<LampSwitch>(FindObjectsSortMode.None);
            foreach (var lamp in switches) lamp.Interact(session);
            yield return Capture("05-lights-off");
            foreach (var lamp in switches) lamp.Interact(session);
            for (int warmup=0;warmup<180;warmup++) yield return null;
            var switching = new float[192];
            for(int cycle=0;cycle<16;cycle++){
                foreach(var lamp in switches)lamp.Interact(session);
                for(int frame=0;frame<12;frame++){yield return null;switching[cycle*12+frame]=Time.unscaledDeltaTime*1000;}
            }
            Array.Sort(switching);
            var timings = new float[600];
            float elapsed = 0;
            int frames = 0;
            float slowest = 0;
            while (frames < timings.Length)
            {
                yield return null;
                timings[frames] = Time.unscaledDeltaTime * 1000;
                elapsed += Time.unscaledDeltaTime;
                slowest = Mathf.Max(slowest, Time.unscaledDeltaTime);
                frames++;
            }
            Array.Sort(timings);
            session.player.Restore(new Vector3(7.75f,11.72f,3.55f),0,5,false);
            for(int i=0;i<30;i++)yield return null;
            var mirrorTimings=new float[240];
            for(int i=0;i<mirrorTimings.Length;i++){yield return null;mirrorTimings[i]=Time.unscaledDeltaTime*1000;}
            Array.Sort(mirrorTimings);
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Result
            {
                width=Screen.width,height=Screen.height,renderScale=UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset.renderScale,
                mirrorP95Ms=mirrorTimings[227],mirrorMaxMs=mirrorTimings[239],
                lightSwitchP95Ms=switching[182], lightSwitchMaxMs=switching[191], p95FrameMs=timings[569], p99FrameMs=timings[593],
                errors = errors, frames = frames, meanFps = frames / Mathf.Max(.001f, elapsed),
                slowestFrameMs = slowest * 1000, gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion
            }, true));
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Apartment_M1"){
                session.player.Restore(new Vector3(5.20f,11.72f,-.65f),190,30,false);
                var fogs=FindObjectsByType<LocalLightFog>(FindObjectsSortMode.None);
                var fogReport=new FogResult();int previousFps=Application.targetFrameRate;Application.targetFrameRate=-1;
                for(int mode=0;mode<2;mode++){
                    foreach(var fog in fogs)fog.enabled=mode==0;
                    for(int i=0;i<60;i++)yield return null;
                    var samples=new float[240];float total=0;
                    for(int i=0;i<samples.Length;i++){yield return null;samples[i]=Time.unscaledDeltaTime*1000;total+=samples[i];}
                    Array.Sort(samples);if(mode==0){fogReport.onMeanMs=total/samples.Length;fogReport.onP95Ms=samples[227];fogReport.onMaxMs=samples[239];}
                    else{fogReport.offMeanMs=total/samples.Length;fogReport.offP95Ms=samples[227];fogReport.offMaxMs=samples[239];}
                }
                foreach(var fog in fogs)fog.enabled=true;Application.targetFrameRate=previousFps;
                File.WriteAllText(Path.Combine(output,"fog-performance.json"),JsonUtility.ToJson(fogReport,true));
            }
            yield return QualityProfiles(session);
            Finish();
        }
        IEnumerator QualityProfiles(GameSession session)
        {
            var report=new QualityReport();var prior=session.settings.quality;int priorFps=session.settings.fpsLimit;
            var positions=new[]{new Vector3(5.8f,11.72f,3.7f),new Vector3(7.75f,11.72f,3.55f),new Vector3(5.20f,11.72f,-.65f)};
            var yaws=new[]{215f,0f,190f};var pitches=new[]{3f,5f,30f};var names=new[]{"living","mirror","courtyard"};
            Directory.CreateDirectory(Path.Combine(output,"quality"));
            foreach(var mode in new[]{VisualQuality.High,VisualQuality.Medium,VisualQuality.Low}){
                session.settings.quality=mode;session.settings.fpsLimit=-1;
                float start=Time.realtimeSinceStartup;session.ApplySettings();yield return null;
                report.switches.Add(new QualitySwitch{mode=mode.ToString(),firstFrameMs=(Time.realtimeSinceStartup-start)*1000});
                for(int view=0;view<positions.Length;view++){
                    session.player.Restore(positions[view],yaws[view],pitches[view],false);
                    for(int i=0;i<60;i++)yield return null;
                    var samples=new float[180];float total=0;
                    for(int i=0;i<samples.Length;i++){yield return null;samples[i]=Time.unscaledDeltaTime*1000;total+=samples[i];}Array.Sort(samples);
                    report.views.Add(new QualityView{mode=mode.ToString(),view=names[view],renderScale=session.Quality.Pipeline.renderScale,meanMs=total/samples.Length,p95Ms=samples[170],maxMs=samples[179]});
                    yield return Capture("quality/"+mode+"-"+names[view]);
                }
            }
            session.settings.quality=prior;session.settings.fpsLimit=priorFps;session.ApplySettings();
            File.WriteAllText(Path.Combine(output,"quality-performance.json"),JsonUtility.ToJson(report,true));
        }
        [Serializable] sealed class QualityReport {public int samplesPerView=180,targetFps=-1;public System.Collections.Generic.List<QualityView> views=new System.Collections.Generic.List<QualityView>();public System.Collections.Generic.List<QualitySwitch> switches=new System.Collections.Generic.List<QualitySwitch>();}
        [Serializable] sealed class QualityView {public string mode,view;public float renderScale,meanMs,p95Ms,maxMs;}
        [Serializable] sealed class QualitySwitch {public string mode;public float firstFrameMs;}

        [Serializable] sealed class FogResult {public int targetFps=-1,samplesPerMode=240;public float onMeanMs,onP95Ms,onMaxMs,offMeanMs,offP95Ms,offMaxMs;}

        IEnumerator Capture(string name)
        {
            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                Debug.LogError("Smoke: no rendered framebuffer. Run the player in a visible window.");
                Finish();
                yield break;
            }
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
        void CountErrors(string text, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        void Update()
        {
            if (deadline > 0 && Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError("Smoke: timed out before verification completed.");
                Finish();
            }
        }
        void Finish()
        {
            Application.logMessageReceived -= CountErrors;
            Debug.Log("PLAYER_SMOKE_COMPLETE errors=" + errors);
            Application.Quit(errors == 0 ? 0 : 1);
        }
        [Serializable] sealed class Result
        {
            public int errors, frames,width,height;
            public float renderScale;
            public float lightSwitchP95Ms, lightSwitchMaxMs, mirrorP95Ms, mirrorMaxMs;
            public float meanFps, slowestFrameMs, p95FrameMs, p99FrameMs;
            public string gpu, graphicsApi, unity;
        }
    }
}
#endif
