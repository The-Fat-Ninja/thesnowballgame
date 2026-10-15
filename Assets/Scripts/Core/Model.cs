using System.Collections.Generic;

namespace SnowballGame.Core
{
    public enum ThrowStyle
    {
        /// <summary>Flat and fast; stopped by any cover in the way.</summary>
        Line,
        /// <summary>Arcs over cover and lands on a spot; can be dodged.</summary>
        Lob,
    }

    public enum Ground : byte
    {
        Fresh,
        Packed,
        Ice,
        /// <summary>Houses and other solid ground features.</summary>
        Blocked,
    }

    public enum CoverKind
    {
        Snowbank,
        SnowWall,
        Snowman,
        Car,
        GardenWall,
        Hedge,
        Tree,
        TrashCan,
        Streetlight,
    }

    public enum KidAction
    {
        Idle,
        Walk,
        Windup,
        Throw,
        Pack,
        Build,
        Soaked,
    }

    /// <summary>A kid's fixed profile: who they are and their five stats (1-5).</summary>
    public sealed class KidDef
    {
        public string Name;
        public string Role;
        public int Arm, Aim, Hustle, Grit, Craft;
        public int HitRadius = Rules.DefaultHitRadius;
        public int MaxAmmo = 2;
        /// <summary>Coat colour for the greybox view, as #RRGGBB.</summary>
        public string Coat = "#888888";

        public KidDef(string name, string role, int arm, int aim, int hustle, int grit, int craft, string coat)
        {
            Name = name;
            Role = role;
            Arm = arm;
            Aim = aim;
            Hustle = hustle;
            Grit = grit;
            Craft = craft;
            Coat = coat;
        }
    }

    public sealed class Kid
    {
        public int Id;
        public int Team;
        public KidDef Def;
        public Vec2 Pos;
        /// <summary>Facing direction, length 100.</summary>
        public Vec2 Facing;
        public int Warmth;
        public int MaxWarmth;
        public int Ammo;
        public bool Soaked;

        // Per-turn working state.
        internal KidOrder Order;
        internal int PathIndex;
        internal bool Thrown;
        internal int Windup;
        internal int WorkTicks;
        internal bool BuildDone;
        internal bool MovedThisTurn;
        internal KidAction Action;

        public int MaxAmmo => Def.MaxAmmo;
        public int SpeedPerTick => Rules.SpeedPerTick(Def.Hustle);
    }

    public sealed class Cover
    {
        public int Id;
        public CoverKind Kind;
        public Vec2 Pos;
        public int Radius;
        /// <summary>Remaining hit points, or -1 for indestructible cover.</summary>
        public int Hp;
        public int MaxHp;
        public bool Alive = true;

        public bool Destructible => MaxHp > 0;
    }

    /// <summary>One kid's plan for one turn.</summary>
    public sealed class KidOrder
    {
        public int KidId;
        /// <summary>Waypoints walked in order from the kid's position.</summary>
        public List<Vec2> Path = new List<Vec2>();
        public bool HasThrow;
        public ThrowStyle Style;
        public Vec2 ThrowTarget;
        /// <summary>Throw from where you stand, then move.</summary>
        public bool ThrowFirst;
        public bool HasBuild;
        public Vec2 BuildAt;

        public KidOrder(int kidId)
        {
            KidId = kidId;
        }
    }

    public enum EventKind
    {
        Thrown,
        HitKid,
        Splash,
        CoverHit,
        CoverDestroyed,
        CoverBuilt,
        Soaked,
        Packed,
    }

    public struct MatchEvent
    {
        public int Tick;
        public EventKind Kind;
        public int KidId;
        /// <summary>Victim kid, cover id or projectile id depending on the event.</summary>
        public int OtherId;
        public Vec2 Pos;
        public int Amount;
    }

    public struct KidFrame
    {
        public int Id;
        public Vec2 Pos;
        public Vec2 Facing;
        public int Warmth;
        public int Ammo;
        public KidAction Action;
    }

    public struct ProjectileFrame
    {
        public int Id;
        public ThrowStyle Style;
        public Vec2 Pos;
        /// <summary>Height above the snow in centimetres.</summary>
        public int Height;
    }

    public sealed class Frame
    {
        public KidFrame[] Kids;
        public List<ProjectileFrame> Projectiles = new List<ProjectileFrame>();
    }

    /// <summary>Everything that happened in one resolved turn, tick by tick, for playback.</summary>
    public sealed class TurnLog
    {
        public int Turn;
        /// <summary>TurnTicks + 1 frames; frame 0 is the state before the turn.</summary>
        public List<Frame> Frames = new List<Frame>();
        public List<MatchEvent> Events = new List<MatchEvent>();
    }

    public enum MatchOutcome
    {
        InProgress,
        HomeWins,
        AwayWins,
        Draw,
    }
}
