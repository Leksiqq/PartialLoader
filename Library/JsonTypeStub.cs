namespace Net.Leksi.PartialLoader
{
    public class JsonTypeStub<T>
    {
        public static JsonTypeStub<T> Instance { get; private set; } = new();
        private JsonTypeStub() { }

    }
}
