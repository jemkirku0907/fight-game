using System;

namespace FightingGame
{
    public abstract class Character
    {
        // Private fields — no outside code can touch these directly.
        private readonly string name;
        private readonly int maxHp;
        private int hp;
        private readonly int attackPower;
        private readonly int defensePower;
        private bool isDefending;

        // Public getters — the only way other classes can read the data.
        public string GetName() => name;
        public int GetHP() => hp;
        public int GetMaxHP() => maxHp;
        public int GetAttackPower() => attackPower;
        public int GetDefensePower() => defensePower;
        public bool IsAlive() => hp > 0;
        public bool IsDefending() => isDefending;

        // Fired whenever this character has something to say (attack text,
        // damage taken, etc). The UI layer (console or WinForms) subscribes
        // to this instead of Character writing directly to Console.
        public event Action<string>? OnLog;
        protected void Log(string message) => OnLog?.Invoke(message);

        // Fired once per individual hit this character takes — even a
        // multi-hit special attack raises this once per hit, in order, so
        // the UI can animate each impact separately instead of guessing.
        public event Action<HitInfo>? OnHit;

        protected Character(string name, int maxHp, int attackPower, int defensePower)
        {
            this.name = name;
            this.maxHp = maxHp;
            this.hp = maxHp;
            this.attackPower = attackPower;
            this.defensePower = defensePower;
            this.isDefending = false;
        }

        // 1. Basic Attack - same for every character, deals attackPower damage.
        public virtual void BasicAttack(Character target)
        {
            Log($"{name} uses a Basic Attack!");
            target.TakeDamage(attackPower);
        }

        // 2. Special Attack - each subclass implements its own version.
        public abstract void SpecialAttack(Character target);

        // Display name for the special move, shown on the arcade-style
        // intro banner right before it lands (e.g. "SHADOW STRIKE").
        public abstract string GetSpecialName();

        // 3. Defend - reduces the damage of the next incoming hit.
        public virtual void Defend()
        {
            isDefending = true;
            Log($"{name} takes a defensive stance!");
        }

        // 4. Display Status - shows current attributes.
        public void DisplayStatus()
        {
            Log($"--- {name} ---");
            Log($"HP: {hp}/{maxHp}");
            Log($"Attack Power: {attackPower}");
            Log($"Defense Power: {defensePower}");
            Log($"Defending: {(isDefending ? "Yes" : "No")}");
        }

        // Applies damage, reduced by defensePower if the character defended this turn.
        public void TakeDamage(int rawDamage)
        {
            int actualDamage = rawDamage;
            bool blocked = isDefending;

            if (isDefending)
            {
                actualDamage = Math.Max(0, rawDamage - defensePower);
                Log($"{name} blocks! Damage reduced from {rawDamage} to {actualDamage}.");
                isDefending = false; // guard only lasts for the next hit
            }

            hp = Math.Max(0, hp - actualDamage);
            Log($"{name} takes {actualDamage} damage. ({hp}/{maxHp} HP left)");
            OnHit?.Invoke(new HitInfo(rawDamage, actualDamage, blocked, hp, maxHp));
        }
    }
}
