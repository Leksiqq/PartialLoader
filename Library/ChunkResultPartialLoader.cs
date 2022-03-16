namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс <see cref="PartialLoader{T}"/>, для загрузки каждой порции объектов в свойство <see cref="Chunks"/> 
/// и всех объектов в свойство <see cref="Result"/>
/// </para>
/// <para xml:lang="en">
/// Class <see cref="PartialLoader{T}"/>, for loading each portion of objects into the property <see cref="Chunks"/>
/// and all objects into property <see cref="Result"/>
/// </para>
/// </summary>
/// <inheritdoc/>
public class ChunkResultPartialLoader<T>: ChunkPartialLoader<T> where T : class
{
    private readonly List<T> _result = new();

    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, содержащее все объекты
    /// </para>
    /// <para xml:lang="en">
    /// Property containing all objects
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.Full"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.Full"/>
    /// </para>    
    /// </exception>
    public List<T> Result
    {
        get 
        {
            if (State != PartialLoaderState.Full)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Full}, present: {State}");
            }
            return _result; 
        }
    }

    /// <inheritdoc/>
    public override async Task LoadAsync()
    {
        AddUtilizer(Utilizer);
        await base.LoadAsync();
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _result.Clear();
    }

    private void Utilizer(T item)
    {
        _result.Add(item);
    }

}
