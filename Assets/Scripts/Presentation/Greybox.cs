using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>Colours from Docs/Design/ART_DIRECTION.md and primitive-building helpers.</summary>
    public static class Palette
    {
        public static readonly Color SnowLight = Hex("#F7F4EE");
        public static readonly Color SnowGround = Hex("#DCE3EF");
        public static readonly Color SnowShadow = Hex("#AFC2E8");
        public static readonly Color DeepShadow = Hex("#6E7FB8");
        public static readonly Color Packed = Hex("#9AA5BA");
        public static readonly Color Ice = Hex("#8FCBE6");
        public static readonly Color Glow = Hex("#FFC766");
        public static readonly Color Brick = Hex("#B5543C");
        public static readonly Color Pine = Hex("#2F6B57");
        public static readonly Color Ember = Hex("#F28C28");
        public static readonly Color Plum = Hex("#7A4FD6");
        public static readonly Color Cocoa = Hex("#6B3F2A");
        public static readonly Color Ink = Hex("#2A2438");
        public static readonly Color Roof = Hex("#5B4A5E");
        public static readonly Color Skin = Hex("#E2B48C");

        public static Color Team(int team) => team == Match.Home ? Ember : Plum;

        public static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }

    /// <summary>Creates collider-free primitives with shared, colour-keyed materials.</summary>
    public sealed class Greybox
    {
        readonly Material litBase;
        readonly Material unlitBase;
        readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
        readonly Dictionary<Color, Material> unlit = new Dictionary<Color, Material>();

        public Greybox(Material litBase, Material unlitBase)
        {
            this.litBase = litBase;
            this.unlitBase = unlitBase;
        }

        public Material Lit(Color c)
        {
            if (!lit.TryGetValue(c, out var m))
            {
                m = new Material(litBase) { color = c };
                lit[c] = m;
            }
            return m;
        }

        public Material Unlit(Color c)
        {
            if (!unlit.TryGetValue(c, out var m))
            {
                m = new Material(unlitBase) { color = c };
                unlit[c] = m;
            }
            return m;
        }

        public GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color colour, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Lit(colour);
            return go;
        }

        public LineRenderer Line(Transform parent, Color colour, float width, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Unlit(colour);
            lr.widthMultiplier = width;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = 0;
            return lr;
        }

        public static Vector3 World(Vec2 p, float y = 0f) => new Vector3(p.X / 100f, y, p.Z / 100f);
        public static Vec2 Core(Vector3 w) => new Vec2(Mathf.RoundToInt(w.x * 100f), Mathf.RoundToInt(w.z * 100f));
    }
}
