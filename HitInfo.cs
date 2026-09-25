namespace FightingGame
{
    // A snapshot of one single hit, raised by Character.OnHit so the UI can
    // react per-hit (flash, floating damage numbers, combo pacing) even when
    // a special attack lands multiple hits in one method call (e.g. Ninja's
    // Shadow Strike).
    public readonly struct HitInfo
    {
        public int RawDamage { get; }
        public int ActualDamage { get; }
        public bool Blocked { get; }
        public int HpAfter { get; }
        public int MaxHp { get; }

        public HitInfo(int rawDamage, int actualDamage, bool blocked, int hpAfter, int maxHp)
        {
            RawDamage = rawDamage;
            ActualDamage = actualDamage;
            Blocked = blocked;
            HpAfter = hpAfter;
            MaxHp = maxHp;
        }
    }
}
