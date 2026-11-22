using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.ToastPanel;
using Games.MainModule.View.VFX;
using UnityEngine;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.View.Adventurer
{
    public class Caster : AdventurerBase
    {
        public Vector2Int? ExplosionCenter { get; private set; }

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            CancelExplosion();
        }

        public override void OnRemove()
        {
            CancelExplosion();
            base.OnRemove();
        }

        internal void CancelExplosion()
        {
            ExplosionCenter = null;
        }

        public override void Move()
        {
            var summons = Module.LoadController<SummonsController>();
            var map = Module.LoadController<MapInfoController>();
            float damageMultiplier = AttackMultiplier * Mathf.Pow(1.2f, AdventurerController.DieTeammateNumber);
            if (ExplosionCenter is { } lockedCenter)
            {
                CancelExplosion();
                ReleaseExplosion(lockedCenter, damageMultiplier, summons, map);
                return;
            }

            var explosionCenter = FindExplosionCenter(summons, map);
            float missileDamage = adventurerConfigs.Skill1Value * damageMultiplier;
            var missileTarget = FindMissileTarget(missileDamage, summons, map);
            float explosionRoll = explosionCenter.HasValue ? Random.Range(0f, 0.6f) : -1f;
            float missileRoll = missileTarget.HasValue ? Random.Range(0f, 0.6f) : -1f;
            float defenceRoll = Random.Range(0f, 0.5f);

            if (missileTarget is { } target && missileRoll >= explosionRoll && missileRoll >= defenceRoll)
            {
                var hitCell = summons.DamageSummon(target, AttackType.MagicProjectile, missileDamage);
                bool intercepted = hitCell != target;
                VFXEffect.PlayAt(Module, "CasterAttack", hitCell, intercepted ? 0.5f : 1f);
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.MagicAttack);
                if (!intercepted)
                    Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "魔法师释放了奥术飞弹", 0.75f);
            }
            else if (explosionCenter.HasValue && explosionRoll > defenceRoll)
            {
                ExplosionCenter = explosionCenter;
                EventManager.TriggerEvent(EventConst.BlastWarningRaised);
                Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "魔法师正在咏唱爆焱", 0.75f);
            }
            else
            {
                AddDefence();
            }
        }

        private Vector2Int? FindMissileTarget(float damage, SummonsController summons, MapInfoController map)
        {
            Vector2Int? nearestRanged = null;
            Vector2Int? farthest = null;
            for (int distance = 1; distance <= 7; distance++)
            {
                var cell = gridPos + Vector2Int.right * distance;
                if (map.GetTileDetailsByGridPosition(cell) == null)
                    break;

                var summon = summons.GetSummon(cell);
                if (summon == null || summon.CurrentHp <= 0)
                    continue;
                if (summons.CanDefeatWithDirectHit(summon, AttackType.MagicProjectile, damage))
                    return cell;

                if (summon.SummonData.summonAttackType == SummonType.LongRange)
                    nearestRanged ??= cell;
                farthest = cell;
            }
            return nearestRanged ?? farthest;
        }

        private Vector2Int? FindExplosionCenter(SummonsController summons, MapInfoController map)
        {
            Vector2Int? best = null;
            int bestCount = 0;
            bool bestHitsCenter = false;
            for (int distance = 1; distance <= 5; distance++)
            {
                var center = gridPos + Vector2Int.right * distance;
                if (map.GetTileDetailsByGridPosition(center) == null)
                    break;

                int count = 0;
                bool hitsCenter = false;
                for (int offset = -1; offset <= 1; offset++)
                {
                    var cell = center + Vector2Int.right * offset;
                    if (map.GetTileDetailsByGridPosition(cell) == null)
                        continue;
                    var summon = summons.GetSummon(cell);
                    if (!summon || !summon.isActiveAndEnabled || summon.CurrentHp <= 0)
                        continue;
                    count++;
                    if (offset == 0) hitsCenter = true;
                }
                if (count > bestCount || (count == bestCount && hitsCenter && !bestHitsCenter))
                {
                    best = center;
                    bestCount = count;
                    bestHitsCenter = hitsCenter;
                }
            }
            return best;
        }

        private void ReleaseExplosion(Vector2Int center, float multiplier, SummonsController summons, MapInfoController map)
        {
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.MagicAttack);
            for (int offset = -1; offset <= 1; offset++)
            {
                var cell = center + Vector2Int.right * offset;
                if (map.GetTileDetailsByGridPosition(cell) == null)
                    continue;

                float damage = offset == 0 ? adventurerConfigs.Skill3Value : adventurerConfigs.ExplosionSplashDamage;
                summons.DamageSummon(cell, AttackType.MagicProjectile, damage * multiplier, areaAttack: true);
                VFXEffect.PlayAt(Module, "Caterqungong", cell, offset == 0 ? 1f : 0.6f);
            }
        }

        public bool CanWardCell(Vector2Int cell) =>
            cell.y == gridPos.y && Mathf.Abs(cell.x - gridPos.x) <= adventurerConfigs.CasterWardRadius;

        private void AddDefence()
        {
            foreach (var adventurer in AdventurerController.Adventurers)
            {
                if (!CanWardCell(adventurer.gridPos))
                    continue;

                VFXEffect.PlayWard(Module, "CaterDefence", adventurer.gridPos, adventurer.hpBar.transform.position.y);
                adventurer.ApplyBuff(MagicGuard, adventurerConfigs.Skill2Value, 1);
            }
            Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null,
                $"魔法护盾保护法师与左右 {adventurerConfigs.CasterWardRadius} 格的勇者", 0.75f);
        }
    }
}
