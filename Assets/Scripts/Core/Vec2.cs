using System;

namespace SnowballGame.Core
{
    /// <summary>
    /// A ground-plane position or offset in whole centimetres. The rules core uses
    /// integer maths only so a match plays out identically on every platform
    /// (needed for WEGO plan exchange and replays).
    /// </summary>
    [Serializable]
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly int X;
        public readonly int Z;

        public Vec2(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static readonly Vec2 Zero = new Vec2(0, 0);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Z == b.Z;
        public static bool operator !=(Vec2 a, Vec2 b) => !(a == b);

        public long LengthSq => (long)X * X + (long)Z * Z;
        public int Length => (int)IntMath.Sqrt(LengthSq);

        public static long Dot(Vec2 a, Vec2 b) => (long)a.X * b.X + (long)a.Z * b.Z;
        public static long DistSq(Vec2 a, Vec2 b) => (a - b).LengthSq;
        public static int Dist(Vec2 a, Vec2 b) => (a - b).Length;

        /// <summary>This vector scaled to the given length (zero stays zero).</summary>
        public Vec2 WithLength(int length)
        {
            int len = Length;
            if (len == 0) return Zero;
            return new Vec2((int)((long)X * length / len), (int)((long)Z * length / len));
        }

        /// <summary>Rotated 90 degrees counter-clockwise.</summary>
        public Vec2 Perp => new Vec2(-Z, X);

        /// <summary>Moves from a towards b by at most step centimetres.</summary>
        public static Vec2 MoveTowards(Vec2 a, Vec2 b, int step)
        {
            Vec2 d = b - a;
            int len = d.Length;
            if (len <= step || len == 0) return b;
            return a + new Vec2((int)((long)d.X * step / len), (int)((long)d.Z * step / len));
        }

        /// <summary>Linear interpolation with t in thousandths.</summary>
        public static Vec2 Lerp(Vec2 a, Vec2 b, int tPermille) =>
            new Vec2(a.X + (int)((long)(b.X - a.X) * tPermille / 1000), a.Z + (int)((long)(b.Z - a.Z) * tPermille / 1000));

        public bool Equals(Vec2 other) => this == other;
        public override bool Equals(object obj) => obj is Vec2 v && this == v;
        public override int GetHashCode() => unchecked(X * 397 ^ Z);
        public override string ToString() => $"({X},{Z})";
    }

    public static class IntMath
    {
        public static long Sqrt(long n)
        {
            if (n <= 0) return 0;
            long x = (long)Math.Sqrt(n);
            // Correct any floating-point rounding so the result is exact everywhere.
            while (x * x > n) x--;
            while ((x + 1) * (x + 1) <= n) x++;
            return x;
        }

        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
