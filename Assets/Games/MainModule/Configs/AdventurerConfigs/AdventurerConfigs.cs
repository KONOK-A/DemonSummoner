using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.MainModule.Configs.AdventurerConfigs
{
    public enum PastorSupportAction
    {
        None,
        Heal,
        Shield,
        Blessing
    }

    [Serializable]
    public struct PastorSupportWeights
    {
        [Min(0)] public int Heal;
        [Min(0)] public int Shield;
        [Min(0)] public int Blessing;

        public PastorSupportWeights(int heal, int shield, int blessing)
        {
            Heal = heal;
            Shield = shield;
            Blessing = blessing;
        }
    }

    [CreateAssetMenu(menuName = "Resources/Adventurer")]

    public class AdventurerConfigs : ScriptableObject
    {
        public float life;
        public float currentLife;
        public float Skill1Value;
        public float Skill2Value;
        public float Skill3Value;
        [Tooltip("法师爆焱在相邻格造成的基础法术伤害")] public float ExplosionSplashDamage;
        [Min(0), Tooltip("法师施放法幕时，沿走廊能保护到的左右格数。")] public int CasterWardRadius = 1;

        [Header("牧师")]
        [Tooltip("队友最低生命比例高于 80% 时的技能权重")] public PastorSupportWeights PastorHighHealthWeights = new(0, 35, 65);
        [Tooltip("队友最低生命比例在 30% 至 80% 时的技能权重")] public PastorSupportWeights PastorMidHealthWeights = new(35, 30, 35);
        [Tooltip("队友最低生命比例低于 30% 时的技能权重")] public PastorSupportWeights PastorLowHealthWeights = new(60, 35, 5);
        [Min(0), Tooltip("复仇可选的前方最大格数")] public int PastorRevengeRange = 7;
        [Min(0), Tooltip("每发复仇的基础法术伤害")] public float PastorRevengeDamage = 35f;

        public PastorSupportAction DrawPastorSupport(float lowestHealthRatio)
        {
            var weights = lowestHealthRatio > 0.8f ? PastorHighHealthWeights :
                lowestHealthRatio < 0.3f ? PastorLowHealthWeights : PastorMidHealthWeights;
            int heal = lowestHealthRatio < 1f ? Mathf.Max(0, weights.Heal) : 0;
            int shield = Mathf.Max(0, weights.Shield);
            int total = heal + shield + Mathf.Max(0, weights.Blessing);
            if (total == 0)
                return PastorSupportAction.None;

            int roll = Random.Range(0, total);
            if (roll < heal)
                return PastorSupportAction.Heal;

            return roll < heal + shield ? PastorSupportAction.Shield : PastorSupportAction.Blessing;
        }
    }
}
