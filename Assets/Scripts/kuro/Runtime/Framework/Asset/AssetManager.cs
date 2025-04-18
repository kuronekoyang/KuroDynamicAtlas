using System.Threading;
using System.Threading.Tasks;

namespace kuro
{
    // 我是假装的
    public class AssetManager : ManagerBase<AssetManager>
#if UNITY_EDITOR
        , IEditorManager
#endif
    {
        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
        }

        protected override void OnDestroy()
        {
        }

        public async ValueTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            throw new System.NotImplementedException();
#endif
        }

        public void UnloadResource(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            // 假装的
#else
            throw new System.NotImplementedException();
#endif
        }
    }
}