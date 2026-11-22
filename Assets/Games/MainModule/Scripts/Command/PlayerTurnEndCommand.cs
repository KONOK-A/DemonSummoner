using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using XFGameFramework;

namespace Games.MainModule.Scripts.Command
{
    public readonly struct PlayerTurnEndCommand : ICommand
    {
        private readonly Module _module;
        public PlayerTurnEndCommand(Module module)
        {
            _module = module;
        }
        public void Execute()
        {
            var controller = _module.LoadController<FightController>();
            if (!controller.CanIssuePlayerCommand)
                return;

            var fight = controller.GetFightModel();
            fight.IsPlayerCommandTurn = false;
            fight.RepositionAvailable = false;
            //玩家回合结束事件
            EventManager.TriggerEvent(EventConst.PlayerTurnEnd);
            //所有召唤物不能攻击了
            _module.LoadController<SummonsController>().AllSummonCanNotAttack();
            //地图法阵运行生成召唤物
            _module.LoadController<MapInfoController>().MagicCircleLaunch();
            //清除地图法阵
            _module.LoadController<MagicCircleController>().ClearAllMagicCircles();
            //切换到敌方回合
            fight.IsEnemyTurn = true;
        }
    }
}
