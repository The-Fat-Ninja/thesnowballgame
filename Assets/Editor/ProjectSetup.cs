using System.IO;
using SnowballGame.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SnowballGame.EditorTools
{
    /// <summary>
    /// One-click (or batch-mode) project setup so the project can be rebuilt from the
    /// repository: URP asset, greybox materials, player settings and the prototype scene.
    ///   Unity -batchmode -quit -projectPath . -executeMethod SnowballGame.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        const string SettingsDir = "Assets/Settings";
        const string MaterialsDir = "Assets/Materials";
        const string ScenePath = "Assets/Scenes/MapleCourt.unity";

        [MenuItem("Snowball Game/Setup Project")]
        public static void Run()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory("Assets/Scenes");
            SetupRenderPipeline();
            SetupPlayer();
            var lit = MakeMaterial("GreyboxLit", "Universal Render Pipeline/Lit");
            var unlit = MakeMaterial("GreyboxUnlit", "Universal Render Pipeline/Unlit");
            MakeScene(lit, unlit);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Done.");
        }

        static void SetupRenderPipeline()
        {
            string assetPath = $"{SettingsDir}/URP-Default.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, $"{SettingsDir}/URP-Default-Renderer.asset");
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.shadowDistance = 60f;
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void SetupPlayer()
        {
            PlayerSettings.companyName = "The Fat Ninja";
            PlayerSettings.productName = "The Snowball Game";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.thefatninja.snowballgame");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.thefatninja.snowballgame");
            // Landscape only (owner decision 9).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Input System package only.
            var player = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (player.Length > 0)
            {
                var so = new SerializedObject(player[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null)
                {
                    prop.intValue = 1;
                    so.ApplyModifiedProperties();
                }
            }
        }

        static Material MakeMaterial(string name, string shader)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void MakeScene(Material lit, Material unlit)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Hex("#CFE3F5");
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            camGo.transform.position = new Vector3(20f, 27f, -6f);
            camGo.transform.LookAt(new Vector3(20f, 0f, 16.5f));

            var sunGo = new GameObject("Sun", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Palette.Hex("#FFF1DC");
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("#9FB6D9");
            RenderSettings.ambientEquatorColor = Palette.Hex("#8E9CC0");
            RenderSettings.ambientGroundColor = Palette.Hex("#7C8DB8");

            // Neutral tonemapping keeps sunlit snow from clipping to flat white.
            string profilePath = "Assets/Settings/GreyboxPost.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.Neutral);
                var colour = profile.Add<ColorAdjustments>(true);
                colour.postExposure.Override(-0.1f);
                colour.saturation.Override(12f);
                foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
                AssetDatabase.SaveAssets();
            }
            var volume = new GameObject("Post", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var game = new GameObject("Match", typeof(MatchController));
            var so = new SerializedObject(game.GetComponent<MatchController>());
            so.FindProperty("litMaterial").objectReferenceValue = lit;
            so.FindProperty("unlitMaterial").objectReferenceValue = unlit;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }

    /// <summary>
    /// Renders the prototype scene after a few AI-vs-AI turns to a PNG, without
    /// entering play mode, for visual checks from batch mode:
    ///   Unity -batchmode -quit -projectPath . -executeMethod SnowballGame.EditorTools.SceneCapture.Run
    /// Output: Logs/Captures/MapleCourt_turnN.png
    /// </summary>
    public static class SceneCapture
    {
        [MenuItem("Snowball Game/Capture Maple Court")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MapleCourt.unity", OpenSceneMode.Single);
            var ctrl = Object.FindAnyObjectByType<MatchController>();
            var cam = Camera.main;
            Directory.CreateDirectory("Logs/Captures");
            foreach (int turns in new[] { 0, 4 })
            {
                ctrl.BuildWorld(7);
                ctrl.SimulateForCapture(turns);
                DynamicGI.UpdateEnvironment();
                var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render(); // warm-up: ambient and shadows settle on the first frame
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Logs/Captures/MapleCourt_turn{turns}.png", tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                ctrl.ClearForCapture();
            }
            Debug.Log("[SceneCapture] Wrote Logs/Captures.");
        }
    }
}
