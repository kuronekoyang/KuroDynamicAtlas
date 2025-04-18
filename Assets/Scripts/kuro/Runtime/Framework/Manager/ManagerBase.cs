using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace kuro
{
    public interface IManager
    {
        ValueTask InitializeAsync(CancellationToken cancellationToken);
        void OnAfterInitialize();
        void Destroy();
    }

#if UNITY_EDITOR
    public interface IEditorManager : IManager
    {
    }
#endif

    public interface IManagerUpdate
    {
        void Update();
    }

    public abstract class ManagerBase<T> : IManager where T : ManagerBase<T>, new()
    {
        protected bool Initialized = false;

        public static T Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected set;
        }

        public async ValueTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (Initialized)
                return;
            await OnInitializeAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            Instance = (T)this;
            Initialized = true;
        }

        public virtual void OnAfterInitialize()
        {
        }

        public void Destroy()
        {
            if (!Initialized)
                return;
            OnDestroy();
            Initialized = false;
            Instance = null;
        }

        protected abstract ValueTask OnInitializeAsync(CancellationToken cancellationToken);
        protected abstract void OnDestroy();

        public static async ValueTask<bool> WaitInstanceAsync(int count, CancellationToken cancellationToken)
        {
            for (int i = 0; i < count; ++i)
            {
                if (Instance != null)
                    return true;
                await Task.Yield();
                if (cancellationToken.IsCancellationRequested)
                    return false;
            }

            return false;
        }
    }
}