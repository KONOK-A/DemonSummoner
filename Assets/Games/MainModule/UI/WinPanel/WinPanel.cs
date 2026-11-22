using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using XFGameFramework;

namespace Games.MainModule.UI.WinPanel
{
    public class WinPanel : Panel
    {
        public void OnBtnBackMenuClick()
        {
            Module.LoadController<GameController>().TransitionToState(GameState.Start);
        }

        public void OnBtnRestartClick()
        {
            Module.LoadController<GameController>().TransitionToState(GameState.Game);
        }
    }
}
