using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace kuro
{
    public static class KSpriteDrawUtility
    {
        private static readonly List<Vector2> s_sharedUvList = new();
        public static readonly LazyUnityObject<Material> s_materialForThumbnail = new(() => new Material(Shader.Find("UI/Default")));

        private static readonly LazyUnityObject<Mesh> s_meshForThumbnail = new(() =>
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 0, 0),
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
            };
            mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
            mesh.RecalculateBounds();
            return mesh;
        });

        private static readonly OBB2D s_obbForThumbnail = new OBB2D(new Vector2(0.5f, 0.5f), new Vector2(1, 1), new Vector2(1, 0));

        public static Texture2D CreateThumbnail(this KSprite sprite, int thumbnailSize)
        {
            if (!sprite.IsValid)
                return null;
            var data = sprite.Data;
            var sizeF = data.GetTrimmedSize();
            var size = new Vector2Int((int)sizeF.x, (int)sizeF.y);
            int width, height;
            if (size.x >= size.y)
            {
                width = Mathf.Min(thumbnailSize, size.x);
                height = (width * size.y) / size.x;
            }
            else
            {
                height = Mathf.Min(thumbnailSize, size.y);
                width = (height * size.x) / size.y;
            }

            var mat = s_materialForThumbnail.Value;
            var mesh = s_meshForThumbnail.Value;

            data.GetUv(out var lb, out var lt, out var rt, out var rb);

            s_sharedUvList.Clear();
            s_sharedUvList.Add(lb);
            s_sharedUvList.Add(lt);
            s_sharedUvList.Add(rt);
            s_sharedUvList.Add(rb);
            mesh.SetUVs(0, s_sharedUvList);
            mat.SetTexture("_MainTex", sprite.Texture);

            using (var helper = new OverheadOrthoRenderHelper(s_obbForThumbnail, 2, width, height))
            {
                mat.SetPass(0);
                helper.DrawMesh(mesh);
                return helper.CreateTexture();
            }
        }
    }
}