using System;

namespace FightingGame
{
    public class Mage : Character
    {
        private const int SpecialMultiplier = 3;

        public Mage(string name) : base(name, maxHp: 90, attackPower: 11, defensePower: 4)
        {
        }

        public override void BasicAttack(Character target)
        {
            Log($"{GetName()} casts a scorching FIREBALL!");
            target.TakeDamage(GetAttackPower());
        }

        public override void SpecialAttack(Character target)
        {
            int damage = GetAttackPower() * SpecialMultiplier + 5;
            Log($"{GetName()} conjures the apocalyptic INFERNO METEOR!");
            target.TakeDamage(damage);
        }

        public override string GetSpecialName() => "INFERNO METEOR";
    }
}
