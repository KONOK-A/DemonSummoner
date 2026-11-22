using System.Collections.Generic;
using Game;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.View.Adventurer;
using UnityEngine;

namespace Games.MainModule.UI.CursorPanel
{
    [RequireComponent(typeof(CursorPanel))]
    public sealed class BattlefieldPreview : MonoBehaviour
    {
        [SerializeField] private Material floorInk;
        [SerializeField] private Sprite castingFlame;
        [SerializeField] private Sprite focusSigil;
        [SerializeField] private Material focusMaterial;

        private CursorPanel cursor;
        private Grid grid;
        private GameObject marks;
        private readonly Transform[] blast = new Transform[3];
        private readonly Transform[] playerBlast = new Transform[3];
        private readonly Transform[] steps = new Transform[2];
        private Transform halt;
        private Transform advanceLimit;
        private Transform casterSeal;
        private Transform groundSeal;
        private Transform focusMark;
        private MaterialPropertyBlock focusOutline;
        private readonly Dictionary<SummonBase, bool> focusAvailability = new();
        private AdventurerController adventurers;
        private SummonsController summons;
        private MapInfoController map;

        private void Awake() => cursor = GetComponent<CursorPanel>();

        private void LateUpdate()
        {
            if (cursor.Module?.GetModel<FightModel>() is not { BattleEnd: false })
            {
                if (marks && marks.activeSelf) marks.SetActive(false);
                RestoreSummonOutlines();
                return;
            }
            if (!marks && !CreateMarks()) return;
            if (!marks.activeSelf) marks.SetActive(true);

            Caster caster = null;
            foreach (var member in AdventurerController.Adventurers)
                if (member is Caster mage) { caster = mage; break; }
            DrawBlast(caster);
            DrawPlayerBlast();
            DrawSteps(cursor.RepositionSelection);
            DrawFocus(cursor.FocusTarget);

            Vector2Int front = default, stop = default, limit = default;
            bool showAdvance = cursor.isPlayerTurn && adventurers.GetAdvancePreview(out front, out stop, out limit);
            Place(halt, stop, showAdvance);
            Place(advanceLimit, limit, showAdvance && limit != stop);
        }

        private bool CreateMarks()
        {
            grid = FindFirstObjectByType<Grid>();
            if (!grid || !floorInk) return false;
            map = cursor.Module.LoadController<MapInfoController>();
            summons = cursor.Module.LoadController<SummonsController>();
            adventurers = cursor.Module.LoadController<AdventurerController>();
            marks = new GameObject("Battlefield marks");
            marks.transform.SetParent(grid.transform, false);
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            blast[0] = MakeMark("Blast left", quad, 0, false);
            blast[1] = MakeMark("Blast center", quad, 1, false);
            blast[2] = MakeMark("Blast right", quad, 0, true);
            playerBlast[0] = MakeMark("Player blast left", quad, 5, false);
            playerBlast[1] = MakeMark("Player blast center", quad, 6, false);
            playerBlast[2] = MakeMark("Player blast right", quad, 5, true);
            steps[0] = MakeMark("Step left", quad, 2, true);
            steps[1] = MakeMark("Step right", quad, 2, false);
            halt = MakeMark("Advance stop", quad, 3, false);
            advanceLimit = MakeMark("Advance limit", quad, 4, false);
            focusMark = MakeSeal("Focus target", "Ground", focusSigil, grid.cellSize.y * 0.39f, grid.cellSize.x * 0.98f);
            if (focusMaterial) focusMark.GetComponent<SpriteRenderer>().sharedMaterial = focusMaterial;
            focusOutline = new MaterialPropertyBlock();
            focusOutline.SetFloat("_OutlineWidth", 20f);
            focusOutline.SetFloat("_Light", 1f);
            focusOutline.SetColor("_OutlineColor", new Color(0.83f, 0.77f, 1f));
            casterSeal = MakeSeal("Chanting", "Front", castingFlame, 0.30f);
            groundSeal = MakeSeal("Blast seal", "Ground", castingFlame, 0.34f);
            return true;
        }

