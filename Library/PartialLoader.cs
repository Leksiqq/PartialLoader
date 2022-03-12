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
    private TimeSpan _timeout = TimeSpan.Zero;
    private int _paging = 0;
    private CancellationToken _cancellationToken = CancellationToken.None;
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
    /// Свойство, описывающее значение интервала, по истечении которого происходит возврат 
    /// в вызывающий код из метода <see cref="LoadAsync"/>.
    /// Неположительное значение означает, что такой интервал не установлен.
    /// </para>
    /// <para xml:lang="en">
    /// Property describing the value of the interval after which the return occurs
    /// to the calling code from the <see cref="LoadAsync"/> method.
    /// A non-positive value means that such an interval is not set.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    public TimeSpan Timeout
    {
        get
        {
            return _timeout;
        }
        set
        {
            SetTimeout(value);
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, описывающее количество полученных объектов, по достижении которого 
    /// происходит возврат в вызывающий код из метода 
    /// <see cref="LoadAsync"/>.
    /// Неположительное значение означает, что соответствующее значение 
    /// не установлено.
    /// </para>
    /// <para xml:lang="en">
    /// A property that describes the number of received objects, upon reaching which
    /// returns to the calling code from the method
    /// <see cref="LoadAsync"/>.
    /// A non-positive value means that the corresponding value
    /// not installed.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    public int Paging
    {
        get
        {
            return _paging;
        }
        set
        {
            SetPaging(value);
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// <see cref="CancellationToken"/> передаваемый из внешнего кода
    /// </para>
    /// <para xml:lang="en">
    /// <see cref="CancellationToken"/> passed from external code
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    public CancellationToken CancellationToken
    {
        get { 
            return _cancellationToken; 
        }
        set
        {
            SetCancellationToken(value);
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, устанавливающий <see cref="CancellationToken"/> передаваемый из внешнего кода
    /// </para>
    /// <para xml:lang="en">
    /// Method that sets <see cref="CancellationToken"/> passed from external code
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <returns><see cref="PartialLoader{T}"/>
    /// <para xml:lang="ru">
    /// Возвращает сам объект для Flow-стиля
    /// </para>
    /// <para xml:lang="en">
    /// Returns the object itself for the Flow style
    /// </para>
    /// </returns>
    public PartialLoader<T> SetCancellationToken(CancellationToken cancellationToken)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, устанавливающий количество полученных объектов, по достижении которого 
    /// происходит возврат в вызывающий код из метода 
    /// <see cref="LoadAsync"/>.
    /// Неположительное значение означает, что соответствующее значение 
    /// не установлено.
    /// </para>
    /// <para xml:lang="en">
    /// Method that sets the number of received objects, upon reaching which
    /// returns to the calling code from the method
    /// <see cref="LoadAsync"/>.
    /// A non-positive value means that the corresponding value
    /// not installed.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <returns><see cref="PartialLoader{T}"/>
    /// <para xml:lang="ru">
    /// Возвращает сам объект для Flow-стиля
    /// </para>
    /// <para xml:lang="en">
    /// Returns the object itself for the Flow style
    /// </para>
    /// </returns>
    public PartialLoader<T> SetPaging(int paging)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _paging = paging;
        return this;
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, устанавливающий значение интервала, по истечении которого происходит возврат 
    /// в вызывающий код из метода <see cref="LoadAsync"/>.
    /// Неположительное значение означает, что такой интервал не установлен.
    /// </para>
    /// <para xml:lang="en">
    /// Method that sets the value of the interval after which the return occurs
    /// to the calling code from the <see cref="LoadAsync"/> method.
    /// A non-positive value means that such an interval is not set.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <returns><see cref="PartialLoader{T}"/>
    /// <para xml:lang="ru">
    /// Возвращает сам объект для Flow-стиля
    /// </para>
    /// <para xml:lang="en">
    /// Returns the object itself for the Flow style
    /// </para>
    /// </returns>
    public PartialLoader<T> SetTimeout(TimeSpan timeout)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Метод, устанавливающий поставщика данных
    /// </para>
    /// <para xml:lang="en">
    /// Method that sets the data provider
    /// </para> 
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns><see cref="PartialLoader{T}"/>
    /// <para xml:lang="ru">
    /// Возвращает сам объект для Flow-стиля
    /// </para>
    /// <para xml:lang="en">
    /// Returns the object itself for the Flow style
    /// </para>
    /// </returns>
    public PartialLoader<T> SetDataProvider(IAsyncEnumerable<T> dataProvider)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        if (dataProvider is null)
        {
            throw new ArgumentNullException(nameof(dataProvider));
        }
        _dataProvider = dataProvider;
        return this;
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Добавляет новый "утилизатор" в цепочка применяемых к каждому загруженному объекту "утилизаторов". 
    /// </para>
    /// <para xml:lang="en">
    /// Adds a new "utilizer" to the chain of "utilizers" applied to each loaded object.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается при вызове в состоянии объекта, оличающемся от <see cref="PartialLoaderState.New"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than <see cref="PartialLoaderState.New"/> 
    /// </para> 
    /// </exception>
    /// <returns><see cref="PartialLoader{T}"/>
    /// <para xml:lang="ru">
    /// Возвращает сам объект для Flow-стиля
    /// </para>
    /// <para xml:lang="en">
    /// Returns the object itself for the Flow style
    /// </para>
    /// </returns>
    public PartialLoader<T> AddUtilizer(Func<T, T> utilizer)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _utilizers.Add(utilizer);
        return this;
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
    /// <see cref="PartialLoaderState.New"/> и <see cref="PartialLoaderState.Partial"/>
    /// </para>
    /// <para xml:lang="en">
    /// Thrown when called in an object state other than
    /// <see cref="PartialLoaderState.New"/> and <see cref="PartialLoaderState.Partial"/>
    /// </para>    
    /// </exception>
    public virtual async Task LoadAsync()
    {
        if (State is not PartialLoaderState.New && State is not PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New} or {PartialLoaderState.Partial}, present: {State}");
        }
        if (State is PartialLoaderState.New)
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
        _utilizers.Clear();
        _timeout = TimeSpan.Zero;
        _paging = 0;
        _cancellationToken = CancellationToken.None;
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
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            State = PartialLoaderState.Canceled;
            return;
        }

        _manualReset.Reset();

        _loadTask = Task.Run(async () =>
        {
            await foreach (T item in _dataProvider!.ConfigureAwait(false))
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
            TimeSpan limeLeft = _timeout.Ticks <= 0 ?
                TimeSpan.MaxValue : _timeout - (DateTimeOffset.Now - _start);
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
                    await _loadTask.ConfigureAwait(false);
                }
                if (_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    await _loadTask.ConfigureAwait(false);
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
                    await _loadTask.ConfigureAwait(false);
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

                if (_paging > 0 && _count >= _paging || _timeout.Ticks > 0 && (_timeout - (DateTimeOffset.Now - _start)).Ticks <= 0)
                {
                    State = PartialLoaderState.Partial;
                    return true;
                }
            }
        }
        return false;
    }
}
