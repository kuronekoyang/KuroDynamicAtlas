using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace kuro
{
    public class AtlasManager : ManagerBase<AtlasManager>, IManagerUpdate
#if UNITY_EDITOR
        , IEditorManager
#endif
    {
        public const int DynamicAtlasAstcBlockSize = 6;
        public const TextureFormat DynamicAtlasTextureFormat = TextureFormat.ASTC_6x6;
        public const int DynamicAtlasSize = (1024 / DynamicAtlasAstcBlockSize) * DynamicAtlasAstcBlockSize;
        public const string KAtlasDbPath = "Assets/GameResources/Data/AtlasDb.json";

        private AtlasDb _atlasDb;
        private readonly Dictionary<SpriteId, AtlasSprite> _spriteAtlasDictionary = new(2048);
        private readonly List<AtlasHandle> _allAtlasList = new(1024);
        private readonly DummyAtlas _dummyAtlas = new();
        private readonly List<DynamicAtlasTexture> _dynamicTextureList = new(4);
        private readonly Dictionary<int, AtlasHandle> _tickAtlasDictionary = new(32);
        private readonly Dictionary<int, DynamicAtlasTexture> _delayApplyDynamicTextureDictionary = new(4);
        private KSprite _fallbackRenderSprite;
        private int _frameCount;

        private static readonly List<DynamicSprite> s_delayInitializeDynamicSpriteList = new(8);

        public AtlasDb AtlasDb
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _atlasDb;
        }

        public List<AtlasHandle> AllAtlasList
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _allAtlasList;
        }

        public List<DynamicAtlasTexture> DynamicTextureList
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dynamicTextureList;
        }

        public static KSprite FallbackRenderSprite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Instance?._fallbackRenderSprite ?? default;
        }

        public static Texture2D FallbackTexture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Instance?._fallbackRenderSprite.Texture;
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitFallbackRenderSprite();
            await LoadDbAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
        }

        public override void OnAfterInitialize()
        {
            InvokeDelayInitializeDynamicSprite();
        }

        protected override void OnDestroy()
        {
            DestroyFallbackRenderSprite();
            UnloadResource();
            UnloadDb();
        }


