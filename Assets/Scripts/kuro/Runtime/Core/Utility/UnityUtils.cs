using UnityEngine;

namespace kuro
{
    public static class UnityUtils
    {
        public static void SafeDestroy(this UnityEngine.Object obj)
        {
            if (!obj)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(obj);
            else
#endif
                UnityEngine.Object.Destroy(obj);
        }
    }
}