using System.Linq;
using UnityEngine;

namespace Games.MainModule.Scripts.Utility
{
    public sealed class WindowSizes
    {
        private static readonly Vector2Int[] Presets =
        {
            new(960, 540), new(1024, 576), new(1152, 648), new(1280, 720), new(1366, 768),
            new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160)
        };

        private readonly Vector2Int[] choices;
        private readonly Vector2Int workArea;
        public Vector2Int Limit { get; }
        public Vector2Int Default { get; }

        // The work area already excludes the taskbar. Allow space for the window borders and title bar.
        public WindowSizes(Vector2Int workArea, int dpi = 96)
        {
            this.workArea = workArea;
            float scale = Mathf.Max(1f, dpi / 96f);
            Limit = new Vector2Int(Mathf.Max(1, workArea.x - Mathf.CeilToInt(16 * scale)),
                Mathf.Max(1, workArea.y - Mathf.CeilToInt(48 * scale)));
            choices = Presets.Where(p => p.x <= Limit.x && p.y <= Limit.y).ToArray();
            if (choices.Length == 0)
                choices = new[] { Fit(Presets[0]) };
            Default = choices.Last(p => p.x <= 1280);
        }

        private Vector2Int Fit(Vector2Int size)
        {
            float scale = Mathf.Min(1f, Mathf.Min((float)Limit.x / size.x, (float)Limit.y / size.y));
            return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(size.x * scale), 1, Limit.x),
                Mathf.Clamp(Mathf.RoundToInt(size.y * scale), 1, Limit.y));
        }

        public Vector2Int Restore(Vector2Int saved)
        {
            if (saved.x <= 0 || saved.y <= 0)
                return Default;
            // A maximized or hand-sized client can use more of the work area than a preset.
            var fitted = saved.x <= workArea.x && saved.y <= workArea.y ? saved : Fit(saved);
            return fitted.x < Mathf.Min(320, Limit.x) || fitted.y < Mathf.Min(180, Limit.y) ? Default : fitted;
        }

        // Free resizing can leave the window between presets, including at a different aspect ratio.
        public Vector2Int Adjacent(Vector2Int current, bool larger)
        {
            long area = (long)current.x * current.y;
            for (int i = 0; i < choices.Length; i++)
            {
                var candidate = choices[larger ? i : choices.Length - 1 - i];
                int order = ((long)candidate.x * candidate.y).CompareTo(area);
                if (order == 0) order = candidate.x.CompareTo(current.x);
                if (larger ? order > 0 : order < 0)
                    return candidate;
            }
            return current;
        }
    }
}
