using System.Collections;
using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.VFX;
using UnityEngine;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.View.Summons
{
    public class SkeletonWarrior : SummonBase
    {
        private const float ReturnSlashDelay = 0.16f;
        private Coroutine returnSlash;
        private AdventurerBase returnTarget;
        private FightModel resolvingFight;

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            RecordBuff();
        }

        public override void ReleaseSkill(Vector2Int releasePos)
        {
            var fight = Module.LoadController<FightController>().GetFightModel();
            int distance = Mathf.Abs(releasePos.x - gridPos.x);
            if (!isActiveAndEnabled || IsAttacked || fight.BattleEnd || fight.IsResolvingPlayerSkill ||
                Time.timeScale <= 0f || releasePos.y != gridPos.y || distance == 0 || distance > summonData.attackRadius)
                return;

            var target = AdventurerController.Adventurers.Find(unit => unit != null && unit.gridPos == releasePos);
            if (target == null || !target.isActiveAndEnabled)
                return;

            bool adjacent = distance == 1;
            float baseDamage = adjacent ? summonData.CloseStrikeDamage : summonData.skillValue;
            float damage = baseDamage * (1f + AttackBuff) *
                           (1f + Module.LoadController<SummonsController>().FlagBearerProvidedBuff(gridPos));
            IsAttacked = true;
            Strike(target, damage, false);
            if (!adjacent || fight.BattleEnd || !AdventurerController.Adventurers.Contains(target))
                return;

            resolvingFight = fight;
            returnTarget = target;
            Module.LoadController<FightController>().SetPendingPlayerSkill(CancelReturnSlash);
            target.onViewRemove += CancelReturnSlash;
            returnSlash = StartCoroutine(StrikeAgain(target, releasePos, damage));
        }

        private IEnumerator StrikeAgain(AdventurerBase target, Vector2Int cell, float damage)
        {
            yield return new WaitForSeconds(ReturnSlashDelay);
            returnSlash = null;
            try
            {
                if (resolvingFight is { BattleEnd: false } && target != null && target.isActiveAndEnabled &&
                    target.gridPos == cell && AdventurerController.Adventurers.Contains(target))
                    Strike(target, damage, true);
            }
            finally
            {
                CancelReturnSlash();
            }
        }

        private void Strike(AdventurerBase target, float damage, bool returning)
        {
            SkirmishEffects.PlaySlash(this, target, returning);
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.SwordAttack);
            Module.LoadController<AdventurerController>().BeHurt(damage, target, summonData.SkillAttackType);
        }

        private void CancelReturnSlash()
        {
            if (resolvingFight == null)
                return;

            Module.LoadController<FightController>().SetPendingPlayerSkill(null);
            resolvingFight = null;
            if (returnTarget != null)
                returnTarget.onViewRemove -= CancelReturnSlash;
            returnTarget = null;
            if (returnSlash != null)
                StopCoroutine(returnSlash);
            returnSlash = null;
        }

        protected override void OnDisable()
        {
            CancelReturnSlash();
            base.OnDisable();
        }

        public override void OnDead()
        {
        }
    }
}
