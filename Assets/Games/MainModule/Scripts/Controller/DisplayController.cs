using System;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.Scripts.Controller
{
    public sealed class DisplayController : XFGameFramework.Controller
    {
        private const string FullscreenKey = "Display.Fullscreen";
        private const string WidthKey = "Display.WindowWidth";
        private const string HeightKey = "Display.WindowHeight";

        public static bool Supported => Application.platform == RuntimePlatform.WindowsEditor ||
                                        Application.platform == RuntimePlatform.WindowsPlayer;
        private static bool InPlayer => Application.platform == RuntimePlatform.WindowsPlayer;

        private WindowSizes sizes;
        private Vector2Int workArea;
        private int windowDpi;
        private bool waitingForNative;
        private bool persistChanges;
        private double nextPoll;
        private double requestDeadline;

        public bool Fullscreen { get; private set; }
        public Vector2Int WindowSize { get; private set; }
        public Vector2Int DesktopSize { get; private set; }
        public event Action Changed;

        public override void OnInit()
        {
            ReadMonitor();
            WindowSize = sizes.Default;
            if (!InPlayer) return;

            persistChanges = !Array.Exists(Environment.GetCommandLineArgs(), IsDisplayArgument);
            if (persistChanges)
            {
                WindowSize = sizes.Restore(new Vector2Int(PlayerPrefs.GetInt(WidthKey), PlayerPrefs.GetInt(HeightKey)));
                Fullscreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
                ApplyNative();
            }
            else
            {
                Fullscreen = Screen.fullScreen;
                if (!Fullscreen && ValidClientSize(ClientSize)) WindowSize = ClientSize;
                else WindowSize = sizes.Restore(new Vector2Int(PlayerPrefs.GetInt(WidthKey), PlayerPrefs.GetInt(HeightKey)));
            }
            UpdateManager.AddUpdateEvent(Poll);
            Application.quitting += SaveOnQuit;
        }

        public override void OnExit()
        {
            if (!InPlayer) return;
            UpdateManager.RemoveUpdateEvent(Poll);
            Application.quitting -= SaveOnQuit;
        }

        private static bool IsDisplayArgument(string argument) =>
            argument.Equals("-screen-width", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-screen-height", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-screen-fullscreen", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-window-mode", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-popupwindow", StringComparison.OrdinalIgnoreCase);

        private static Vector2Int ClientSize => new(Screen.width, Screen.height);
        private static bool ValidClientSize(Vector2Int size) => size.x > 1 && size.y > 1;

        private bool ReadMonitor()
        {
            var display = Screen.mainWindowDisplayInfo;
            var desktop = new Vector2Int(display.width, display.height);
            if (!ValidClientSize(desktop))
                desktop = new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
            if (!ValidClientSize(desktop))
                desktop = new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            var area = display.workArea.size;
            if (!ValidClientSize(area)) area = desktop;
            int dpi = Mathf.Max(96, Mathf.RoundToInt(Screen.dpi));
            if (sizes != null && desktop == DesktopSize && area == workArea && dpi == windowDpi) return false;
            DesktopSize = desktop;
            workArea = area;
            windowDpi = dpi;
            sizes = new WindowSizes(area, dpi);
            return true;
        }

        public Vector2Int AdjacentSize(bool larger) => sizes.Adjacent(WindowSize, larger);

        public void SetFullscreen(bool fullscreen)
        {
            if (fullscreen == Fullscreen) return;
            ReadMonitor();
            if (InPlayer && !Fullscreen && !waitingForNative && !Screen.fullScreen && ValidClientSize(ClientSize))
                WindowSize = ClientSize;
            WindowSize = sizes.Restore(WindowSize);
            Fullscreen = fullscreen;
            ApplyChoice();
        }

        public void StepWindowSize(bool larger)
        {
            if (Fullscreen) return;
            ReadMonitor();
            var next = AdjacentSize(larger);
            if (next == WindowSize) return;
            WindowSize = next;
            ApplyChoice();
        }

        private void ApplyChoice()
        {
            if (InPlayer)
            {
                persistChanges = true;
                ApplyNative();
                Save(true);
            }
            Changed?.Invoke();
        }

        private void ApplyNative()
        {
            var target = Fullscreen ? DesktopSize : WindowSize;
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            // A different request may already be queued for the end of this frame.
            if (!waitingForNative && Screen.fullScreenMode == mode && ClientSize == target) return;
            Screen.SetResolution(target.x, target.y, mode);
            waitingForNative = true;
            requestDeadline = Time.realtimeSinceStartupAsDouble + 1.5;
        }

        private void Poll()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextPoll) return;
            nextPoll = now + .12;
            bool monitorChanged = ReadMonitor();
            var actual = ClientSize;
            if (!ValidClientSize(actual)) return;

            bool nativeFull = Screen.fullScreen;
            if (waitingForNative && now < requestDeadline &&
                (nativeFull != Fullscreen || actual != (Fullscreen ? DesktopSize : WindowSize))) return;
            waitingForNative = false;

            if (nativeFull != Fullscreen)
            {
                // Alt+Enter bypasses the settings panel. Keep the previous window size on both paths.
                Fullscreen = nativeFull;
                WindowSize = sizes.Restore(WindowSize);
                ApplyNative();
                if (persistChanges) Save(true);
                Changed?.Invoke();
                return;
            }
            if (monitorChanged && (Fullscreen || actual.x > workArea.x || actual.y > workArea.y))
            {
                WindowSize = sizes.Restore(WindowSize);
                ApplyNative();
            }
            else if (!Fullscreen && actual != WindowSize)
            {
                WindowSize = actual;
                if (persistChanges) Save(false);
                Changed?.Invoke();
                return;
            }
            if (monitorChanged) Changed?.Invoke();
        }

        private void SaveOnQuit()
        {
            if (!persistChanges) return;
            if (!waitingForNative)
            {
                Fullscreen = Screen.fullScreen;
                if (!Fullscreen && ValidClientSize(ClientSize)) WindowSize = ClientSize;
            }
            Save(true);
        }

        private void Save(bool flush)
        {
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, WindowSize.x);
            PlayerPrefs.SetInt(HeightKey, WindowSize.y);
            if (flush) PlayerPrefs.Save();
        }
    }
}
