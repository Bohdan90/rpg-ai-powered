namespace RPG.Core
{
    // xorshift32 (13,17,5). Fixed unsigned arithmetic; algorithm is part of replay identity.
    public struct CombatRandom
    {
        public uint State { get; private set; }
        public CombatRandom(uint seed) { State = seed == 0 ? 0x6D2B79F5u : seed; }
        public uint NextUInt()
        {
            uint x = State;
            // Also make default(CombatRandom) safe and deterministic.
            if (x == 0) x = 0x6D2B79F5u;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }
        public int NextPercent()
        {
            // Reject the short tail of the 2^32 range to avoid modulo bias.
            uint value;
            do { value = NextUInt(); } while (value < 96u);
            return (int)(value % 100u); // Success iff roll < chance, with rolls in [0,99].
        }
    }
}
