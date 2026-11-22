using Games.MainModule.Scripts.Controller;
using TMPro;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.UI.StartDescriptionPanel
{
    public class StartDescriptionPanel : Panel
    {
        public const string FromPauseMenu = "FromPauseMenu";

        [SerializeField] private RectTransform guideFrame;
        [SerializeField] private TextMeshProUGUI actionLabel;
        [SerializeField] private TextMeshProUGUI spaceHint;
        [SerializeField] private GameObject firstTurnPage;
        [SerializeField] private GameObject battlefieldPage;

        private bool ownsBattlePause;
        private float previousTimeScale;

        public override bool AlwaysInFront => true;

        protected override void OnEnable()
        {
            base.OnEnable();
            FitGuide();
        }

        public override void OnLoaded(LoadViewArgs args)
        {
            base.OnLoaded(args);

            bool fromPause = args.GetBool(FromPauseMenu);
            actionLabel.text = fromPause ? "返回暂停" : "开始守城";
            spaceHint.text = fromPause ? "空格可返回" : "空格可开始";
            ShowBattlefieldPage(fromPause);

            ownsBattlePause = !fromPause;
            if (ownsBattlePause)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!ownsBattlePause)
                return;

            ownsBattlePause = false;
            Module.LoadController<GameController>().SetFirstState(false);
            Time.timeScale = previousTimeScale;
        }

        public void ShowBattlefieldPage(bool battlefield)
        {
            firstTurnPage.SetActive(!battlefield);
            battlefieldPage.SetActive(battlefield);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Close();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ShowBattlefieldPage(false);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ShowBattlefieldPage(true);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            FitGuide();
        }

        private void FitGuide()
        {
            if (guideFrame == null)
                return;

            var screen = ((RectTransform)transform).rect;
            if (screen.width <= 0f || screen.height <= 0f)
                return;

            const float margin = 48f;
            float width = (screen.width - 2f * margin) / guideFrame.rect.width;
            float height = (screen.height - 2f * margin) / guideFrame.rect.height;
            guideFrame.localScale = Vector3.one * Mathf.Clamp01(Mathf.Min(width, height));
        }
    }
}
