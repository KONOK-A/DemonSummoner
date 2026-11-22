using Games.MainModule.Scripts.Command;
using Games.MainModule.Scripts.Model;
using UnityEngine;

namespace Games.MainModule.Scripts.Controller
{
    public class FightController : XFGameFramework.Controller
    {
        public bool CanInspectBattle => Module.GetModel<FightModel>() is { BattleEnd: false } && Time.timeScale > 0f;

        public bool CanIssuePlayerCommand => Time.timeScale > 0f &&
            Module.GetModel<FightModel>() is { IsPlayerCommandTurn: true, IsResolvingPlayerSkill: false };

        public FightModel GetFightModel()
        {
            FightModel model = Module.GetModel<FightModel>();
            if (model != null) return model;
            model = new FightModel();
            Module.AddModel(model);
            return model;
        }

        internal void SetPendingPlayerSkill(System.Action cancel)
        {
            GetFightModel().CancelPendingPlayerSkill = cancel;
        }

        internal void CancelPendingPlayerSkill() => Module.GetModel<FightModel>()?.CancelPendingPlayerSkill?.Invoke();

        public void PlayerTurnBegin()
        {
            new PlayerTurnBeginCommand(Module).Execute();
        }

        public void ClearBattle()
        {
            if (Module.GetModel<FightModel>() is not { } fight)
                return;

            fight.BattleEnd = true;
            fight.IsPlayerCommandTurn = false;
            fight.RepositionAvailable = false;
            CancelPendingPlayerSkill();
            Module.LoadController<SummonerController>().ClearPreparation();
            fight.IsPlayerTurn = false;
            fight.IsEnemyTurn = false;
            fight.IsVictory = false;
            fight.BoutCount = 0;
            Module.LoadController<AdventurerController>().NewGame();
            Module.LoadController<SummonsController>().NewGame();
            Module.LoadController<MagicCircleController>().ClearAllMagicCircles();
            Module.LoadController<MapInfoController>().ClearBattleMap();
        }
        public void EnemyTurnBegin()
        {
            new EnemyTurnBeginCommand(Module).Execute(); 
        }
        public void EnemyTurnEnd()
        {
            new EnemyTurnEndCommand(Module).Execute();
        }
    }
}
