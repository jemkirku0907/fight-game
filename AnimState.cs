namespace FightingGame
{
    // The animation "state" a fighter can be in — used both by the
    // procedural stick-figure fallback and by real sprite sheets once you
    // drop art into the Sprites/ folder.
    public enum AnimState
    {
        Idle,
        Walk,
        Jump,
        Attack,
        Special,
        Block,
        Hit,
        KO
    }
}
