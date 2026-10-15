using System.IO;
using SnowballGame.Presentation;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SnowballGame.EditorTools
{
    /// <summary>
    /// Records side-by-side comparison footage of the two play styles as MP4
    /// (1920x1080, 30 fps) without entering play mode:
    ///   Unity -batchmode -quit -projectPath . -executeMethod SnowballGame.EditorTools.DemoTapeRecorder.Run
    /// Output: Recordings/RealtimeDemo.mp4, Recordings/WegoDemo.mp4 and
    /// Recordings/SnowballTypes.mp4, plus stills every ten seconds in Recordings/Stills/.
    /// "Snowball Game > Record Snowball Showcase" records only the snowball tour.
    /// </summary>
    public static class DemoTapeRecorder
    {
        const int Width = 1920;
        const int Height = 1080;
        const int Fps = 30;
        const int Seed = 11;
        const float MaxSeconds = 120f;

        [MenuItem("Snowball Game/Record Demo Tapes")]
        public static void Run()
        {
            Directory.CreateDirectory("Recordings/Stills");
            Record("Realtime", "Recordings/RealtimeDemo.mp4", 10f, (lit, unlit, parent, cam) => new DemoDirector(DemoDirector.Style.Realtime, lit, unlit, parent, cam, Seed));
            Record("Wego", "Recordings/WegoDemo.mp4", 10f, (lit, unlit, parent, cam) => new DemoDirector(DemoDirector.Style.Wego, lit, unlit, parent, cam, Seed));
            RecordShowcase();
            Debug.Log("[DemoTapeRecorder] Wrote Recordings/.");
        }

        [MenuItem("Snowball Game/Record Snowball Showcase")]
        public static void RecordShowcase()
        {
            Directory.CreateDirectory("Recordings/Stills");
            Record("SnowballTypes", "Recordings/SnowballTypes.mp4", 1f, (lit, unlit, parent, cam) => new SnowballShowcase(lit, unlit, parent, cam));
        }

        static void Record(string name, string path, float stillEvery, System.Func<Material, Material, Transform, Camera, IDemoTape> make)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MapleCourt.unity", OpenSceneMode.Single);
            var ctrl = Object.FindAnyObjectByType<MatchController>();
            var so = new SerializedObject(ctrl);
            var lit = (Material)so.FindProperty("litMaterial").objectReferenceValue;
            var unlit = (Material)so.FindProperty("unlitMaterial").objectReferenceValue;
            var cam = Camera.main;

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var director = make(lit, unlit, ctrl.transform, cam);
            DynamicGI.UpdateEnvironment();

            var video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = Width,
                height = Height,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
            };
            if (File.Exists(path)) File.Delete(path);
            int frame = 0;
            float nextStill = 0f;
            using (var encoder = new MediaEncoder(path, video))
            {
                director.PrepareRender();
                cam.Render(); // warm-up frame: ambient and shadows settle
                while (!director.Finished && frame < MaxSeconds * Fps)
                {
                    director.Advance(1f / Fps);
                    director.PrepareRender();
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    // Raw bytes, not the Texture2D: the texture overload loses its GPU resource under D3D12.
                    encoder.AddFrame(Width, Height, Width * 4, TextureFormat.RGBA32, tex.GetRawTextureData<byte>());
                    if (director.Seconds >= nextStill)
                    {
                        tex.Apply();
                        File.WriteAllBytes($"Recordings/Stills/{name}_{nextStill:00}s.png", tex.EncodeToPNG());
                        nextStill += stillEvery;
                    }
                    frame++;
                }
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            director.Dispose();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[DemoTapeRecorder] {name}: {frame} frames ({frame / (float)Fps:0.0} s) -> {path}");
        }
    }
}
