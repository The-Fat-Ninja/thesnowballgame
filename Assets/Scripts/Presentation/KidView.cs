using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// A greybox kid: boots, puffy coat, head, team-coloured hat, scarf and mittens
    /// (the team colour lives in the scarf and hat, as in the art direction),
    /// plus a warmth bar and snowball pips that face the camera.
    /// </summary>
    public sealed class KidView
    {
        public readonly int Id;
        public readonly int Team;
        public readonly Transform Root;

        readonly Transform body;
        readonly Transform hud;
        readonly Transform warmthFill;
        readonly Renderer coat;
        readonly Material coatMat;
        readonly Material flashMat;
        readonly GameObject ring;
        readonly GameObject[] pips;
        readonly Greybox box;
        float flash;
        float bob;
        bool walking;
        bool soaked;

        public KidView(Greybox box, Transform parent, Kid kid)
        {
            this.box = box;
            Id = kid.Id;
            Team = kid.Team;
            var go = new GameObject($"Kid_{kid.Def.Name}");
            go.transform.SetParent(parent, false);
            Root = go.transform;

            // Drawn larger than life so kids read at the tactical camera's height.
            const float readability = 1.45f;
            float s = readability * (kid.Def.HitRadius < Rules.DefaultHitRadius ? 0.8f : kid.Def.Grit >= 5 ? 1.12f : 1f);
            body = new GameObject("Body").transform;
            body.SetParent(Root, false);
            body.localScale = Vector3.one * s;

            Color team = Palette.Team(kid.Team);
            Color coatColour = Palette.Hex(kid.Def.Coat);
            coatMat = box.Lit(coatColour);
            flashMat = box.Lit(Color.white);

            box.Prim(PrimitiveType.Cylinder, body, new Vector3(-0.12f, 0.15f, 0), new Vector3(0.18f, 0.15f, 0.2f), Palette.Ink, "BootL");
            box.Prim(PrimitiveType.Cylinder, body, new Vector3(0.12f, 0.15f, 0), new Vector3(0.18f, 0.15f, 0.2f), Palette.Ink, "BootR");
            coat = box.Prim(PrimitiveType.Capsule, body, new Vector3(0, 0.62f, 0), new Vector3(0.62f, 0.36f, 0.55f), coatColour, "Coat").GetComponent<Renderer>();
            box.Prim(PrimitiveType.Sphere, body, new Vector3(-0.36f, 0.55f, 0.05f), Vector3.one * 0.18f, team, "MittenL");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(0.36f, 0.55f, 0.05f), Vector3.one * 0.18f, team, "MittenR");
            box.Prim(PrimitiveType.Cylinder, body, new Vector3(0, 0.93f, 0), new Vector3(0.5f, 0.05f, 0.5f), team, "Scarf");
            box.Prim(PrimitiveType.Cube, body, new Vector3(0.12f, 0.78f, 0.24f), new Vector3(0.1f, 0.3f, 0.05f), team, "ScarfTail");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(0, 1.2f, 0), Vector3.one * 0.5f, Palette.Skin, "Head");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(-0.09f, 1.23f, 0.22f), Vector3.one * 0.06f, Palette.Ink, "EyeL");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(0.09f, 1.23f, 0.22f), Vector3.one * 0.06f, Palette.Ink, "EyeR");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(0, 1.36f, 0), new Vector3(0.52f, 0.34f, 0.52f), team, "Hat");
            box.Prim(PrimitiveType.Sphere, body, new Vector3(0, 1.56f, 0), Vector3.one * 0.16f, Color.white, "Pom");

            ring = box.Prim(PrimitiveType.Cylinder, Root, new Vector3(0, 0.01f, 0), new Vector3(1.4f, 0.01f, 1.4f), Palette.Glow, "SelectionRing");
            ring.SetActive(false);

            hud = new GameObject("Hud").transform;
            hud.SetParent(Root, false);
            hud.localPosition = new Vector3(0, 1.9f * s, 0);
            hud.localScale = Vector3.one * 1.4f;
            box.Prim(PrimitiveType.Cube, hud, Vector3.zero, new Vector3(0.84f, 0.12f, 0.02f), Palette.Ink, "WarmthBack");
            warmthFill = box.Prim(PrimitiveType.Cube, hud, new Vector3(0, 0, -0.012f), new Vector3(0.8f, 0.08f, 0.02f), team, "WarmthFill").transform;
            pips = new GameObject[3];
            for (int i = 0; i < pips.Length; i++)
                pips[i] = box.Prim(PrimitiveType.Sphere, hud, new Vector3(-0.2f + i * 0.2f, 0.18f, 0), Vector3.one * 0.13f, Color.white, "Snowball");

            Apply(new KidFrame { Pos = kid.Pos, Facing = kid.Facing, Warmth = kid.Warmth, Ammo = kid.Ammo, Action = kid.Soaked ? KidAction.Soaked : KidAction.Idle }, kid.MaxWarmth, kid.MaxAmmo);
        }

        public void Apply(KidFrame f, int maxWarmth, int maxAmmo) => Apply(f, f, 0f, maxWarmth, maxAmmo);

        /// <summary>Shows the kid between two simulation frames.</summary>
        public void Apply(KidFrame a, KidFrame b, float t, int maxWarmth, int maxAmmo)
        {
            Vector3 pa = Greybox.World(a.Pos), pb = Greybox.World(b.Pos);
            Root.localPosition = Vector3.Lerp(pa, pb, t);
            Vector3 fa = Greybox.World(a.Facing), fb = Greybox.World(b.Facing);
            Vector3 face = Vector3.Slerp(fa.sqrMagnitude > 0 ? fa : Vector3.forward, fb.sqrMagnitude > 0 ? fb : Vector3.forward, t);
            if (face.sqrMagnitude > 0.0001f) body.localRotation = Quaternion.LookRotation(face.normalized, Vector3.up);

            KidAction action = t < 0.5f ? a.Action : b.Action;
            walking = false;
            soaked = action == KidAction.Soaked;
            float frac = Mathf.Clamp01(b.Warmth / (float)Mathf.Max(1, maxWarmth));
            warmthFill.localScale = new Vector3(0.8f * frac, 0.08f, 0.02f);
            warmthFill.localPosition = new Vector3(-0.4f * (1f - frac), 0, -0.012f);
            for (int i = 0; i < pips.Length; i++) pips[i].SetActive(i < maxAmmo && i < b.Ammo && !soaked);

            Vector3 offset = Vector3.zero;
            Quaternion tilt = Quaternion.identity;
            switch (action)
            {
                case KidAction.Walk:
                    walking = true;
                    offset.y = Mathf.Abs(Mathf.Sin(bob)) * 0.08f;
                    break;
                case KidAction.Pack:
                case KidAction.Build:
                    tilt = Quaternion.Euler(28f, 0, 0);
                    offset.y = -0.08f;
                    break;
                case KidAction.Windup:
                    tilt = Quaternion.Euler(-12f, 0, 0);
                    break;
                case KidAction.Throw:
                    tilt = Quaternion.Euler(18f, 0, 0);
                    break;
                case KidAction.Soaked:
                    tilt = Quaternion.Euler(80f, 0, 0);
                    offset.y = 0.15f;
                    break;
            }
            body.localPosition = offset;
            body.localRotation *= tilt;
            hud.gameObject.SetActive(!soaked);
            coat.sharedMaterial = flash > 0 ? flashMat : soaked ? box.Lit(Color.Lerp(coatMat.color, Palette.DeepShadow, 0.6f)) : coatMat;
        }

        public void Flash() => flash = 0.18f;

        public void SetSelected(bool on) => ring.SetActive(on);

        public void Tick(Camera cam, float dt)
        {
            if (walking) bob += dt * 14f;
            if (flash > 0)
            {
                flash -= dt;
                if (flash <= 0) coat.sharedMaterial = soaked ? box.Lit(Color.Lerp(coatMat.color, Palette.DeepShadow, 0.6f)) : coatMat;
            }
            if (cam != null) hud.rotation = cam.transform.rotation;
        }
    }
}
