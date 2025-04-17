using System.Runtime.CompilerServices;
using UnityEngine;

namespace kuro
{
    public abstract class DynamicSpriteHookBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private DynamicSprite _dynamicSprite = new();
#if UNITY_EDITOR
        private bool _isDynamicSpriteDirty = false;
#endif

        public SpriteId SpriteId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dynamicSprite.Id;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _dynamicSprite.Id = value;
        }


        protected abstract DynamicSprite.SpriteCallback ApplyDynamicSprite { get; }

        protected void Awake()
        {
            _dynamicSprite.Initialize(this, ApplyDynamicSprite);
        }

        protected void OnDestroy()
        {
            _dynamicSprite.UnInitialize();
        }

        protected void OnEnable()
        {
#if UNITY_EDITOR
            if (_isDynamicSpriteDirty && !Application.isPlaying)
            {
                _isDynamicSpriteDirty = false;
                _dynamicSprite.EditorReInitialize(this, ApplyDynamicSprite);
            }
#endif
            _dynamicSprite.Enable();
        }

        protected void OnDisable()
        {
            _dynamicSprite.Disable();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            _isDynamicSpriteDirty = true;
#endif
        }
    }

    public abstract class DynamicSpriteHookBase<T> : DynamicSpriteHookBase where T : Component
    {
        private T _target;

        public T Target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_target)
                    _target = GetComponent<T>();
                return _target;
            }
        }
    }
}