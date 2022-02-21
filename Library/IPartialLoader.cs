using System.Collections.ObjectModel;

namespace Net.Leksi;
public interface IPartialLoader<T>
{
    PartialLoaderState State { get; }
    Collection<T> Result { get; }
    List<T> Chunk { get; }
    Task StartAsync(IAsyncEnumerable<T> data, PartialLoaderOptions options, Collection<T> result = null);
    Task ContinueAsync();
    void Reset();
}

