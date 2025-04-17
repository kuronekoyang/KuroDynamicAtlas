using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace kuro
{
    public class DummyAtlas : AtlasHandle
    {
        public AtlasSprite DummyAtlasSprite
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get;
        }

        public override KSprite GetSprite(SpriteId id) => AtlasManager.FallbackRenderSprite;
        protected override string GetTextureResource() => "DummyAtlas";

        protected override async ValueTask LoadResourceAsync()
        {
            // do nothing
        }

        public override void UnloadResource()
        {
        }

        public DummyAtlas()
        {
            var atlasSpriteData = new AtlasSpriteData();
            atlasSpriteData.SpriteData = new();
            DummyAtlasSprite = new AtlasSprite(atlasSpriteData, this);
        }
    }
}