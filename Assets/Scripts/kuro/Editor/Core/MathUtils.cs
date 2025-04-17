using UnityEngine;

namespace kuro
{
    public static class MathUtils
    {
        public static Vector2 RotateCcw(this Vector2 self, float angle)
        {
            var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
            var p = matrix * new Vector4(self.x, self.y, 0, 1);
            return new Vector2(p.x, p.y);
        }

        public static float GetRotateAngleCcw(this Vector2 from, Vector2 to)
        {
            var a = from.normalized;
            var b = to.normalized;
            var dot = Vector2.Dot(a, b);
            var angle = 0.0f;
            if (Mathf.Abs(dot - 1) <= 1e-5)
                angle = 0.0f;
            else if (Mathf.Abs(dot + 1) <= 1e-5)
                angle = Mathf.PI;
            else
                angle = Mathf.Acos(dot);
            if (Cross(a, b) < 0)
                angle = -angle;

            return angle * Mathf.Rad2Deg;
        }

        public static float Cross(this Vector2 vector1, Vector2 vector2) => vector1.x * vector2.y - vector1.y * vector2.x;
    }
}