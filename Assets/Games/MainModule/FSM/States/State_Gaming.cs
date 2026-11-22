using System.Collections;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.CursorPanel;
using Games.MainModule.UI.GamePanel;
using Games.MainModule.UI.StartDescriptionPanel;
using UnityEngine;
using XFABManager;
using XFFSM;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.FSM.States
{
    public class State_Gaming : FSMState
    {
        private static readonly Vector2Int AdventurerStart = new(-6, -3);
        private Module _mainModule;
        private GamePanel _gamePanel;
        private CursorPanel _cursorPanel;
        private FightController _fightController;
        private float _timeCounter;
        private FightModel _fightModel;
        private bool _isFightBegin;
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
            //加载游戏场景
            _isFightBegin = false;
            _fightController = _mainModule.LoadController<FightController>();
            _fightModel = _fightController.GetFightModel();
            _mainModule.LoadController<SceneController>().LoadScene("Game", InitGameScene,
                () => _mainModule.LoadController<GameController>().SetGameState(GameState.Start), PrepareBattle);
        }

        private IEnumerator PrepareBattle()
        {
            // Let the loader integrate the large HUD and audio assets across frames.
            var project = _mainModule.ProjectName;
            var hud = AssetBundleManager.LoadAssetAsync<GameObject>(project, nameof(GamePanel));
            var music = AssetBundleManager.LoadAssetAsync<AudioClip>(project, AudioCueNames.GameBgm);
            yield return hud;
            yield return music;
            if (!string.IsNullOrEmpty(hud.error) || !string.IsNullOrEmpty(music.error))
                Debug.LogWarning($"Battle preload failed; normal loading will retry. HUD: {hud.error}; BGM: {music.error}");
        }

        private void InitGameScene()
        {
            _fightController.ClearBattle();
            _mainModule.LoadController<MapInfoController>().InitDataList(AdventurerStart, AdventurerController.PartySize);
            _mainModule.LoadController<SummonBankController>().InitDataList();
            FightStart();
            _mainModule.LoadController<AudioController>().PlayBGM(AudioCueNames.GameBgm);
            Cursor.visible = false;
            if (_mainModule.LoadController<GameController>().IsFirst)
            {
                _mainModule.LoadPanel<StartDescriptionPanel>("StartDescriptionPanel",UIType.UI,null);
            }
            _isFightBegin = true;
        }
        private void FightStart()
        {
            _timeCounter = 0;
            _mainModule.LoadController<SummonerController>().ResetForNewBattle();
            _fightModel.InnerHallEntrant = null;
            
            _mainModule.LoadController<AdventurerController>().Init(AdventurerStart);
            _gamePanel = _mainModule.LoadPanel<GamePanel>();
            _cursorPanel = _mainModule.LoadPanel<CursorPanel>("CursorImage");
            _fightModel.BattleEnd = false;
            _fightModel.IsEnemyTurn = true;
        }
        public override void OnUpdate()
        {
            base.OnUpdate();
            if(_isFightBegin == false) return;
            if(_fightModel.BattleEnd) return;
            
            if (_fightModel.IsEnemyTurn)
            {
                _timeCounter += Time.deltaTime;
                if (_timeCounter >= FightModel.TurnDuration)
                {
                    //敌人回合正式开始
                    _fightController.EnemyTurnBegin();
                    _timeCounter = 0f;
                }
            }

            if (_fightModel.IsPlayerTurn)
            {
                _timeCounter += Time.deltaTime;
                if (_timeCounter >= FightModel.TurnDuration)
                {
                    //玩家回合正式开始
                    _fightController.PlayerTurnBegin();
                    _timeCounter = 0f;
                }
            }
        }
        
        public override void OnExit()
        {
            base.OnExit();
            _isFightBegin = false;
            bool stopMovement = !_fightModel.BattleEnd;
            _fightModel.BattleEnd = true;
            _fightModel.IsPlayerCommandTurn = false;
            _fightModel.RepositionAvailable = false;
            _fightController.CancelPendingPlayerSkill();
            _mainModule.LoadController<SummonerController>().ClearPreparation();
            _mainModule.LoadController<AdventurerController>().StopEnemyTurn(stopMovement);
            _fightModel.IsPlayerTurn = false;
            _fightModel.IsEnemyTurn = false;
            if (_gamePanel != null)
            {
                _gamePanel.Close();
                _gamePanel = null;
            }
            if (_cursorPanel != null)
            {
                _cursorPanel.Close();
                _cursorPanel = null;
            }
        }
    }
}
