using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace kuro
{
    public class AtlasTool
    {
        private const int Padding = 2;
        private const string KSourceDirectory = "Design";
        private const string KTempDirectory = "Assets/TempTextures";
        private const string KDestinationDirectory = "Assets/GameResources/DynamicTextures";
        private const string KAstcEncoderPath = "Tools/astcenc-5.2.0-windows-x64/bin/astcenc-sse2.exe";

        private static readonly string UnityProjectPath = IOUtils.GetDirectoryName(Application.dataPath);


        [MenuItem("Tools/Generate ASTC Textures")]
        private static void GenerateAstcTextures()
        {
            var sourceDirectory = IOUtils.CombinePath(UnityProjectPath, KSourceDirectory);
            var tempDirectory = IOUtils.CombinePath(UnityProjectPath, KTempDirectory);
            var destinationDirectory = IOUtils.CombinePath(UnityProjectPath, KDestinationDirectory);

            var sourceFiles = Directory.GetFiles(sourceDirectory, "*.png", SearchOption.AllDirectories);

            var atlasDb = new AtlasDb();
            var editorAtlasDb = LoadOrCreateEditorAtlasDb();

            const int blockSize = AtlasManager.DynamicAtlasAstcBlockSize;

            var astcFileList = new List<string>();
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = IOUtils.GetRelativePath(sourceDirectory, sourceFile);
                var tempPath = IOUtils.CombinePath(tempDirectory, relativePath);
                var destinationPath = IOUtils.CombinePath(destinationDirectory, IOUtils.GetDirectoryName(relativePath),
                    IOUtils.GetFileNameWithoutExtension(relativePath) + ".astc");

                IOUtils.CopyFile(sourceFile, tempPath);
                var sourceTextureAssetPath = IOUtils.GetRelativePath(UnityProjectPath, tempPath);
                var sourceTexture = LoadTempTexture(sourceTextureAssetPath);

                var width = sourceTexture.width;
                var height = sourceTexture.height;

                var newWidth = (((width + Padding * 2) + blockSize - 1) / blockSize) * blockSize;
                var newHeight = (((height + Padding * 2) + blockSize - 1) / blockSize) * blockSize;

                var tempTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                Graphics.CopyTexture(sourceTexture, 0, 0, 0, 0, width, height, tempTexture, 0, 0, Padding, Padding);

                using var tempPixels = tempTexture.GetPixelData<Color32>(0);

                RectInt pixelRect = new RectInt(Padding, Padding, width - 1, height - 1);
                FillPadding(tempPixels, newWidth, newHeight, pixelRect);

                FlipY(tempPixels, newWidth, newHeight);

                tempTexture.Apply();
                IOUtils.WriteAllBytes(tempPath, tempTexture.EncodeToPNG());

                IOUtils.CreateParentDirectoryIfNotExists(destinationPath);
                RunCommand(KAstcEncoderPath, $"-cl {tempPath.AddDoubleQuotation()} {destinationPath.AddDoubleQuotation()} {blockSize}x{blockSize} -thorough");
                astcFileList.Add(destinationPath);

                var dynamicAtlasData = new DynamicAtlasData();
                atlasDb.DynamicAtlasList.Add(dynamicAtlasData);
                dynamicAtlasData.TextureResource = IOUtils.GetRelativePath(UnityProjectPath, destinationPath);
                dynamicAtlasData.TextureWidth = newWidth;
                dynamicAtlasData.TextureHeight = newHeight;
                dynamicAtlasData.SpriteData.Id = new SpriteId(IOUtils.GetFileNameWithoutExtension(destinationPath));

                var frame = new Frame();
                frame.frame = new Rect(Padding, Padding, width, height);
                frame.rotated = false;
                frame.trimmed = false;
                frame.spriteSourceSize = new Rect(0, 0, width, height);
                frame.sourceSize = new Vector2(width, height);
                ApplyAtlasSpriteData(dynamicAtlasData.SpriteData.SpriteData, frame, new Vector2(newWidth, newHeight), Vector4.zero, true, 100);

                var editorSpriteData = ScriptableObject.CreateInstance<EditorSpriteData>();
                editorSpriteData.name = dynamicAtlasData.SpriteData.Id.Name;
                editorSpriteData.SpriteData = dynamicAtlasData.SpriteData;
                editorAtlasDb.SpriteDataList.Add(editorSpriteData);
                AssetDatabase.AddObjectToAsset(editorSpriteData, editorAtlasDb);
            }

            EditorUtility.SetDirty(editorAtlasDb);
            AssetDatabase.SaveAssetIfDirty(editorAtlasDb);
            IOUtils.WriteAllText(AtlasManager.KAtlasDbPath, JsonUtility.ToJson(atlasDb));

            IOUtils.DeleteDirectory(tempDirectory);
            IOUtils.DeleteFile(tempDirectory + ".meta");
            IOUtils.CleanupDirectory(destinationDirectory, ".astc", astcFileList);

            AssetDatabase.Refresh();
        }

        private static EditorAtlasDb LoadOrCreateEditorAtlasDb()
        {
            var path = EditorAtlasManager.EditorAtlasDbPath;
            var editorAtlasDb = AssetDatabase.LoadAssetAtPath<EditorAtlasDb>(path);
            if (editorAtlasDb == null)
            {
                IOUtils.DeleteFile(path);
                IOUtils.CreateParentDirectoryIfNotExists(path);

                editorAtlasDb = ScriptableObject.CreateInstance<EditorAtlasDb>();
                AssetDatabase.CreateAsset(editorAtlasDb, path);
            }

            if (editorAtlasDb.SpriteDataList != null)
            {
                for (int i = editorAtlasDb.SpriteDataList.Count - 1; i >= 0; i--)
                    AssetDatabase.RemoveObjectFromAsset(editorAtlasDb.SpriteDataList[i]);
            }

            editorAtlasDb.SpriteDataList.Clear();
            EditorUtility.SetDirty(editorAtlasDb);
            AssetDatabase.SaveAssetIfDirty(editorAtlasDb);
            return editorAtlasDb;
        }

        private static Texture2D LoadTempTexture(string path)
        {
            AssetDatabase.Refresh();
            var sourceTextureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            var ts = sourceTextureImporter!.GetDefaultPlatformTextureSettings();
            ts.format = TextureImporterFormat.RGBA32;
            sourceTextureImporter.SetPlatformTextureSettings(ts);
            sourceTextureImporter.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void FillPadding(NativeArray<Color32> pixels, int width, int height, RectInt rect)
        {
            for (int x = 0; x < width; ++x)
            for (int y = 0; y < height; ++y)
            {
                var xx = Mathf.Clamp(x, rect.xMin, rect.xMax);
                var yy = Mathf.Clamp(y, rect.yMin, rect.yMax);
                if (x == xx && y == yy)
                    continue;

                pixels[x + y * width] = pixels[xx + yy * width];
            }
        }

        private static void FlipY(NativeArray<Color32> pixels, int width, int height)
        {
            for (int x = 0; x < width; ++x)
            for (int y = 0; y < height / 2; ++y)
            {
                var i0 = x + y * width;
                var i1 = x + (height - 1 - y) * width;
                (pixels[i0], pixels[i1]) = (pixels[i1], pixels[i0]);
            }
        }


        private static void RunCommand(string command, string arguments)
        {
            var info = new ProcessStartInfo(command, arguments);
            info.WorkingDirectory = ".";
            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = false;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var process = Process.Start(info);
            process?.WaitForExit();
        }

        public struct Frame
        {
            public Rect frame;
            public bool rotated;
            public bool trimmed;
            public Rect spriteSourceSize;
            public Vector2 sourceSize;
        }

        private static void ApplyAtlasSpriteData(SpriteData data, Frame frame, Vector2 atlasSize, Vector4 border, bool flipY,
            float pixelsPerUnit)
        {
            var originalSize = new Vector2(frame.sourceSize.x, frame.sourceSize.y);
            var trimmedSize = new Vector2(frame.frame.width, frame.frame.height);

            var sizeInAtlas = frame.rotated ? new Vector2(trimmedSize.y, trimmedSize.x) : trimmedSize;
            var posInAtlas = new Vector2
            {
                x = frame.frame.x,
                y = atlasSize.y - frame.frame.y - sizeInAtlas.y,
            };
            var clipPos = new Vector2
            {
                x = frame.spriteSourceSize.x,
                y = originalSize.y - frame.spriteSourceSize.y - frame.spriteSourceSize.height,
            };

            var uv = new Vector4
            {
                x = posInAtlas.x / atlasSize.x,
                y = posInAtlas.y / atlasSize.y,
                z = (posInAtlas.x + sizeInAtlas.x) / atlasSize.x,
                w = (posInAtlas.y + sizeInAtlas.y) / atlasSize.y
            };
            var innerUv = new Vector4
            {
                x = (posInAtlas.x + border.x) / atlasSize.x,
                y = (posInAtlas.y + border.y) / atlasSize.y,
                z = (posInAtlas.x + sizeInAtlas.x - border.z) / atlasSize.x,
                w = (posInAtlas.y + sizeInAtlas.y - border.w) / atlasSize.y,
            };

            data.PixelsPerUnit = pixelsPerUnit;
            data.Size = originalSize;
            data.PaddingFactor = new Vector4
            {
                x = clipPos.x / originalSize.x,
                y = clipPos.y / originalSize.y,
                z = (clipPos.x + trimmedSize.x) / originalSize.x,
                w = (clipPos.y + trimmedSize.y) / originalSize.y,
            };
            if (frame.rotated)
            {
                data.Uv = new Vector4
                {
                    x = uv.x,
                    y = uv.w,
                    z = uv.z,
                    w = uv.y,
                };
                data.InnerUv = new Vector4
                {
                    x = innerUv.x,
                    y = innerUv.w,
                    z = innerUv.z,
                    w = innerUv.y,
                };
            }
            else
            {
                data.Uv = uv;
                data.InnerUv = innerUv;
            }

            data.Border = border;
            data.IsRotated = frame.rotated;
            data.IsPacked = true;

            if (flipY)
            {
                (data.PaddingFactor.y, data.PaddingFactor.w) = (1f - data.PaddingFactor.w, 1f - data.PaddingFactor.y);
                (data.Border.y, data.Border.w) = (data.Border.w, data.Border.y);
                (data.Uv.y, data.Uv.w) = (1f - data.Uv.w, 1f - data.Uv.y);
                (data.InnerUv.y, data.InnerUv.w) = (1f - data.InnerUv.w, 1f - data.InnerUv.y);
            }
        }
    }
}