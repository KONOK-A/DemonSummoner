using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Games.MainModule.Scripts.Command;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using UnityEngine;
using XFABManager;

namespace Games.MainModule.Scripts.Controller
{
    public class AdventurerController : XFGameFramework.Controller
    {
        
        private bool _isFirst = true;
        private static readonly string[] PartyPrefabs = { nameof(Saber), nameof(Rider), nameof(Caster), nameof(Pastor) };
        private Coroutine _enemyTurn;

        public static readonly List<AdventurerBase> Adventurers = new();
        public static AdventurerBase LivingAt(Vector2Int cell)
        {
            foreach (var member in Adventurers)
                if (member && member.isActiveAndEnabled && member.gridPos == cell && member.adventurerConfigs.currentLife > 0f)
                    return member;
            return null;
        }

        public static int PartySize => PartyPrefabs.Length;
        public static int DieTeammateNumber => PartySize - Adventurers.Count;
        private int AdvanceBudget => _isFirst ? 1 : 2;
        
        /// <summary>
        /// 勇者小队受到伤害提供的接口
        /// </summary>
        /// <param name="value">伤害的值</param>
        /// <param name="position">受击者所在的世界格</param>
        /// <param name="attackType">本次攻击的伤害属性和命中方式</param>
        public void BeHurt(float value, Vector2Int position, AttackType attackType)
        {
            foreach (var target in Adventurers.Where(a => a != null && a.gridPos == position).ToList())
                BeHurt(value, target, attackType);
        }

        public void BeHurt(float value, AdventurerBase target, AttackType attackType)
        {
            if (Module.LoadController<FightController>().GetFightModel().BattleEnd ||
                target == null || !Adventurers.Contains(target))
                return;

            target.BeHurt((int)value, attackType);
            if (target.adventurerConfigs.currentLife > 0f)
                return;

            Adventurers.Remove(target);
            if (Adventurers.Count == 0)
                new GameWinCommand(Module).Execute();
        }

        public void Init(Vector2Int gridPos)
        {
            Adventurers.Clear();
            _isFirst = true;
            var map = Module.LoadController<MapInfoController>();
            for (int i = 0; i < PartySize; i++)
            {
                string prefab = PartyPrefabs[i];
                var cell = gridPos - Vector2Int.right * i;
                var adventurer = Module.LoadView<AdventurerBase>(prefab);
                adventurer.gridPos = cell;
                adventurer.transform.position = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                adventurer.Init(prefab);
                adventurer.adventurerConfigs.currentLife = adventurer.adventurerConfigs.life;

                var tile = map.GetTileDetailsByGridPosition(cell);
                tile.CanSummon = false;
                tile.IsHaveCharacter = true;
                tile.CharacterType = CharacterType.Adventurer;
            }
        }
        

        public void NewGame()
        {
            foreach (var adventurer in Adventurers)
            {
                adventurer.Close();
            }
            Adventurers.Clear();
            _isFirst = true;
        }

        /// <summary>返回队首当前位置、按当前阻挡可到达的位置，以及阻挡清除后本回合最远的位置。</summary>
        public bool GetAdvancePreview(out Vector2Int front, out Vector2Int destination, out Vector2Int furthest)
        {
            front = destination = furthest = default;
            if (Adventurers.Count == 0)
                return false;

            front = Adventurers[0].gridPos;
            int tail = Adventurers[Adventurers.Count - 1].gridPos.x;
            // 队尾先花步数补齐中间的空位，队首只推进全队跟得上的距离。
            int gaps = front.x - tail - (Adventurers.Count - 1);
            int limit = Mathf.Max(0, AdvanceBudget - gaps);
            destination = GetDestination(Module.LoadController<MapInfoController>(), front, limit, out furthest);
            return true;
        }

        private static Vector2Int GetDestination(MapInfoController map, Vector2Int start, int limit,
            out Vector2Int furthest)
        {
            var destination = start;
            furthest = start;
            bool blocked = false;
            for (int step = 0; step < limit; step++)
            {
                var next = furthest + Vector2Int.right;
                var tile = map.GetTileDetailsByGridPosition(next);
                if (tile == null)
                    break;

                blocked |= tile.IsHaveCharacter || tile.AboutToSummon;
                if (!blocked)
                    destination = next;
                furthest = next;
            }
            return destination;
        }

