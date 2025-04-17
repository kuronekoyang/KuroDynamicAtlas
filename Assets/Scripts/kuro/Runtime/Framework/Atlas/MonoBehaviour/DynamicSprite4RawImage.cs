using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace kuro
{
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    [DisallowMultipleComponent]
    public class DynamicSprite4RawImage : DynamicSpriteHookBase<RawImage>
    {
        private DynamicSprite.SpriteCallback _applyDynamicSprite;

        protected override DynamicSprite.SpriteCallback ApplyDynamicSprite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _applyDynamicSprite ??= (value) =>
            {
                var t = Target;
                if (!t)
                    return;
                t.texture = value.Texture;
                if (value.IsValid)
                {
                    var uv = value.Data.Uv;
                    t.uvRect = new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
                }
                else
                {
                    t.uvRect = new Rect(0, 0, 1, 1);
                }
            };
        }
    }
}