using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.View.Summons
{
    public class ProtectiveMage : SummonBase
    {
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            RecordBuff();
        }

        public override void ReleaseSkill(Vector2Int releasePos)
        {
            var summons = Module.LoadController<SummonsController>();
            float reduction = summonData.skillValue * (1f + AttackBuff);
            for (int i = -1; i <= 1; i++)
            {
                var cell = releasePos + Vector2Int.right * i;
                var ally = summons.GetSummon(cell);
                if (!ally || !ally.isActiveAndEnabled || ally.CurrentHp <= 0) continue;
                ally.TakeEffect(reduction, EffectType.ProtectiveWard);
                VFXEffect.PlayWard(Module, "防护法师特效", cell, ally.Hpbar.transform.position.y);
            }
            //音效
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.HealAttack);
            IsAttacked = true;
        }

        public override void OnDead()
        {
            
        }
    }
}
