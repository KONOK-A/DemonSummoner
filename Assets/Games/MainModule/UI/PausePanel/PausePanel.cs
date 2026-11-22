using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using XFGameFramework;
using BattleGuide = Games.MainModule.UI.StartDescriptionPanel.StartDescriptionPanel;

namespace Games.MainModule.UI.PausePanel
{
    public class PausePanel : Panel
    {
        private float previousTimeScale;

        protected override void OnEnable()
        {
            base.OnEnable();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Time.timeScale = previousTimeScale;
        }
        public void OnBtnBackClick()
        {
            Close();
        }

        public void OnBtnMenuClick()
        {
            Module.LoadController<GameController>().TransitionToState(GameState.Start);
        }

        public void OnBtnSettingClick()
        {
            Module.LoadPanel<SettingPanel.SettingPanel>();
        }

        public void OnBtnMenuBookClick()
        {
            var args = LoadViewArgs.New();
            args.SetBool(BattleGuide.FromPauseMenu, true);
            Module.LoadPanel<BattleGuide>(UIType.UI, null, args);
        }
    }
}
