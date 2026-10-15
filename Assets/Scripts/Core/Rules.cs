namespace SnowballGame.Core
{
    /// <summary>
    /// Every tuning number in one place. Distances in centimetres, time in ticks.
    /// </summary>
    public static class Rules
    {
        public const int TicksPerSecond = 10;
        /// <summary>One WEGO turn plays out for this many ticks (3 seconds).</summary>
        public const int TurnTicks = 30;
        /// <summary>"When the streetlights come on, game's over."</summary>
        public const int TurnLimit = 15;

        public const int CellSize = 100;
        public const int KidBodyRadius = 30;
        public const int DefaultHitRadius = 35;
        public const int SmallHitRadius = 25;

        // Movement: base speed in cm per second, plus per Hustle point.
        public const int BaseSpeed = 250;
        public const int SpeedPerHustle = 50;
        public const int FreshSnowSpeedPermille = 850;
        public const int PackedSpeedPermille = 1100;
        public const int IceSpeedPermille = 1000;

        // Warmth.
        public const int BaseWarmth = 60;
        public const int WarmthPerGrit = 15;

        // Throwing.
        public const int WindupTicks = 3;
        public const int LineSpeedPerTick = 180;
        public const int LineBaseRange = 800;
        public const int LobBaseRange = 1000;
        public const int RangePerArm = 200;
        public const int LineChill = 25;
        public const int LobChill = 20;
        public const int BackHitBonusPermille = 1500;
        public const int DuckedLobPermille = 500;
        public const int LobSpeedPerTick = 100;
        public const int LobMinFlightTicks = 6;
        public const int LobSplashRadius = 60;
        public const int LineHeight = 120;
        public const int ProjectileRadius = 8;
        /// <summary>A kid who held still this turn within this distance of cover ducks.</summary>
        public const int DuckDistance = 100;

        // Snow work.
        public const int PackBaseTicks = 16;
        public const int PackTicksPerCraft = 2;
        public const int BuildBaseTicks = 30;
        public const int BuildTicksPerCraft = 4;
        public const int BuildReach = 150;
        public const int BuiltWallRadius = 45;
        public const int BuiltWallHp = 40;
        public const int SnowbankHp = 60;
        public const int SnowmanHp = 40;
        public const int CoverDamagePerHit = 20;

        public static int SpeedPerTick(int hustle) => (BaseSpeed + SpeedPerHustle * hustle) / TicksPerSecond;
        public static int MaxWarmth(int grit) => BaseWarmth + WarmthPerGrit * grit;
        public static int MaxRange(ThrowStyle style, int arm) =>
            (style == ThrowStyle.Lob ? LobBaseRange : LineBaseRange) + RangePerArm * arm;
        public static int PackTicks(int craft) => PackBaseTicks - PackTicksPerCraft * craft;
        public static int BuildTicks(int craft) => BuildBaseTicks - BuildTicksPerCraft * craft;
    }
}
