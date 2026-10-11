using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
namespace Ataka
{
    // Diagnostic only: not a kitchen story build and never runs during ordinary play.
    public sealed class KitchenRenderCheck : MonoBehaviour
    {
        string output; int errors; float deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartRequestedCheck()
        {
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-kitchen-render-check");
            if(i<0||i+1>=args.Length)return;
            var check=new GameObject("Kitchen render diagnostics").AddComponent<KitchenRenderCheck>();
            check.output=Path.GetFullPath(args[i+1]); DontDestroyOnLoad(check.gameObject);
        }
        void CountError(string text,string trace,LogType kind)
        { if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors++; }
        IEnumerator Start()
        {
            Application.logMessageReceived+=CountError;
            deadline=Time.realtimeSinceStartup+90;Application.runInBackground=true;
            Directory.CreateDirectory(output);yield return null;yield return null;
            var session=FindFirstObjectByType<GameSession>();
            if(!session||!session.Quality){Debug.LogError("No running game session.");Finish();yield break;}
            session.settings.quality=VisualQuality.High;session.settings.rasterStyle=RasterStyle.Standard;
            session.settings.vhs=true;session.settings.fpsLimit=60;session.ApplySettings();
            yield return Capture("00-menu-540");session.NewGame();
            yield return new WaitForSecondsRealtime(4.2f);
            session.player.Restore(new Vector3(7.3f,11.72f,2.5f),150,4,false);
            session.player.inputEnabled=false;
            var report=new Report{unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height};
            for(int i=0;i<3;i++)
            {
                var style=(RasterStyle)i; session.settings.rasterStyle=style;session.ApplySettings();
                yield return Capture("kitchen-"+RetroRaster.TargetHeight(style));
                report.modes[i]=new Mode{style=style.ToString(),targetHeight=RetroRaster.TargetHeight(style),scale=session.Quality.Pipeline.renderScale,upscale=session.Quality.Pipeline.upscalingFilter.ToString(),msaa=session.Quality.Pipeline.msaaSampleCount};
            }
            // Same old geometry/light, native-resolution color path, for causal renderer comparison.
            var material=session.vhsMaterial;float levels=material.GetFloat("_ColorLevels"),dither=material.GetFloat("_DitherStrength"),grain=material.GetFloat("_Grain");
            session.Quality.Pipeline.renderScale=1;material.SetFloat("_ColorLevels",65536);material.SetFloat("_DitherStrength",0);material.SetFloat("_Grain",0);material.SetFloat("_Strength",0);
            yield return Capture("kitchen-native-baseline");
            material.SetFloat("_ColorLevels",levels);material.SetFloat("_DitherStrength",dither);material.SetFloat("_Grain",grain);material.SetFloat("_Strength",1);
            session.settings.rasterStyle=RasterStyle.Standard;session.ApplySettings();session.SetPaused(true);
            yield return Capture("01-return-to-menu-540");
            report.errors=errors;File.WriteAllText(Path.Combine(output,"render-report.json"),JsonUtility.ToJson(report,true));Finish();
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(!texture){Debug.LogError("No framebuffer.");yield break;}
            File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);
        }
        void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){Debug.LogError("Render check timeout.");Finish();}}
        void Finish(){Application.logMessageReceived-=CountError;Application.Quit(errors==0?0:1);}
        [Serializable] sealed class Mode{public string style,upscale;public int targetHeight,msaa;public float scale;}
        [Serializable] sealed class Report{public string unity,gpu;public int width,height,errors;public Mode[] modes=new Mode[3];}
    }
}
#endif
