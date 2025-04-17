using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace kuro
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class DynamicSprite4Image : DynamicSpriteHookBase<Image>
    {
        private KSprite _lastSprite;
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
                t.sprite = value.CreateUnitySprite();
            };
        }
    }
}