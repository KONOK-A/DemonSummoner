namespace Games.MainModule.Scripts.Utility
{
    public enum GameState
    {
        Start,
        Game,
        GameEnd
    }
    public enum AudioType
    {
        BGM,
        Sound
    }

    public enum GridType
    {
        CanSummon,NotCanSummon
    }

    public enum CharacterType
    {
        Adventurer,
        Monster
    }
    
    public enum SummonMagicType
    {
        Shieldguard,
        LongRange,
        Attacker,
        Skull,
        Wizard,
        Slime,
        Random,
        Orientation
    }
    public enum EffectType
    {
        PhysicDamageReduction,
        MagicDamageReduction,
        PhysicFragility,
        MagicFragility,
        AttackBuff,
        EvasionValue,
        ProtectiveWard
    }

    public enum DamageType
    {
        Physical,
        Magic
    }

    public enum HitDelivery
    {
        Contact,
        Projectile
    }

    public enum AttackType
    {
        MagicProjectile = 0,
        PhysicalContact = 1,
        PhysicalProjectile = 2,
        MagicContact = 3,
        // 旧调用将远程当作法术、近战当作物理。
        LongRange = MagicProjectile,
        CloseRange = PhysicalContact
    }

    public static class AttackTypeExtensions
    {
        public static DamageType GetDamageType(this AttackType attackType) => attackType switch
        {
            AttackType.PhysicalContact or AttackType.PhysicalProjectile => DamageType.Physical,
            AttackType.MagicContact or AttackType.MagicProjectile => DamageType.Magic,
            _ => throw new System.ArgumentOutOfRangeException(nameof(attackType), attackType, null)
        };

        public static bool IsContact(this AttackType attackType) =>
            attackType is AttackType.PhysicalContact or AttackType.MagicContact;
    }

    public enum SummonType
    {
        ShieldGuard,
        LongRange,
        Attacker,
    }

    
    public enum OccupationType
    {
        None,Skull,Wizard,Slime
    }

    public enum OperationType
    {
        None,ReleaseMagic,SummonControl,Reposition
    }
}
