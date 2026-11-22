using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.UI.MainMenuPanel;
using UnityEngine;
using XFFSM;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.FSM.States
{
    public class GameStart : FSMState
    {
        private MainMenuPanel _mainMenuPanel;
        private Module _mainModule;
        public override void OnEnter()
        {
            base.OnEnter();
            if (userData is XFGameFramework.Module mainModule)
            {
                _mainModule = mainModule;
                _mainModule.LoadController<SceneController>().LoadMenuScene(ShowMainMenu);
            }
            else
            {
                throw new System.Exception("参数传递异常");
            }
           
        }

        private void ShowMainMenu()
        {
            _mainModule.LoadController<FightController>().ClearBattle();
            _mainMenuPanel = _mainModule.LoadPanel<MainMenuPanel>();
            _mainModule.LoadController<AudioController>().PlayBGM(AudioCueNames.MenuBgm);
            Cursor.visible = true;
        }

        public override void OnExit()
        {
            base.OnExit();
            if (_mainMenuPanel != null)
            {
                //关闭起始面板
                _mainMenuPanel.Close();
                _mainMenuPanel = null;
            }
        }
    }
}
