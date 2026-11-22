using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.View.VFX
{
    [RequireComponent(typeof(Animator))]
    public sealed class VFXEffect : XFGameFramework.View
    {
        [SerializeField] private float heightAboveCell = 1f;
        private Vector3 authoredScale;
        private Animator animator;

        private void Awake()
        {
            authoredScale = transform.localScale;
            animator = GetComponent<Animator>();
        }

        public static VFXEffect PlayAt(Module module, string assetName, Vector2Int cell, float scale = 1f)
        {
            var effect = module.LoadView<VFXEffect>(assetName, loadViewArgs: null);
            if (!effect) return null;
            effect.transform.position = new Vector3(cell.x + .5f, cell.y + effect.heightAboveCell, 0f);
            effect.transform.localScale = effect.authoredScale * scale;
            effect.animator.Play(0, 0, 0f);
            effect.animator.Update(0f);
            return effect;
        }

        public static void PlayWard(Module module, string assetName, Vector2Int cell, float healthBarY)
        {
            var effect = PlayAt(module, assetName, cell);
            if (!effect) return;
            // Leave a gap below the health bar and keep adjacent arches within their tiles.
            const float archHeight = 1.5f;
            float height = Mathf.Clamp((healthBarY - effect.transform.position.y - .14f) / archHeight, .30f, 1.6f);
            effect.transform.localScale = Vector3.Scale(effect.authoredScale, new Vector3(Mathf.Min(height, .96f), height, 1f));
        }

        public override void OnRemove()
        {
            transform.localScale = authoredScale;
            base.OnRemove();
        }
    }
}
