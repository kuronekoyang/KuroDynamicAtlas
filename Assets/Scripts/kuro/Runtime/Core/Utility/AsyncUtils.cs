using System.Threading.Tasks;

namespace kuro
{
    public static class AsyncUtils
    {
        public static void Forget(this ValueTask task)
        {
            // do nothing
        }
    }
}