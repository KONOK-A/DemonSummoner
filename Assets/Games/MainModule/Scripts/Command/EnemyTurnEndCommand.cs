using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using XFGameFramework;

namespace Games.MainModule.Scripts.Command
{
    public struct EnemyTurnEndCommand : ICommand
    {
        private readonly Module _module;
        public EnemyTurnEndCommand(Module module)
        {
            _module = module;
        }
        public void Execute()
        {
            _module.LoadController<FightController>().GetFightModel().IsEnemyTurn = false;
            EventManager.TriggerEvent(EventConst.EnemyTurnEnd);
            _module.LoadController<FightController>().GetFightModel().IsPlayerTurn = true;
        }
    }
}