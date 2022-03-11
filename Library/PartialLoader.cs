using System.Collections.Concurrent;

namespace Net.Leksi.PartialLoader;

/// <summary>
/// <para xml:lang="ru">
/// Класс для загрузки частями данных, предоставляемых асихронным поставщиком. 
/// При вызове метода <see cref="LoadAsync"/> загружаемые объекты 
/// подаются на вход настраиваемой цепочки "утилизаторов" 
/// по мере поступления от поставщика. По истечении заданного времени или достижения 
/// заданного числа обработанных объектов после очередного вызова
/// управление возвращается вызывающему коду. Анализ свойства <see cref="State"/> 
/// позволяет сделать вывод о необходимости дальнейших вызовов. 
/// Также предусмотрена возможность прерывания работы поставщика.
/// </para>
/// <para xml:lang="en">
/// Class for loading parts of data provided by an asynchronous provider.
/// When calling the <see cref="LoadAsync"/> method, the objects to be loaded
/// are fed to the input of a custom chain of "utilizers"
/// as received from the provider. After the specified time has elapsed or
/// the specified number of processed objects after the next call
/// control is returned to the calling code. Parsing the property <see cref="State"/>
/// allows you to conclude that further calls are needed.
/// It is also possible to interrupt the work of the provider.
/// </para>
/// </summary>
/// <typeparam name="T">
/// <para xml:lang="ru">Тип загружаемых объектов</para>
/// <para xml:lang="en">Type of loaded objects</para>
/// </typeparam>
public class PartialLoader<T>
{
    private PartialLoaderOptions? _options = null;
    private ConcurrentQueue<T> _queue = new();
    private Task _loadTask = Task.CompletedTask;
    private IAsyncEnumerable<T>? _dataProvider = null;
    private ManualResetEventSlim _manualReset = new ManualResetEventSlim();
    private CancellationTokenSource? _cancellationTokenSource = null;
    DateTimeOffset _start;
    int _count;
    private readonly List<Func<T, T>> _utilizers = new();



    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, описывающее текущее состояние объекта. <see cref="PartialLoaderState"/>
    /// </para>
    /// <para xml:lang="en">
    /// A property that describes the current state of the object. <see cref="PartialLoaderState"/>
    /// </para> 
    /// </summary>
    public PartialLoaderState State { get; private set; } = PartialLoaderState.New;

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, устанавливающий поставщика данных и настройки для работы объекта
    /// </para>
    /// <para xml:lang="en">
    /// Method that sets the data provider and settings for the object to work
    /// </para> 
    /// </summary>
    /// <param name="dataProvider">
    /// <para xml:lang="ru">
    /// Поставщик данных. <see cref="IAsyncEnumerable{T}"/>
    /// </para>
    /// <para xml:lang="en">
    /// Data provider. <see cref="IAsyncEnumerable{T}"/>
    /// </para> 
    /// </param>
    /// <param name="options">
    /// <para xml:lang="ru">
    /// Настройки. <see cref="PartialLoaderOptions"/>
    /// </para>
    /// <para xml:lang="en">
    /// Options settings. <see cref="PartialLoaderOptions"/>
    /// </para> 
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual void Initialize(IAsyncEnumerable<T> dataProvider, PartialLoaderOptions options)
    {
        if (options is null || dataProvider is null)
        {
            throw new ArgumentNullException(options is null ? nameof(options) : nameof(dataProvider));
        }
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _dataProvider = dataProvider;
        _options = options;
        State = PartialLoaderState.Initialized;
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Добавляет новый "утилизатор" в цепочка применяемых к каждому загруженному объекту "утилизаторов". 
    /// <para xml:lang="en">
    /// Adds a new "utilizer" to the chain of "utilizers" applied to each loaded object.
    /// </para>
    /// </summary>
    public void AddUtilizer(Func<T, T> utilizer)
    {
        _utilizers.Add(utilizer);
    }


    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, начинающий  или продолжающий загрузку объектов из поставщика.
    /// Возврат в вызывающий код происходит по истечении заданного времени или достижения 
    /// заданного числа обработанных объектов
    /// </para>
    /// <para xml:lang="en">
    /// Method that starts or continues loading objects from the provider.
    /// Return to the calling code occurs after the specified time has elapsed or the
    /// the specified number of processed objects
    /// </para>
    /// </summary>
    /// <returns>
    /// <para xml:lang="ru">
    /// Задача. <see cref="Task"/>
    /// </para>
    /// <para xml:lang="en">
    /// Task. <see cref="Task"/>
    /// </para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от
    /// <see cref="PartialLoaderState.Initialized"/> и <see cref="PartialLoaderState.Partial"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than
    /// <see cref="PartialLoaderState.Initialized"/> and <see cref="PartialLoaderState.Partial"/>
    /// </para>    
    /// </exception>
    public virtual async Task LoadAsync()
    {
        if (State is not PartialLoaderState.Initialized && State is not PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.Initialized} or {PartialLoaderState.Partial}, present: {State}");
        }
        if(State is PartialLoaderState.Initialized)
        {
            State = PartialLoaderState.Started;
            PrepareToExecute();
        }
        else
        {
            State = PartialLoaderState.Continued;
        }
        await ExecuteAsync();
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, сбрасывающий объект в исходное состояние
    /// </para>
    /// <para xml:lang="en">
    /// Method that resets the object to its original state
    /// </para>
    /// </summary>
    public virtual void Reset()
    {
        if (_cancellationTokenSource is not null)
        {
            if (!_loadTask.IsCompleted)
            {
                _cancellationTokenSource.Cancel();
                _loadTask.Wait();
            }
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        State = PartialLoaderState.New;
        _queue.Clear();
        _dataProvider = null;
        _options = null;
        _utilizers.Clear();
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, прерывающий работу объекта
    /// </para>
    /// /// <para xml:lang="en">
    /// Method that interrupts the operation of the object
    /// </para>
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource!.Cancel(false);
    }

    private void PrepareToExecute()
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_options!.CancellationToken);
        if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            State = PartialLoaderState.Canceled;
            return;
        }

