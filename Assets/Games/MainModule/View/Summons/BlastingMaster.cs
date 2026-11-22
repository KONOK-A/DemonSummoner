using System.Collections.Generic;
using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.VFX;
using UnityEngine;

namespace Games.MainModule.View.Summons
{
    public class BlastingMaster : SummonBase
    {
        public const float BaseMagicReduction = 0.3f;

        public readonly struct BlastCell
        {
            public readonly Vector2Int Position;
            public readonly int Offset;
            public readonly AdventurerBase Target;
            public readonly float BaseDamage;

            internal BlastCell(Vector2Int position, int offset, AdventurerBase target, float baseDamage)
            {
                Position = position;
                Offset = offset;
                Target = target;
                BaseDamage = baseDamage;
            }
        }

        public bool CanTarget(Vector2Int center) => TryGetBlastCells(center, null);

        public bool TryGetBlastCells(Vector2Int center, List<BlastCell> cells)
        {
            cells?.Clear();
            if (!isActiveAndEnabled || IsAttacked || CurrentHp <= 0 || center.y != gridPos.y ||
                System.Math.Abs((long)center.x - gridPos.x) > summonData.attackRadius ||
                !Module.LoadController<FightController>().CanIssuePlayerCommand)
                return false;

            var summons = Module.LoadController<SummonsController>();
            var map = Module.LoadController<MapInfoController>();
            var centerTile = map.GetTileDetailsByGridPosition(center);
            if (summons.GetSummon(gridPos) != this || centerTile == null || centerTile.AboutToSummon ||
                summons.GetSummon(center) || (centerTile.IsHaveCharacter && centerTile.CharacterType != CharacterType.Adventurer))
                return false;

            var centerTarget = AdventurerController.LivingAt(center);
            if (centerTile.IsHaveCharacter && !centerTarget)
                return false;

            bool hasTarget = false;
            for (int offset = -1; offset <= 1; offset++)
            {
                var cell = center + Vector2Int.right * offset;
                if (offset != 0 && map.GetTileDetailsByGridPosition(cell) == null)
                    continue;

                var target = offset == 0 ? centerTarget : AdventurerController.LivingAt(cell);
                if (target) hasTarget = true;
                cells?.Add(new BlastCell(cell, offset, target,
                    offset == 0 ? summonData.skillValue : summonData.BlastSplashDamage));
            }

            if (!hasTarget) cells?.Clear();
            return hasTarget;
        }

        public override void ReleaseSkill(Vector2Int releasePos)
        {
            var castCells = new List<BlastCell>(3);
            if (!TryGetBlastCells(releasePos, castCells))
                return;

            var summons = Module.LoadController<SummonsController>();
            var heroes = Module.LoadController<AdventurerController>();
            var multiplier = (1f + AttackBuff) * (1f + summons.FlagBearerProvidedBuff(gridPos)) *
                             (1f + summons.BlastingMasterAdjoinGain(gridPos));
            IsAttacked = true;
            Module.LoadController<AudioController>().PlaySound(AudioCueNames.MagicAttack);
            foreach (var cell in castCells)
                VFXEffect.PlayAt(Module, "爆破法师特效", cell.Position, cell.Offset == 0 ? 1f : 0.72f);

            // 每格锁定一次实际勇者，先命中的目标阵亡、离队，也不影响旁格的命中。
            foreach (var cell in castCells)
                if (cell.Target)
                    heroes.BeHurt(cell.BaseDamage * multiplier, cell.Target, summonData.SkillAttackType);
        }

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            _magicDamageReduction = BaseMagicReduction;
            RecordBuff();
        }

        public override void OnDead()
        {
            
        }
    }
}
