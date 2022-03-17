using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс для загрузки данных порциями непосредственно в JSON
/// </para>
/// <para xml:lang="en">
/// Class for loading data in chunks directly into JSON
/// </para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class PartialLoadingJsonSerializer<T> : JsonConverter<JsonTypeStub<T>> where T : class
{
    private readonly PartialLoader<T> _partialLoader;

    /// <summary>
    /// <para xml:lang="ru">
    /// Конструктор
    /// </para>
    /// <para xml:lang="en">
    /// Constructor
    /// </para>
    /// </summary>
    /// <param name="partialLoader">
    /// <para xml:lang="ru">
    /// Экземпляр <see cref="PartialLoader{T}"/>, используемый для частичной загрузки
    /// </para>
    /// <para xml:lang="en">
    /// Instance <see cref="PartialLoader{T}"/> used for partial loading
    /// </para>
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public PartialLoadingJsonSerializer(PartialLoader<T> partialLoader)
    {
        if(partialLoader is null)
        {
            throw new ArgumentNullException(nameof(partialLoader));
        }
        _partialLoader = partialLoader;
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(JsonTypeStub<T>) == typeToConvert;
    }

    /// <inheritdoc>
    /// <para xml:lang="ru">
    /// Данный класс не предназначен для выполнения десериализации
    /// </para>
    /// <para xml:lang="en">
    /// This class is not designed to perform deserialization
    /// </para>
    /// </inheritdoc>
    public override JsonTypeStub<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async void Write(Utf8JsonWriter writer, JsonTypeStub<T> value, JsonSerializerOptions options)
    {
        if (_partialLoader.State is not PartialLoaderState.New && _partialLoader.State is not PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New} or {PartialLoaderState.Partial}, present: {_partialLoader.State}");
        }

        _partialLoader.AddUtilizer(item => JsonSerializer.Serialize(writer, item, item.GetType(), options));
        writer.WriteStartArray();
        try
        {
            await _partialLoader.LoadAsync();
        }
        catch (Exception)
        {
            throw;
        }


        if (_partialLoader.State is PartialLoaderState.Full)
        {
            writer.WriteNullValue();
        }
        writer.WriteEndArray();
    }


}
