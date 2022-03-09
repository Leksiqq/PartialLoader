namespace Net.Leksi;
public class PartialLoaderOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(-1);
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    public Action<object?>? OnItem { get; set; } = null;
    public int Paging { get; set; } = 0;
    public bool ConfigureAwait { get; set; } = false;
    public bool StoreResult { get; set; } = false;

}

