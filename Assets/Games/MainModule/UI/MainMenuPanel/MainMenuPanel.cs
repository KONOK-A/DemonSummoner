using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.UI.MainMenuPanel
{
    public class MainMenuPanel : Panel
    {
        private GameController GameController => Module.LoadController<GameController>();
        #region UI事件监听

        public void OnBtnNewGameClick()
        {
            GameController.TransitionToState(GameState.Game);
        }

        public void OnBtnAboutUsClick()
        {
            Module.LoadPanel<AboutUsPanel.AboutUsPanel>();
        }

        public void OnBtnSettingClick()
        {
            Module.LoadPanel<SettingPanel.SettingPanel>();
        }

        public void OnBtnExitClick()
        {
            Application.Quit();
        }
        #endregion
    }
}
