namespace SnowballGame.Core
{
    /// <summary>Deterministic xorshift32 generator. Same seed, same match.</summary>
    public sealed class Rng
    {
        uint state;

        public Rng(int seed)
        {
            state = (uint)seed;
            if (state == 0) state = 0x9E3779B9u;
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>Uniform integer in [min, max] inclusive.</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            uint span = (uint)(max - min + 1);
            return min + (int)(NextUInt() % span);
        }

        public int State => (int)state;
    }
}
