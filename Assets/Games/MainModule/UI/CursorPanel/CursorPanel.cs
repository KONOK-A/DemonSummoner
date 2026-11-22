using System.Collections.Generic;
using Game;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.Summons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.VectorGraphics;
using XFGameFramework;

namespace Games.MainModule.UI.CursorPanel
{
    public class CursorPanel : Panel
    {
        [SerializeField, FormerlySerializedAs("Normal")] private Sprite normal;
        [SerializeField, FormerlySerializedAs("Magic")] private Sprite magic;
        [SerializeField, FormerlySerializedAs("Attack")] private Sprite attack;
        [SerializeField] private Sprite ward;
        [SerializeField] private Sprite magicBlocked;
        [SerializeField] private Sprite attackBlocked;
        [SerializeField] private Sprite wardBlocked;

        private readonly List<RaycastResult> hits = new();
        private readonly List<Collider2D> characterHits = new();
        private readonly List<BlastingMaster.BlastCell> blastAim = new(3);
        internal IReadOnlyList<BlastingMaster.BlastCell> BlastAim => blastAim;
        private PointerEventData pointerEvent;
        private EventSystem pointerEventSystem;
        private SVGImage vectorCursor;
        private Image legacyCursor;
        private RectTransform cursorRect;
        private Grid battleGrid;
        private Camera battleCamera;
        private OperationType operation;
        private SummonBase pendingSelection;
        private SummonBase selectedSummon;

        public bool isPlayerTurn { get; private set; }
        public AdventurerBase FocusTarget { get; private set; }
        public SummonBase RepositionSelection => isActiveAndEnabled && isPlayerTurn &&
            operation == OperationType.Reposition && Time.timeScale > 0f ? selectedSummon : null;
        public override PanelType PanelType => PanelType.Overlay;
        public override bool AlwaysInFront => true;

        private bool ControlsSummon => operation is OperationType.SummonControl or OperationType.Reposition;
        private bool IsResolvingPlayerSkill => Module.GetModel<FightModel>()?.IsResolvingPlayerSkill == true;
        private SummonerController Summoner => Module.LoadController<SummonerController>();
        private SummonsController Summons => Module.LoadController<SummonsController>();

        protected override void OnSortingOrderChange(int sortingOrder)
        {
            base.OnSortingOrderChange(sortingOrder);
            // Other AlwaysInFront panels can receive the same framework order.
            Canvas.sortingOrder = sortingOrder + 1;
        }

