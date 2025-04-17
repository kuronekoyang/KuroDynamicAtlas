namespace kuro
{
    public class AtlasSprite
    {
        public AtlasSpriteData Data;
        public AtlasHandle Atlas;

        public AtlasSprite(AtlasSpriteData data, AtlasHandle atlas)
        {
            this.Data = data;
            this.Atlas = atlas;
        }
    }
}