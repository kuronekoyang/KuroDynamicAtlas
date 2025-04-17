using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace kuro
{
    public class GameApp : AppBase
    {
        public override async ValueTask InitializeAsync(CancellationToken cancellationToken)
        {
            await AddManager<AssetManager>(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            await AddManager<AtlasManager>(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            OnAfterInitialize();
        }

        public override void Destroy()
        {
            for (int i = _managerList.Count - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (_managerList[i] is IEditorManager)
                    continue;
#endif
                _managerList[i].Destroy();
            }

            _managerList.Clear();
            _updaterList.Clear();
        }
    }

    public class GameAppLauncher : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoadMethod()
        {
            var gameObject = new GameObject("kuro.GameAppLauncher");
            gameObject.AddComponent<GameAppLauncher>();
            DontDestroyOnLoad(gameObject);
        }

        private GameApp _app;

        private void Awake()
        {
            _app = new GameApp();
            _app.Initialize();

        }

        private void OnDestroy()
        {
            _app?.Destroy();
            _app = null;
        }

        private void Update()
        {
            _app.Update();
        }
    }
}