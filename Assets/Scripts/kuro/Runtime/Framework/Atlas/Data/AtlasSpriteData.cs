using System;

namespace kuro
{
    [Serializable]
    public class AtlasSpriteData
    {
        public SpriteId Id;
        public SpriteData SpriteData = new();
    }
}