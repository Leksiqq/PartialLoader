namespace Net.Leksi;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

public class PartialLoader<T> : IPartialLoader<T>
{
    private PartialLoaderOptions _options = null!;
    private ConcurrentQueue<T> _queue = new();
    private Task _loadTask = Task.CompletedTask;
    private Collection<T> _list = new();
    private List<T>? _chunk = null;
    private int _offset = 0;
    private ManualResetEventSlim _manualReset = new ManualResetEventSlim();
    private List<int> _cancelationTrace = new();
    private CancellationTokenSource? _cancellationTokenSource = null;

    public PartialLoaderState State { get; private set; } = PartialLoaderState.New;

    public Collection<T> Result
    {
        get
        {
            if (State is not PartialLoaderState.Full)
            {
                throw new InvalidOperationException($"Expected State: {PartialLoaderState.Full}, present: {State}");
            }
            return _list;
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
            return _chunk ?? new();
        }
    }

    public string CancelationTrace {
        get 
        {
            lock (_cancelationTrace)
            {
                return string.Join(",", _cancelationTrace);
            }
        }
    }


    public Task StartAsync(IAsyncEnumerable<T> data, PartialLoaderOptions options, Collection<T> result = null)
    {
        _options = options;
        if (State is not PartialLoaderState.New)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.New}, present: {State}");
        }
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_options.CancellationToken);
        if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            lock (_cancelationTrace)
            {
                _cancelationTrace.Add(1);
            }
            Output(PartialLoaderState.Canceled);
            return Task.CompletedTask;
        }
        State = PartialLoaderState.Started;
        if(result is null)
        {
            _list = new();
        } 
        else
        {
            _list = result;
        }

        _manualReset.Reset();
        _loadTask = Task.Run(async () =>
        {
            await foreach (T item in data.ConfigureAwait(options.ConfigureAwait))
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    lock(_cancelationTrace)
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
            } else
            {
                lock (_cancelationTrace)
                {
                    _cancelationTrace.Add(3);
                }
            }
        });
        return ExecuteAsync();
    }

    public Task ContinueAsync()
    {
        if (State != PartialLoaderState.Partial)
        {
            throw new InvalidOperationException($"Expected State: {PartialLoaderState.Partial}, present: {State}");
        }
        State = PartialLoaderState.Continued;
        return ExecuteAsync();
    }

    public void Reset()
    {
        if(_cancellationTokenSource is not null)
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
    }

    private Task ExecuteAsync()
    {
        return Task.Run(async () =>
        {
            DateTimeOffset start = DateTimeOffset.Now;
            
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
                    catch(OperationCanceledException) 
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
                        _options.OnItem?.Invoke(item);
                        _list.Add(item);
                        if(_options.Paging > 0 && _list.Count - _offset == _options.Paging)
                        {
                            Output(PartialLoaderState.Partial);
                            return;
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
                _options.OnItem?.Invoke(item);
                _list.Add(item);
                if (_options.Paging > 0 && _list.Count - _offset == _options.Paging)
                {
                    Output(PartialLoaderState.Partial);
                    return;
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
        });
    }

    private void Output(PartialLoaderState state)
    {
        State = state;
        if(State == PartialLoaderState.Partial || State == PartialLoaderState.Full)
        {
            _chunk = _list.Skip(_offset).Take(_list.Count - _offset).ToList();
            _offset = _list.Count;
        }
    }
}
