using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace kuro
{
    public abstract class AppBase : IManager, IManagerUpdate
#if UNITY_EDITOR
        , IEditorManager
#endif
    {
        protected List<IManager> _managerList = new();
        protected List<IManagerUpdate> _updaterList = new();

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= new();

        public void Initialize() => InitializeAsync(CancellationTokenSource.Token).Forget();

        public abstract ValueTask InitializeAsync(CancellationToken cancellationToken);

        public void OnAfterInitialize()
        {
            foreach (var manager in _managerList)
                manager.OnAfterInitialize();
        }

        protected async ValueTask<T> AddManager<T>(CancellationToken cancellationToken) where T : ManagerBase<T>, IManager, new()
        {
            var manager = ManagerBase<T>.Instance ?? new T();
            await manager.InitializeAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return null;

            _managerList.Add(manager);

            if (manager is IManagerUpdate updater)
                _updaterList.Add(updater);

            return manager;
        }

        public virtual void Destroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            for (int i = _managerList.Count - 1; i >= 0; i--)
                _managerList[i].Destroy();
            _managerList.Clear();
            _updaterList.Clear();
        }

        public virtual void Update()
        {
            foreach (var manager in _updaterList)
                manager.Update();
        }

#if UNITY_EDITOR
        public void OnEnteredEditMode()
        {
            foreach (var manager in _managerList)
            {
                if (manager is IEditorManager editorManager)
                    editorManager.OnEnteredEditMode();
            }
        }
#endif
    }
}