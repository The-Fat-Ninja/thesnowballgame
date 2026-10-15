using UnityEngine;
using UnityEngine.UI;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// Caption overlay for demo tapes: a title strip (top left), clock and score
    /// (top right) and a centred title card. Rendered by the camera
    /// (screen-space camera canvas) so it ends up in recorded frames.
    /// </summary>
    public sealed class DemoCaptions
    {
        public Text Title { get; }
        public Text Subtitle { get; }
        public Text Clock { get; }
        public Text Score { get; }
        public GameObject Card { get; }
        readonly Text cardTitle, cardBody;

        public DemoCaptions(Transform parent, Camera cam)
        {
            var go = new GameObject("Captions", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 1f;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var strip = Panel(go.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(1180, 150), new Color(1, 1, 1, 0.88f));
            Title = Label(strip, font, 46, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(24, -12), new Vector2(1140, 60));
            Subtitle = Label(strip, font, 25, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(24, -68), new Vector2(1140, 72));

            var right = Panel(go.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(520, 120), new Color(1, 1, 1, 0.88f));
            Clock = Label(right, font, 46, FontStyle.Bold, TextAnchor.UpperRight, new Vector2(-24, -12), new Vector2(480, 60));
            Score = Label(right, font, 28, FontStyle.Bold, TextAnchor.UpperRight, new Vector2(-24, -70), new Vector2(480, 40));

            var cardT = Panel(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 380), new Color(0.165f, 0.141f, 0.22f, 0.9f));
            Card = cardT.gameObject;
            cardTitle = Label(cardT, font, 96, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 90), new Vector2(1200, 130), true);
            cardTitle.color = Palette.Ember;
            cardBody = Label(cardT, font, 38, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0, -70), new Vector2(1200, 180), true);
            cardBody.color = Color.white;
        }

        public void ShowCard(string heading, string body, Color? headingColour = null)
        {
            Card.SetActive(true);
            cardTitle.text = heading;
            cardTitle.color = headingColour ?? Palette.Ember;
            cardBody.text = body;
        }

        /// <summary>Call just before rendering so the captions lay out in edit mode.</summary>
        public static void PrepareRender() => Canvas.ForceUpdateCanvases();

        static Transform Panel(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color colour)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = colour;
            return go.transform;
        }

        static Text Label(Transform parent, Font font, int size, FontStyle fs, TextAnchor align, Vector2 pos, Vector2 box, bool centred = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Vector2 a = centred ? new Vector2(0.5f, 0.5f)
                : align == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = a;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = fs;
            t.alignment = align;
            t.color = Palette.Ink;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }
    }
}
