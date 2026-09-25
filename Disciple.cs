using System;

namespace FightingGame
{
    public class Disciple : Character
    {
        private const int SummonHits = 3;

        public Disciple(string name) : base(name, maxHp: 95, attackPower: 12, defensePower: 6)
        {
        }

        public override void BasicAttack(Character target)
        {
            Log($"{GetName()} fires a mid-range DEMONIC SHADOW ORB!");
            target.TakeDamage(GetAttackPower());
        }

        public override void SpecialAttack(Character target)
        {
            Log($"{GetName()} summons a DEMONIC SHADOW SLAM ({SummonHits} heavy blows)!");
            for (int i = 0; i < SummonHits && target.IsAlive(); i++)
            {
                target.TakeDamage(GetAttackPower() + 3);
            }
        }

        public override string GetSpecialName() => "DEMONIC SHADOW SLAM";
    }
}
