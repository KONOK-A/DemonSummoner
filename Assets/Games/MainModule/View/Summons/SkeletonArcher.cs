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
    public class SkeletonArcher : SummonBase
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
                // Capture the torso before damage can remove the last hero from the board.
                var victim = AdventurerController.LivingAt(releasePos);
                Vector3 impact = new Vector3(releasePos.x + 0.5f, releasePos.y + 1f);
                if (victim)
                    impact = new Vector3(victim.transform.position.x, victim.Renderer.bounds.center.y, victim.transform.position.z);
                Vector3 bow = transform.TransformPoint(new Vector3(-0.26f, 0.52f, 0f));

                float damage = summonData.skillValue *
                               (1 + AttackBuff) *
                               (1 + Module.LoadController<SummonsController>().FlagBearerProvidedBuff(gridPos));
                //TODO调用勇者管理器伤害勇者
                Module.LoadController<AdventurerController>().BeHurt(damage, releasePos,
                    summonData.SkillAttackType);
                //音效
                Module.LoadController<AudioController>().PlaySound(AudioCueNames.ArcherAttack);
                //生成特效
                Module.LoadView<ArcherShotEffect>("骷髅弓箭手特效")?.Launch(bow, impact);
            }
            IsAttacked = true;
        }
        public override void OnDead()
        {
            
        }
    }
}
