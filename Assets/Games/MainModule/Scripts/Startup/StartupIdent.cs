using System.Collections;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Games.MainModule.Scripts.Startup
{
    public sealed class StartupIdent : MonoBehaviour
    {
        private const float HoldUntil = 1.46f;
        private const float ReducedHold = 0.7f;
        private const float MenuFade = 0.26f;
        private const float SkipFade = 0.16f;
        private const float MaximumFrameStep = 0.05f;
        private static readonly int Reveal = Shader.PropertyToID("_Reveal");

        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RawImage emblem;
        [SerializeField] private CanvasGroup wordmark;

        private Material originalMaterial;
        private Material revealMaterial;
        private RectTransform wordmarkRect;
        private Vector2 wordmarkPosition;
        private float elapsed;
        private bool reducedMotion;
        private bool skipped;
        private bool dismissing;

        private void Awake()
        {
            originalMaterial = emblem.material;
            wordmarkRect = (RectTransform)wordmark.transform;
            wordmarkPosition = wordmarkRect.anchoredPosition;
        }

        private void OnEnable()
        {
            elapsed = 0f;
            skipped = false;
            dismissing = false;
            reducedMotion = ReducedMotionRequested();

            revealMaterial = new Material(originalMaterial);
            emblem.material = revealMaterial;
            overlay.alpha = 1f;
            Paint();
        }

        private void Update()
        {
            if (dismissing || skipped) return;

            skipped = Input.anyKeyDown;
            if (skipped) return;

            // Startup loading can make the first unscaled frame longer than the entire sweep.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFrameStep);
            Paint();
        }

        private void Paint()
        {
            float trace = reducedMotion ? 1f : Mathf.InverseLerp(.08f, .78f, elapsed);
            float title = reducedMotion ? 1f : AnimationEasing.EaseOut(Mathf.InverseLerp(.44f, .92f, elapsed));
            revealMaterial.SetFloat(Reveal, trace);
            wordmark.alpha = title;
            wordmarkRect.anchoredPosition = wordmarkPosition + Vector2.down * ((1f - title) * 9f);
        }

        // The menu has its own canvas and must be drawn behind the veil before the fade.
        public IEnumerator RevealMenu()
        {
            while (!skipped && elapsed < (reducedMotion ? ReducedHold : HoldUntil))
                yield return null;

            dismissing = true;
            yield return null;
            float duration = skipped || reducedMotion ? SkipFade : MenuFade;
            for (float time = 0f; time < duration; time += Mathf.Min(Time.unscaledDeltaTime, MaximumFrameStep))
            {
                overlay.alpha = 1f - AnimationEasing.EaseOut(time / duration);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            emblem.material = originalMaterial;
            if (revealMaterial)
                Destroy(revealMaterial);
            revealMaterial = null;
        }

        private static bool ReducedMotionRequested()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            const uint GetClientAreaAnimation = 0x1042;
            return SystemParametersInfo(GetClientAreaAnimation, 0, out int enabled, 0) && enabled == 0;
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint flags);
#endif
    }
}

