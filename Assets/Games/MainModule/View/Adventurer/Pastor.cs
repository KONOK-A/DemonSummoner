using Games.MainModule.Configs.AdventurerConfigs;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.ToastPanel;
using Games.MainModule.View.VFX;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.View.Adventurer
{
    [MovedFrom(true, sourceNamespace: "Games.MainModule.View.Adventurer", sourceClassName: "Paster")]
    public class Pastor : AdventurerBase
    {
        public override void Move()
        {
            AdventurerBase supportTarget = null;
            float lowestHealthRatio = float.PositiveInfinity;
            foreach (var ally in AdventurerController.Adventurers)
            {
                float healthRatio = ally.adventurerConfigs.currentLife / ally.adventurerConfigs.life;
                if (healthRatio >= lowestHealthRatio)
                    continue;

                lowestHealthRatio = healthRatio;
                supportTarget = ally;
            }

            if (supportTarget == null)
                return;

            switch (adventurerConfigs.DrawPastorSupport(lowestHealthRatio))
            {
                case PastorSupportAction.Heal:
                    HealAlly(supportTarget);
                    break;
                case PastorSupportAction.Shield:
                    supportTarget.ApplyBuff("Defence", adventurerConfigs.Skill2Value, 1);
                    break;
                case PastorSupportAction.Blessing:
                    AddAttack();
                    break;
            }

            int shots = AdventurerController.DieTeammateNumber;
            if (shots == 0)
                return;

            var summons = Module.LoadController<SummonsController>();
            var map = Module.LoadController<MapInfoController>();
            float damage = adventurerConfigs.PastorRevengeDamage * AttackMultiplier;
            for (int i = 0; i < shots; i++)
            {
                var target = FindRevengeTarget(summons, map);
                if (!target.HasValue)
                    break;

                var hitCell = summons.DamageSummon(target.Value, AttackType.MagicProjectile, damage);
                VFXEffect.PlayAt(Module, "PasterRevenge", hitCell);
            }
        }

        private Vector2Int? FindRevengeTarget(SummonsController summons, MapInfoController map)
        {
            Vector2Int? best = null;
            int lowestHp = int.MaxValue;
            for (int distance = 1; distance <= adventurerConfigs.PastorRevengeRange; distance++)
            {
                var cell = gridPos + Vector2Int.right * distance;
                if (map.GetTileDetailsByGridPosition(cell) == null)
                    break;

                var summon = summons.GetSummon(cell);
                if (!summon || !summon.isActiveAndEnabled || summon.CurrentHp <= 0 || summon.CurrentHp >= lowestHp)
                    continue;

                lowestHp = summon.CurrentHp;
                best = cell;
            }
            return best;
        }

        private void HealAlly(AdventurerBase target)
        {
            VFXEffect.PlayAt(Module, "PastorHeal", target.gridPos);
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.HealAttack);
            Module.LoadPanel<ToastPanel>("ToastPanel",UIType.UI,null,"牧师发动了治疗魔法",0.75f);
            target.Heal((int)adventurerConfigs.Skill1Value);
        }
        //攻击buff
        private void AddAttack()
        {
            foreach (var a in AdventurerController.Adventurers)
            {
                //生成特效
                VFXEffect.PlayAt(Module, "PastorAddAttack", a.gridPos);
                a.ApplyBuff("AddAttack", adventurerConfigs.Skill3Value, 2);
            }
            Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "牧师为全队施加了祝福", 0.75f);
        }
    }
}
