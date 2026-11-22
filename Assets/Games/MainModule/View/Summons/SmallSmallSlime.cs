using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.View.Summons
{
    public class SmallSmallSlime : SummonBase
    {
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            RecordBuff();
        }

        public override void ReleaseSkill(Vector2Int releasePos)
        {
            TileDetails details = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(releasePos);
            if (details is { IsHaveCharacter: true, CharacterType: CharacterType.Adventurer })
            {
                float damage = summonData.skillValue *
                               (1 + AttackBuff) *
                               (1 + Module.LoadController<SummonsController>().FlagBearerProvidedBuff(gridPos));
                //调用勇者管理器伤害勇者
                Module.LoadController<AdventurerController>().BeHurt(damage, releasePos,
                    summonData.SkillAttackType);
                //音效
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.SlimeAttack);
                //生成特效
                VFXEffect.PlayAt(Module, "SlimeAttack", releasePos);
            }
            IsAttacked = true;
        }

        public override void OnDead()
        {
        }
    }
}
