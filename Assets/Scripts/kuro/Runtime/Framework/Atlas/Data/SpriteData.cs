using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace kuro
{
    [Serializable]
    public class SpriteData
    {
        public static readonly Vector4 KNormalizedOutterUV = new Vector4(0, 0, 1, 1);

        public float PixelsPerUnit;

        // 精灵尺寸（裁掉空白像素前的大小）
        public Vector2 Size;

        // 精灵尺寸变换向量【无视旋转】
        public Vector4 PaddingFactor;

        //
        // not rotated
        //      ↑──────────┐ z,w
        //      │          │
        // x,y ─┼───────────→
        //
        // rotated
        // x',y' ─┼──────→
        //        │     │
        //        │     │
        //        │     │
        //        ↓─────┘ z',w'
        //
        // 外uv
        public Vector4 Uv;

        // 内uv
        public Vector4 InnerUv;

        // 如果有border那么肯定不会裁掉空白像素（就这么规定的）【无视旋转】
        public Vector4 Border;

        // uv是否旋转过
        public bool IsRotated;

        public bool IsPacked;

        // 精灵在纹理中的尺寸（裁掉空白像素后的大小）
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetTrimmedSize() => new Vector2((PaddingFactor.z - PaddingFactor.x) * Size.x, (PaddingFactor.w - PaddingFactor.y) * Size.y);

        // 精灵裁掉的空白像素大小
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetPadding() => new Vector4(PaddingFactor.x * Size.x, PaddingFactor.y * Size.y, (1.0f - PaddingFactor.z) * Size.x, (1.0f - PaddingFactor.w) * Size.y);

        // 归一化的内UV
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetNormalizedInnerUV()
        {
            Vector4 r;
            var textureSize = GetTrimmedSize();
            if (textureSize.x != 0.0f)
            {
                r.x = Border.x / textureSize.x;
                r.z = (textureSize.x - Border.z) / textureSize.x;
            }
            else
            {
                r.x = 0.0f;
                r.z = 0.0f;
            }

            if (textureSize.y != 0.0f)
            {
                r.y = Border.y / textureSize.y;
                r.w = (textureSize.y - Border.w) / textureSize.y;
            }
            else
            {
                r.y = 0.0f;
                r.w = 0.0f;
            }

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetHasPadding() => PaddingFactor.x != 0.0f || PaddingFactor.y != 0.0f || PaddingFactor.z != 1.0f || PaddingFactor.w != 1.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetHasBorder() => Border.x != 0.0f || Border.y != 0.0f || Border.z != 0.0f || Border.w != 0.0f;


        // 本地空间的uv转换为纹理空间的uv
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetLocalSpace2TextureSpaceUv(Vector2 local)
        {
            if (IsRotated)
                return new Vector2((Uv.z - Uv.x) * local.y + Uv.x, (Uv.w - Uv.y) * local.x + Uv.y);
            else
                return new Vector2((Uv.z - Uv.x) * local.x + Uv.x, (Uv.w - Uv.y) * local.y + Uv.y);
        }

        // 本地空间的uv转换为纹理空间的uv的矩阵
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 GetLocalSpace2TextureSpaceUvMatrix()
        {
            if (IsRotated)
                return Matrix4x4.Translate(new Vector3(Uv.x, Uv.y, 0.0f)) *
                       new Matrix4x4(
                           new Vector4(0, Uv.w - Uv.y, 0, 0),
                           new Vector4(Uv.z - Uv.x, 0, 0, 0),
                           new Vector4(0, 0, 1, 0),
                           new Vector4(0, 0, 0, 1)
                       );
            else
                return Matrix4x4.Translate(new Vector3(Uv.x, Uv.y, 0.0f)) *
                       new Matrix4x4(
                           new Vector4(Uv.z - Uv.x, 0, 0, 0),
                           new Vector4(0, Uv.w - Uv.y, 0, 0),
                           new Vector4(0, 0, 1, 0),
                           new Vector4(0, 0, 0, 1)
                       );
        }

        // 本地空间的uv
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetUv(out Vector2 lb, out Vector2 lt, out Vector2 rt, out Vector2 rb)
        {
            if (IsRotated)
            {
                lb.x = Uv.x;
                lb.y = Uv.y;
                lt.x = Uv.z;
                lt.y = Uv.y;
                rt.x = Uv.z;
                rt.y = Uv.w;
                rb.x = Uv.x;
                rb.y = Uv.w;
            }
            else
            {
                lb.x = Uv.x;
                lb.y = Uv.y;
                lt.x = Uv.x;
                lt.y = Uv.w;
                rt.x = Uv.z;
                rt.y = Uv.w;
                rb.x = Uv.z;
                rb.y = Uv.y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetMinSize() => new Vector2(Border.x + Border.z, Border.y + Border.w);
    }
}