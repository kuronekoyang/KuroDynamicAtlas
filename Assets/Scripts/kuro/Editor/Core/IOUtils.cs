using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace kuro
{
    public static class IOUtils
    {
        [ThreadStatic] private static StringBuilder? s_builder;
        private static StringBuilder SharedStringBuilder => s_builder ??= new();


        public static void DeleteDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (!Directory.Exists(path))
                return;
            //Logger.Log($"DeleteDirectory, {path}");
            Directory.Delete(path, true);
        }

        public static void DeleteFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (!File.Exists(path))
                return;
            //Logger.Log($"DeleteFile, {path}");
            File.Delete(path);
        }

        public static void DeleteFileOrDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (File.Exists(path))
            {
                //Logger.Log($"DeleteFile, {path}");
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                //Logger.Log($"DeleteDirectory, {path}");
                Directory.Delete(path, true);
            }
        }

        public static void CopyFile(string? source, string? target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return;
            if (source == target)
                return;
            if (!File.Exists(source))
                return;
            var dir = Path.GetDirectoryName(target)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            try
            {
                //Logger.Log($"CopyFile, {source} => {target}");
                File.Copy(source, target, true);
            }
            catch
            {
                // do nothing
            }
        }

        public static string CopyFileToDirectory(string? source, string? targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(targetDirectory))
                return "";
            var fileName = Path.GetFileName(source);
            var target = Path.Combine(targetDirectory, fileName);
            CopyFile(source, target);
            return target;
        }

        public static void CopyDirectory(string? sourceDirectory, string? targetDirectory, Func<string, bool>? filter = null)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(targetDirectory))
                return;
            sourceDirectory = NormalizedPath(sourceDirectory);
            targetDirectory = NormalizedPath(targetDirectory);
            if (!Directory.Exists(sourceDirectory))
            {
                //Logger.Log($"Source directory does not exist, {sourceDirectory} => {targetDirectory}");
                return;
            }

            if (targetDirectory.StartsWith(sourceDirectory))
            {
                //Logger.LogError($"Can't copy to target directory for it's sub directory, {sourceDirectory} => {targetDirectory}");
                return;
            }

            //Logger.Log($"CopyDirectory start, {sourceDirectory} => {targetDirectory}");
            CopyImpl(sourceDirectory, targetDirectory, filter);

            static void CopyImpl(string source, string target, Func<string, bool>? filter)
            {
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);

                foreach (var file in Directory.GetFiles(source))
                {
                    if (filter != null && !filter(file))
                        continue;
                    var fileName = Path.GetFileName(file);
                    var targetFilePath = Path.Combine(target, fileName);
                    //Logger.Log($"CopyFile, {file} => {targetFilePath}");
                    File.Copy(file, targetFilePath);
                }

                foreach (var dir in Directory.GetDirectories(source))
                {
                    var dirName = Path.GetFileName(dir);
                    CopyImpl(dir, Path.Combine(target, dirName), filter);
                }
            }
        }

        public static string ReadAllText(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "";
            if (!File.Exists(filePath))
                return "";

            try
            {
                using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }

        [ThreadStatic] private static StringBuilder? s_builderForWriteAllText0;
        [ThreadStatic] private static StringBuilder? s_builderForWriteAllText1;
        private static StringBuilder BuilderForWriteAllText0 => s_builderForWriteAllText0 ??= new();
        private static StringBuilder BuilderForWriteAllText1 => s_builderForWriteAllText1 ??= new();

        public static void WriteAllBytes(string? filePath, byte[] bytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;
                var dir = Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.Write(bytes);
                }
            }
            catch
            {
            }
        }

        public static void WriteAllText(string? filePath, string content, bool skipIfContentSame = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;
                var dir = Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(filePath))
                {
                    if (skipIfContentSame)
                    {
                        BuilderForWriteAllText0.Clear();
                        BuilderForWriteAllText1.Clear();
                        try
                        {
                            BuilderForWriteAllText0.Append(ReadAllText(filePath));
                        }
                        catch
                        {
                            BuilderForWriteAllText0.Clear();
                        }

                        BuilderForWriteAllText1.Append(content);

                        BuilderForWriteAllText0.Replace("\r", "");
                        BuilderForWriteAllText1.Replace("\r", "");

                        if (BuilderForWriteAllText0.Equals(BuilderForWriteAllText1))
                            return;
                    }

                    File.Delete(filePath);
                }

                using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                }

                //Logger.Log($"WriteAllText, {filePath}");
            }
            catch
            {
            }
        }

        public static void CreateParentDirectoryIfNotExists(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;
            var dir = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static readonly object s_lockerForCreateParentDirectoryIfNotExistsThreadSafe = new object();

        public static void CreateParentDirectoryIfNotExistsThreadSafe(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;
            var dir = Path.GetDirectoryName(filePath)!;
            lock (s_lockerForCreateParentDirectoryIfNotExistsThreadSafe)
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// 归一化的路径
        /// 1. 使用 / 代替 \
        /// 2. 结尾不带 /
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizedPath(this FileSystemInfo? self)
        {
            if (self == null || !self.Exists)
                return "";
            return NormalizedPath(self.FullName);
        }

        /// <summary>
        /// 归一化的路径
        /// 1. 使用 / 代替 \
        /// 2. 结尾不带 /
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizedPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            var sb = SharedStringBuilder;
            sb.Clear();
            if (AppendNormalizedPath(sb, path))
                return sb.ToString();
            return path!;
        }

        public static string NormalizedLowerPath(string? path) => NormalizedPath(path)?.ToLower() ?? "";

        private static bool IsAnyPathSeparator(this char c)
        {
            return c == '/' || c == '\\';
        }

        private static bool AppendNormalizedPath(this StringBuilder builder, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            bool changed = false;
            var span = path.AsSpan();
            var len = span.Length;
            for (int i = 0; i < len - 1; ++i)
            {
                if (i == 0 && builder.Length > 0)
                {
                    changed = true;
                    if (!builder[builder.Length - 1].IsAnyPathSeparator())
                        builder.Append('/');
                    if (!span[i].IsAnyPathSeparator())
                        builder.Append(span[i]);
                }
                else if (span[i] == '\\')
                {
                    changed = true;
                    builder.Append('/');
                }
                else
                {
                    builder.Append(span[i]);
                }
            }

            if (span[len - 1] != '\\' && span[len - 1] != '/')
            {
                builder.Append(span[len - 1]);
            }
            else
            {
                changed = true;
            }

            return changed;
        }

        public static string CombinePath(string? path1, string? path2)
        {
            var sb = SharedStringBuilder;
            sb.Clear();
            AppendNormalizedPath(sb, path1);
            AppendNormalizedPath(sb, path2);
            return sb.ToString();
        }

        public static string CombinePath(string? path1, params string?[]? paths)
        {
            var sb = SharedStringBuilder;
            sb.Clear();
            AppendNormalizedPath(sb, path1);
            if (paths != null)
            {
                foreach (var path in paths)
                    AppendNormalizedPath(sb, path);
            }

            return sb.ToString();
        }

        public static List<string> CollectAllFiles(string directory, Func<string, bool> filter)
        {
            var result = new List<string>();
            CollectImpl(result, directory, filter);
            return result;

            static void CollectImpl(List<string> result, string directory, Func<string, bool> filter)
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                result.AddRange(files);

                var subDirs = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(filter)
                    .ToArray();

                foreach (var subDir in subDirs)
                    CollectImpl(result, subDir, filter);
            }
        }

        public static void CleanupDirectory(string directory, Func<string, bool> fileFilter, Func<string, bool> folderFilter)
        {
            var willDeleteFiles = Directory.GetFiles(directory)
                .Where(fileFilter)
                .ToArray();
            var willDeleteDirs = Directory.GetDirectories(directory)
                .Where(folderFilter)
                .ToArray();
            foreach (var file in willDeleteFiles)
                DeleteFile(file);
            foreach (var dir in willDeleteDirs)
                DeleteDirectory(dir);
        }

        public static void CleanupDirectory(string directory, string searchPattern, IEnumerable<string> ignorePaths)
        {
            var normalizedIgnorePaths = ignorePaths.Select(NormalizedLowerPath).ToHashSet();
            var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)
                .Select(NormalizedLowerPath)
                .ToArray();
            foreach (var path in files)
                if (!normalizedIgnorePaths.Contains(path))
                    DeleteFile(path);
            CleanupEmptyDirectory(directory);
        }

        public static void CleanupEmptyDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return;
            directory = NormalizedPath(directory);
            foreach (var c in Directory.GetDirectories(directory))
                CleanupEmptyDirectory(c);
            foreach (var c in Directory.GetFiles(directory, "*.meta", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileNameWithoutExtension(c);
                var p = Path.Combine(directory, fileName);
                if (File.Exists(p) || Directory.Exists(p))
                    continue;
                DeleteFile(c);
            }

            if (Directory.GetDirectories(directory).Length > 0 || Directory.GetFiles(directory).Length > 0)
                return;
            DeleteDirectory(directory);
            var name = Path.GetFileName(directory);
            var parent = Path.GetDirectoryName(directory);
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(parent))
                return;
            var meta = Path.Combine(parent, name + ".meta");
            DeleteFile(meta);
        }

        public static string ComputeMd5(byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                var builder = SharedStringBuilder;
                for (int i = 0, len = hash.Length; i < len; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string GetFileMd5(string filepath)
        {
            return ComputeMd5(File.ReadAllBytes(filepath));
        }

        public static string GetFullPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            return NormalizedPath(fullPath);
        }

        public static string GetFileName(string path) => Path.GetFileName(path);
        public static string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

        public static string GetDirectoryName(string path)
        {
            var directory = Path.GetDirectoryName(path);
            return NormalizedPath(directory);
        }

        public static string GetExtension(string path) => Path.GetExtension(path);

        public static bool FileExists(string path) => File.Exists(path);
        public static bool DirectoryExists(string path) => Directory.Exists(path);
        public static bool FileOrDirectoryExists(string path) => FileExists(path) || DirectoryExists(path);

        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void MakeDirectoryExistsAndEmpty(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        public static string[] GetFilesByExtensions(string directory, SearchOption searchOption, params string[]? extensions)
        {
            var files = Directory.GetFiles(directory, "*", searchOption);
            if (extensions != null && extensions.Length != 0)
                return files.Where(x => extensions.Contains(Path.GetExtension(x))).ToArray();
            return files;
        }


        public static string GetRelativePath(string relativeTo, string path) => NormalizedPath(Path.GetRelativePath(relativeTo, path));
    }
}