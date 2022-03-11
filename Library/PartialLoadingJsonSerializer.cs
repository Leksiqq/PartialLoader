using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Leksi.PartialLoader;

public class PartialLoadingJsonSerializer<T> : JsonConverter<JsonTypeStub<T>>
{
    private readonly PartialLoader<T> _partialLoader;
    private Utf8JsonWriter _writer = null!;
    private JsonSerializerOptions _jsonOptions = null!;

    public PartialLoadingJsonSerializer(PartialLoader<T> partialLoader)
    {
        if(partialLoader is null)
        {
            throw new ArgumentNullException(nameof(partialLoader));
        }
        _partialLoader = partialLoader;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(JsonTypeStub<T>) == typeToConvert;
    }

    public override JsonTypeStub<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override async void Write(Utf8JsonWriter writer, JsonTypeStub<T> value, JsonSerializerOptions options)
    {
        //_writer = writer;
        //_jsonOptions = options;
        //if(_partialLoader.State != PartialLoaderState.Initialized && _partialLoader.State != PartialLoaderState.Partial)
        //{
        //    throw new InvalidOperationException($"Expected State: {PartialLoaderState.Initialized} or {PartialLoaderState.Partial}, present: {_partialLoader}");
        //}

        //if(_partialLoader.State == PartialLoaderState.Initialized)
        //{
        //    _partialLoader.
        //}

        //writer.WriteStartArray();

        //switch (State)
        //{
        //    case PartialLoaderState.New:
        //        await StartAsync();
        //        break;
        //    case PartialLoaderState.Partial:
        //        await ContinueAsync();
        //        break;
        //    default:
        //        throw new InvalidOperationException($"Expected State: {PartialLoaderState.New} or {PartialLoaderState.Partial}, present: {State}");
        //}
        //if (State == PartialLoaderState.Full)
        //{
        //    writer.WriteNullValue();
        //}
        writer.WriteEndArray();
    }

    private object Utilizer(object item)
    {
        JsonSerializer.Serialize(_writer, item, item.GetType(), _jsonOptions);
        return item;
    }

}
