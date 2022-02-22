namespace Net.Leksi;
public interface IPartialLoader<T>
{
    PartialLoaderState State { get; }
    List<T> Result { get; }
    List<T> Chunk { get; }
    Task StartAsync(IAsyncEnumerable<T> data, PartialLoaderOptions options);
    Task ContinueAsync();
    void Reset();
}

