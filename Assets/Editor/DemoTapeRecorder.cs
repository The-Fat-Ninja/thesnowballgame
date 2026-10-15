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
    /// Output: Recordings/RealtimeDemo.mp4 and Recordings/WegoDemo.mp4, plus stills
    /// every ten seconds in Recordings/Stills/.
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
            Record(DemoDirector.Style.Realtime, "Recordings/RealtimeDemo.mp4");
            Record(DemoDirector.Style.Wego, "Recordings/WegoDemo.mp4");
            Debug.Log("[DemoTapeRecorder] Wrote Recordings/.");
        }

        static void Record(DemoDirector.Style style, string path)
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
            var director = new DemoDirector(style, lit, unlit, ctrl.transform, cam, Seed);
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
                        File.WriteAllBytes($"Recordings/Stills/{style}_{(int)nextStill:00}s.png", tex.EncodeToPNG());
                        nextStill += 10f;
                    }
                    frame++;
                }
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            director.Dispose();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[DemoTapeRecorder] {style}: {frame} frames ({frame / (float)Fps:0.0} s) -> {path}");
        }
    }
}
