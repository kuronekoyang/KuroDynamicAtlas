using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.iOS;
using UnityEngine;
using UnityEngine.Pool;

namespace kuro
{
    public abstract class AtlasHandle
    {
        private static int s_id = 0;

        private readonly int _id = 0;
        private int _lifeRefCount = 0;
        private float _lifeTime = 0.0f;
        private bool _isTick = false;
        private bool _isNeeded = false;

        protected AtlasStatus _status = AtlasStatus.None;
        protected CancellationTokenSource _cancellationTokenSource;
        protected CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= new();
        protected readonly HashSet<DynamicSprite> _dynamicSprites = new();

        private int LifeRefCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lifeRefCount;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_lifeRefCount == value)
                    return;
                _lifeRefCount = value;
                RefreshNeeded();
            }
        }

        public float LifeTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lifeTime;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_lifeTime == value)
                    return;
                _lifeTime = value;
                RefreshNeeded();
                RefreshTick();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshTick()
        {
            bool value = _lifeTime > 0.0f;
            if (_isTick == value)
                return;

            _isTick = value;
            if (value)
                AtlasManager.RegisterTick(this);
            else
                AtlasManager.UnRegisterTick(this);
        }

        public bool IsNeeded
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isNeeded;
        }

        public HashSet<DynamicSprite> DynamicSprites
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dynamicSprites;
        }

        public int Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _id;
        }

        protected AtlasHandle()
        {
            _id = ++s_id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CleanupInvalidDynamicSprites()
        {
            var destroyList = ListPool<DynamicSprite>.Get();
            destroyList.AddRange(_dynamicSprites);
            foreach (var sprite in destroyList)
            {
                if (!sprite.IsOwnerValid)
                {
                    sprite.UnInitialize();
                    _dynamicSprites.Remove(sprite);
                }
            }

            ListPool<DynamicSprite>.Release(destroyList);
        }

        protected static void ApplyDynamicSpriteResource(AtlasHandle self)
        {
            var count = self._dynamicSprites.Count;
            if (count == 0)
                return;

            var destroyList = ListPool<DynamicSprite>.Get();
            var handleList = ListPool<DynamicSprite>.Get();
            foreach (var sprite in self._dynamicSprites)
            {
                if (sprite.IsOwnerValid)
                    handleList.Add(sprite);
                else
                    destroyList.Add(sprite);
            }

            foreach (var sprite in destroyList)
            {
                sprite.UnInitialize();
                self._dynamicSprites.Remove(sprite);
            }

            foreach (var sprite in handleList)
                sprite.ApplyAtlas(self);

            ListPool<DynamicSprite>.Release(handleList);
            ListPool<DynamicSprite>.Release(destroyList);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshNeeded()
        {
            bool value = _lifeRefCount > 0 || _lifeTime != 0.0f;
            if (_isNeeded == value)
                return;
            _isNeeded = value;
            if (value)
                LoadResourceAsync().Forget();
            else
                UnloadResource();
        }

        public void ReloadResourceIfNeeded()
        {
            if (_isNeeded)
                LoadResourceAsync().Forget();
        }

        public void Tick(float deltaTime)
        {
            var t = LifeTime;
            if (t > 0.0f)
            {
                t = Mathf.Max(this.LifeTime - deltaTime, 0.0f);
                LifeTime = t;
            }
        }

        // 增加引用才会加载资源
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRef() => ++LifeRefCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubRef() => --LifeRefCount;

        public abstract KSprite GetSprite(SpriteId id);
        protected abstract string GetTextureResource();

        // 绑定只是建立关系，并不会立即加载资源
        public void Bind(DynamicSprite sprite)
        {
            if (!_dynamicSprites.Add(sprite))
            {
                Debug.LogError($"绑定{nameof(DynamicSprite)}失败，sprite:{sprite._id.Name} 已经存在 atlas:{this.GetTextureResource()}");
            }
        }

        public void UnBind(DynamicSprite sprite)
        {
            if (!_dynamicSprites.Remove(sprite))
            {
                Debug.LogError($"解绑{nameof(DynamicSprite)}失败，sprite:{sprite._id.Name} 不存在 atlas:{this.GetTextureResource()}");
            }
        }

        protected abstract ValueTask LoadResourceAsync();
        public abstract void UnloadResource();

        protected async ValueTask<T> LoadResourceAsync<T>(string path, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (!await AssetManager.WaitInstanceAsync(10, cancellationToken))
                return null;
            return await AssetManager.Instance.LoadAssetAsync<T>(path, cancellationToken);
        }
    }
}