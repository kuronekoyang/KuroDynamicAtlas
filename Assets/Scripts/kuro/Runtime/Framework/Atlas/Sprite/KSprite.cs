using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;

namespace kuro
{
    public struct KSprite : IEquatable<KSprite>
    {
        public static readonly KSprite Empty = new();

        public SpriteData Data;
        public Texture2D Texture;

        public KSprite(SpriteData data, Texture2D texture)
        {
            this.Data = data;
            this.Texture = texture;
        }

        public KSprite(Texture2D value)
        {
            if (value != null)
            {
                Texture = value;
                Data = new();
                Data.PixelsPerUnit = 100;
                Data.Size = new Vector2(value.width, value.height);
                Data.PaddingFactor = new Vector4(0, 0, 1, 1);
                Data.Uv = new Vector4(0, 0, 1, 1);
                Data.InnerUv = new Vector4(0, 0, 1, 1);
                Data.IsRotated = false;
                Data.Border = new Vector4(0, 0, 0, 0);
            }
            else
            {
                Texture = null;
                Data = null;
            }
        }

        public KSprite(Sprite value)
        {
            if (value != null)
            {
                Texture = value.texture;
                Data = new();
                Data.PixelsPerUnit = 100;
                Data.Size = value.rect.size;
                var padding = UnityEngine.Sprites.DataUtility.GetPadding(value);
                if (Data.Size.x != 0.0f)
                {
                    Data.PaddingFactor.x = padding.x / Data.Size.x;
                    Data.PaddingFactor.z = (Data.Size.x - padding.z) / Data.Size.x;
                }
                else
                {
                    Data.PaddingFactor.x = 0.0f;
                    Data.PaddingFactor.x = 1.0f;
                }

                if (Data.Size.y != 0.0f)
                {
                    Data.PaddingFactor.y = padding.y / Data.Size.y;
                    Data.PaddingFactor.w = (Data.Size.y - padding.w) / Data.Size.y;
                }
                else
                {
                    Data.PaddingFactor.y = 0.0f;
                    Data.PaddingFactor.w = 1.0f;
                }

                Data.Uv = UnityEngine.Sprites.DataUtility.GetOuterUV(value);
                Data.InnerUv = UnityEngine.Sprites.DataUtility.GetInnerUV(value);
                Data.IsRotated = false;
                Data.Border = value.border;
            }
            else
            {
                Texture = null;
                Data = null;
            }
        }

        public readonly Rect GetTrimmedRect()
        {
            if (!IsValid)
                return default;
            if (Data.IsRotated)
                throw new Exception("Not supported");
            var w = Texture.width;
            var h = Texture.height;
            return new Rect(w * Data.Uv.x, h * Data.Uv.y, w * (Data.Uv.z - Data.Uv.x), h * (Data.Uv.w - Data.Uv.y));
        }

        public readonly Rect GetPaddedRect()
        {
            if (!IsValid)
                return default;
            if (Data.IsRotated)
                throw new Exception("Not supported");
            var textureRect = GetTrimmedRect();
            var padding = Data.GetPadding();
            return new Rect(textureRect.x - padding.x, textureRect.y - padding.y, textureRect.width + padding.x + padding.z, textureRect.height + padding.y + padding.w);
        }

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data != null && Texture != null;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(KSprite lhs, KSprite rhs) => lhs.Texture == rhs.Texture && lhs.Data == rhs.Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(KSprite lhs, KSprite rhs) => !(lhs == rhs);

        [BurstDiscard]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(KSprite other)
        {
            return Texture == other.Texture && Data == other.Data;
        }

        public override bool Equals(object obj)
        {
            return obj is KSprite other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Texture, Data);
        }

        public Sprite CreateUnitySprite()
        {
            if (!IsValid)
                return null;
            var uv = Data.Uv;
            var width = Texture.width;
            var height = Texture.height;
            var sprite = Sprite.Create(
                Texture,
                new Rect(uv.x * width, uv.y * height, Data.Size.x, Data.Size.y),
                new Vector2(0.5f, 0.5f),
                100
            );
            return sprite;
        }
    }
}