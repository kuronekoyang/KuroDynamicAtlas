using System.Runtime.CompilerServices;
using UnityEngine;

namespace kuro
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class DynamicSprite4SpriteRenderer : DynamicSpriteHookBase<SpriteRenderer>
    {
        private KSprite _lastSprite;
        private Sprite _lastUnitySprite;
        private DynamicSprite.SpriteCallback _applyDynamicSprite;

        protected override DynamicSprite.SpriteCallback ApplyDynamicSprite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _applyDynamicSprite ??= (value) =>
            {
                var t = Target;
                if (!t)
                    return;
                if (_lastSprite == value)
                    return;
                _lastSprite = value;
                _lastUnitySprite.SafeDestroy();
                _lastUnitySprite = value.CreateUnitySprite();
                if (_lastUnitySprite)
                    _lastUnitySprite.hideFlags = HideFlags.DontSave;
                t.sprite = _lastUnitySprite;
            };
        }

        protected override void OnDestroy()
        {
            _lastUnitySprite.SafeDestroy();
            base.OnDestroy();
        }
    }
}