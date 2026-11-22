using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using XFGameFramework;

namespace Games.MainModule.UI.LoadingPanel
{
    public class LoadingPanel : Panel
    {
        private const float CoverDuration = 0.16f;
        private const float RevealDuration = 0.24f;
        private const float WaitingDelay = 0.55f;
        private const float WaitingFade = 0.12f;
        private static readonly int CoverageId = Shader.PropertyToID("_Coverage");

        [SerializeField] private Image curtain;
        [SerializeField] private CanvasGroup waiting;

        private Material curtainMaterial;
        private bool isWaiting;
        private float waitStarted;

        public override bool AlwaysInFront => true;

        protected override void Awake()
        {
            base.Awake();
            if (curtain != null && curtain.material.HasProperty(CoverageId))
            {
                curtainMaterial = new Material(curtain.material);
                curtain.material = curtainMaterial;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            isWaiting = false;
            if (curtainMaterial != null)
                curtainMaterial.SetFloat(CoverageId, 0f);
            if (waiting != null)
                waiting.alpha = 0f;
        }

        protected override void OnDestroy()
        {
            if (curtainMaterial != null)
                Destroy(curtainMaterial);
            base.OnDestroy();
        }

        public IEnumerator Cover() => AnimateTo(1f, CoverDuration);

        public void BeginWaiting()
        {
            waitStarted = Time.unscaledTime;
            isWaiting = true;
        }

        public IEnumerator Reveal()
        {
            isWaiting = false;
            yield return AnimateTo(0f, RevealDuration);
        }

        private void Update()
        {
            if (!isWaiting || waiting == null || Time.unscaledTime - waitStarted < WaitingDelay)
                return;

            // A stalled load frame must not make the waiting label jump to full opacity.
            float step = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / WaitingFade;
            waiting.alpha = Mathf.MoveTowards(waiting.alpha, 1f, step);
        }

        private IEnumerator AnimateTo(float target, float duration)
        {
            if (curtainMaterial == null)
                yield break;

            float start = curtainMaterial.GetFloat(CoverageId);
            float labelAlpha = waiting != null ? waiting.alpha : 0f;
            double previousFrame = Time.realtimeSinceStartupAsDouble;
            float elapsed = 0f;
            float t = 0f;
            while (t < 1f)
            {
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                // A scene activation hitch must not skip the visible part of the wipe.
                elapsed += Mathf.Min((float)(now - previousFrame), 1f / 30f);
                previousFrame = now;
                t = Mathf.Clamp01(elapsed / duration);
                curtainMaterial.SetFloat(CoverageId, Mathf.SmoothStep(start, target, t));
                if (waiting != null && labelAlpha > 0f)
                    waiting.alpha = labelAlpha * (1f - Mathf.Clamp01(3f * t));
            }

            curtainMaterial.SetFloat(CoverageId, target);
            if (waiting != null)
                waiting.alpha = 0f;
        }
    }
}
