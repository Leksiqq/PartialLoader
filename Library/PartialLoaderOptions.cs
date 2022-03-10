using Net.Leksi.PartialLoader;

namespace Net.Leksi;
public class PartialLoaderOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(-1);
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    public int Paging { get; set; } = 0;
    public bool ConfigureAwait { get; set; } = false;
    public IList<IUtilizer> Utilizers { get; init; } = new List<IUtilizer>();

}

