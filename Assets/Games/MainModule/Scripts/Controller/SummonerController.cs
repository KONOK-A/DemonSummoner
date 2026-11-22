using System;
using System.Collections.Generic;
using System.Linq;
using Games.MainModule.Configs.SummonMagic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.SpecificCallPanel;
using Games.MainModule.UI.ToastPanel;
using UnityEngine;
using XFABManager;
using XFGameFramework;

namespace Games.MainModule.Scripts.Controller
{
    public class SummonerController : XFGameFramework.Controller
    {
        private SpecificCallPanel _wishTemplate;
        private SpecificCallPanel _wishPanel;
        private int _wishRequest;

        public int CurrentMana => GetSummonerModel().Mana;
        public int MaxMana => GetSummonerModel().MaxMana;
        public SummonDataSo PendingSummon => Module.GetModel<SummonerModel>()?.PendingSummon;
        public IReadOnlyList<SummonDataSo> PendingChoices =>
            Module.GetModel<SummonerModel>()?.PendingChoices ?? Array.Empty<SummonDataSo>();
        public bool HasPendingContract => Module.GetModel<SummonerModel>()?.HasPendingContract == true;
        // 当前玩家回合尚未成功购买大随机。上一回合保留的契约不占本轮资格。
        public bool HasRandomChoiceThisTurn => Module.GetModel<SummonerModel>()?.HasRandomChoiceThisTurn ?? true;
        public SummonMagicType PendingMagicType => Module.GetModel<SummonerModel>()?.PendingMagicType ?? default;
        public int PendingManaCost => Module.GetModel<SummonerModel>()?.PendingManaCost ?? 0;
        public bool HasAvailableSummonCell =>
            Module.GetModel<MapModel>()?.TileDetailsDict.Values.Any(IsAvailableCell) == true;

        private bool CanStartPreparation => Module.LoadController<FightController>().CanIssuePlayerCommand &&
            !HasPendingContract && _wishPanel == null && HasAvailableSummonCell;

        public int GetManaCost(SummonMagicType magicType) =>
            GetSummonerModel().SummonMagicConfig.SummonMagicList
                .First(magic => magic.summonMagicType == magicType).ManaCost;

        public SummonerModel GetSummonerModel()
        {
            SummonerModel model = Module.GetModel<SummonerModel>();
            if (model == null)
            {
                model = new SummonerModel();
                model.SummonMagicConfig =
                    AssetBundleManager.LoadAsset<SummonMagicSO>(Module.ProjectName, "SummonMagicConfig");
                model.ResetForNewBattle();
                Module.AddModel(model);
            }
            return model;
        }

        public void ResetForNewBattle()
        {
            ClearPreparation();
            var model = GetSummonerModel();
            model.ResetForNewBattle();
            EventManager.TriggerEvent(EventConst.ManaValueChange, model.Mana);
        }

        public int GetIncomeForPlayerTurn(int turn) =>
            GetSummonerModel().SummonMagicConfig.GetIncomeForPlayerTurn(turn);

        internal void BeginPlayerTurn(int turn)
        {
            var model = GetSummonerModel();
            if (turn <= model.LastPlayerTurn)
                return;

            model.LastPlayerTurn = turn;
            model.HasRandomChoiceThisTurn = true;
            int income = GetIncomeForPlayerTurn(turn);
            if (income > 0)
                ChangeManaValue(1, income);
        }

        public bool CanPrepareSummon(SummonMagicType type)
        {
            if (!CanStartPreparation)
                return false;

            var model = GetSummonerModel();
            if (type != SummonMagicType.Orientation)
                return model.SummonMagicConfig.SummonMagicList.Exists(
                    spell => spell.summonMagicType == type && spell.ManaCost <= model.Mana);

            // 祈愿只出售选择器列出的魔物，按各自的价格判断能否购买。
            if (_wishTemplate == null)
            {
                var prefab = AssetBundleManager.LoadAsset<GameObject>(Module.ProjectName, nameof(SpecificCallPanel));
                if (prefab == null || !prefab.TryGetComponent(out _wishTemplate))
                    return false;
            }
            return _wishTemplate.SelectSummonDataList.Exists(
                choice => choice.SummonData != null && choice.SummonData.magicPoint <= model.Mana);
        }

