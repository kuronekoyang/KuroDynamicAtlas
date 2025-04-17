using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace kuro
{
    public abstract class ImagePackingAlgorithm
    {
        private Vector2Int _size;
        private int _padding;
        private int _freeArea;

        public Vector2Int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size;
        }

        public int Padding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _padding;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _padding = value;
        }

        public int TotalArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size.x * _size.y;
        }

        public int FreeArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _freeArea;
        }

        public int FillArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TotalArea - FreeArea;
        }

        public float FillRate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (float)FillArea / (float)TotalArea;
        }

        private static int s_imageId = 0;

        protected ImagePackingAlgorithm()
        {
        }

        protected ImagePackingAlgorithm(Vector2Int size, int padding)
        {
            _size = size;
            _padding = padding;
            _freeArea = size.x * size.y;
        }

        public bool AddImage(Vector2Int size, out Vector2Int pos, out int imageId)
        {
            pos = default;
            imageId = -1;

            if (size.x <= 0 || size.y <= 0)
                return false;

            int width = size.x;
            int height = size.y;

            if (_padding > 0)
            {
                int spaceX = Mathf.Clamp((_size.x - size.x) / 2, 0, _padding);
                int spaceY = Mathf.Clamp((_size.y - size.y) / 2, 0, _padding);

                width += spaceX * 2;
                height += spaceY * 2;
            }

            if (FreeArea < width * height)
                return false;

            imageId = ++s_imageId;

            if (!OnAddImage(imageId, width, height, out pos))
                return false;

            pos.x += _padding;
            pos.y += _padding;

            if (_freeArea < width * height)
                throw new Exception("FreeArea < width * height");
            _freeArea -= width * height;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FreeImage(int imageId) => OnFreeImage(imageId);

        public virtual void ClearAllImages() => _freeArea = _size.x * _size.y;

        protected abstract bool OnAddImage(int imageId, int width, int height, out Vector2Int pos);
        protected abstract bool OnFreeImage(int imageId);
    }
}