namespace Net.Leksi.PartialLoader
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Тип-заглушка для <see cref="PartialLoadingJsonSerializer{T}"/>
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonTypeStub<T>
    {
        /// <summary>
        /// <para xml:lang="ru">
        /// Объект-заглушка для для формального возврата из 
        /// <see cref="PartialLoadingJsonSerializer{T}.Write(System.Text.Json.Utf8JsonWriter, JsonTypeStub{T}, System.Text.Json.JsonSerializerOptions)"/>
        /// </para>
        /// <para xml:lang="en">
        /// Stub object for formal return from
        /// <see cref="PartialLoadingJsonSerializer{T}.Write(System.Text.Json.Utf8JsonWriter, JsonTypeStub{T}, System.Text.Json.JsonSerializerOptions)"/>
        /// </para>        /// </summary>
        public static JsonTypeStub<T> Instance { get; private set; } = new();
        private JsonTypeStub() { }

    }
}
