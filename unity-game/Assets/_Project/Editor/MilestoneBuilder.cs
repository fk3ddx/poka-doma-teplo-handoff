using System;
using System.IO;
using Ataka;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ataka.Editor
{
    public static class MilestoneBuilder
    {
        const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Apartment_M0.unity";
        const float Floor = 11.7f;
        static Transform apartment;
        static Material wall, floor, ceiling, door, prop, metal;

        [MenuItem("Ataka/M0/Generate test apartment")]
        public static void Generate()
        {
            foreach (string dir in new[] { "Scenes", "Settings", "Art/Materials", "Art/Shaders" })
                Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureProject();
            var vhs = ConfigurePipeline();
            wall = Material("M0_plaster", new Color(.47f, .45f, .41f));
            floor = Material("M0_floor", new Color(.22f, .19f, .15f));
            ceiling = Material("M0_ceiling", new Color(.57f, .55f, .49f));
            door = Material("M0_door", new Color(.25f, .20f, .16f));
            prop = Material("M0_prop", new Color(.38f, .4f, .36f));
            metal = Material("M0_metal", new Color(.2f, .22f, .24f));
            apartment = new GameObject("Apartment • M0 metric shell").transform;
            apartment.position = Vector3.up * Floor;

            Box("Bedroom floor", new Vector3(1.4f, -.09f, 2.2f), new Vector3(2.8f, .18f, 4.4f), floor);
            Box("Apartment floor", new Vector3(5.8f, -.09f, 3), new Vector3(6, .18f, 6), floor);
            Box("Bedroom ceiling", new Vector3(1.4f, 2.59f, 2.2f), new Vector3(3.5f, .18f, 5.1f), ceiling);
            Box("Apartment ceiling", new Vector3(5.8f, 2.59f, 3), new Vector3(6.7f, .18f, 6.7f), ceiling);
            Box("West exterior", new Vector3(-.175f, 1.25f, 2.2f), new Vector3(.35f, 2.5f, 4.75f), wall);
            Box("East exterior", new Vector3(8.975f, 1.25f, 3), new Vector3(.35f, 2.5f, 6.35f), wall);
            Box("Bedroom rear exterior", new Vector3(1.4f, 1.25f, 4.575f), new Vector3(2.8f, 2.5f, .35f), wall);
            Box("Bath west exterior", new Vector3(2.725f, 1.25f, 5.4f), new Vector3(.35f, 2.5f, 1.55f), wall);
            Front(0, .5f, 0, 2.5f); Front(.5f, 2.1f, 0, .85f); Front(.5f, 2.1f, 2.3f, 2.5f);
            Front(2.1f, 3.2f, 0, 2.5f); Front(3.2f, 4.55f, 0, .85f); Front(3.2f, 4.55f, 2.3f, 2.5f);
            Front(4.55f, 4.8f, 0, 2.5f); Front(4.8f, 5.55f, 2, 2.5f);
            Front(5.55f, 7.1f, 0, 2.5f); Front(7.1f, 8.3f, 0, .85f); Front(7.1f, 8.3f, 2.3f, 2.5f);
            Front(8.3f, 8.8f, 0, 2.5f);
            Window(.5f, 2.1f); Window(3.2f, 4.55f); Window(7.1f, 8.3f);
            PartitionZ("Bedroom / living", 2.85f, 0, 4.4f, 3.35f, 4.15f);
            PartitionX("Living / hall", 4.45f, 2.9f, 6.5f, 5.25f, 6.05f);
            Box("Living / kitchen", new Vector3(6.55f, 1.25f, 2.2f), new Vector3(.1f, 2.5f, 4.4f), wall);
            PartitionX("Kitchen / hall", 2.95f, 6.6f, 8.8f, 6.9f, 7.6f);
            PartitionZ("Bathroom / hall", 4.65f, 4.5f, 6, 4.9f, 5.6f);
            Box("Rear exterior left", new Vector3(5.2f, 1.25f, 6.175f), new Vector3(4.8f, 2.5f, .35f), wall);
            Box("Rear exterior right", new Vector3(8.65f, 1.25f, 6.175f), new Vector3(.3f, 2.5f, .35f), wall);
            Box("Entrance lintel", new Vector3(8.05f, 2.25f, 6.175f), new Vector3(.9f, .5f, .35f), wall);
            AddDoor("living-door", "дверь в зал", new Vector3(5.25f, 0, 4.45f), .8f, 0);
            AddDoor("bedroom-door", "дверь в спальню", new Vector3(2.85f, 0, 4.15f), .8f, 90);
            AddDoor("kitchen-door", "дверь на кухню", new Vector3(6.9f, 0, 2.95f), .7f, 0);
            AddDoor("bath-door", "дверь в ванную", new Vector3(4.65f, 0, 5.6f), .7f, 90);
            AddDoor("entrance-door", "входная дверь", new Vector3(7.6f, 0, 6.15f), .9f, 0);
            AddDoor("balcony-door", "балконная дверь", new Vector3(4.8f, 0, -.12f), .75f, 0);
            Balcony(); Landing();
            var fridge = Box("M0 refrigerator clearance proxy", new Vector3(8.39f, .65f, .7f), new Vector3(.65f, 1.3f, .65f), prop);
            var matches = Box("Matches", new Vector3(8.2f, 1.325f, .83f), new Vector3(.1f, .04f, .06f), door);
            var pickup = matches.AddComponent<PickUp>();
            pickup.persistentId = "matches"; pickup.itemId = "matches"; pickup.label = "спички"; pickup.priority = 20;
            var notebook = Box("Recipe notebook", new Vector3(4.3f, .78f, 2.1f), new Vector3(.16f, .025f, .23f), door);
            var table = Box("M0 table clearance proxy", new Vector3(4.3f, .375f, 2.1f), new Vector3(.65f, .75f, .7f), prop);
            pickup = notebook.AddComponent<PickUp>();
            pickup.persistentId = "recipe"; pickup.itemId = "recipe"; pickup.label = "тетрадь"; pickup.priority = 20;
            Lamp("living", new Vector3(4.8f, 2.27f, 2.4f), new Vector3(6.43f, 1.3f, 3.85f), 1.8f);
            Lamp("kitchen", new Vector3(7.65f, 2.25f, 1.5f), new Vector3(8.72f, 1.3f, 2.5f), 1.4f);
            Lamp("bedroom", new Vector3(1.4f, 2.25f, 2.1f), new Vector3(2.74f, 1.3f, 3), 1.3f);
            Lamp("hall", new Vector3(7.4f, 2.3f, 4.5f), new Vector3(8.72f, 1.3f, 5.5f), 1);
            Lamp("bath", new Vector3(3.7f, 2.25f, 5.2f), new Vector3(4.53f, 1.3f, 4.7f), .7f);
            SealWallTops();
            Exterior();
            var actor = new GameObject("Player"); actor.layer = 2;
            actor.transform.position = new Vector3(7.4f, Floor + .02f, 5.25f);
            actor.transform.rotation = Quaternion.Euler(0, 180, 0);
            var body = actor.AddComponent<CharacterController>();
            body.radius = .25f; body.height = 1.7f; body.center = Vector3.up * .85f;
            body.stepOffset = .15f; body.skinWidth = .025f; body.slopeLimit = 50;
            actor.AddComponent<PlayerControls>();
            var controller = actor.AddComponent<FirstPersonController>();
            var cameraObject = new GameObject("Player view"); cameraObject.tag = "MainCamera"; cameraObject.layer = 2;
            cameraObject.transform.SetParent(actor.transform, false); cameraObject.transform.localPosition = Vector3.up * 1.55f;
            var view = cameraObject.AddComponent<Camera>(); view.nearClipPlane = .04f; view.farClipPlane = 200; view.fieldOfView = 70;
            view.backgroundColor = new Color(.15f, .17f, .19f); view.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            controller.view = view;
            var session = new GameObject("Game Session").AddComponent<GameSession>();
            session.player = controller; session.vhsMaterial = vhs;
            session.gameObject.AddComponent<PrototypeMenu>().session = session;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.16f, .17f, .19f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.15f, .17f, .19f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .022f;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("M0_SCENE_GENERATED " + ScenePath);
        }

        static void SealWallTops()
        {
            // A 20mm overlap avoids exposed raster cracks at the ceiling/wall boundary.
            foreach(var renderer in apartment.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial!=wall)continue;
                var t=renderer.transform;float top=t.localPosition.y+t.localScale.y*.5f;
                if(Mathf.Abs(top-2.5f)>.001f)continue;
                t.localPosition+=Vector3.up*.01f;t.localScale+=Vector3.up*.02f;
            }
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "AtakaProject"; PlayerSettings.productName = "Атака на запоминание — M0";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            // Input System only. Serialized project setting is needed because the public API has no setter.
            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = serialized.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static Material ConfigurePipeline()
        {
            var rendererPath = Root + "/Settings/M0_Renderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null) { renderer = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(renderer, rendererPath); }
            var pipelinePath = Root + "/Settings/M0_URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null) { pipeline = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline, pipelinePath); }
            pipeline.msaaSampleCount = 2; pipeline.shadowDistance = 25; pipeline.renderScale = 1;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            var shader = Shader.Find("Ataka/SubtleVHS");
            if (shader == null) throw new InvalidOperationException("VHS shader missing");
            var materialPath = Root + "/Art/Materials/VHS.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            if (renderer.rendererFeatures.Count == 0)
            {
                var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = "Subtle VHS";
                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                feature.requirements = ScriptableRenderPassInput.Color;
                feature.passMaterial = material;
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                feature.Create();
                EditorUtility.SetDirty(renderer);
            }
            return material;
        }

        static Material Material(string name, Color color)
        {
            string path = Root + "/Art/Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result, path); }
            result.color = color; result.SetFloat("_Smoothness", .15f); return result;
        }

        static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(apartment, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }

        static void Front(float a, float b, float bottom, float top) => Box("Exterior wall", new Vector3((a+b)/2, (bottom+top)/2, -.175f), new Vector3(b-a, top-bottom, .35f), wall);
        static void Window(float left, float right)
        {
            foreach (float x in new[] { left, right, (left+right)/2 })
                Box("Window frame", new Vector3(x, 1.575f, -.175f), new Vector3(.045f, 1.45f, .09f), ceiling);
            Box("Window sill", new Vector3((left+right)/2, .84f, -.09f), new Vector3(right-left+.08f, .06f, .48f), ceiling);
            var blocker = new GameObject("Closed window collision"); blocker.transform.SetParent(apartment, false);
            blocker.transform.localPosition = new Vector3((left+right)/2, 1.575f, -.175f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(right-left, 1.45f, .025f);
        }
        static void PartitionX(string name, float z, float min, float max, float doorMin, float doorMax)
        {
            Box(name, new Vector3((min+doorMin)/2,1.25f,z),new Vector3(doorMin-min,2.5f,.1f),wall);
            Box(name, new Vector3((doorMax+max)/2,1.25f,z),new Vector3(max-doorMax,2.5f,.1f),wall);
            Box(name+" lintel", new Vector3((doorMin+doorMax)/2,2.25f,z),new Vector3(doorMax-doorMin,.5f,.1f),wall);
        }
        static void PartitionZ(string name, float x, float min, float max, float doorMin, float doorMax)
        {
            Box(name,new Vector3(x,1.25f,(min+doorMin)/2),new Vector3(.1f,2.5f,doorMin-min),wall);
            Box(name,new Vector3(x,1.25f,(doorMax+max)/2),new Vector3(.1f,2.5f,max-doorMax),wall);
            Box(name+" lintel",new Vector3(x,2.25f,(doorMin+doorMax)/2),new Vector3(.1f,.5f,doorMax-doorMin),wall);
        }
        static void AddDoor(string id, string label, Vector3 position, float width, float yaw)
        {
            var root = new GameObject(id); root.transform.SetParent(apartment, false);
            root.transform.localPosition = position; root.transform.localRotation = Quaternion.Euler(0,yaw,0);
            var hinge = new GameObject("Hinge"); hinge.transform.SetParent(root.transform, false);
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube); leaf.name = "Leaf";
            leaf.transform.SetParent(hinge.transform,false); leaf.transform.localPosition = new Vector3(width/2,1,0);
            leaf.transform.localScale = new Vector3(width-.02f,1.99f,.05f); leaf.GetComponent<Renderer>().sharedMaterial = door;
            UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());
            var collider = hinge.AddComponent<BoxCollider>(); collider.center = new Vector3(width/2,1,0); collider.size = new Vector3(width-.02f,1.99f,.05f);
            var component = root.AddComponent<Door>(); component.persistentId = id; component.label = label;
            component.hinge = hinge.transform; component.leaf = collider;
        }
        static void Lamp(string id, Vector3 position, Vector3 switchPosition, float intensity)
        {
            var go = new GameObject("Lamp " + id); go.transform.SetParent(apartment, false); go.transform.localPosition = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1,.73f,.43f);
            light.intensity = intensity; light.range = 5; light.shadows = id == "living" || id == "kitchen" ? LightShadows.Soft : LightShadows.None;
            var button = Box("Switch " + id, switchPosition, new Vector3(.09f,.13f,.045f), ceiling);
            var toggle = button.AddComponent<LampSwitch>(); toggle.persistentId = "lamp-"+id; toggle.label = "свет";
            toggle.controlledLight = light; toggle.intensity = intensity;
        }
        static void Balcony()
        {
            Box("Balcony floor", new Vector3(4.7f,-.1f,-.825f),new Vector3(3.5f,.2f,.95f),floor);
            Box("Balcony threshold",new Vector3(5.175f,-.08f,-.35f),new Vector3(.75f,.16f,.4f),floor);
            Box("Balcony railing",new Vector3(4.7f,.53f,-1.31f),new Vector3(3.5f,1.06f,.055f),metal);
            Box("Balcony west railing",new Vector3(2.95f,.53f,-.83f),new Vector3(.055f,1.06f,.95f),metal);
            Box("Balcony east railing",new Vector3(6.45f,.53f,-.83f),new Vector3(.055f,1.06f,.95f),metal);
        }
        static void Landing()
        {
            Box("Entrance threshold",new Vector3(8.05f,-.09f,6.15f),new Vector3(.9f,.18f,.4f),floor);
            Box("Fifth floor landing",new Vector3(7.7f,-.1f,7.2f),new Vector3(2.3f,.2f,1.8f),floor);
            Box("Landing rear",new Vector3(7.7f,1.25f,8.1f),new Vector3(2.5f,2.5f,.2f),wall);
            Box("Landing west",new Vector3(6.55f,1.25f,7.2f),new Vector3(.2f,2.5f,1.8f),wall);
            Box("Landing east",new Vector3(8.85f,1.25f,7.2f),new Vector3(.2f,2.5f,1.8f),wall);
            Box("Landing ceiling",new Vector3(7.7f,2.6f,7.2f),new Vector3(2.5f,.2f,1.8f),ceiling);
        }
        static void Exterior()
        {
            Box("Courtyard ground proxy",new Vector3(4,-Floor-.1f,-25),new Vector3(100,.2f,80),floor);
            Box("Five storey opposite building proxy",new Vector3(5, -Floor+7,-30),new Vector3(40,14,9),wall);
            Box("Distant block proxy",new Vector3(-28, -Floor+12,-55),new Vector3(20,24,12),wall);
        }

        [MenuItem("Ataka/M0/Build Windows")]
        public static void Build()
        {
            if (!File.Exists(ScenePath)) Generate();
            Directory.CreateDirectory("Builds/M0");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/M0/Ataka-M0.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("M0_BUILD_SUCCESS " + report.summary.totalSize);
        }
    }
}
