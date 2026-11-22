using System;
using System.Collections.Generic;
using Games.MainModule.Configs.SummonMagic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Utility;
using UnityEngine;

namespace Games.MainModule.Scripts.Model
{
    public class SummonerModel : XFGameFramework.Model
    {
        public int Mana;
        public int MaxMana = 1000;
        public SummonMagicSO SummonMagicConfig;
        internal SummonDataSo PendingSummon;
        internal IReadOnlyList<SummonDataSo> PendingChoices = Array.Empty<SummonDataSo>();
        internal SummonMagicType PendingMagicType;
        internal int PendingManaCost;
        internal bool HasPendingContract => PendingSummon != null || PendingChoices.Count > 0;
        internal bool HasRandomChoiceThisTurn = true;
        internal int LastPlayerTurn;

        internal void ResetForNewBattle()
        {
            ClearPendingSummon();
            LastPlayerTurn = 0;
            HasRandomChoiceThisTurn = true;
            MaxMana = SummonMagicConfig.MaxMana;
            Mana = Mathf.Clamp(SummonMagicConfig.StartingMana, 0, MaxMana);
        }

        internal void ClearPendingSummon()
        {
            PendingSummon = null;
            PendingChoices = Array.Empty<SummonDataSo>();
            PendingMagicType = default;
            PendingManaCost = 0;
        }
    }
}