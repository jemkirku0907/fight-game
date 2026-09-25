using System;

namespace FightingGame
{
    public class Warrior : Character
    {
        private const int SpecialMultiplier = 2;

        public Warrior(string name) : base(name, maxHp: 105, attackPower: 14, defensePower: 8)
        {
        }

        public override void SpecialAttack(Character target)
        {
            int damage = GetAttackPower() * SpecialMultiplier + 8;
            Log($"{GetName()} unleashes the legendary DRAGON BLADE CRASH!");
            target.TakeDamage(damage);
        }

        public override string GetSpecialName() => "DRAGON BLADE CRASH";
    }
}
