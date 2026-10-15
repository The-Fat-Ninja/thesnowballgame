using System.Collections.Generic;

namespace SnowballGame.Core
{
    public enum ThrowStyle
    {
        /// <summary>Flat and fast; stopped by any cover in the way.</summary>
        Line,
        /// <summary>Arcs over cover and lands on a spot; can be dodged.</summary>
        Lob,
        /// <summary>Boulders only: rolled along the ground, growing as they go.</summary>
        Roll,
    }

    /// <summary>The kinds of snowball a kid can make (Docs/Design/GameDesignVision.md).</summary>
    public enum SnowballType : byte
    {
        /// <summary>The all-rounder.</summary>
        Packed,
        /// <summary>Made in a moment, flies fast and true, stings less.</summary>
        Quick,
        /// <summary>Wet and heavy: splashes a wide area and slows whoever it hits.</summary>
        Slush,
        /// <summary>No sting at all: bursts into a cloud that blocks line drives and spoils aim.</summary>
        Powder,
        /// <summary>Rolled, not thrown: grows as it rolls, knocks kids back, ends as a snowbank.</summary>
        Boulder,
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
        /// <summary>The snowball this kid makes unless told otherwise.</summary>
        public SnowballType Favourite = SnowballType.Packed;
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
        public bool Soaked;
        /// <summary>Snowballs carried, oldest first. A boulder fills the pouch on its own.</summary>
        public readonly List<SnowballType> Pouch = new List<SnowballType>();
        /// <summary>What the kid packs when they have time (persists between turns).</summary>
        public SnowballType MakeType;
        /// <summary>Ticks of slush-soaked slowness left.</summary>
        public int SlowTicks;

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

        /// <summary>
        /// Snowballs carried. Setting it fills the pouch with packed snowballs
        /// (tests and set-up only).
        /// </summary>
        public int Ammo
        {
            get => Pouch.Count;
            set
            {
                Pouch.Clear();
                for (int i = 0; i < value; i++) Pouch.Add(SnowballType.Packed);
            }
        }

        public bool CarryingBoulder => Pouch.Count == 1 && Pouch[0] == SnowballType.Boulder;
        public bool Carries(SnowballType t) => Pouch.Contains(t);

        /// <summary>True when the kid has room to pack one more of the given type.</summary>
        public bool CanPack(SnowballType t) =>
            t == SnowballType.Boulder ? Pouch.Count == 0 : !CarryingBoulder && Pouch.Count < MaxAmmo;
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
        /// <summary>Which snowball to throw; null throws the oldest one carried.</summary>
        public SnowballType? Ball;
        public Vec2 ThrowTarget;
        /// <summary>Throw from where you stand, then move.</summary>
        public bool ThrowFirst;
        public bool HasBuild;
        public Vec2 BuildAt;
        /// <summary>Change what the kid packs from now on; null keeps their current choice.</summary>
        public SnowballType? Make;

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
        /// <summary>A powder puff burst: Pos, Amount = radius, OtherId = cloud id.</summary>
        CloudCreated,
        /// <summary>A slushball slowed a kid: KidId = thrower, OtherId = victim, Amount = ticks.</summary>
        Slowed,
    }

    /// <summary>A powder cloud: line drives can't see through it, aim inside it goes wild.</summary>
    public sealed class Cloud
    {
        public int Id;
        public Vec2 Pos;
        public int Radius;
        public int TicksLeft;
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
        /// <summary>The pouch at this moment, oldest first.</summary>
        public SnowballType[] Pouch;
        public bool Slowed;
        public KidAction Action;
    }

    public struct ProjectileFrame
    {
        public int Id;
        public ThrowStyle Style;
        public SnowballType Type;
        public Vec2 Pos;
        /// <summary>Height above the snow in centimetres (a boulder's centre).</summary>
        public int Height;
        /// <summary>Radius in centimetres (boulders grow as they roll).</summary>
        public int Radius;
    }

    public struct CloudFrame
    {
        public int Id;
        public Vec2 Pos;
        public int Radius;
        public int TicksLeft;
    }

    public sealed class Frame
    {
        public KidFrame[] Kids;
        public List<ProjectileFrame> Projectiles = new List<ProjectileFrame>();
        public List<CloudFrame> Clouds = new List<CloudFrame>();
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