#if UNITY_EDITOR
        void IEditorManager.OnEnteredEditMode()
        {
            UnloadResource();
            DestroyFallbackRenderSprite();

            InitFallbackRenderSprite();
            foreach (var atlas in _allAtlasList)
                atlas.ReloadResourceIfNeeded();
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDelayInitializeDynamicSprite(DynamicSprite dynamicSprite)
        {
            s_delayInitializeDynamicSpriteList.Add(dynamicSprite);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeDelayInitializeDynamicSprite()
        {
            foreach (var dynamicSprite in s_delayInitializeDynamicSpriteList)
                dynamicSprite.OnDelayInitialize();
            s_delayInitializeDynamicSpriteList.Clear();
        }

        private void InitFallbackRenderSprite()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[4];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.clear;
            texture.SetPixels(0, 0, 2, 2, colors);
            texture.Apply();
            _fallbackRenderSprite = new(texture);
        }

        private void DestroyFallbackRenderSprite()
        {
            _fallbackRenderSprite.Texture.SafeDestroy();
        }

        public void UnloadDb()
        {
            _spriteAtlasDictionary.Clear();
            _allAtlasList.Clear();
            _tickAtlasDictionary.Clear();
            _atlasDb = null;
        }

        public async ValueTask LoadDbAsync(CancellationToken cancellationToken)
        {
            _allAtlasList.Add(_dummyAtlas);

            var asset = await AssetManager.Instance.LoadAssetAsync<TextAsset>(KAtlasDbPath, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            if (asset != null)
            {
                _atlasDb = JsonUtility.FromJson<AtlasDb>(asset.text);
                AssetManager.Instance.UnloadResource(asset);

                foreach (var atlasData in _atlasDb.DynamicAtlasList)
                {
                    var atlas = new DynamicAtlas(atlasData);
                    var sprite = new AtlasSprite(atlasData.SpriteData, atlas);
                    _spriteAtlasDictionary[atlasData.SpriteData.Id] = sprite;
                    _allAtlasList.Add(atlas);
                }
            }
        }

        public void UnloadResource()
        {
            foreach (var atlas in _allAtlasList)
                atlas.UnloadResource();
            foreach (var tex in _dynamicTextureList)
                tex.UnloadResource();
            _dynamicTextureList.Clear();
            _delayApplyDynamicTextureDictionary.Clear();
        }


        public static bool TryGetAtlasSpriteData(SpriteId id, out AtlasSpriteData spriteData)
        {
            if (Instance == null)
            {
                spriteData = null;
                return false;
            }

            if (id.IsEmpty)
            {
                spriteData = null;
                return false;
            }

            if (!Instance._spriteAtlasDictionary.TryGetValue(id, out var sprite))
            {
                spriteData = null;
                return false;
            }

            spriteData = sprite.Data;
            return true;
        }

        public static AtlasSprite GetAtlasSprite(SpriteId id, Component source = null)
        {
            if (Instance == null)
                return null;

            if (id.IsEmpty)
                return null;

            if (Instance._spriteAtlasDictionary.TryGetValue(id, out var sprite))
                return sprite;

#if UNITY_EDITOR
            if (source != null)
                Debug.LogError($"Sprite {id} not found in AtlasManager");
#endif

            return Instance._dummyAtlas.DummyAtlasSprite;
        }

        public static bool TryPackDynamicAtlas(Vector2Int size, out DynamicAtlasTexture dynamicTexture, out Vector2Int pos, out int dynamicImageId)
        {
            if (size.x > DynamicAtlasSize || size.y > DynamicAtlasSize)
                throw new Exception($"Dynamic atlas size {size} is larger than max dynamic atlas size {DynamicAtlasSize}.");

            dynamicTexture = null;
            pos = default;
            dynamicImageId = -1;
            if (Instance == null)
                return false;

            foreach (var tex in Instance._dynamicTextureList)
            {
                if (tex.PackingAlgorithm.AddImage(size, out pos, out dynamicImageId))
                {
                    dynamicTexture = tex;
                    return true;
                }
            }

            dynamicTexture = new DynamicAtlasTexture();
            Instance._dynamicTextureList.Add(dynamicTexture);
            if (!dynamicTexture.PackingAlgorithm.AddImage(size, out pos, out dynamicImageId))
                throw new Exception($"Failed to pack dynamic atlas image {size}.");

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayApplyDynamicTexture(DynamicAtlasTexture texture)
        {
            if (Instance == null)
                return;
            Instance._delayApplyDynamicTextureDictionary[texture.Id] = texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterTick(AtlasHandle atlas)
        {
            Instance?._tickAtlasDictionary.Add(atlas.Id, atlas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnRegisterTick(AtlasHandle atlas)
        {
            Instance?._tickAtlasDictionary.Remove(atlas.Id);
        }

        public void LateUpdate()
        {
            if (_delayApplyDynamicTextureDictionary.Count > 0)
            {
                foreach (var (_, tex) in _delayApplyDynamicTextureDictionary)
                    if (tex.Texture)
                        tex.Texture.Apply(false);
                _delayApplyDynamicTextureDictionary.Clear();
            }
        }

        public void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
#endif
            {
                var deltaTime = Time.unscaledDeltaTime;
                if (_tickAtlasDictionary.Count > 0)
                {
                    using (ListPool<AtlasHandle>.Get(out var temp))
                    {
                        foreach (var (_, atlas) in _tickAtlasDictionary)
                            temp.Add(atlas);
                        foreach (var atlas in temp)
                            atlas.Tick(deltaTime);
                    }
                }
            }

            // Editor下也会用，所以自己记录帧号
            ++_frameCount;
            var idx = _frameCount % _allAtlasList.Count;
            _allAtlasList[idx].CleanupInvalidDynamicSprites();
        }
    }
}