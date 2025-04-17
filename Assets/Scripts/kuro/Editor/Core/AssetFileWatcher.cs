using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace kuro
{
    public static class AssetFileWatcher
    {
        public class AssetFileWatcherPostProcessor : AssetPostprocessor
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                AssetFileWatcher.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
        }

        private class FileWatcher
        {
            public string Path;
            public Action<string> Callback;
            public bool IsDeleted;
        }

        private class DirectoryWatcher : FileWatcher
        {
            public string EndWiths;
        }

        private readonly struct Callback
        {
            public readonly FileWatcher Watcher;
            public readonly string Path;

            public Callback(FileWatcher watcher, string path)
            {
                Watcher = watcher;
                Path = path;
            }
        }

        private static readonly List<DirectoryWatcher> s_directoryWatchers = new();
        private static readonly List<FileWatcher> s_fileWatchers = new();

        private static readonly List<Callback> s_tempCallbacks = new();
        private static readonly HashSet<string> s_tempFiles = new();

        public static void AddDirectoryWatcher(string path, string endWithFilter, Action<string> callback)
        {
            if (callback == null)
                return;
            var normalizedPath = IOUtils.NormalizedPath(path);
            if (s_directoryWatchers.Any(x => normalizedPath.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase) && x.Callback == callback))
                return;
            s_directoryWatchers.Add(new DirectoryWatcher()
            {
                Path = normalizedPath,
                EndWiths = endWithFilter,
                Callback = callback
            });
        }

        public static void AddFileWatcher(string path, Action<string> callback)
        {
            if (callback == null)
                return;
            var normalizedPath = IOUtils.NormalizedPath(path);
            if (s_fileWatchers.Any(x => normalizedPath.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase) && x.Callback == callback))
                return;
            s_fileWatchers.Add(new FileWatcher()
            {
                Path = normalizedPath,
                Callback = callback
            });
        }

        private static void RemoveWatcher<T>(List<T> list, string path, Action<string> callback) where T : FileWatcher
        {
            if (callback == null)
                return;
            var normalizedPath = IOUtils.NormalizedPath(path);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var watcher = list[i];
                if (!normalizedPath.StartsWith(watcher.Path, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (watcher.Callback != callback)
                    continue;
                watcher.IsDeleted = true;
                list.RemoveAt(i);
            }
        }

        private static void RemoveWatcher<T>(List<T> list, Action<string> callback) where T : FileWatcher
        {
            if (callback == null)
                return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var watcher = list[i];
                if (watcher.Callback != callback)
                    continue;
                watcher.IsDeleted = true;
                list.RemoveAt(i);
            }
        }

        public static void RemoveDirectoryWatcher(string path, Action<string> callback) => RemoveWatcher(s_directoryWatchers, path, callback);

        public static void RemoveFileWatcher(string path, Action<string> callback) => RemoveWatcher(s_fileWatchers, path, callback);

        public static void RemoveDirectoryWatcherByCallback(Action<string> callback) => RemoveWatcher(s_directoryWatchers, callback);

        public static void RemoveFileWatcherByCallback(Action<string> callback) => RemoveWatcher(s_fileWatchers, callback);

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (s_fileWatchers.Count == 0 && s_directoryWatchers.Count == 0)
                return;

            s_tempFiles.Clear();
            s_tempFiles.UnionWith(importedAssets);
            s_tempFiles.UnionWith(deletedAssets);
            s_tempFiles.UnionWith(movedAssets);
            s_tempFiles.UnionWith(movedFromAssetPaths);

            s_tempCallbacks.Clear();

            foreach (var file in s_tempFiles)
            {
                foreach (var watcher in s_fileWatchers)
                {
                    if (!file.Equals(watcher.Path, StringComparison.OrdinalIgnoreCase))
                        continue;
                    s_tempCallbacks.Add(new(watcher, file));
                }

                foreach (var watcher in s_directoryWatchers)
                {
                    if (!file.StartsWith(watcher.Path, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.IsNullOrEmpty(watcher.EndWiths) && !file.EndsWith(watcher.EndWiths, StringComparison.OrdinalIgnoreCase))
                        continue;
                    s_tempCallbacks.Add(new(watcher, file));
                }
            }

            foreach (var callback in s_tempCallbacks)
            {
                if (callback.Watcher.IsDeleted)
                    continue;
                callback.Watcher.Callback(callback.Path);
            }

            s_tempFiles.Clear();
            s_tempCallbacks.Clear();
        }
    }
}