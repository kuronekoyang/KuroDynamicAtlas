using System;
using UnityEngine;

namespace kuro
{
    public class OverheadOrthoRenderHelper : IDisposable
    {
        private readonly int _texWidth;
        private readonly int _texHeight;
        private RenderTexture _renderTexture;
        private readonly bool _didPushMatrix;
        private readonly RenderTexture _oldRenderTexture;
        private readonly Camera _oldCamera;

        public OverheadOrthoRenderHelper(OBB2D obb, float maxY, int texWidth, int texHeight)
        {
            _didPushMatrix = false;
            _texWidth = texWidth;
            _texHeight = texHeight;

            _oldCamera = Camera.current;

            _renderTexture = RenderTexture.GetTemporary(texWidth, texHeight, 32);
            _oldRenderTexture = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            GL.Clear(true, true, Color.clear);

            var center = obb.Center;
            var pos = new Vector3(center.x, maxY + 1, center.y);
            var halfSize = obb.Size * 0.5f;

            var viewMatrix = Matrix4x4.TRS(pos, Quaternion.Euler(90, 0, obb.RotationCCW), new Vector3(1, 1, -1)).inverse;
            var projectionMatrix = Matrix4x4.Ortho(-halfSize.x, halfSize.x, -halfSize.y, halfSize.y, 0.03f, 2000.0f);

            _didPushMatrix = true;
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.LoadProjectionMatrix(projectionMatrix);
            GL.modelview = viewMatrix;

            if (_oldCamera)
            {
                _oldCamera.worldToCameraMatrix = viewMatrix;
                _oldCamera.projectionMatrix = projectionMatrix;
            }
        }

        public void DrawMesh(Mesh mesh)
        {
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix)
        {
            Graphics.DrawMeshNow(mesh, matrix);
        }

        public Texture2D CreateTexture(TextureFormat format = TextureFormat.RGBA32)
        {
            var texture = new Texture2D(_texWidth, _texHeight, format, false);
            texture.ReadPixels(new Rect(0, 0, _texWidth, _texHeight), 0, 0);
            texture.Apply();
            return texture;
        }

        public RenderTexture TransferRenderTexture()
        {
            var rt = _renderTexture;
            _renderTexture = null;
            return rt;
        }

        public void Dispose()
        {
            if (_didPushMatrix)
            {
                GL.PopMatrix();
            }

            RenderTexture.active = _oldRenderTexture;

            if (_renderTexture)
                RenderTexture.ReleaseTemporary(_renderTexture);

            if (_oldCamera)
            {
                _oldCamera.ResetWorldToCameraMatrix();
                _oldCamera.ResetProjectionMatrix();
            }
        }
    }
}