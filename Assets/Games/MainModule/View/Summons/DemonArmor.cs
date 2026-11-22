using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.View.Summons
{
    public class DemonArmor : SummonBase
    {
        public override void ReleaseSkill(Vector2Int releasePos)
        {
            TileDetails details = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(releasePos);
            if (details is { IsHaveCharacter: true, CharacterType: CharacterType.Adventurer })
            {
                float damage = summonData.skillValue *
                               (1 + AttackBuff) *
                               (1 + Module.LoadController<SummonsController>().FlagBearerProvidedBuff(gridPos));
                //音效
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.HitAttack);
                //生成特效
                VFXEffect.PlayAt(Module, "魔铠甲特效", releasePos);
                //TODO调用勇者管理器伤害勇者
                Module.LoadController<AdventurerController>().BeHurt(damage, releasePos,
                    summonData.SkillAttackType);
            }
            IsAttacked = true;
            
        }
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            _magicFragility = 0.3f;
            _physicDamageReduction = 0.6f;
            RecordBuff();
        }

        public override void OnDead()
        {
            
        }
    }
}
