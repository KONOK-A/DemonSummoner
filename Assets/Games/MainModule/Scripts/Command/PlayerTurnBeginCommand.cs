using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using XFGameFramework;

namespace Games.MainModule.Scripts.Command
{
    public struct PlayerTurnBeginCommand : ICommand
    {
        private readonly Module _module;

        public PlayerTurnBeginCommand(Module module)
        {
            _module = module;
        }
        public void Execute()
        {
            var fight = _module.LoadController<FightController>().GetFightModel();
            if (fight.BattleEnd || !fight.IsPlayerTurn || fight.IsPlayerCommandTurn)
                return;

            fight.IsPlayerTurn = false;
            fight.IsPlayerCommandTurn = true;
            fight.RepositionAvailable = true;
            _module.LoadController<SummonerController>().BeginPlayerTurn(fight.BoutCount);
            _module.LoadController<SummonsController>().ClearAllSummonBuff();
            _module.LoadController<SummonsController>().RecordAllSummonBuff();
            _module.LoadController<SummonsController>().AllSummonCanAttack();
            EventManager.TriggerEvent(EventConst.PlayerTurnBegin);
        }
    }
}