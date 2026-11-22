using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using TMPro;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.UI.FailPanel
{
    public class FailPanel : Panel
    {
        [SerializeField] private TextMeshProUGUI summary;

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            var fight = Module.LoadController<FightController>().GetFightModel();
            summary.text = string.IsNullOrEmpty(fight.InnerHallEntrant)
                ? "勇者小队抵达内殿。"
                : $"第 {fight.BoutCount} 回合，{fight.InnerHallEntrant}抵达内殿。";
        }

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