        private bool AdvanceParty()
        {
            if (!GetAdvancePreview(out _, out var frontDestination, out _))
                return false;

            var map = Module.LoadController<MapInfoController>();
            var preceding = frontDestination;
            bool moved = false;
            for (int i = 0; i < Adventurers.Count; i++)
            {
                var adventurer = Adventurers[i];
                var start = adventurer.gridPos;
                var destination = i == 0 ? frontDestination : GetDestination(map, start,
                    Mathf.Min(AdvanceBudget, preceding.x - start.x - 1), out _);
                preceding = destination;
                if (start == destination)
                    continue;

                map.GetTileDetailsByGridPosition(start).IsHaveCharacter = false;
                for (int x = start.x; x < destination.x; x++)
                    map.GetTileDetailsByGridPosition(new Vector2Int(x, start.y)).CanSummon = false;

                var tile = map.GetTileDetailsByGridPosition(destination);
                tile.CanSummon = false;
                tile.IsHaveCharacter = true;
                tile.CharacterType = CharacterType.Adventurer;
                adventurer.gridPos = destination;
                adventurer.transform.DOMove(new Vector2(destination.x + 0.5f, adventurer.transform.position.y), 1.35f)
                    .SetLink(adventurer.gameObject);
                moved = true;
            }

            if (map.GetTileDetailsByGridPosition(frontDestination + Vector2Int.right) == null)
            {
                Module.GetModel<FightModel>().InnerHallEntrant = Adventurers[0].DisplayName;
                new GameFailCommand(Module).Execute();
            }
            return moved;
        }
        /// <summary>
        /// 勇者小队回合开始时调用，执行不同的行为逻辑
        /// </summary>
        public void Move()
        {
            if (_enemyTurn == null)
                _enemyTurn = CoroutineStarter.Start(StartMove());
        }

        public void StopEnemyTurn(bool stopMovement)
        {
            foreach (var adventurer in Adventurers)
            {
                if (adventurer is Caster caster)
                    caster.CancelExplosion();
            }
            if (_enemyTurn == null)
                return;

            CoroutineStarter.Stop(_enemyTurn);
            _enemyTurn = null;
            foreach (var adventurer in Adventurers)
            {
                adventurer.Renderer.SetPropertyBlock(AdventurerBase.NormalPropertyBlock);
                if (stopMovement)
                    adventurer.transform.DOKill();
            }
        }

        private IEnumerator StartMove()
        {
            var fight = Module.LoadController<FightController>().GetFightModel();
            //勇者回合开始。先减一轮buff
            foreach (var adventurerBase in Adventurers)
            {
                foreach (var stateBuff in adventurerBase.StateBuff)
                {
                    if (stateBuff.Value.persistTurn > 0) stateBuff.Value.persistTurn--;
                    if (stateBuff.Value.persistTurn <= 0) stateBuff.Value.value = 0;
                }
            }
            var summons = Module.LoadController<SummonsController>().GetAllSummonsOnScene();
            var turnOrder = Adventurers.ToArray();
            for (int i = turnOrder.Length - 1; i >= 0; i--)
            {
                var adventurer = turnOrder[i];
                if (!Adventurers.Contains(adventurer) ||
                    (summons.Count == 0 && adventurer is not Caster { ExplosionCenter: not null }))
                    continue;

                adventurer.Renderer.SetPropertyBlock(AdventurerBase.OutlinePropertyBlock);
                adventurer.gameObject.SetActive(false);
                adventurer.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);

                adventurer.Move();
                if (fight.BattleEnd)
                    yield break;

                yield return new WaitForSeconds(0.8f);
                if (Adventurers.Contains(adventurer))
                {
                    adventurer.Renderer.SetPropertyBlock(AdventurerBase.NormalPropertyBlock);
                    adventurer.gameObject.SetActive(false);
                    adventurer.gameObject.SetActive(true);
                }
            }
            bool moved = AdvanceParty();
            _isFirst = false;
            if (moved)
                yield return new WaitForSeconds(1.5f);
            else
                yield return null;
            _enemyTurn = null;
            if (!fight.BattleEnd)
                new EnemyTurnEndCommand(Module).Execute();
        }
    }
}
