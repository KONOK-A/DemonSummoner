using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using XFABManager;
using XFFSM;

namespace Games.MainModule.Scripts.Controller
{
    public class GameController : XFGameFramework.Controller
    {
        public bool IsFirst => GetGameModel().isFirst;
        private GameModel GetGameModel()
        {
            GameModel model = Module.GetModel<GameModel>();
            if (model == null)
            {
                model = new GameModel();
                Module.AddModel(model);
            }

            return model;
        }

        public void TransitionToState(GameState gameState)
        {
            if (GetGameModel().gameState == gameState)
                return;

            Module.LoadController<SceneController>().PlayTransition(() => SetGameState(gameState));
        }

        public void SetGameState(GameState gameState)
        {
            GetGameModel().gameState = gameState;
            //装态机的状态修改,并负责启动状态机
            var fsm = FSMController.GetFSM("GameState");
            if (fsm == null)
            {
                RuntimeFSMController RunFsm =
                    AssetBundleManager.LoadAsset<RuntimeFSMController>(Module.ProjectName, "GameState");
                fsm = FSMController.StartupFSM("GameState", RunFsm, Module);
            }

            fsm.SetInt("State", (int)gameState);
        }

        public void SetFirstState(bool b)
        {
            GetGameModel().isFirst = b;
        }


    }

}
