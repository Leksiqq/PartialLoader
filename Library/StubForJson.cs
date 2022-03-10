namespace Net.Leksi;

public class StubForJson<T>
{
    public static StubForJson<T> Instance { get; private set; } = new();
    private StubForJson() { }

}
