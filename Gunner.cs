using System;

namespace FightingGame
{
    // Fast and hard-hitting with mid-range firearm projectiles and an ammo/reload system.
    public class Gunner : Character
    {
        private const float SpecialMultiplier = 2.8f;
        public const int DefaultMaxAmmo = 6;
        public int MaxAmmo { get; } = DefaultMaxAmmo;
        public int CurrentAmmo { get; private set; } = DefaultMaxAmmo;
        public bool IsReloading { get; private set; } = false;

        public Gunner(string name) : base(name, maxHp: 75, attackPower: 14, defensePower: 3)
        {
        }

        public bool CanShoot() => CurrentAmmo > 0 && !IsReloading;

        public bool ConsumeBullet()
        {
            if (CurrentAmmo > 0)
            {
                CurrentAmmo--;
                return true;
            }
            return false;
        }

        public void Reload()
        {
            CurrentAmmo = MaxAmmo;
            IsReloading = false;
            Log($"{GetName()} finished reloading! Ammo full ({MaxAmmo}/{MaxAmmo}).");
        }

        public void SetReloading(bool reloading)
        {
            IsReloading = reloading;
            if (reloading)
                Log($"{GetName()} is RELOADING magazine...");
        }

        public override void BasicAttack(Character target)
        {
            Log($"{GetName()} fires a mid-range bullet!");
            target.TakeDamage(GetAttackPower());
        }

        public override void SpecialAttack(Character target)
        {
            int damage = (int)(GetAttackPower() * SpecialMultiplier);
            Log($"{GetName()} unloads a BULLET STORM barrage!");
            target.TakeDamage(damage);
        }

        public override string GetSpecialName() => "Bullet Storm";
    }
}
