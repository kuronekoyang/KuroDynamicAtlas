using System;
using System.Threading.Tasks;
using UnityEngine;

namespace kuro
{
    public class DynamicAtlas : AtlasHandle
    {
        // 原始精灵数据
        private readonly DynamicAtlasData _data;

        // 精灵
        private KSprite _sprite;

        // 图集的引用id
        private int _dynamicImageId;

        // 图集
        private DynamicAtlasTexture _dynamicTexture;

        public DynamicAtlas(DynamicAtlasData data)
        {
            var spriteData = data.SpriteData.SpriteData;
            _data = data;
            _sprite.Texture = AtlasManager.FallbackTexture;
            _sprite.Data = new SpriteData();
            _sprite.Data.PixelsPerUnit = spriteData.PixelsPerUnit;
            _sprite.Data.Size = spriteData.Size;
            _sprite.Data.PaddingFactor = spriteData.PaddingFactor;
            _sprite.Data.Border = spriteData.Border;
            _sprite.Data.IsPacked = true;
            _sprite.Data.IsRotated = false;

            // 这个动态拼完图才知道
            _sprite.Data.Uv = default;
            _sprite.Data.InnerUv = default;
        }

        public override KSprite GetSprite(SpriteId id)
        {
            if (id == _data.SpriteData.Id)
            {
                if (_status != AtlasStatus.Loaded)
                    return AtlasManager.FallbackRenderSprite;
                return _sprite;
            }

            return KSprite.Empty;
        }

        protected override string GetTextureResource() => _data.TextureResource;

        protected override async ValueTask LoadResourceAsync()
        {
            if (_status == AtlasStatus.None)
            {
                _status = AtlasStatus.Loading;
                var resource = await LoadResourceAsync<Texture2D>(_data.TextureResource, CancellationTokenSource.Token);
                if (resource == null)
                    return;

                try
                {
                    _status = AtlasStatus.Loaded;
                    ApplyResourceImpl(this, resource);
                }
                finally
                {
                    AssetManager.Instance?.UnloadResource(resource);
                }
            }

            static void ApplyResourceImpl(DynamicAtlas self, Texture2D texture)
            {
                var size = self._data.TextureSize;
                if (texture.width != size.x || texture.height != size.y)
                    throw new Exception($"Texture size mismatch, {self._data.TextureResource}, dataSize:{size.x}x{size.y}, textureSize:{texture.width}x{texture.height}");

                if (!AtlasManager.TryPackDynamicAtlas(size, out var dynamicTexture, out var posInAtlas, out var dynamicImageId))
                    return;

                dynamicTexture.FillTexture(texture, posInAtlas);

                // 纹理空间uv
                var x = (float)posInAtlas.x / (float)AtlasManager.DynamicAtlasSize;
                var y = (float)posInAtlas.y / (float)AtlasManager.DynamicAtlasSize;
                var w = (float)size.x / (float)AtlasManager.DynamicAtlasSize;
                var h = (float)size.y / (float)AtlasManager.DynamicAtlasSize;

                // 本地空间uv
                var outer = self._data.SpriteData.SpriteData.Uv;
                var inner = self._data.SpriteData.SpriteData.InnerUv;

                self._dynamicTexture = dynamicTexture;
                self._dynamicImageId = dynamicImageId;
                self._sprite.Data.Uv = new Vector4
                {
                    x = x + outer.x * w,
                    y = y + outer.y * h,
                    z = x + outer.z * w,
                    w = y + outer.w * h,
                };
                self._sprite.Data.InnerUv = new Vector4
                {
                    x = x + inner.x * w,
                    y = y + inner.y * h,
                    z = x + inner.z * w,
                    w = y + inner.w * h,
                };
                self._sprite.Texture = dynamicTexture.Texture;

                ApplyDynamicSpriteResource(self);
            }
        }


        public override void UnloadResource()
        {
            switch (_status)
            {
                case AtlasStatus.Loading:
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = null;
                    _status = AtlasStatus.None;
                    break;
                }
                case AtlasStatus.Loaded:
                {
                    _sprite.Texture = AtlasManager.FallbackTexture;
                    var dynamicAtlas = _dynamicTexture;
                    var dynamicImageId = _dynamicImageId;
                    _dynamicImageId = -1;
                    _dynamicTexture = null;
                    _status = AtlasStatus.None;

                    if (dynamicAtlas != null)
                    {
                        if (!dynamicAtlas.PackingAlgorithm.FreeImage(dynamicImageId))
                            throw new Exception($"Free Image Packing Algorithm Failed");
                    }

                    break;
                }
            }
        }
    }
}