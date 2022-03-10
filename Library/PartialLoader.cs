namespace Net.Leksi;

using Net.Leksi.PartialLoader;
using System.Collections.Concurrent;

public class PartialLoader<T>
{
    private PartialLoaderOptions? _options = null;
    private ConcurrentQueue<T> _queue = new();
    private Task _loadTask = Task.CompletedTask;
    private IAsyncEnumerable<T>? _data = null;
    private ManualResetEventSlim _manualReset = new ManualResetEventSlim();
    private List<int> _cancelationTrace = new();
    private CancellationTokenSource? _cancellationTokenSource = null;
    DateTimeOffset _start;
    int _count;

    public PartialLoaderState State { get; private set; } = PartialLoaderState.New;

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


    public async Task LoadAsync()
    {
        if (State is not PartialLoaderState.New && State is not PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New} or {PartialLoaderState.Partial}, present: {State}");
        }
        if (_options is null || _data is null) 
        {
            throw new InvalidOperationException($"Not initialized");
        }
        if(State is PartialLoaderState.New)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_options.CancellationToken);
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                lock (_cancelationTrace)
                {
                    _cancelationTrace.Add(1);
                }
                State = PartialLoaderState.Canceled;
                return;
            }
            State = PartialLoaderState.Started;

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
        }
        else
        {
            State = PartialLoaderState.Continued;
        }
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
        _cancelationTrace.Clear();
        _queue.Clear();
        _data = null;
        _options = null;
    }

    private async Task ExecuteAsync()
    {
        _start = DateTimeOffset.Now;
        _count = 0;

        while (!_loadTask.IsCompleted)
        {
            TimeSpan limeLeft = _options.Timeout.TotalMilliseconds < 0 ?
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
                    State = PartialLoaderState.Canceled;
                    return;
                }
                if (UtilizeAndReturn())
                {
                    return;
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
        if (UtilizeAndReturn())
        {
            return;
        }
        if (_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            lock (_cancelationTrace)
            {
                _cancelationTrace.Add(7);
            }
            State = PartialLoaderState.Canceled;
            return;
        }
        if (_loadTask.IsFaulted)
        {
            throw _loadTask.Exception!;
        }
        State = PartialLoaderState.Full;
    }

    private bool UtilizeAndReturn()
    {
        while ((_options.Timeout - (DateTimeOffset.Now - _start)).Ticks > 0 && _queue.TryDequeue(out T? item))
        {
            if (item is not null)
            {
                foreach (IUtilizer utilizer in _options.Utilizers)
                {
                    item = (T)utilizer.Utilize(item);
                }

                _count++;

                if (_options.Paging > 0 && _count >= _options.Paging || (_options.Timeout - (DateTimeOffset.Now - _start)).Ticks > 0)
                {
                    State = PartialLoaderState.Partial;
                    return true;
                }
            }
        }
        return false;
    }
}
