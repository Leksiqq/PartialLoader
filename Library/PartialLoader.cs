namespace Net.Leksi;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

public class PartialLoader<T> : JsonConverter<StubForJson<T>>, IPartialLoader<T>
{
    private PartialLoaderOptions? _options = null;
    private ConcurrentQueue<T> _queue = new();
    private Task _loadTask = Task.CompletedTask;
    private IAsyncEnumerable<T>? _data = null;
    private List<T>? _list = null;
    private List<T>? _chunk = null;
    private int _offset = 0;
    private ManualResetEventSlim _manualReset = new ManualResetEventSlim();
    private List<int> _cancelationTrace = new();
    private CancellationTokenSource? _cancellationTokenSource = null;
    private Utf8JsonWriter? _writer = null;
    private JsonSerializerOptions? _jsonOptions = null;
    public PartialLoaderState State { get; private set; } = PartialLoaderState.New;

    public List<T> Result
    {
        get
        {
            if (State is not PartialLoaderState.Full)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Full}, present: {State}");
            }
            return _list!;
        }
    }

    public List<T> Chunk
    {
        get
        {
            if (State is not PartialLoaderState.Full && State is not PartialLoaderState.Partial)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Full} or {PartialLoaderState.Partial}, present: {State}");
            }
            if(_writer is not null)
            {
                throw new InvalidOperationException($"Using as JsonConverter");
            }
            return _chunk!;
        }
    }

    public string CancelationTrace
    {
        get
        {
            lock (_cancelationTrace)
            {
                return string.Join(",", _cancelationTrace);
            }
        }
    }

    public void Initialize(IAsyncEnumerable<T> data, PartialLoaderOptions options)
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _data = data;
        _options = options;
    }


    public async Task StartAsync()
    {
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        if (_options is null || _data is null) 
        {
            throw new InvalidOperationException($"Not initialized");
        }
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_options.CancellationToken);
        if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            lock (_cancelationTrace)
            {
                _cancelationTrace.Add(1);
            }
            Output(PartialLoaderState.Canceled);
            return;
        }
        State = PartialLoaderState.Started;
        _list = new();

        _manualReset.Reset();

        _loadTask = Task.Run(async () =>
        {
            await foreach (T item in _data!.ConfigureAwait(_options.ConfigureAwait))
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    lock (_cancelationTrace)
                    {
                        _cancelationTrace.Add(2);
                    }
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
            else
            {
                lock (_cancelationTrace)
                {
                    _cancelationTrace.Add(3);
                }
            }
        });
        await ExecuteAsync();
    }

    public async Task StartAsync(IAsyncEnumerable<T> data, PartialLoaderOptions options)
    {
        Initialize(data, options);
        await StartAsync();
    }

    public async Task ContinueAsync()
    {
        if (State != PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.Partial}, present: {State}");
        }
        State = PartialLoaderState.Continued;
        await ExecuteAsync();
    }

    public void Reset()
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
        _list = null;
        _offset = 0;
        _cancelationTrace.Clear();
        _queue.Clear();
        _data = null;
        _writer = null;
        _options = null;
        _jsonOptions = null;
    }

    private async Task ExecuteAsync()
    {
        DateTimeOffset start = DateTimeOffset.Now;
        int count = 0;

        while (!_loadTask.IsCompleted)
        {
            TimeSpan limeLeft = _options.Timeout.TotalMilliseconds < 0 ?
                TimeSpan.MaxValue : _options.Timeout - (DateTimeOffset.Now - start);
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
                    lock (_cancelationTrace)
                    {
                        _cancelationTrace.Add(4);
                    }
                }
                if (_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    await _loadTask.ConfigureAwait(_options.ConfigureAwait);
                    lock (_cancelationTrace)
                    {
                        _cancelationTrace.Add(5);
                    }
                    Output(PartialLoaderState.Canceled);
                    return;
                }
                while (_queue.TryDequeue(out T? item))
                {
                    if(item is not null)
                    {
                        _options.OnItem?.Invoke(item);
                        if (_writer is not null)
                        {
                            JsonSerializer.Serialize(_writer, item, item.GetType(), _jsonOptions);
                        }
                        if (_writer is null || _options.StoreResult)
                        {
                            _list.Add(item);
                        }

                        count++;

                        if (_options.Paging > 0 && count == _options.Paging)
                        {
                            Output(PartialLoaderState.Partial);
                            return;
                        }
                    }
                }
            }
            else
            {
                if (_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    await _loadTask.ConfigureAwait(_options.ConfigureAwait);
                    lock (_cancelationTrace)
                    {
                        _cancelationTrace.Add(6);
                    }
                    Output(PartialLoaderState.Canceled);
                    return;
                }
                Output(PartialLoaderState.Partial);
                return;
            }
            if (!_loadTask.IsCompleted)
            {
                _manualReset.Reset();
            }
        }
        while (_queue.TryDequeue(out T? item))
        {
            if(item is not null)
            {
                _options.OnItem?.Invoke(item);

                if (_writer is not null)
                {
                    JsonSerializer.Serialize(_writer, item, item.GetType(), _jsonOptions);
                }
                if (_writer is null || _options.StoreResult)
                {
                    _list.Add(item);
                }

                count++;

                if (_options.Paging > 0 && count == _options.Paging)
                {
                    Output(PartialLoaderState.Partial);
                    return;
                }
            }
        }
        if (_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            lock (_cancelationTrace)
            {
                _cancelationTrace.Add(7);
            }
            Output(PartialLoaderState.Canceled);
            return;
        }
        if (_loadTask.IsFaulted)
        {
            throw _loadTask.Exception!;
        }
        Output(PartialLoaderState.Full);
    }

    private void Output(PartialLoaderState state)
    {
        State = state;
        if (State == PartialLoaderState.Partial || State == PartialLoaderState.Full)
        {
            if(_writer is null)
            {
                _chunk = _list!.GetRange(_offset, _list.Count - _offset);

                if (_options.StoreResult)
                {
                    _offset = _list.Count;
                }
                else
                {
                    _list.Clear();
                }
            }
            else
            {
                if (_options.StoreResult)
                {
                    _offset = _list.Count;
                }
            }
        }
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(StubForJson<T>) == typeToConvert;
    }

    public override StubForJson<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override async void Write(Utf8JsonWriter writer, StubForJson<T> value, JsonSerializerOptions options)
    {
        _writer = writer;
        _jsonOptions = options;

        writer.WriteStartArray();

        switch(State)
        {
            case PartialLoaderState.New:
                await StartAsync();
                break;
            case PartialLoaderState.Partial:
                await ContinueAsync();
                break;
            default:
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.New} or {PartialLoaderState.Partial}, present: {State}");
        }
        if(State == PartialLoaderState.Full)
        {
            writer.WriteNullValue();
        }
        writer.WriteEndArray();
    }
}
