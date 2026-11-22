using System.Collections.Generic;
using Game;
using Games.MainModule.Scripts.Model;
using Games.MainModule.View.Adventurer;
using UnityEngine;

namespace Games.MainModule.View.VFX
{
    // The grid owns the effects, so a scene change clears every outstanding stroke.
    public sealed class SkirmishEffects : MonoBehaviour
    {
        private enum Motif { Cut, ReturnCut, Intercept, Ward, Shatter }

        private sealed class Stroke
        {
            public Transform root;
            public MeshRenderer ink;
            public SpriteRenderer shield;
            public Motif motif;
            public float elapsed, duration;
            public Vector3 start, end;
        }

        private sealed class Ward
        {
            public AdventurerBase owner;
            public MeshRenderer[] panes;
            public float age;
        }

        [SerializeField] private Material inkMaterial;
        [SerializeField] private Sprite guardKite;
        private static SkirmishEffects current;
        private readonly List<Stroke> active = new();
        private readonly Stack<Stroke> free = new();
        private readonly List<Ward> wards = new();
        private MaterialPropertyBlock properties;
        private static readonly int MotifId = Shader.PropertyToID("_Motif");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private Mesh quad;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }
        private void OnEnable() => current = this;
        private void OnDisable()
        {
            if (current == this) current = null;
            for (int i = active.Count - 1; i >= 0; i--) Retire(i);
            foreach (var ward in wards)
                foreach (var pane in ward.panes)
                    if (pane) Destroy(pane.gameObject);
            wards.Clear();
        }

        public static void PlayIntercept(SummonBase guard, SummonBase ally)
        {
            if (!current || !guard || !ally) return;
            var start = guard.transform.position + new Vector3(.26f, .42f);
            var end = ally.transform.position + new Vector3(-.34f, .52f);
            var cue = current.Spawn(Motif.Intercept, start, .52f, new Vector2(.96f, .98f));
            cue.end = end;
            current.Paint(cue);
        }

        public static void PlaySlash(SummonBase attacker, AdventurerBase target, bool returning)
        {
            if (!current || !attacker || !target) return;
            var contact = Torso(target) + Vector3.right * .08f;
            float facing = attacker.GridPos.x >= target.gridPos.x ? 1f : -1f;
            var size = new Vector2((returning ? 1.44f : 1.29f) * facing, returning ? 1.15f : .97f);
            current.Spawn(returning ? Motif.ReturnCut : Motif.Cut, contact, returning ? .39f : .30f, size);
        }

        public static void ShowSacredWard(AdventurerBase unit, int charges)
        {
            if (!current || !unit || charges <= 0) return;
            foreach (var existing in current.wards)
                if (existing.owner == unit) { existing.age = 0f; return; }
            var ward = new Ward { owner = unit, panes = new MeshRenderer[2] };
            for (int i = 0; i < ward.panes.Length; i++)
            {
                var pane = current.MakeInk(i == 0 ? "Sacred ward, inner" : "Sacred ward, outer", 18 + i);
                pane.transform.localScale = new Vector3(.36f, .70f, 1f);
                pane.transform.rotation = Quaternion.Euler(0, 0, -5f - i * 3f);
                ward.panes[i] = pane;
            }
            current.wards.Add(ward);
            current.PaintWard(ward, charges);
        }

        public static void BreakSacredWard(AdventurerBase unit, int remaining)
        {
            if (!current || !unit) return;
            int slot = Mathf.Clamp(remaining, 0, 1);
            current.Spawn(Motif.Shatter, WardPoint(unit, slot), .32f, new Vector2(.92f, 1.02f));
            foreach (var ward in current.wards)
                if (ward.owner == unit) { current.PaintWard(ward, remaining); break; }
        }

        private static Vector3 Torso(AdventurerBase unit)
        {
            var b = unit.Renderer.bounds;
            return new Vector3(unit.transform.position.x, Mathf.Lerp(b.min.y, b.max.y, .53f), 0);
        }

        private static Vector3 WardPoint(AdventurerBase unit, int slot) =>
            Torso(unit) + new Vector3(.23f + .22f * slot, .08f - .15f * slot);

