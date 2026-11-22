using Games.MainModule.Scripts.Utility;
using UnityEngine;

namespace Games.MainModule.View.HealthBar
{
    [DisallowMultipleComponent]
    public sealed class HealthBarFill : MonoBehaviour
    {
        private const float Duration = 0.20f;
        private const float RecoveryHold = 0.12f;
        private const float RecoveryDuration = 0.28f;

        [SerializeField] private SpriteRenderer recoveryStrip;

        private float startRatio;
        private float targetRatio;
        private float elapsed = Duration;
        private float recoveryFrom;
        private float recoveryElapsed = RecoveryDuration;
        private Vector3 recoveryOrigin;
        private Vector3 recoveryScale;
        private float recoveryWidth;

        public static HealthBarFill Attach(Transform fill)
        {
            return fill.TryGetComponent(out HealthBarFill bar) ? bar : fill.gameObject.AddComponent<HealthBarFill>();
        }

        public void SetInstant(float ratio)
        {
            targetRatio = Mathf.Clamp01(ratio);
            ApplyRatio(targetRatio);
            elapsed = Duration;
            ClearRecovery();
            enabled = false;
        }

        public void SetTarget(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Approximately(ratio, targetRatio))
                return;

            if (recoveryStrip != null && ratio > targetRatio)
            {
                recoveryFrom = recoveryElapsed < RecoveryDuration
                    ? Mathf.Min(recoveryFrom, targetRatio)
                    : targetRatio;
                recoveryElapsed = 0f;
            }
            else
                ClearRecovery();

            startRatio = transform.localScale.x;
            targetRatio = ratio;
            elapsed = Mathf.Approximately(startRatio, targetRatio) ? Duration : 0f;
            if (elapsed == Duration)
                ApplyRatio(targetRatio);
            enabled = elapsed < Duration || recoveryElapsed < RecoveryDuration;
        }

        private void Awake()
        {
            if (recoveryStrip == null)
                return;

            recoveryOrigin = recoveryStrip.transform.localPosition;
            recoveryScale = recoveryStrip.transform.localScale;
            recoveryWidth = recoveryStrip.sprite.bounds.size.x * recoveryScale.x;
            ClearRecovery();
        }

        private void Update()
        {
            if (elapsed < Duration)
            {
                elapsed = Mathf.Min(elapsed + Time.deltaTime, Duration);
                float progress = AnimationEasing.EaseOut(elapsed / Duration);
                ApplyRatio(Mathf.Lerp(startRatio, targetRatio, progress));
            }

            if (recoveryElapsed < RecoveryDuration)
            {
                recoveryElapsed = Mathf.Min(recoveryElapsed + Time.deltaTime, RecoveryDuration);
                UpdateRecovery();
            }

            enabled = elapsed < Duration || recoveryElapsed < RecoveryDuration;
        }

        private void UpdateRecovery()
        {
            float width = Mathf.Min(transform.localScale.x, targetRatio) - recoveryFrom;
            recoveryStrip.enabled = width > 0f && recoveryElapsed < RecoveryDuration;
            if (!recoveryStrip.enabled)
                return;

            var display = recoveryStrip.transform;
            display.localPosition = recoveryOrigin + Vector3.right * (recoveryFrom * recoveryWidth);
            display.localScale = new Vector3(recoveryScale.x * width, recoveryScale.y, recoveryScale.z);

            float fade = Mathf.InverseLerp(RecoveryHold, RecoveryDuration, recoveryElapsed);
            var color = recoveryStrip.color;
            color.a = .88f * (1f - AnimationEasing.EaseOut(fade));
            recoveryStrip.color = color;
        }

        private void ClearRecovery()
        {
            recoveryElapsed = RecoveryDuration;
            if (recoveryStrip != null)
                recoveryStrip.enabled = false;
        }

        private void ApplyRatio(float ratio)
        {
            var scale = transform.localScale;
            scale.x = ratio;
            transform.localScale = scale;
        }
    }
}
