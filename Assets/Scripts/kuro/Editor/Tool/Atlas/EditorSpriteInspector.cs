using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace kuro
{
    [CustomEditor(typeof(EditorSpriteData))]
    public class EditorSpriteInspector : Editor
    {
        private static LazyUnityObject<Texture2D> s_backgroundTexture = new(
            () =>
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Color[] colors = new Color[4];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.clear;
                texture.SetPixels(0, 0, 2, 2, colors);
                texture.Apply();
                return texture;
            }
        );

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var data = target as EditorSpriteData;
            if (data == null)
                return;

            var texture = EditorAtlasManager.GetThumbnail(data.SpriteData.Id);
            var frameSize = new Vector2(texture.width, texture.height);
            float zoomLevel = Mathf.Min(r.width / frameSize.x, r.height / frameSize.y);
            Rect wantedRect = new Rect(r.x, r.y, frameSize.x * zoomLevel, frameSize.y * zoomLevel);
            wantedRect.center = r.center;

            EditorGUI.DrawTextureTransparent(r, s_backgroundTexture, ScaleMode.ScaleToFit);
            EditorGUI.DrawTextureTransparent(wantedRect, texture, ScaleMode.ScaleToFit);
        }

        public override string GetInfoString()
        {
            var data = target as EditorSpriteData;
            if (data == null)
                return "";

            var spriteData = data.SpriteData.SpriteData;
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, "({0}x{1}) ({2}, {3}, {4}, {5})",
                (int)spriteData.Size.x, (int)spriteData.Size.y,
                spriteData.Uv.x, spriteData.Uv.y, spriteData.Uv.z, spriteData.Uv.w);
        }
    }
}