        _manualReset.Reset();

        _loadTask = Task.Run(async () =>
        {
            await foreach (T item in _dataProvider!.ConfigureAwait(_options.ConfigureAwait))
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }
                _queue.Enqueue(item);
                _manualReset.Set();
            }
        }).ContinueWith(_ =>
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _manualReset.Set();
            }
        });
    }

    private async Task ExecuteAsync()
    {
        _start = DateTimeOffset.Now;
        _count = 0;

        while (!_loadTask.IsCompleted)
        {
            TimeSpan limeLeft = _options!.Timeout.Ticks <= 0 ?
                TimeSpan.MaxValue : _options.Timeout - (DateTimeOffset.Now - _start);
            if (limeLeft == TimeSpan.MaxValue || limeLeft.TotalMilliseconds > 0)
            {
                try
                {
                    if (limeLeft == TimeSpan.MaxValue)
                    {
                        _manualReset.Wait(_cancellationTokenSource!.Token);
                    }
                    else
                    {
                        _manualReset.Wait(limeLeft, _cancellationTokenSource!.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    await _loadTask.ConfigureAwait(_options.ConfigureAwait);
                }
                if (_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    await _loadTask.ConfigureAwait(_options.ConfigureAwait);
                    State = PartialLoaderState.Canceled;
                    return;
                }
                if (UtilizeAndPossiblySetPArtialStateAndReturn())
                {
                    return;
                }
            }
            else
            {
                if (_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    await _loadTask.ConfigureAwait(_options.ConfigureAwait);
                    State = PartialLoaderState.Canceled;
                    return;
                }
                State = PartialLoaderState.Partial;
                return;
            }
            if (!_loadTask.IsCompleted)
            {
                _manualReset.Reset();
            }
        }
        if (UtilizeAndPossiblySetPArtialStateAndReturn())
        {
            return;
        }
        if (_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            State = PartialLoaderState.Canceled;
            return;
        }
        if (_loadTask.IsFaulted)
        {
            throw _loadTask.Exception!;
        }
        State = PartialLoaderState.Full;
    }

    private bool UtilizeAndPossiblySetPArtialStateAndReturn()
    {
        while (_queue.TryDequeue(out T? item))
        {
            if (item is not null)
            {
                foreach (Func<T, T> utilizer in _utilizers)
                {
                    item = utilizer.Invoke(item);
                }

                _count++;

                if (_options.Paging > 0 && _count >= _options.Paging || _options.Timeout.Ticks > 0 && (_options.Timeout - (DateTimeOffset.Now - _start)).Ticks <= 0)
                {
                    State = PartialLoaderState.Partial;
                    return true;
                }
            }
        }
        return false;
    }
}
