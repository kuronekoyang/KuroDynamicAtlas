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

        private readonly Dictionary<SpriteId, EditorSpriteData> _editorSpriteDataDictionary = new();
        private readonly Dictionary<SpriteId, KSprite> _spriteDictionary = new();
        private Dictionary<SpriteId, Texture2D> _thumbnailDictionary = null;

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= new();

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

        public static EditorSpriteData GetEditorSpriteData(SpriteId spriteId) => Instance?._editorSpriteDataDictionary.GetValueOrDefault(spriteId);
        public static KSprite GetSprite(SpriteId id) => Instance?._spriteDictionary.GetValueOrDefault(id) ?? KSprite.Empty;
        public static Texture2D GetThumbnail(SpriteId id)
        {
            if (Instance == null)
                return Texture2D.whiteTexture;

            if (Instance._thumbnailDictionary == null)
            {
                Instance._thumbnailDictionary = new();
                foreach (var (k, v) in Instance._spriteDictionary)
                {
                    if (!v.IsValid)
                        continue;
                    var texture = v.CreateThumbnail(128);
                    texture.hideFlags = HideFlags.DontSave;
                    Instance._thumbnailDictionary[k] = texture;
                }
            }

            return Instance._thumbnailDictionary.GetValueOrDefault(id) ?? Texture2D.whiteTexture;
        }

        private void Load()
        {
            var editorAtlasDb = AssetDatabase.LoadAssetAtPath<EditorAtlasDb>(EditorAtlasDbPath);
            if (editorAtlasDb && editorAtlasDb.SpriteDataList != null)
            {
                foreach (var editorSpriteData in editorAtlasDb.SpriteDataList)
                    _editorSpriteDataDictionary[editorSpriteData.SpriteId] = editorSpriteData;
            }

            var atlasDb = AtlasManager.Instance?.AtlasDb;
            if (atlasDb != null)
            {
                foreach (var atlasData in atlasDb.DynamicAtlasList)
                {
                    var spriteData = atlasData.SpriteData;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasData.TextureResource);
                    _spriteDictionary[spriteData.Id] = new KSprite(spriteData.SpriteData, texture);
                }
            }
        }

        private void Unload()
        {
            _editorSpriteDataDictionary.Clear();
            _spriteDictionary.Clear();
            if (_thumbnailDictionary != null)
            {
                foreach (var (_, texture) in _thumbnailDictionary)
                    if (texture)
                        UnityEngine.Object.DestroyImmediate(texture);
                _thumbnailDictionary = null;
            }
        }
    }
}