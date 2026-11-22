using System.Collections;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.UI
{
    public abstract class FadingModalPanel : Panel
    {
        private const float OpenDuration = 0.25f;
        private const float CloseDuration = 0.20f;

        private CanvasGroup canvasGroup;
        private Coroutine fade;
        private bool closing;

        protected override void Awake()
        {
            base.Awake();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            closing = false;
            if (canvasGroup == null)
                return;

            canvasGroup.interactable = true;
            canvasGroup.alpha = 0f;
            fade = StartCoroutine(FadeTo(1f, OpenDuration));
        }

        protected override void OnDisable()
        {
            StopFade();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
            base.OnDisable();
        }

        protected void CloseWithFade()
        {
            if (closing)
                return;
            if (canvasGroup == null || !isActiveAndEnabled || canvasGroup.alpha <= 0f)
            {
                Close();
                return;
            }

            closing = true;
            canvasGroup.interactable = false;
            StopFade();
            fade = StartCoroutine(FadeTo(0f, CloseDuration * canvasGroup.alpha));
        }

        private void StopFade()
        {
            if (fade == null)
                return;

            StopCoroutine(fade);
            fade = null;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = canvasGroup.alpha;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(start, target, AnimationEasing.EaseOut(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = target;
            fade = null;
            if (closing)
                Close();
        }
    }
}
