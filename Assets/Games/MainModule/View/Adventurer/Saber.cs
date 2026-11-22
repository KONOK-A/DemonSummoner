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
    public class Saber : AdventurerBase
    {
        protected override float DamageTakenMultiplier =>
            Mathf.Pow(0.8f, AdventurerController.DieTeammateNumber);

        public override void Move()
        {
            var summons = Module.LoadController<SummonsController>();
            if (FindNearestTarget(summons) is not { } target)
            {
                AddAvoidance();
                return;
            }

            float damageBuff = AttackMultiplier;
            float lostLifeFraction = (adventurerConfigs.life - adventurerConfigs.currentLife) / adventurerConfigs.life;
            float attackRoll = Random.Range(0f, 0.5f);
            float guardRoll = Random.Range(0f, 0.4f);
            float bloodRoll = adventurerConfigs.currentLife < adventurerConfigs.life * 0.5f
                ? Random.Range(0f, lostLifeFraction) : -1f;

            //猛劈
            if (attackRoll >= guardRoll && attackRoll >= bloodRoll)
            {
                var hitCell = summons.DamageSummon(target, AttackType.PhysicalContact, adventurerConfigs.Skill1Value * damageBuff);
                VFXEffect.PlayAt(Module, "SaberAttack", hitCell);
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.SwordAttack);
                if (hitCell == target)
                    Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "战士发动猛劈", 0.75f);
            }
            //坚韧
            else if (guardRoll >= bloodRoll)
            {
                AddAvoidance();
            }
            //血怒击
            else
            {
                float damage = (adventurerConfigs.Skill3Value + 40f * lostLifeFraction) * damageBuff;
                var hitCell = summons.DamageSummon(target, AttackType.PhysicalContact, damage);
                VFXEffect.PlayAt(Module, "SaberBloodAttack", hitCell);
                if (hitCell == target)
                    Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "战士发动血怒斩", 0.75f);
            }
        }

        private Vector2Int? FindNearestTarget(SummonsController summons)
        {
            for (int distance = 1; distance <= 2; distance++)
            {
                var cell = gridPos + Vector2Int.right * distance;
                var summon = summons.GetSummon(cell);
                if (summon && summon.isActiveAndEnabled && summon.CurrentHp > 0)
                    return cell;
            }
            return null;
        }

        private void AddAvoidance()
        {
            //生成特效
            VFXEffect.PlayAt(Module, "Saberjianreng", gridPos);
            ApplyBuff(PhysicalGuard, adventurerConfigs.Skill2Value, 1);
            //提示
            Module.LoadPanel<ToastPanel>("ToastPanel",UIType.UI,null,"战士释放坚韧",0.75f);
        }
    }
}
