using System;

namespace FightingGame
{
    // High-flying basketball player with mid-range basketball projectile throws
    // and an explosive "MAMA DUNK" aerial posterizer ultimate.
    public class Baller : Character
    {
        private const int DunkHits = 3;

        public Baller(string name) : base(name, maxHp: 100, attackPower: 13, defensePower: 5)
        {
        }

        public override void BasicAttack(Character target)
        {
            Log($"{GetName()} shoots a fiery crossover BASKETBALL PASS!");
            target.TakeDamage(GetAttackPower());
        }

        public override void SpecialAttack(Character target)
        {
            Log($"{GetName()} screams 'MAMAAAAA!' and executes a vicious POSTER SLAM DUNK ({DunkHits} rim-rocking hits)!");
            for (int i = 0; i < DunkHits && target.IsAlive(); i++)
            {
                int dmg = i switch
                {
                    0 => GetAttackPower() + 2,      // Aerial takeoff & backboard shatter
                    1 => GetAttackPower() + 5,      // Rim-hanging posterizer
                    _ => GetAttackPower() * 2 + 6   // Crushing meteorite ground slam
                };
                target.TakeDamage(dmg);
            }
        }

        public override string GetSpecialName() => "MAMA DUNK";
    }
}
