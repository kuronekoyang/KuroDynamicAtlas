using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace kuro
{
    public class DynamicAtlasTexture
    {
        private static bool? s_enableGraphicCopyTexture;

        public static bool EnableGraphicCopyTexture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_enableGraphicCopyTexture ??= SystemInfo.copyTextureSupport.HasFlag(UnityEngine.Rendering.CopyTextureSupport.Basic);
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct AstcBlock
        {
            private long Data0;
            private long Data1;
        }

        private const int AtlasSize = AtlasManager.DynamicAtlasSize;
        private const TextureFormat TextureFormat = AtlasManager.DynamicAtlasTextureFormat;
        private const int AstcBlockSize = AtlasManager.DynamicAtlasAstcBlockSize;

        private static int s_id = 0;
        private readonly int _id;
        private readonly ImagePackingAlgorithm _packingAlgorithm;
        private Texture2D _texture;

        public int Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _id;
        }

        public ImagePackingAlgorithm PackingAlgorithm
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _packingAlgorithm;
        }

        public Texture2D Texture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _texture;
        }

        public DynamicAtlasTexture()
        {
            _id = ++s_id;
            _packingAlgorithm = new ImagePackingBinaryTree(new Vector2Int(AtlasSize, AtlasSize), 0);
            _texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat, false);
            _texture.hideFlags = HideFlags.DontSave;

            if (EnableGraphicCopyTexture)
                _texture.Apply(false, true);
        }

        public void UnloadResource()
        {
            if (_texture != null)
            {
                _texture.SafeDestroy();
                _texture = null;
            }

            _packingAlgorithm.ClearAllImages();
        }

        public void FillTexture(Texture2D texture, Vector2Int pos)
        {
            if (texture.format != TextureFormat)
                throw new Exception($"Texture {texture.name} TextureFormat {texture.format} is not supported for dynamic atlas");

            if (EnableGraphicCopyTexture)
            {
                Graphics.CopyTexture(texture, 0, 0, 0, 0, texture.width, texture.height, _texture, 0, 0, pos.x, pos.y);
            }
            else
            {
                if (!texture.isReadable)
                    throw new Exception($"Texture {texture.name} is not readable");
                using (var src = texture.GetPixelData<AstcBlock>(0))
                using (var dst = _texture.GetPixelData<AstcBlock>(0))
                {
                    CopyTexture(src, texture.width, texture.height, dst, pos.x, pos.y, AtlasSize, AstcBlockSize);
                    AtlasManager.DelayApplyDynamicTexture(this);
                }
            }
        }

        private static void CopyTexture(
            NativeArray<AstcBlock> src, int srcWidth, int srcHeight,
            NativeArray<AstcBlock> dst, int dstPosX, int dstPosY,
            int dstWidth, int astcBlockSize)
        {
            var srcBlockWidth = srcWidth / astcBlockSize;
            var srcBlockHeight = srcHeight / astcBlockSize;
            var dstBlockWidth = dstWidth / astcBlockSize;
            var dstBlockPoxX = dstPosX / astcBlockSize;
            var dstBlockPoxY = dstPosY / astcBlockSize;

            for (int y = 0; y < srcBlockHeight; ++y)
                NativeArray<AstcBlock>.Copy(src, y * srcBlockWidth, dst, (dstBlockPoxY + y) * dstBlockWidth + dstBlockPoxX, srcBlockWidth);
        }
    }
}