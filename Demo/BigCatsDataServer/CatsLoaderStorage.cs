using Net.Leksi;

namespace BigCatsDataServer
{
    public class CatsLoaderStorage
    {
        public Dictionary<string, PartialLoader<Cat>> Data { get; init; } = new();
    }
}