        protected override void Awake()
        {
            base.Awake();
            vectorCursor = GetComponent<SVGImage>();
            legacyCursor = GetComponent<Image>();
            cursorRect = (RectTransform)transform;
            RegisterEvent<int>(EventConst.MagicBookOpen, OnMagicBookOpen);
            RegisterEvent(EventConst.MagicBookClose, () =>
            {
                if (operation == OperationType.ReleaseMagic)
                    ClearOperation();
            });
            RegisterEvent(EventConst.CancelBattleOperation, CancelOperation);
            RegisterEvent(EventConst.ClearFocusTarget, ClearFocusTarget);
            RegisterEvent(EventConst.PlayerTurnBegin, () =>
            {
                ClearOperation();
                isPlayerTurn = true;
            });
            RegisterEvent(EventConst.PlayerTurnEnd, EndPlayerTurn);
            RegisterEvent(EventConst.EnemyTurnBegin, EndPlayerTurn);
            RegisterEvent<SummonBase>(EventConst.SelectControlSummonToCursor, summon => pendingSelection = summon);
            RegisterEvent(EventConst.DeleteSummon, RecycleSelectedSummon);
            RegisterEvent<bool>(EventConst.RepositionTargetingChanged, targeting =>
            {
                if (isPlayerTurn && ControlsSummon && selectedSummon && Time.timeScale > 0f)
                    operation = targeting ? OperationType.Reposition : OperationType.SummonControl;
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EndPlayerTurn();
            battleGrid = FindFirstObjectByType<Grid>();
            battleCamera = Camera.main;
            Show(normal);
        }

        protected override void OnDisable()
        {
            EndPlayerTurn();
            base.OnDisable();
        }

        private void LateUpdate()
        {
            UpdatePointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonDown(1));
        }

        private void UpdatePointer(Vector2 screenPosition, bool leftPressed, bool rightPressed)
        {
            blastAim.Clear();
            var candidate = pendingSelection;
            pendingSelection = null;
            cursorRect.position = new Vector3(Mathf.Round(screenPosition.x), Mathf.Round(screenPosition.y), 0);

            if (rightPressed && Time.timeScale != 0)
                EventManager.TriggerEvent(EventConst.CancelBattleOperation);

            if (!isPlayerTurn || Time.timeScale <= 0f || Module?.GetModel<FightModel>() is not { IsPlayerCommandTurn: true } fight)
            {
                ClearFocusTarget();
                Show(normal);
                return;
            }

            if (!ReferenceEquals(FocusTarget, null) && (!FocusTarget || FocusTarget.adventurerConfigs.currentLife <= 0f ||
                                                        !AdventurerController.Adventurers.Contains(FocusTarget)))
                ClearFocusTarget();

            if (rightPressed || fight.IsResolvingPlayerSkill)
            {
                Show(normal);
                return;
            }

            if (ControlsSummon && (!selectedSummon || !selectedSummon.isActiveAndEnabled))
                FinishSummonControl();

            if ((operation == OperationType.None && !FocusTarget && !leftPressed) ||
                (operation == OperationType.ReleaseMagic && !Summoner.PendingSummon))
            {
                Show(normal);
                return;
            }

            // These decisions run after UI input has been processed in the current frame.
            if (IsOverUi(screenPosition))
            {
                Show(normal);
                return;
            }

            if (!TryGetBattleCell(screenPosition, out var cell, out var hitSummon))
            {
                if (leftPressed && operation == OperationType.None)
                    ClearFocusTarget();
                Show(normal);
                return;
            }

            if (leftPressed && candidate && candidate.isActiveAndEnabled &&
                (operation == OperationType.None || (operation == OperationType.SummonControl && selectedSummon is not BlastingMaster)))
            {
                hitSummon = candidate;
                cell = candidate.GridPos;
            }

            if (leftPressed)
                HandleClick(cell, hitSummon);
            Show(ControlsSummon && SelectableSummonAt(cell, hitSummon) ? normal : GetCursor(cell));
        }

        private SummonBase SelectableSummonAt(Vector2Int cell, SummonBase hitSummon)
        {
            // Bodies select allies. Exposed floor stays a ward target, including occupied tiles.
            if (!hitSummon && operation == OperationType.SummonControl && selectedSummon is ProtectiveMage)
                return null;

            var unit = Summons.GetSummon(hitSummon ? hitSummon.GridPos : cell);
            return unit && unit.isActiveAndEnabled && (!hitSummon || unit == hitSummon) ? unit : null;
        }

        private void HandleClick(Vector2Int cell, SummonBase hitSummon)
        {
            if (operation == OperationType.ReleaseMagic)
            {
                Summoner.TryPlacePreparedSummon(cell);
                return;
            }

            var unit = SelectableSummonAt(cell, hitSummon);
            if (ControlsSummon && unit)
            {
                if (unit == selectedSummon)
                    FinishSummonControl();
                else
                    SelectSummon(unit);
                return;
            }
            if (operation == OperationType.Reposition)
            {
                if (Summons.TryReposition(selectedSummon, cell))
                    FinishSummonControl();
                return;
            }
            if (operation == OperationType.SummonControl)
            {
                if (CanUseSkillOn(selectedSummon, cell))
                {
                    selectedSummon.ReleaseSkill(cell);
                    FinishSummonControl();
                }
                return;
            }

            if (!unit)
            {
                bool reading = Module.GetView<GamePanel.GamePanel>()?.IsReadingMonsterManual == true;
                var target = reading ? null : AdventurerController.LivingAt(cell);
                SetFocusTarget(target == FocusTarget ? null : target);
                return;
            }

            if (FocusTarget)
            {
                if (unit is not ProtectiveMage)
                {
                    if (CanFocusAttack(unit))
                        unit.ReleaseSkill(FocusTarget.gridPos);
                    return;
                }
                if (!CanAct(unit))
                    return;
                ClearFocusTarget();
            }

            SelectSummon(unit);
        }

        private void SelectSummon(SummonBase unit)
        {
            selectedSummon = unit;
            operation = OperationType.SummonControl;
            EventManager.TriggerEvent(EventConst.SelectControlSummonToGame, unit);
        }

        private Sprite GetCursor(Vector2Int cell)
        {
            if (operation == OperationType.ReleaseMagic)
            {
                if (!Summoner.PendingSummon) return normal;
                return Summoner.CanPlacePreparedSummon(cell) || !magicBlocked ? magic : magicBlocked;
            }

            if (operation == OperationType.SummonControl)
                return SkillCursor(selectedSummon, TryAimSkill(selectedSummon, cell, null, blastAim));

            if (FocusTarget && Summons.GetSummon(cell) is { } unit)
                return SkillCursor(unit, unit is ProtectiveMage ? CanAct(unit) : CanAimAtFocus(unit, blastAim));
            return normal;
        }

        private Sprite SkillCursor(SummonBase unit, bool available)
        {
            bool protecting = unit is ProtectiveMage && ward;
            var icon = protecting ? ward : attack;
            var blocked = protecting ? wardBlocked : attackBlocked;
            return available || !blocked ? icon : blocked;
        }

        private bool TryGetBattleCell(Vector2 screenPosition, out Vector2Int cell, out SummonBase hitSummon)
        {
            cell = default;
            hitSummon = null;
            if (!battleCamera || !battleGrid)
                return false;

            var ray = battleCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(battleGrid.transform.forward, battleGrid.transform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;

            var point = ray.GetPoint(distance);
            cell = (Vector2Int)battleGrid.WorldToCell(point);
            // Keep floor aiming precise even where adjacent artwork overhangs a blast or move destination.
            if (operation == OperationType.ReleaseMagic ||
                (ControlsSummon && (operation == OperationType.Reposition || selectedSummon is BlastingMaster) &&
                 cell.y == selectedSummon.GridPos.y))
                return true;

            // Standing artwork extends above the lane. The body and its floor tile select the same unit.
            Physics2D.OverlapPoint(point, ContactFilter2D.noFilter, characterHits);
            foreach (var hit in characterHits)
            {
                Vector2Int actorCell;
                if (hit.TryGetComponent<AdventurerBase>(out var hero))
                    actorCell = hero.gridPos;
                else if (hit.TryGetComponent(out hitSummon))
                    actorCell = hitSummon.GridPos;
                else
                    continue;

                if (operation != OperationType.Reposition)
                    cell = actorCell;
                break;
            }
            return true;
        }

        public bool CanFocusAttack(SummonBase unit) => CanAimAtFocus(unit, null);

        private bool CanAimAtFocus(SummonBase unit, List<BlastingMaster.BlastCell> blastPreview) =>
            operation == OperationType.None && FocusTarget && unit is not ProtectiveMage &&
            TryAimSkill(unit, FocusTarget.gridPos, FocusTarget, blastPreview);

        private bool CanAct(SummonBase unit) =>
            isActiveAndEnabled && isPlayerTurn && Module.LoadController<FightController>().CanIssuePlayerCommand &&
            unit && unit.isActiveAndEnabled && !unit.IsAttacked && unit.CurrentHp > 0 &&
            Summons.GetSummon(unit.GridPos) == unit;

        private bool CanUseSkillOn(SummonBase unit, Vector2Int cell, AdventurerBase requiredTarget = null) =>
            TryAimSkill(unit, cell, requiredTarget, null);

        private bool TryAimSkill(SummonBase unit, Vector2Int cell, AdventurerBase requiredTarget,
            List<BlastingMaster.BlastCell> blastPreview)
        {
            if (unit is BlastingMaster mage)
                return isActiveAndEnabled && isPlayerTurn && (!requiredTarget || requiredTarget == AdventurerController.LivingAt(cell)) &&
                       mage.TryGetBlastCells(cell, blastPreview);

            if (!CanAct(unit) || unit.GridPos.y != cell.y ||
                Mathf.Abs(unit.GridPos.x - cell.x) > unit.SummonData.attackRadius)
                return false;

            var tile = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(cell);
            if (unit is ProtectiveMage)
                return tile != null;
            if (tile is not { IsHaveCharacter: true, CharacterType: CharacterType.Adventurer })
                return false;
            var victim = AdventurerController.LivingAt(cell);
            return victim && (!requiredTarget || requiredTarget == victim);
        }

        private void SetFocusTarget(AdventurerBase target)
        {
            if (ReferenceEquals(FocusTarget, target))
                return;

            if (FocusTarget)
                FocusTarget.onViewRemove -= ClearFocusTarget;
            FocusTarget = target;
            if (FocusTarget)
                FocusTarget.onViewRemove += ClearFocusTarget;
            EventManager.TriggerEvent(EventConst.FocusTargetChanged, target);
        }

        private void ClearFocusTarget() => SetFocusTarget(null);

        private void OnMagicBookOpen(int _)
        {
            if (!isPlayerTurn || operation != OperationType.None || IsResolvingPlayerSkill)
                return;

            ClearFocusTarget();
            operation = OperationType.ReleaseMagic;
        }

        private void CancelOperation()
        {
            ClearFocusTarget();
            if (operation == OperationType.ReleaseMagic)
                EventManager.TriggerEvent(EventConst.MagicBookClose);
            else if (ControlsSummon)
                FinishSummonControl();
        }

        private void RecycleSelectedSummon()
        {
            if (!ControlsSummon || !selectedSummon || !isPlayerTurn || Time.timeScale == 0 || IsResolvingPlayerSkill)
                return;

            Module.LoadController<SummonsController>().DeleteSummon(selectedSummon.GridPos);
            FinishSummonControl();
        }

        private void FinishSummonControl()
        {
            ClearOperation();
            EventManager.TriggerEvent(EventConst.ControlSummonAttackEnd);
        }

        private void EndPlayerTurn()
        {
            isPlayerTurn = false;
            ClearOperation();
        }

        private void ClearOperation()
        {
            ClearFocusTarget();
            blastAim.Clear();
            operation = OperationType.None;
            selectedSummon = null;
            pendingSelection = null;
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            var system = EventSystem.current;
            if (!system)
                return false;
            if (pointerEventSystem != system)
            {
                pointerEventSystem = system;
                pointerEvent = new PointerEventData(system);
            }

            pointerEvent.Reset();
            pointerEvent.position = screenPosition;
            system.RaycastAll(pointerEvent, hits);
            foreach (var hit in hits)
            {
                if (hit.module is GraphicRaycaster)
                    return true;
            }
            return false;
        }

        private void Show(Sprite icon)
        {
            if (!icon)
                return;

            if (!vectorCursor)
            {
                if (legacyCursor && legacyCursor.sprite != icon)
                    legacyCursor.sprite = icon;
                return;
            }

            if (vectorCursor.sprite == icon)
                return;
            var hotspot = cursorRect.position;
            vectorCursor.sprite = icon;
            cursorRect.pivot = icon.pivot / icon.rect.size;
            cursorRect.position = hotspot;
        }
    }
}
