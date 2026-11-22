namespace Games.MainModule.Scripts.Model
{
    public class FightModel : XFGameFramework.Model
    {
        public bool IsPlayerTurn = false;
        public bool IsEnemyTurn = false;
        private bool _isPlayerCommandTurn;
        public bool IsPlayerCommandTurn
        {
            get => _isPlayerCommandTurn && !BattleEnd;
            internal set => _isPlayerCommandTurn = value;
        }
        public bool BattleEnd = true;
        public bool IsVictory;
        internal System.Action CancelPendingPlayerSkill;
        public bool IsResolvingPlayerSkill => CancelPendingPlayerSkill != null;
        // 玩家实际操作期间，全队共用一次调位机会。
        public bool RepositionAvailable;
        public const float TurnDuration = 0.4f;
        public int BoutCount;
        public string InnerHallEntrant { get; internal set; }
    }
    
}
