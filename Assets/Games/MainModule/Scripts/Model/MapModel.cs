using System.Collections.Generic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Utility;
using UnityEngine;

namespace Games.MainModule.Scripts.Model
{
    public class MapModel : XFGameFramework.Model
    {
        public Dictionary<Vector2Int, TileDetails> TileDetailsDict = new();
    }

    public class TileDetails
    {
        public int GridX, GridY;
        public bool CanSummon;
        public SummonDataSo SummonData;
        public int? PaidManaCost;
        public bool AboutToSummon;
        public bool IsHaveCharacter;
        public CharacterType CharacterType;
    }
}
