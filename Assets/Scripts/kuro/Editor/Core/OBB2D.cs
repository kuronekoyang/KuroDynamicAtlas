using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace kuro
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OBB2D
    {
        private Vector2 _center;
        private Vector2 _size;
        private Vector2 _right;
        private Vector2 _up;

        public static readonly OBB2D Zero = new OBB2D(Vector2.zero, Vector2.zero, new Vector2(1, 0));

        public Vector2 Center
        {
            readonly get => _center;
            set => _center = value;
        }

        public Vector2 Size
        {
            readonly get => _size;
            set => _size = value;
        }

        public Vector2 Right
        {
            readonly get => _right;
            set
            {
                _right = value.normalized;
                _up = _right.RotateCcw(90.0f);
            }
        }

        public readonly Vector2 Up => _up;

        public void SetCenterSizeRight(Vector2 center, Vector2 size, Vector2 right)
        {
            this._center = center;
            this._size = size;
            this._right = right.normalized;
            this._up = right.RotateCcw(90.0f);
        }

        public readonly float RotationCCW
        {
            get => new Vector2(1, 0).GetRotateAngleCcw(_right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OBB2D(Vector2 center, Vector2 size, Vector2 right)
        {
            this._center = center;
            this._size = size;
            this._right = right.normalized;
            this._up = right.RotateCcw(90.0f);
        }
    }
}