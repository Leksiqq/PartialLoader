namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс <see cref="PartialLoader{T}"/>, для загрузки всех объектов в свойство <see cref="Result"/>
/// </para>
/// <para xml:lang="en">
/// Class <see cref="PartialLoader{T}"/>, for loading all objects into property <see cref="Result"/>
/// </para>
/// </summary>
/// <inheritdoc/>
public class ResultPartialLoader<T> : PartialLoader<T>
{
    private readonly List<T> _result = new();

    /// <inheritdoc cref="ChunksResultPartialLoader{T}"/>
    public List<T> Result
    {
        get
        {
            if(State != PartialLoaderState.Full)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Full}, present: {State}");
            }
            return _result;
        }
    }

    /// <inheritdoc/>
    public override async Task LoadAsync()
    {
        if (State is PartialLoaderState.New)
        {
            AddUtilizer(Utilizer);
        }
        await base.LoadAsync();
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _result.Clear();
    }

    private T Utilizer(T item)
    {
        _result.Add(item);
        return item;
    }

}