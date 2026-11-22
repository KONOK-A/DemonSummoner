using UnityEngine;

namespace Games.MainModule.Scripts.Utility
{
    internal static class AnimationEasing
    {
        // The weighted tangents reproduce cubic-bezier(0.23, 1, 0.32, 1).
        private static readonly AnimationCurve EaseOutCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1f / 0.23f, 0f, 0.23f) { weightedMode = WeightedMode.Out },
            new Keyframe(1f, 1f, 0f, 0f, 0.68f, 0f) { weightedMode = WeightedMode.In });

        public static float EaseOut(float progress) => EaseOutCurve.Evaluate(progress);
    }
}
