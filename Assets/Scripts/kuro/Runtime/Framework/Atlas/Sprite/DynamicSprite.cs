using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace kuro
{
    [Serializable]
    public class DynamicSprite
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        public delegate void SpriteCallback(KSprite sprite);

        [SerializeField] public SpriteId _id;

        public SpriteId Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._id;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _id = value;
                if (!_initialized)
                    return;
                UpdateSpriteHandle();
            }
        }

        private bool _initialized;
        private bool _enable;
        private AtlasSprite _spriteHandle;
        private SpriteCallback _spriteCallback;
        private KSprite _sprite;
        private Behaviour _owner;

        public KSprite Sprite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sprite;
        }

        public bool IsOwnerValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _owner;
        }

        private void UpdateSpriteHandle(bool force = false)
        {
            var spriteHandle = AtlasManager.GetAtlasSprite(_id, _owner);
            if (force || this._spriteHandle != spriteHandle)
            {
                var oldEnable = _enable;
                if (oldEnable)
                    Disable();

                this._spriteHandle?.Atlas?.UnBind(this);
                this._spriteHandle = spriteHandle;
                this._spriteHandle?.Atlas?.Bind(this);

                if (oldEnable)
                    Enable();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(Behaviour owner, SpriteCallback callback)
        {
            if (_initialized)
                return;

            InitializeImpl(owner, callback);
        }

        private void InitializeImpl(Behaviour owner, SpriteCallback callback)
        {
            _initialized = true;
            _enable = false;
            _owner = owner;
            _spriteCallback = callback;
            if (AtlasManager.Instance == null)
            {
                AtlasManager.AddDelayInitializeDynamicSprite(this);
            }
            else
            {
                UpdateSpriteHandle();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDelayInitialize()
        {
            if (!_initialized)
                return;
            if (!_owner)
                return;
            UpdateSpriteHandle();
        }

        public void OnAtlasDbChanged()
        {
            if (!_initialized)
                return;
            if (!_owner)
                return;
            UpdateSpriteHandle(true);
        }


#if UNITY_EDITOR

        public void EditorReInitialize(Behaviour owner, SpriteCallback callback) => InitializeImpl(owner, callback);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // do nothing
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            EditorDelayUpdateSpriteHandle().Forget();
        }

        private async ValueTask EditorDelayUpdateSpriteHandle()
        {
            await Task.Yield();
            if (!_initialized)
                return;
            if (!_owner)
                return;
            UpdateSpriteHandle();
        }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnInitialize()
        {
            if (!_initialized)
                return;

            // 释放绑定
            _spriteHandle?.Atlas?.UnBind(this);

            // 释放引用
            Disable();

            // 清除数据
            _initialized = false;
            _owner = null;
            _spriteCallback = null;
            _spriteHandle = null;
            _sprite = default;
        }

        /// <summary>
        /// 图集更新时的回调
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyAtlas(AtlasHandle atlas)
        {
            if (!_owner)
                return;

            _sprite = atlas?.GetSprite(_id) ?? KSprite.Empty;
            _spriteCallback?.Invoke(_sprite);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enable()
        {
            if (!_initialized)
                return;
            if (_enable)
                return;
            _enable = true;
            var atlas = _spriteHandle?.Atlas;
            atlas?.AddRef();
            ApplyAtlas(atlas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Disable()
        {
            if (!_initialized)
                return;
            if (!_enable)
                return;
            _enable = false;
            _spriteHandle?.Atlas?.SubRef();
        }
    }
}