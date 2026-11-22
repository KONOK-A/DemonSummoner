using Games.MainModule.Scripts.Utility;
using UnityEngine;

namespace Games.MainModule.Configs.Summons
{
    [CreateAssetMenu(menuName = "Summon/SummonDataSO", fileName = "SummonData_")]
    public class SummonDataSo : ScriptableObject
    {
        public string prefabName;
        public string summonName;
        [Tooltip("战斗职责，供勇者选敌使用")] public SummonType summonAttackType;
        [Tooltip("主动技能的伤害属性")] public DamageType skillDamageType;
        [Tooltip("主动技能的命中方式")] public HitDelivery skillDelivery;
        public OccupationType occupationType;
        public int maxHp;
        public int magicPoint;
        public float skillValue;
        [SerializeField, Min(0f), Tooltip("爆破法师左右相邻格的基础法术伤害。")]
        private float blastSplashDamage;
        [SerializeField, Range(0f, 1f), Tooltip("射手有相邻射手时的闪避率。两侧各有一名射手也只取一份。")]
        private float archerPairDodge;
        [Min(0f), Tooltip("骷髅武士贴身连斩每段相对单斩的伤害倍率。")]
        public float closeStrikeMultiplier = 0.6f;
        public int attackRadius;
        public bool isAuto;

        public float BlastSplashDamage => blastSplashDamage;
        public float ArcherPairDodge => Mathf.Clamp01(archerPairDodge);
        public float CloseStrikeDamage => skillValue * closeStrikeMultiplier;
        public AttackType SkillAttackType => (skillDamageType, skillDelivery) switch
        {
            (DamageType.Physical, HitDelivery.Contact) => AttackType.PhysicalContact,
            (DamageType.Physical, HitDelivery.Projectile) => AttackType.PhysicalProjectile,
            (DamageType.Magic, HitDelivery.Contact) => AttackType.MagicContact,
            (DamageType.Magic, HitDelivery.Projectile) => AttackType.MagicProjectile,
            _ => throw new System.InvalidOperationException($"Invalid skill attack type for {summonName}.")
        };
    }
}
