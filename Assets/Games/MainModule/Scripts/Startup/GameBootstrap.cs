using System.Collections;
using Games.MainModule.UI.MainMenuPanel;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using XFABManager;
using XFGameFramework;

namespace Games.MainModule.Scripts.Startup
{
    [MovedFrom(true, sourceNamespace: "Games.MainModule.Scripts", sourceClassName: "Main")]
    public class GameBootstrap : MonoBehaviour
    {
        private const string ResourceProject = nameof(MainModule);

        [SerializeField] private StartupErrorPanel tipPanel;
        [SerializeField] private StartupIdent intro;

        public IEnumerator Start()
        {
            if (ModuleManager.GetModule<MainModule>() != null)
                yield break;

            if (intro)
            {
                intro.gameObject.SetActive(true);
                // Present the cover before starting resource preparation on the next frame.
                yield return null;
            }

            if (AssetBundleManager.GetProfile(ResourceProject).updateModel != UpdateMode.LOCAL)
            {
                ShowStartupFailure("主模块未配置为本地资源模式。", canRetry: false);
                yield break;
            }

            while (true)
            {
                var resources = AssetBundleManager.ReadyRes(ResourceProject);
                yield return resources;

                if (string.IsNullOrEmpty(resources.error))
                    break;

                ShowStartupFailure(resources.error, canRetry: true);
                yield return new WaitWhile(() => tipPanel.gameObject.activeSelf);
            }

            var startup = ModuleManager.StartUpModule<MainModule>();
            yield return startup;
            if (!string.IsNullOrEmpty(startup.error))
            {
                ShowStartupFailure(startup.error, canRetry: false);
                yield break;
            }

            if (intro && intro.isActiveAndEnabled)
            {
                var module = ModuleManager.GetModule<MainModule>();
                yield return new WaitUntil(() => module.GetView<MainMenuPanel>() is { isActiveAndEnabled: true });
                yield return intro.RevealMenu();
            }
        }

        private void ShowStartupFailure(string reason, bool canRetry)
        {
            if (intro) intro.gameObject.SetActive(false);
            Debug.LogError($"游戏启动失败：{reason}", this);
            tipPanel.ShowTips(
                "启动失败",
                "游戏资源缺失或无法读取，请重新安装游戏。",
                onConfirm: canRetry ? tipPanel.Hide : () => Application.Quit(),
                onCancel: () => Application.Quit(),
                isShowCancel: canRetry,
                btnConfirmText: canRetry ? "重试" : "退出游戏",
                btnCancelText: "退出游戏");
        }
    }
}

