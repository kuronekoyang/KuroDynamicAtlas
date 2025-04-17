using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace kuro
{
    public class EditorAtlasManager : ManagerBase<EditorAtlasManager>, IEditorManager
    {
        public const string EditorAtlasDbPath = "Assets/EditorResources/Data/EditorAtlasDb.asset";

        private readonly LazyUnityObject<Internal> _internal;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= new();

        public EditorAtlasManager()
        {
            _internal = new(() =>
            {
                var r = ScriptableObject.CreateInstance<Internal>();
                r.Load();
                return r;
            });
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            AssetFileWatcher.AddFileWatcher(AtlasManager.KAtlasDbPath, OnAtlasDbChanged);
            Load();
        }

        protected override void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            AssetFileWatcher.RemoveFileWatcher(AtlasManager.KAtlasDbPath, OnAtlasDbChanged);
            Unload();
        }

        void IEditorManager.OnEnteredEditMode()
        {
            // do nothing
        }

        private void OnAtlasDbChanged(string _)
        {
            OnAtlasDbChangedAsync(CancellationTokenSource.Token).Forget();
        }

        private async ValueTask OnAtlasDbChangedAsync(CancellationToken cancellationToken)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var runtimeManager = AtlasManager.Instance;
            var dynamicSpriteList = runtimeManager.AllAtlasList
                .SelectMany(x => x.DynamicSprites)
                .Where(x => x.IsOwnerValid)
                .ToArray();
            Unload();
            runtimeManager.UnloadResource();
            runtimeManager.UnloadDb();
            await runtimeManager.LoadDbAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            Load();
            foreach (var dynamicSprite in dynamicSpriteList)
                dynamicSprite.OnAtlasDbChanged();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public static IReadOnlyList<EditorSpriteData> EditorSpriteDataList => Instance?._internal.Value.EditorSpriteDataList ?? Array.Empty<EditorSpriteData>();
        public static EditorSpriteData GetEditorSpriteData(SpriteId spriteId) => Instance?._internal.Value.GetEditorSpriteData(spriteId);
        public static KSprite GetSprite(SpriteId id) => Instance?._internal.Value.GetSprite(id) ?? KSprite.Empty;
        public static Texture2D GetThumbnail(SpriteId id) => Instance?._internal.Value.GetThumbnail(id) ?? Texture2D.whiteTexture;

        private void Load()
        {
            _internal.EnsureValue();
        }

        private void Unload()
        {
            if (_internal.HasValue)
                _internal.Value.Unload();
            _internal.ClearValue();
        }

        private class Internal : ScriptableObject
        {
            private readonly List<EditorSpriteData> _editorSpriteDataList = new();
            private readonly Dictionary<SpriteId, EditorSpriteData> _editorSpriteDataDictionary = new();
            private readonly Dictionary<SpriteId, KSprite> _spriteDictionary = new();
            private Dictionary<SpriteId, Texture2D> _thumbnailDictionary = null;

            public void Load()
            {
                var atlasDb = AtlasManager.Instance?.AtlasDb;
                if (atlasDb == null)
                    return;

                foreach (var atlasData in atlasDb.DynamicAtlasList)
                {
                    var spriteData = atlasData.SpriteData;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasData.TextureResource);
                    var editorSpriteData = CreateInstance<EditorSpriteData>();
                    editorSpriteData.name = spriteData.Id.Name;
                    editorSpriteData.SpriteData = spriteData;
                    _editorSpriteDataList.Add(editorSpriteData);
                    _editorSpriteDataDictionary[spriteData.Id] = editorSpriteData;
                    _spriteDictionary[spriteData.Id] = new KSprite(spriteData.SpriteData, texture);
                }
            }

            public void Unload()
            {
                foreach (var data in _editorSpriteDataList)
                    if (data)
                        DestroyImmediate(data);

                _editorSpriteDataList.Clear();
                _editorSpriteDataDictionary.Clear();
                _spriteDictionary.Clear();
                if (_thumbnailDictionary != null)
                {
                    foreach (var (_, texture) in _thumbnailDictionary)
                        if (texture)
                            DestroyImmediate(texture);
                    _thumbnailDictionary = null;
                }
            }

            public IReadOnlyList<EditorSpriteData> EditorSpriteDataList => _editorSpriteDataList;
            public EditorSpriteData GetEditorSpriteData(SpriteId spriteId) => _editorSpriteDataDictionary.GetValueOrDefault(spriteId);
            public KSprite GetSprite(SpriteId id) => _spriteDictionary.GetValueOrDefault(id);

            public Texture2D GetThumbnail(SpriteId id)
            {
                if (_thumbnailDictionary == null)
                {
                    _thumbnailDictionary = new();
                    foreach (var (k, v) in _spriteDictionary)
                    {
                        if (!v.IsValid)
                            continue;
                        var texture = v.CreateThumbnail(128);
                        _thumbnailDictionary[k] = texture;
                    }
                }

                return _thumbnailDictionary.GetValueOrDefault(id);
            }
        }
    }
}