using BigCatsDataContract;
using Net.Leksi;

namespace BigCatsDataServer
{
    public class CatsLoaderStorage
    {
        public Dictionary<string, IPartialLoader<Cat>> Data { get; init; } = new();
    }
}
