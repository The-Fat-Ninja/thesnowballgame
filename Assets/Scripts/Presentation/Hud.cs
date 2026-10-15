using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// Greybox heads-up display built in code: turn banner, crew roster, selected-kid
    /// card, order buttons and the GO! button. Landscape, touch-sized buttons.
    /// </summary>
    public sealed class Hud
    {
        public Text Banner { get; private set; }
        public Text Hint { get; private set; }
        public Text KidCard { get; private set; }
        public Text RosterHome { get; private set; }
        public Text RosterAway { get; private set; }
        public GameObject ResultPanel { get; private set; }
        public Text ResultText { get; private set; }

        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        readonly Font font;
        Transform canvas;

        public Hud()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public void Build(Transform parent)
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(parent, false);
            }

            var go = new GameObject("Hud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvas = go.transform;

            var top = Panel("TopBar", new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(760, 92));
            Banner = Label(top, "Banner", "", 34, TextAnchor.MiddleCenter, new Vector2(0, 14), new Vector2(740, 50));
            Hint = Label(top, "Hint", "", 22, TextAnchor.MiddleCenter, new Vector2(0, -24), new Vector2(740, 34));

            var home = Panel("HomeRoster", new Vector2(0f, 1f), new Vector2(16, -16), new Vector2(330, 190), new Vector2(0, 1));
            RosterHome = Label(home, "Text", "", 22, TextAnchor.UpperLeft, Vector2.zero, new Vector2(306, 174));
            var away = Panel("AwayRoster", new Vector2(1f, 1f), new Vector2(-16, -16), new Vector2(330, 190), new Vector2(1, 1));
            RosterAway = Label(away, "Text", "", 22, TextAnchor.UpperLeft, Vector2.zero, new Vector2(306, 174));

            var card = Panel("KidCard", new Vector2(0f, 0f), new Vector2(16, 132), new Vector2(560, 150), new Vector2(0, 0));
            KidCard = Label(card, "Text", "", 22, TextAnchor.UpperLeft, Vector2.zero, new Vector2(536, 134));

            ResultPanel = Panel("Result", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 260)).gameObject;
            ResultText = Label(ResultPanel.transform, "Text", "", 44, TextAnchor.MiddleCenter, new Vector2(0, 40), new Vector2(720, 140));
            ResultPanel.SetActive(false);
        }

        public void AddButton(string key, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color colour, Action onClick, Transform parent = null)
        {
            var go = new GameObject($"Button_{key}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent ?? canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = colour;
            var b = go.GetComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            Label(go.transform, "Label", label, 26, TextAnchor.MiddleCenter, Vector2.zero, size).color = Palette.Ink;
            buttons[key] = b;
        }

        public void AddResultButton(string key, string label, Action onClick) =>
            AddButton(key, label, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(300, 80), Palette.Ember, onClick, ResultPanel.transform);

        public void SetButtonLabel(string key, string label)
        {
            if (buttons.TryGetValue(key, out var b)) b.GetComponentInChildren<Text>().text = label;
        }

        public void SetButtonColour(string key, Color c)
        {
            if (buttons.TryGetValue(key, out var b)) b.GetComponent<Image>().color = c;
        }

        public void SetInteractable(bool on)
        {
            foreach (var kv in buttons)
                if (!kv.Key.StartsWith("result", StringComparison.Ordinal)) kv.Value.interactable = on;
        }

        Transform Panel(string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 1f);
            if (pivot == null && anchor == new Vector2(0.5f, 0.5f)) rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.86f);
            return go.transform;
        }

        Text Label(Transform parent, string name, string text, int size, TextAnchor align, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Palette.Ink;
            t.text = text;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }
    }
}
