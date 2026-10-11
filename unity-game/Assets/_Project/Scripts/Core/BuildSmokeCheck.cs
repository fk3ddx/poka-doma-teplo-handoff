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
            deadline = Time.realtimeSinceStartup + 90;
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
            session.player.Restore(new Vector3(4.1f, 11.72f, .6f), 180, 12, false);
            yield return Capture("04-courtyard");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Apartment_M1")
            {
                session.player.Restore(new Vector3(3.88f,11.72f,3.0f),125,6,false);
                yield return Capture("06-cabinet-tv");
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
            Finish();
        }

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
