namespace Net.Leksi;
public interface IPartialLoader<T>
{
    PartialLoaderState State { get; }
    List<T> Result { get; }
    List<T> Chunk { get; }
    void Initialize(IAsyncEnumerable<T> data, PartialLoaderOptions options);
    Task StartAsync();
    Task StartAsync(IAsyncEnumerable<T> data, PartialLoaderOptions options);
    Task ContinueAsync();
    void Reset();
}