        private void PaintWard(Ward ward, int charges)
        {
            for (int i = 0; i < ward.panes.Length; i++)
            {
                var pane = ward.panes[i];
                pane.enabled = i < charges;
                if (!pane.enabled) continue;
                float phase = Mathf.Clamp01((ward.age - .045f * i) / .23f);
                pane.transform.position = WardPoint(ward.owner, i) + Vector3.down * ((1f-phase) * .09f);
                SetInk(pane, Motif.Ward, phase);
            }
        }

        private MeshRenderer MakeInk(string label, int order)
        {
            var obj = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(transform, false);
            obj.GetComponent<MeshFilter>().sharedMesh = quad;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = inkMaterial;
            renderer.sortingLayerName = "Front";
            renderer.sortingOrder = order;
            return renderer;
        }

        private Stroke Spawn(Motif motif, Vector3 at, float duration, Vector2 size)
        {
            Stroke cue;
            if (free.Count > 0) cue = free.Pop();
            else
            {
                var ink = MakeInk("Combat stroke", 24);
                var kite = new GameObject("Guard kite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                kite.transform.SetParent(ink.transform, false);
                kite.sprite = guardKite;
                kite.sortingLayerName = "Front";
                kite.sortingOrder = 23;
                cue = new Stroke { root = ink.transform, ink = ink, shield = kite };
            }
            cue.root.gameObject.SetActive(true);
            cue.root.localScale = new Vector3(size.x, size.y, 1);
            cue.root.rotation = Quaternion.identity;
            cue.motif = motif; cue.duration = duration; cue.elapsed = 0;
            cue.start = cue.end = at;
            cue.shield.enabled = motif == Motif.Intercept;
            active.Add(cue);
            Paint(cue);
            return cue;
        }

        private void SetInk(MeshRenderer renderer, Motif motif, float phase)
        {
            properties.SetFloat(MotifId, (int)motif);
            properties.SetFloat(PhaseId, phase);
            renderer.SetPropertyBlock(properties);
        }

        private void Paint(Stroke cue)
        {
            float p = Mathf.Clamp01(cue.elapsed / cue.duration);
            cue.root.position = cue.start;
            if (cue.motif == Motif.Intercept)
            {
                float reach = Mathf.SmoothStep(0f, 1f, p / .23f);
                float recoil = Mathf.SmoothStep(0f, 1f, (p - .53f) / .47f);
                cue.root.position = cue.end;
                cue.shield.transform.position = Vector3.Lerp(cue.start, cue.end, reach * (1f - recoil));
                cue.shield.transform.localRotation = Quaternion.Euler(0,0,Mathf.Lerp(-14, 10, reach) - 10 * recoil);
                float turn = Mathf.Lerp(.63f, 1f, reach) - .30f * recoil;
                cue.shield.transform.localScale = new Vector3(.81f * turn, .81f, 1);
                cue.shield.color = new Color(1, 1, 1, 1f - Mathf.SmoothStep(0, 1, (p-.70f)/.30f));
            }
            SetInk(cue.ink, cue.motif, p);
        }

        private void Retire(int index)
        {
            var cue = active[index];
            cue.root.gameObject.SetActive(false);
            active.RemoveAt(index);
            free.Push(cue);
        }

        private void LateUpdate()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var cue = active[i];
                cue.elapsed += Time.deltaTime;
                if (cue.elapsed >= cue.duration) Retire(i);
                else Paint(cue);
            }
            for (int i = wards.Count - 1; i >= 0; i--)
            {
                var ward = wards[i];
                if (ward.owner && ward.owner.isActiveAndEnabled && ward.owner.SacredWardCharges > 0 &&
                    ward.owner.Module?.GetModel<FightModel>() is { BattleEnd: false })
                {
                    ward.age += Time.deltaTime;
                    PaintWard(ward, ward.owner.SacredWardCharges);
                    continue;
                }
                foreach (var pane in ward.panes) Destroy(pane.gameObject);
                wards.RemoveAt(i);
            }
        }
    }
}
