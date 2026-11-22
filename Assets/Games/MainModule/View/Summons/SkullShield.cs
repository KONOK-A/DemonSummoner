using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.VFX;
using UnityEngine;
using XFGameFramework;
using AudioController = Games.MainModule.Scripts.Controller.AudioController;

namespace Games.MainModule.View.Summons
{
    public class SkullShield : SummonBase
    {
        public const float BasePhysicalReduction = 0.3f;
        public bool GuardReady { get; internal set; }

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
                //生成特效
                VFXEffect.PlayAt(Module, "骷髅盾兵特效", releasePos);
                //音效
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.SwordAttack);
            }
            IsAttacked = true;
        }

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            GuardReady = true;
            _physicDamageReduction = BasePhysicalReduction;
            RecordBuff();
        }

        public override void OnDead()
        {
            
        }
    }
}
