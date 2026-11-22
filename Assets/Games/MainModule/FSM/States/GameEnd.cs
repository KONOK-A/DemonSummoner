using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.UI.FailPanel;
using Games.MainModule.UI.WinPanel;
using UnityEngine;
using XFFSM;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.FSM.States
{
    public class GameEnd : FSMState
    {
        private Module _mainModule;
        private WinPanel _winPanel;
        private FailPanel _failPanel;
        public override void OnEnter()
        {
            base.OnEnter();
            if (userData is Module mainModule)
            {
                _mainModule = mainModule;
            }
            else
            {
                throw new System.Exception("参数传递异常");
            }
            ShowResultPanel();
            Cursor.visible = true;
        }
        private void ShowResultPanel()
        {
            if (_mainModule.LoadController<FightController>().GetFightModel().IsVictory)
            {
                _winPanel = _mainModule.LoadPanel<WinPanel>();
                _mainModule.LoadController<AudioController>().PlaySound(AudioCueNames.WinSound);
            }
            else
            {
                _failPanel = _mainModule.LoadPanel<FailPanel>();
                _mainModule.LoadController<AudioController>().PlaySound(AudioCueNames.FailSound);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (_winPanel != null)
            {
                _winPanel.Close();
                _winPanel = null;
            }
            if (_failPanel != null)
            {
                _failPanel.Close();
                _failPanel = null;
            }
            Debug.Log("GameEnd OnExit");
        }
    }
}