        private Transform MakeMark(string label, Mesh mesh, int style, bool mirror)
        {
            var obj = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(marks.transform, false);
            obj.transform.localScale = new Vector3(grid.cellSize.x * (mirror ? -1 : 1), grid.cellSize.y, 1f);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var paint = obj.GetComponent<MeshRenderer>();
            paint.sharedMaterial = floorInk;
            paint.sortingLayerName = "Ground";
            paint.sortingOrder = 60 + style;
            var values = new MaterialPropertyBlock();
            values.SetFloat("_Mark", style);
            paint.SetPropertyBlock(values);
            return obj.transform;
        }

        private Transform MakeSeal(string label, string layer, Sprite artwork, float height, float maxWidth = float.PositiveInfinity)
        {
            var obj = new GameObject(label, typeof(SpriteRenderer));
            obj.transform.SetParent(marks.transform, false);
            var icon = obj.GetComponent<SpriteRenderer>();
            icon.sprite = artwork;
            icon.sortingLayerName = layer;
            icon.sortingOrder = 70;
            if (artwork)
            {
                var size = artwork.bounds.size;
                obj.transform.localScale = Vector3.one * Mathf.Min(height / size.y, maxWidth / size.x);
            }
            return obj.transform;
        }

        private void DrawBlast(Caster caster)
        {
            var center = caster ? caster.ExplosionCenter : null;
            for (int i = 0; i < blast.Length; i++)
            {
                var cell = center.GetValueOrDefault() + Vector2Int.right * (i - 1);
                Place(blast[i], cell, center.HasValue && map.GetTileDetailsByGridPosition(cell) != null);
            }
            SetVisible(casterSeal, center.HasValue);
            SetVisible(groundSeal, center.HasValue);
            if (!center.HasValue) return;
            casterSeal.position = caster.hpBar.transform.position + Vector3.up * 0.23f;
            groundSeal.localPosition = grid.GetCellCenterLocal((Vector3Int)center.Value) + Vector3.down * 0.235f;
        }

        private void DrawPlayerBlast()
        {
            var cells = cursor.BlastAim;
            int next = 0;
            for (int i = 0; i < playerBlast.Length; i++)
            {
                if (next < cells.Count && cells[next].Offset + 1 == i)
                    Place(playerBlast[i], cells[next++].Position, true);
                else
                    SetVisible(playerBlast[i], false);
            }
        }

        private void DrawSteps(SummonBase selected)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                var cell = selected ? selected.GridPos + (i == 0 ? Vector2Int.left : Vector2Int.right) : default;
                Place(steps[i], cell, selected && summons.CanReposition(selected, cell));
            }
        }

        private void DrawFocus(AdventurerBase target)
        {
            Place(focusMark, target ? target.gridPos : default, target);
            if (!target)
            {
                RestoreSummonOutlines();
                return;
            }

            foreach (var unit in summons.GetAllSummonsOnScene().Values)
            {
                if (!unit || !unit.isActiveAndEnabled) continue;
                bool ready = cursor.CanFocusAttack(unit);
                if (focusAvailability.TryGetValue(unit, out bool previous) && previous == ready) continue;
                unit.Renderer.SetPropertyBlock(ready ? focusOutline : SummonBase.NormalPropertyBlock);
                focusAvailability[unit] = ready;
            }
        }

        private void RestoreSummonOutlines()
        {
            if (focusAvailability.Count == 0) return;
            foreach (var unit in focusAvailability.Keys)
                if (unit)
                    unit.Renderer.SetPropertyBlock(unit.IsAttacked ? SummonBase.NormalPropertyBlock : SummonBase.OutlinePropertyBlock);
            focusAvailability.Clear();
        }

        private void Place(Transform marker, Vector2Int cell, bool visible)
        {
            SetVisible(marker, visible);
            if (visible) marker.localPosition = grid.GetCellCenterLocal((Vector3Int)cell);
        }

        private static void SetVisible(Transform marker, bool visible)
        {
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        }

        private void OnDisable()
        {
            RestoreSummonOutlines();
            if (marks) Destroy(marks);
            marks = null;
        }
    }
}
