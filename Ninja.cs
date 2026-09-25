using System;

namespace FightingGame
{
    public class Ninja : Character
    {
        private const int SpecialHits = 4;

        public Ninja(string name) : base(name, maxHp: 100, attackPower: 15, defensePower: 5)
        {
        }

        public override void SpecialAttack(Character target)
        {
            Log($"{GetName()} performs SHADOW OMNI-STRIKE ({SpecialHits} phantom slashes)!");
            for (int i = 0; i < SpecialHits && target.IsAlive(); i++)
            {
                target.TakeDamage(GetAttackPower() / 2 + 2);
            }
        }

        public override string GetSpecialName() => "SHADOW OMNI-STRIKE";
    }
}
