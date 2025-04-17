using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace kuro
{
    [Serializable]
    public class DynamicAtlasData
    {
        public string TextureResource = "";
        public int TextureWidth = 0;
        public int TextureHeight = 0;
        public AtlasSpriteData SpriteData = new();

        public Vector2Int TextureSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(TextureWidth, TextureHeight);
        }
    }
}