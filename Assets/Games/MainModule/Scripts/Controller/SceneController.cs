using System;
using System.Collections;
using Games.MainModule.UI.LoadingPanel;
using UnityEngine;
using UnityEngine.SceneManagement;
using XFABManager;
using XFGameFramework;

namespace Games.MainModule.Scripts.Controller
{
    public class SceneController : XFGameFramework.Controller
    {
        private const string MenuScenePath = "Assets/Games/MainModule/Scenes/Start.unity";
        private bool isTransitioning;
        private bool destinationReady;
        private IEnumerator pendingSceneLoad;

        public void PlayTransition(Action switchView)
        {
            if (!isTransitioning)
                CoroutineStarter.Start(TransitionExecute(switchView));
        }

        public void LoadMenuScene(Action showMenu)
        {
            if (SceneManager.GetActiveScene().path == MenuScenePath)
            {
                CompleteMenuLoad(showMenu);
                return;
            }

            QueueSceneLoad(LoadMenuSceneExecute(showMenu));
        }

        public void LoadScene(string sceneName, Action onLoaded, Action onFailed, Func<IEnumerator> prepare = null)
        {
            QueueSceneLoad(LoadSceneExecute(sceneName, onLoaded, onFailed, prepare));
        }

        private void QueueSceneLoad(IEnumerator sceneLoad)
        {
            pendingSceneLoad = sceneLoad;
            if (!isTransitioning)
                PlayTransition(null);
        }

        private IEnumerator TransitionExecute(Action switchView)
        {
            isTransitioning = true;
            destinationReady = false;
            LoadingPanel loadingPanel = null;
            try
            {
                loadingPanel = Module.LoadPanel<LoadingPanel>(UIType.DontDestroyUI);
                if (loadingPanel != null)
                    yield return loadingPanel.Cover();

                // Let the opaque curtain render before unloading the outgoing view.
                yield return null;
                switchView?.Invoke();
                while (!destinationReady)
                {
                    var sceneLoad = pendingSceneLoad;
                    if (sceneLoad == null)
                    {
                        yield return null;
                        continue;
                    }

                    // A failed battle load can schedule a menu load under the same curtain.
                    pendingSceneLoad = null;
                    if (loadingPanel != null)
                        loadingPanel.BeginWaiting();
                    yield return sceneLoad;
                }

                // Reveal waits a frame before opening, so the destination renders under full cover.
                if (loadingPanel != null)
                    yield return loadingPanel.Reveal();
            }
            finally
            {
                pendingSceneLoad = null;
                isTransitioning = false;
                if (loadingPanel != null)
                    loadingPanel.Close();
            }
        }

        private IEnumerator LoadMenuSceneExecute(Action showMenu)
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(MenuScenePath);
            if (buildIndex >= 0)
                yield return SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            else
                Debug.LogError($"The main menu scene is missing from the build: {MenuScenePath}");

            CompleteMenuLoad(showMenu);
        }

        private void CompleteMenuLoad(Action showMenu)
        {
            destinationReady = true;
            showMenu();
        }

        private IEnumerator LoadSceneExecute(string sceneName, Action onLoaded, Action onFailed, Func<IEnumerator> prepare)
        {
            if (prepare != null)
                yield return prepare();
            var request = AssetBundleManager.LoadSceneAsynchrony(Module.ProjectName, sceneName, LoadSceneMode.Single);
            yield return request;
            if (!string.IsNullOrEmpty(request.error))
            {
                Debug.LogError($"Failed to load scene '{sceneName}': {request.error}");
                onFailed?.Invoke();
                yield break;
            }
            try
            {
                onLoaded();
                destinationReady = true;
            }
            catch (Exception error)
            {
                Debug.LogError($"Failed to initialize scene '{sceneName}'.");
                Debug.LogException(error);
                onFailed?.Invoke();
            }
        }
    }
}