        public bool TryPrepareSummon(SummonMagicType type)
        {
            if (!CanPrepareSummon(type))
                return false;

            if (type == SummonMagicType.Orientation)
            {
                _wishPanel = Module.LoadPanel<SpecificCallPanel>(nameof(SpecificCallPanel));
                if (_wishPanel == null)
                    return false;

                int request = ++_wishRequest;
                _wishPanel.CallBackEvent = choice => CompleteWish(request, choice);
                EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
                return true;
            }

            var bank = Module.LoadController<SummonBankController>();
            if (type == SummonMagicType.Random && HasRandomChoiceThisTurn)
            {
                var choices = bank.DrawRandomPair();
                if (choices.Count < 2)
                    return false;

                PrepareSummon(type, null, GetManaCost(type), choices);
                return true;
            }

            var summon = bank.DrawSummon(type);
            if (summon == null)
                return false;

            PrepareSummon(type, summon, GetManaCost(type));
            return true;
        }

        private void CompleteWish(int request, SummonDataSo choice)
        {
            if (request != _wishRequest || _wishPanel == null)
                return;

            var panel = _wishPanel;
            _wishPanel = null;
            if (choice != null && CanStartPreparation && panel.SelectSummonDataList.Exists(row => row.SummonData == choice))
            {
                if (choice.magicPoint <= CurrentMana)
                {
                    PrepareSummon(SummonMagicType.Orientation, choice, choice.magicPoint);
                    return;
                }
                Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "当前魔力值不足以召唤这只魔物", 1.2f);
            }
            EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
        }

        private void PrepareSummon(SummonMagicType type, SummonDataSo summon, int cost,
            IReadOnlyList<SummonDataSo> choices = null)
        {
            var model = GetSummonerModel();
            model.PendingMagicType = type;
            model.PendingSummon = summon;
            model.PendingManaCost = cost;
            model.PendingChoices = choices ?? Array.Empty<SummonDataSo>();
            if (type == SummonMagicType.Random)
                model.HasRandomChoiceThisTurn = false;
            ChangeManaValue(-1, cost);
            EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
        }

        public bool TryChoosePreparedSummon(SummonDataSo choice)
        {
            if (choice == null || !Module.LoadController<FightController>().CanIssuePlayerCommand)
                return false;
            var model = GetSummonerModel();
            if (!model.PendingChoices.Contains(choice))
                return false;

            model.PendingSummon = choice;
            model.PendingChoices = Array.Empty<SummonDataSo>();
            EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
            return true;
        }

        private static bool IsAvailableCell(TileDetails tile) =>
            tile is { CanSummon: true, IsHaveCharacter: false, AboutToSummon: false };

        public bool CanPlacePreparedSummon(Vector2Int cell) =>
            PendingSummon != null && Module.LoadController<FightController>().CanIssuePlayerCommand &&
            IsAvailableCell(Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(cell));

        public bool TryPlacePreparedSummon(Vector2Int cell)
        {
            if (!CanPlacePreparedSummon(cell))
                return false;

            var model = GetSummonerModel();
            if (!Module.LoadController<MagicCircleController>().TryGenerateMagicCircle(model.PendingMagicType, cell))
                return false;

            Module.LoadController<MapInfoController>().SetSummonMagicCircle(model.PendingSummon, cell, model.PendingManaCost);
            model.ClearPendingSummon();

            EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.SummonSound);
            return true;
        }

        internal void ClearPreparation()
        {
            var panel = _wishPanel;
            _wishPanel = null;
            ++_wishRequest;
            var model = Module.GetModel<SummonerModel>();
            bool changed = panel != null || model?.HasPendingContract == true;
            model?.ClearPendingSummon();
            if (panel != null)
                panel.Close();
            if (changed)
                EventManager.TriggerEvent(EventConst.SummonPreparationChanged);
        }

        public void ChangeManaValue(int addOrSub, int value)
        {
            var model = GetSummonerModel();
            model.Mana = Mathf.Clamp(model.Mana + addOrSub * value, 0, model.MaxMana);
            EventManager.TriggerEvent(EventConst.ManaValueChange, model.Mana);
        }
    }
}
