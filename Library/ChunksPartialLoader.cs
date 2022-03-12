namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс <see cref="PartialLoader{T}"/>, для загрузки каждой порции объектов в свойство <see cref="Chunks"/>
/// </para>
/// <para xml:lang="en">
/// Class <see cref="PartialLoader{T}"/>, for loading each portion of objects into the property <see cref="Chunks"/>
/// </para>
/// </summary>
/// <inheritdoc/>
public class ChunksPartialLoader<T> : PartialLoader<T>
{
    private readonly List<T> _chunks = new();

    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, содержащее очередную порцию объектов
    /// </para>
    /// <para xml:lang="en">
    /// Property containing the next portion of objects
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от
    /// <see cref="PartialLoaderState.Partial"/> и <see cref="PartialLoaderState.Full"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than
    /// <see cref="PartialLoaderState.Partial"/> and <see cref="PartialLoaderState.Full"/>
    /// </para>    
    /// </exception>
    public List<T> Chunks
    {
        get
        {
            if(State != PartialLoaderState.Partial && State != PartialLoaderState.Full)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Partial} or {PartialLoaderState.Full}, present: {State}");
            }
            return _chunks;
        }
    }


    /// <inheritdoc/>
    public override async Task LoadAsync()
    {
        if(State is PartialLoaderState.New)
        {
            AddUtilizer(Utilizer);
        }
        _chunks.Clear();
        await base.LoadAsync();
    }

    private T Utilizer(T item)
    {
        _chunks.Add(item);
        return item;
    }

}