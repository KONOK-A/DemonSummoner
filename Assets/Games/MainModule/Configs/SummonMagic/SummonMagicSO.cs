using System;
using System.Collections.Generic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Utility;
using UnityEngine;

namespace Games.MainModule.Configs.SummonMagic
{
    [CreateAssetMenu(menuName = "Summon/SummonMagicSO", fileName = "SummonMagicConfig")]
    public class SummonMagicSO : ScriptableObject
    {
        [Min(0)] public int StartingMana = 700;
        [Min(0)] public int MaxMana = 1000;
        [Min(2)] public int LastEarlyIncomeTurn = 10;
        [Min(0)] public int EarlyTurnIncome = 60;
        [Min(0)] public int LateTurnIncome = 25;
        public List<SummonMagic> SummonMagicList;

        public int GetIncomeForPlayerTurn(int turn)
        {
            if (turn <= 1)
                return 0;
            return turn <= LastEarlyIncomeTurn ? EarlyTurnIncome : LateTurnIncome;
        }
    }

    [Serializable]
    public class SummonMagic
    {
        public SummonMagicType summonMagicType;
        [Min(0)] public int ManaCost;
        [Tooltip("等权奖池按此职业筛选，None 包含全图鉴。")]
        public OccupationType UniformOccupation;
        public List<SummonWeight> Weights = new();
    }

    [Serializable]
    public struct SummonWeight
    {
        [Tooltip("留空时，此项权重均分给本页未点名的其余魔物。")]
        public SummonDataSo Summon;
        [Min(0)] public int Weight;
    }
}
