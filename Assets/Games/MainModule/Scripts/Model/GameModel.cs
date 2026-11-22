using System;
using Games.MainModule.Scripts.Utility;

namespace Games.MainModule.Scripts.Model
{
    [Serializable]
    public class GameModel : XFGameFramework.Model
    {
        /// <summary>
        /// 当前游戏游玩的关卡ID
        /// </summary>
        public int currentPlayLevelId;
        public bool isFirst = true;
        public GameState gameState = GameState.Start;
    }
}
