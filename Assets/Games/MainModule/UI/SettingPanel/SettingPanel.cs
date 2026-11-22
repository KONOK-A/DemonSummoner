using Games.MainModule.Scripts.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LoadViewArgs = XFGameFramework.LoadViewArgs;
using AudioType = Games.MainModule.Scripts.Utility.AudioType;

namespace Games.MainModule.UI.SettingPanel
{
    public class SettingPanel : FadingModalPanel
    {
        [SerializeField] private Toggle bgmToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private RectTransform frame;
        [SerializeField] private GameObject displayRows;
        [SerializeField] private GameObject windowedMark;
        [SerializeField] private GameObject fullscreenMark;
        [SerializeField] private TextMeshProUGUI sizeLabel;
        [SerializeField] private TextMeshProUGUI sizeValue;
        [SerializeField] private Button smaller;
        [SerializeField] private Button larger;

        private AudioController AudioController => Module.LoadController<AudioController>();
        private DisplayController display;

        public override void OnLoaded(LoadViewArgs args)
        {
            base.OnLoaded(args);
            bgmSlider.SetValueWithoutNotify(AudioController.GetBGMVolume());
            sfxSlider.SetValueWithoutNotify(AudioController.GetSFXVolume());
            bgmToggle.SetIsOnWithoutNotify(!AudioController.GetMute(AudioType.BGM));
            sfxToggle.SetIsOnWithoutNotify(!AudioController.GetMute(AudioType.Sound));

            FitFrame();
            displayRows.SetActive(DisplayController.Supported);
            if (!DisplayController.Supported)
            {
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 544f);
                FitFrame();
                return;
            }
            display = Module.LoadController<DisplayController>();
            display.Changed += RefreshDisplay;
            RefreshDisplay();
        }

        private void OnRectTransformDimensionsChange() => FitFrame();

        private void FitFrame()
        {
            if (!frame) return;
            var room = ((RectTransform)transform).rect.size - new Vector2(48f, 48f);
            if (room.x <= 0f || room.y <= 0f) return;
            frame.localScale = Vector3.one * Mathf.Min(1f, Mathf.Min(room.x / frame.rect.width, room.y / frame.rect.height));
        }

        protected override void OnDisable()
        {
            if (display != null) display.Changed -= RefreshDisplay;
            display = null;
            base.OnDisable();
        }

        private void RefreshDisplay()
        {
            bool full = display.Fullscreen;
            windowedMark.SetActive(!full);
            fullscreenMark.SetActive(full);
            sizeLabel.text = full ? "桌面尺寸" : "窗口尺寸";
            var size = full ? display.DesktopSize : display.WindowSize;
            sizeValue.text = $"{size.x} × {size.y}";
            smaller.gameObject.SetActive(!full);
            larger.gameObject.SetActive(!full);
            smaller.interactable = !full && display.AdjacentSize(false) != size;
            larger.interactable = !full && display.AdjacentSize(true) != size;
        }

        public void SetFullscreen(bool fullscreen) => display.SetFullscreen(fullscreen);
        public void StepWindowSize(bool bigger) => display.StepWindowSize(bigger);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OnBtnCloseClick();
        }

        public void SetSfxVolume(float value) => AudioController.SetSFXVolume(value);
        public void SetBGMVolume(float value) => AudioController.SetBGMVolume(value);
        public void SetBgmMute(bool isOn) => AudioController.SetMute(AudioType.BGM, !isOn);
        public void SetSfxMute(bool isOn) => AudioController.SetMute(AudioType.Sound, !isOn);
        public void OnBtnCloseClick() => CloseWithFade();
    }
}
