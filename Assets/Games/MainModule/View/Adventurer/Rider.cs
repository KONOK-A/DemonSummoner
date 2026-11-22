using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.ToastPanel;
using Games.MainModule.View.VFX;
using UnityEngine;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.View.Adventurer
{
    public class Rider : AdventurerBase
    {
        public override void Move()
        {
            int dieNumber = 4 - AdventurerController.Adventurers.Count;
            Attack();
            
            var probability = Random.Range(0, 1f);
            if (AdventurerController.Adventurers.Contains(this) && probability < 0.2 * dieNumber)
            {
                Attack();
            }
        }

        private void Attack()
        {
            var summons = Module.LoadController<SummonsController>();
            float damageBuff = AttackMultiplier;
            float slashDamage = adventurerConfigs.Skill1Value * damageBuff;
            Vector2Int? chargeTarget = null;
            Vector2Int? slashTarget = null;
            for (int distance = 1; distance <= 3; distance++)
            {
                var cell = gridPos + Vector2Int.right * distance;
                var summon = summons.GetSummon(cell);
                if (!summon || !summon.isActiveAndEnabled || summon.CurrentHp <= 0)
                    continue;

                if (distance <= 2)
                    chargeTarget ??= cell;
                // 迅捷斩击优先选择能一击击倒的目标，其次选最近的单位。
                slashTarget ??= cell;
                if (summons.CanDefeatWithDirectHit(summon, AttackType.PhysicalContact, slashDamage))
                {
                    slashTarget = cell;
                    break;
                }
            }

            if (slashTarget is not { } slashCell)
            {
                Dodge();
                return;
            }

            float lostLifeFraction = (adventurerConfigs.life - adventurerConfigs.currentLife) / adventurerConfigs.life;
            float dodgeRoll = Random.Range(0f, lostLifeFraction);
            float attackRoll = Random.Range(0f, 0.5f);
            float chargeRoll = chargeTarget.HasValue ? Random.Range(0f, 0.5f) : -1f;
            if (dodgeRoll > attackRoll && dodgeRoll > chargeRoll)
            {
                Dodge();
                return;
            }

            float chargeDamage = adventurerConfigs.Skill2Value * damageBuff;
            bool useCharge = chargeRoll >= attackRoll;
            if (chargeTarget == slashCell)
                useCharge = ChooseChargeAgainstBlocker(summons, slashCell, slashDamage, chargeDamage, useCharge);
            if (chargeTarget is { } chargeCell && useCharge)
            {
                VFXEffect.PlayAt(Module, "RiderCharge", chargeCell);
                summons.DamageSummon(chargeCell, AttackType.PhysicalContact, chargeDamage, areaAttack: true);
                if (Module.GetModel<FightModel>().BattleEnd)
                    return;

                var nextCell = chargeCell + Vector2Int.right;
                var nextSummon = summons.GetSummon(nextCell);
                if (nextSummon && nextSummon.isActiveAndEnabled && nextSummon.CurrentHp > 0)
                {
                    VFXEffect.PlayAt(Module, "RiderCharge", nextCell);
                    summons.DamageSummon(nextCell, AttackType.PhysicalContact, chargeDamage, areaAttack: true);
                }
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.SwordAttack);
                Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "骑士进行突刺攻击", 0.75f);
            }
            else
            {
                var hitCell = summons.DamageSummon(slashCell, AttackType.PhysicalContact, slashDamage);
                VFXEffect.PlayAt(Module, "RiderAttack", hitCell);
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.SwordAttack);
                if (hitCell == slashCell)
                    Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "骑士发动迅捷斩击", 0.75f);
            }
        }

        private static bool ChooseChargeAgainstBlocker(SummonsController summons, Vector2Int cell,
            float slashDamage, float chargeDamage, bool rolledCharge)
        {
            var blocker = summons.GetSummon(cell);
            const AttackType contact = AttackType.PhysicalContact;
            if (summons.GetReadyGuard(cell) || blocker.GetDamageOnHit(slashDamage, contact) < blocker.CurrentHp)
                return rolledCharge;

            var rear = summons.GetSummon(cell + Vector2Int.right);
            bool hitsRear = rear && rear.isActiveAndEnabled && rear.CurrentHp > 0 && rear.GetDamageOnHit(chargeDamage, contact) > 0;
            bool chargeFinishes = blocker.GetDamageOnHit(chargeDamage, contact) >= blocker.CurrentHp;
            if (chargeFinishes && hitsRear)
                return true;
            if (!chargeFinishes && !hitsRear)
                return false;
            return rolledCharge;
        }

        private void Dodge()
        {
            //生成特效
            VFXEffect.PlayAt(Module, "RiderDogge", gridPos);
            ApplyBuff("Dodge", adventurerConfigs.Skill3Value, 1);
            //提示
            Module.LoadPanel<ToastPanel>("ToastPanel",UIType.UI,null,"骑士发动瞬身",0.75f);
        }
    }
}
