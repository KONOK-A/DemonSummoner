using Game;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.View.Summons
{
    public class AcidSlime : SummonBase
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
                var damage = summonData.skillValue *
                             (1 + AttackBuff) *
                             (1 + Module.LoadController<SummonsController>().FlagBearerProvidedBuff(gridPos));
                
                
                //生成特效
                VFXEffect.PlayAt(Module, "酸性史莱姆特效", releasePos);
                //调用勇者管理器伤害勇者
                Module.LoadController<AdventurerController>().BeHurt(damage, releasePos,
                    summonData.SkillAttackType);
                
            }

            IsAttacked = true;
        }

        public override void TakeDamage(float damage, AttackType attackType)
        {
            int healthBeforeHit = CurrentHp;
            base.TakeDamage(damage, attackType);
            if (!attackType.IsContact() || CurrentHp >= healthBeforeHit)
                return;

            var map = Module.LoadController<MapInfoController>();
            for (int distance = 1; distance <= 2; distance++)
            {
                var cell = gridPos + Vector2Int.left * distance;
                if (map.GetTileDetailsByGridPosition(cell) is { IsHaveCharacter: true, CharacterType: CharacterType.Adventurer })
                {
                    Module.LoadController<AdventurerController>().BeHurt(60, cell, AttackType.MagicProjectile);
                    VFXEffect.PlayAt(Module, "酸性史莱姆特效", cell, .68f);
                }
            }
        }

        public override void OnDead()
        {
            
        }
    }
}
