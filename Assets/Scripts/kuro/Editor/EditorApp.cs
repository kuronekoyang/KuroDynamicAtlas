using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace kuro
{
    public class EditorApp : AppBase
    {
        private static EditorApp s_app;
        private static bool s_isPlayingMode;


        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            s_isPlayingMode = EditorApplication.isPlayingOrWillChangePlaymode;
            s_app = new EditorApp();
            s_app.Initialize();

            EditorApplication.update += () => s_app?.Update();
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                s_app?.Destroy();
                s_app = null;
            };
            EditorApplication.playModeStateChanged += playMode =>
            {
                if (playMode == PlayModeStateChange.ExitingEditMode)
                {
                    s_isPlayingMode = true;
                }
                else if (playMode == PlayModeStateChange.EnteredEditMode)
                {
                    s_isPlayingMode = false;
                    s_app?.OnEnteredEditMode();
                }
            };
        }

        public override async ValueTask InitializeAsync(CancellationToken cancellationToken)
        {
            await AddManager<AssetManager>(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            await AddManager<AtlasManager>(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            await AddManager<EditorAtlasManager>(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            OnAfterInitialize();
        }

        public override void Update()
        {
            if (s_isPlayingMode)
                return;
            base.Update();
        }
    }
}