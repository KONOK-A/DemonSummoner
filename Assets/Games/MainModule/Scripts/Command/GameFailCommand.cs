using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using XFGameFramework;

namespace Games.MainModule.Scripts.Command
{
    public class GameFailCommand : ICommand
    {
        private Module _module;
        public GameFailCommand(Module module)
        {
            _module = module;
        }
        public void Execute()
        {
            _module.LoadController<FightController>().GetFightModel().BattleEnd = true;
            _module.LoadController<FightController>().GetFightModel().IsVictory = false;
            _module.LoadController<GameController>().SetGameState(GameState.GameEnd);

        }
    }
}