using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using XFGameFramework;

namespace Games.MainModule.Scripts.Command
{
    public struct EnemyTurnBeginCommand : ICommand
    {
        private readonly Module _module;
        public EnemyTurnBeginCommand(Module module)
        {
            _module = module;
        }
        public void Execute()
        {
            _module.LoadController<FightController>().GetFightModel().BoutCount++;
            EventManager.TriggerEvent(EventConst.EnemyTurnBegin);
            _module.LoadController<AdventurerController>().Move();
            _module.LoadController<FightController>().GetFightModel().IsEnemyTurn = false;
        }
    }
}