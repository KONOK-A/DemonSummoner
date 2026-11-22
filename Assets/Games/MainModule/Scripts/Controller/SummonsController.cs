using System;
using System.Collections.Generic;
using Game;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Summons;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.Scripts.Controller
{
    public class SummonsController : XFGameFramework.Controller
    {
        public const int FlagAuraRange = 2;
        public const float FlagAuraBonus = 0.35f;

        private SummonsModel GetSummonsModel()
        {
            SummonsModel model = Module.GetModel<SummonsModel>();
            if (model == null)
            {
                model = new SummonsModel();
                Module.AddModel(model);
            }
            return model;
        }
        public SummonBase GenerateSummon(Vector2Int gridPos, SummonDataSo summonData) =>
            GenerateSummon(gridPos, summonData, null);

        public SummonBase GenerateSummon(Vector2Int gridPos, SummonDataSo summonData, int? paidManaCost)
        {
            if (summonData == null) return null;
            var summon = Module.LoadView<SummonBase>(summonData.prefabName, null, summonData, gridPos,
                paidManaCost ?? summonData.magicPoint);
            if (summon != null)
                GetSummonsModel().SummonsOnScene.Add(gridPos, summon);
            return summon;
        }
        public SummonBase GetSummon(Vector2Int gridPos)
        {
            return GetSummonsModel().SummonsOnScene.GetValueOrDefault(gridPos);
        }

        public bool HasRepositionTarget(SummonBase unit)
        {
            return unit && (CanReposition(unit, unit.GridPos + Vector2Int.left) ||
                            CanReposition(unit, unit.GridPos + Vector2Int.right));
        }

        public bool CanReposition(SummonBase unit, Vector2Int cell)
        {
            if (!unit || !unit.isActiveAndEnabled || unit.IsAttacked || unit.CurrentHp <= 0 || Time.timeScale <= 0f ||
                Module.GetModel<FightModel>() is not { BattleEnd: false, RepositionAvailable: true, IsResolvingPlayerSkill: false })
                return false;

            var origin = unit.GridPos;
            if (origin.y != cell.y || Mathf.Abs(origin.x - cell.x) != 1)
                return false;

            var summons = GetSummonsModel().SummonsOnScene;
            var map = Module.LoadController<MapInfoController>();
            return summons.TryGetValue(origin, out var occupant) && occupant == unit && !summons.ContainsKey(cell) &&
                   map.GetTileDetailsByGridPosition(origin) is { IsHaveCharacter: true, CharacterType: CharacterType.Monster } &&
                   map.GetTileDetailsByGridPosition(cell) is { CanSummon: true, IsHaveCharacter: false, AboutToSummon: false };
        }

        public bool TryReposition(SummonBase unit, Vector2Int cell)
        {
            if (!CanReposition(unit, cell))
                return false;

            var origin = unit.GridPos;
            var map = Module.LoadController<MapInfoController>();
            var source = map.GetTileDetailsByGridPosition(origin);
            var destination = map.GetTileDetailsByGridPosition(cell);
            var summons = GetSummonsModel().SummonsOnScene;
            summons.Remove(origin);
            summons.Add(cell, unit);
            source.CanSummon = true;
            source.IsHaveCharacter = false;
            source.SummonData = null;
            destination.CanSummon = false;
            destination.IsHaveCharacter = true;
            destination.CharacterType = CharacterType.Monster;
            destination.SummonData = null;

            Module.GetModel<FightModel>().RepositionAvailable = false;
            unit.MoveToGrid(cell);
            unit.IsAttacked = true;
            return true;
        }

        public SkullShield GetReadyGuard(Vector2Int target) => FindReadyGuard(GetSummon(target));

        private SkullShield FindReadyGuard(SummonBase target)
        {
            if (!target || !target.isActiveAndEnabled || target.CurrentHp <= 0)
                return null;

            var guard = GetSummon(target.GridPos + Vector2Int.left) as SkullShield;
            return guard && guard.isActiveAndEnabled && guard.CurrentHp > 0 && guard.GuardReady ? guard : null;
        }

        internal bool CanDefeatWithDirectHit(SummonBase target, AttackType attackType, float damage)
        {
            var receiver = FindReadyGuard(target) ?? target;
            return receiver.CurrentHp <= receiver.GetDamageOnHit(damage, attackType);
        }

        // 返回实际命中格，即使承伤者阵亡也能在正确位置播放命中特效。
        public Vector2Int DamageSummon(Vector2Int gridPos, AttackType attackType, float damage, bool areaAttack = false)
        {
            if (Module.GetModel<FightModel>() is { BattleEnd: true })
                return gridPos;
            var target = GetSummon(gridPos);
            if (!target || !target.isActiveAndEnabled || target.CurrentHp <= 0)
                return gridPos;

            var guard = areaAttack ? null : FindReadyGuard(target);
            if (guard)
            {
                guard.GuardReady = false;
                XFGameFramework.EventManager.TriggerEvent<SummonBase, SummonBase>(Games.MainModule.Scripts.Const.EventConst.GuardIntercepted, guard, target);
                SkirmishEffects.PlayIntercept(guard, target);
                target = guard;
            }

            var hitCell = target.GridPos;
            target.TakeDamage(damage, attackType);
            if (target.CurrentHp <= 0)
                RecycleSummon(hitCell, target);
            return hitCell;
        }
        /// <summary>
        /// 获得场上所有的敌人
        /// </summary>
        /// <returns>场上所有的敌人</returns>
        public Dictionary<Vector2Int, SummonBase> GetAllSummonsOnScene()
        {
            return GetSummonsModel().SummonsOnScene;
        }
        public void NewGame()
        {
            if (Module.GetModel<FightModel>() is { } fight)
                fight.RepositionAvailable = false;

            foreach (var summon in GetSummonsModel().SummonsOnScene)
            {
                summon.Value.Close();
            }
            GetSummonsModel().SummonsOnScene.Clear();
        }
        private void RecycleSummon(Vector2Int gridPos,SummonBase summon)
        {
            if (GetSummonsModel().SummonsOnScene.ContainsKey(gridPos))
            {
                GetSummonsModel().SummonsOnScene.Remove(gridPos);
            }
            var tile = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(gridPos);
            tile.SummonData = null;
            tile.PaidManaCost = null;
            tile.CanSummon = true;
            tile.IsHaveCharacter = false;
            summon.OnDead();
            summon.Close();
        }

        public int GetRecycleMana(SummonBase summon)
        {
            if (summon == null || summon.SummonData == null || summon.CurrentHp <= 0 || summon.MaxHp <= 0)
                return 0;

            // 用四分之一点魔力计算满血上限，只在最后按血量折算时向下取整。
            long fullHealthQuarters = Math.Min(2L * summon.SummonData.magicPoint, 3L * summon.PaidManaCost);
            int hp = Math.Min(summon.CurrentHp, summon.MaxHp);
            int amount = (int)(fullHealthQuarters * hp / (4L * summon.MaxHp));
            var wallet = Module.LoadController<SummonerController>().GetSummonerModel();
            return Mathf.Clamp(amount, 0, Math.Max(0, wallet.MaxMana - wallet.Mana));
        }

        public void DeleteSummon(Vector2Int gridPos)
        {
            if (!Module.LoadController<FightController>().CanIssuePlayerCommand)
                return;
            var summons = GetSummonsModel().SummonsOnScene;
            if (!summons.TryGetValue(gridPos, out var summon) || !summon || !summon.isActiveAndEnabled || summon.CurrentHp <= 0)
                return;

            int amount = GetRecycleMana(summon);
            summons.Remove(gridPos);
            var tile = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(gridPos);
            tile.SummonData = null;
            tile.PaidManaCost = null;
            tile.CanSummon = true;
            tile.IsHaveCharacter = false;
            tile.AboutToSummon = false;
            Module.LoadController<SummonerController>().ChangeManaValue(1, amount);
            summon.Close();
        }
        
        public float FlagBearerProvidedBuff(Vector2Int gridPos)
        {
            for (int i = -FlagAuraRange; i <= FlagAuraRange; i++)
            {
                Vector2Int pos = new Vector2Int(gridPos.x + i, gridPos.y);
                SummonBase summon = GetSummon(pos);
                if (summon != null && summon.SummonData.summonName == "持旗手")
                {
                    return FlagAuraBonus;
                }
            }
            return 0f;
        }

        public static float GetSkullLinkDodge(int neighbors) =>
            neighbors <= 0 ? 0f : 1f - Mathf.Pow(0.85f, 1 + Mathf.Min(neighbors, 2));

        public static float GetBlastLinkBonus(int neighbors) =>
            neighbors <= 0 ? 0f : Mathf.Pow(1.15f, 1 + Mathf.Min(neighbors, 2)) - 1f;

        public float SkullAdjoinDodgeGain(Vector2Int gridPos)
        {
            var archer = GetSummon(gridPos) as SkeletonArcher;
            float archerPair = 0f;
            int otherSkulls = 0;
            for (int dx = -1; dx <= 1; dx += 2)
            {
                var neighbor = GetSummon(gridPos + Vector2Int.right * dx);
                if (neighbor == null || neighbor.SummonData.occupationType != OccupationType.Skull)
                    continue;
                if (archer != null && neighbor is SkeletonArcher)
                    archerPair = archer.SummonData.ArcherPairDodge;
                else
                    otherSkulls++;
            }
            return Mathf.Max(archerPair, GetSkullLinkDodge(otherSkulls));
        }

        public float BlastingMasterAdjoinGain(Vector2Int gridPos)
        {
            int neighbors = 0;
            for (int dx = -1; dx <= 1; dx += 2)
            {
                var unit = GetSummon(gridPos + Vector2Int.right * dx);
                if (unit != null && unit.SummonData.summonName == "爆破法师")
                    neighbors++;
            }
            return GetBlastLinkBonus(neighbors);
        }
        //这样做的目的在于让永久Buff效果不被覆盖，且每一个临时Buff仅有一轮
        public void ClearAllSummonBuff()
        {
            foreach (var summon in GetSummonsModel().SummonsOnScene)
            {
                summon.Value.BuffEffectClear();
            }
        }
        public void RecordAllSummonBuff()
        {
            foreach (var summon in GetSummonsModel().SummonsOnScene)
            {
                summon.Value.RecordBuff();
            }
        }

        public void AllSummonCanAttack()
        {
            foreach (var summon in GetSummonsModel().SummonsOnScene)
            {
                // 玩家开始布阵时即可预览下一敌方回合的护卫。
                if (summon.Value is SkullShield guard)
                    guard.GuardReady = true;
                summon.Value.IsAttacked = false;
            }
        }
        public void AllSummonCanNotAttack()
        {
            foreach (var summon in GetSummonsModel().SummonsOnScene)
            {
                summon.Value.IsAttacked = true;
            }
        }
    }
    
}
