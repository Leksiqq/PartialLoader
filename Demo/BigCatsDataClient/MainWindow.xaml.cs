using BigCatsDataContract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BigCatsDataClient
{
    /// <summary>
    ///     Клиент для демонстрации возможностей класса <see cref="PartialLoader"/>
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const string Server = "https://localhost:7209";
        private const string AllUri = "/cats";
        private const string ChunkslUri = "/catsChunks";

        private bool _isDataLoading = false;
        private object _lockIsDataLoading = new();
        private int _count = 1000;
        private int _delay = 0;
        private double _httpTimeout = 0;
        private TimeSpan _elapsedChunks;
        private TimeSpan _elapsedAll;
        private int _timeout = -1;
        private int _paging = 0;

        /// <summary xml:lang="ru">
        ///     Определяет, загружаются ли в данный момент данные. 
        ///     Также это сигнализирует, могут ли быть выполнены команды <see cref="GetAllCommand"/> и <see cref="GetChunksCommand"/>. 
        /// </summary>
        private bool IsDataLOading
        {
            get => _isDataLoading;
            set
            {
                _isDataLoading = value;
                OnPropertyChanged(nameof(GetAllCommand));
                OnPropertyChanged(nameof(GetChunksCommand));
            }
        }

        /// <summary xml:lang="ru">
        ///     Определяет, сколько кошек мы хотим получить.
        /// </summary>
        public int Count
        {
            get => _count;
            set
            {
                if (value > 0)
                {
                    _count = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary xml:lang="ru">
        ///     Определяет какую приблизительную задержку в миллисекундах нужно симулировать при загрузке одной кошки.
        /// </summary>
        public int Delay
        {
            get => _delay;
            set
            {
                if (value >= 0)
                {
                    _delay = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Определяет таймаут Http-клиента в секундах.
        /// </summary>
        public double HttpTimeout
        {
            get => _httpTimeout;
            set
            {
                if (value >= 0)
                {
                    _httpTimeout = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Определяет таймаут, по истечении которого сервер вернёт уже загруженных кошек при загрузке частями.
        ///     Если одновременно <see cref="Paging"/> &gt; 0, срабатывает то условие, которое выполнится раньше.
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set
            {
                if (value >= 0 || value == -1)
                {
                    _timeout = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Определяет фиксированное количество кошек, которое должен вернуть сервер в каждой партии при загрузке частями.
        ///     Если одновременно <see cref="Timeout"/> &gt; 0, срабатывает то условие, которое выполнится раньше.
        /// </summary>
        public int Paging
        {
            get => _paging;
            set
            {
                if (value >= 0)
                {
                    _paging = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Показывает время, прошедшее с начала загрузки в случае загрузки целиком.
        /// </summary>
        public TimeSpan ElapsedAll
        {
            get => _elapsedAll;
            private set
            {
                if (value.TotalMilliseconds >= 0)
                {
                    _elapsedAll = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Показывает время, прошедшее с начала загрузки в случае загрузки частями.
        /// </summary>
        public TimeSpan ElapsedChunks
        {
            get => _elapsedChunks;
            private set
            {
                if (value.TotalMilliseconds >= 0)
                {
                    _elapsedChunks = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary xml:lang="ru">
        ///     Коллекция загруженных кошек.
        /// </summary>
        public ObservableCollection<Cat> Cats { get; init; } = new();

        /// <summary xml:lang="ru">
        ///     Коллекция сведений о ходе текущей/последней загрузки.
        /// </summary>
        public ObservableCollection<Chunk> Chunks { get; init; } = new();

        /// <summary xml:lang="ru">
        ///     Команда для связи метода загрузки целиком с соответствующей кнопкой UI.
        /// </summary>
        public ICommand GetAllCommand { get; init; }

        /// <summary xml:lang="ru">
        ///     Команда для связи метода загрузки частями с соответствующей кнопкой UI.
        /// </summary>
        public ICommand GetChunksCommand { get; init; }

        public MainWindow()
        {
            GetAllCommand = new Command(async _ => await GetAllCats(), _ => !IsDataLOading);
            GetChunksCommand = new Command(async _ => await GetChunksCats(), _ => !IsDataLOading);
            InitializeComponent();
        }

        /// <summary xml:lang="ru">
        ///     Метод загрузки частями.
        /// </summary>
        private async Task GetChunksCats()
        {
            DispatcherOperation? clearOp = null;
            DispatcherOperation? elapsedOp = null;
            try
            {
                IsDataLOading = true;
                DateTimeOffset started = DateTimeOffset.Now;
                ElapsedChunks = DateTimeOffset.Now - started;

                // Тикают часики, пока идёт загрузка
                elapsedOp = Dispatcher.BeginInvoke(async () =>
                {
                    while (IsDataLOading)
                    {
                        bool willContinue = false;
                        lock (_lockIsDataLoading)
                        {
                            if (IsDataLOading)
                            {
                                willContinue = true;
                            }
                        }
                        if (willContinue)
                        {
                            ElapsedChunks = DateTimeOffset.Now - started;
                            await Task.Delay(100);
                        }
                    }
                });

                // Чистим таблицы параллельно с подготовкой к получению кошек.
                clearOp = Dispatcher.BeginInvoke(() =>
                {
                    Cats.Clear();
                    Chunks.Clear();
                });

                using HttpClient _client = new HttpClient();
                if (HttpTimeout <= 0)
                {
                    HttpTimeout = _client.Timeout.TotalSeconds;
                }
                else
                {
                    _client.Timeout = TimeSpan.FromSeconds(HttpTimeout);
                }
                _client.BaseAddress = new Uri(Server);

                // Передаём запрос серверу в стиле REST
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{ChunkslUri}/{Count}/{Timeout}/{Paging}/{Delay}");
                HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

                // Кошки приехали, ждём на случай, если таблицы не дочистились
                await clearOp;

                while(response.StatusCode == System.Net.HttpStatusCode.OK && IsDataLOading)
                {
                    // Обрабатываем данные в Dispatcher, чтобы не влезть в UI из левого потока.
                    await Dispatcher.BeginInvoke(async () =>
                    {
                        List<Cat>? list = await JsonSerializer.DeserializeAsync<List<Cat>>(response.Content.ReadAsStream(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // Добавляем кошек в таблицу с кошками
                        foreach (Cat cat in list)
                        {
                            Cats.Add(cat);
                        }

                        // Добавляем инфу в таблицу с инфой.
                        Chunks.Add(new Chunk
                        {
                            Id = Chunks.Count + 1,
                            Count = list.Count,
                            CountAll = Cats.Count,
                            ElapsedTotal = ElapsedChunks,
                        });
                        Chunks.Last().Elapsed = Chunks.Last().ElapsedTotal;
                        if(Chunks.Count > 1)
                        {
                            Chunks.Last().Elapsed -= Chunks[^2].ElapsedTotal;
                        }

                        if (response.Headers.GetValues(Constants.PartialLoaderStateHeaderName).First() == Constants.Partial)
                        {
                            // Если данные пришли не полностью, повторяем запрос. Можно без параметров, так как сервер подставит значения по умолчанию,
                            // но они всё равно не будут использоваться, так как мы передаём заголовок с идентификатором запроса, который сервер
                            // вернул нам с неполными данными.
                            request = new HttpRequestMessage(HttpMethod.Get, $"{ChunkslUri}");
                            request.Headers.Add(Constants.PartialLoaderSessionKey, response.Headers.GetValues(Constants.PartialLoaderSessionKey).First());
                            response = await _client.SendAsync(request).ConfigureAwait(false);
                        }
                        else
                        {
                            // Данные пришли полностью.
                            lock (_lockIsDataLoading)
                            {
                                IsDataLOading = false;
                            }
                            // Ждём, когда UI остановит часики
                            await elapsedOp;
                        }
                    });
                }
                if(response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    // Сервер сказал: "не ОК"
                    await Dispatcher.Invoke(async () =>
                    {
                        IsDataLOading = false;
                        await elapsedOp;
                        MessageBox.Show($"Сервер вернул {response.StatusCode}");
                    });
                }
            }
            catch (Exception ex)
            {
                // Что-то пошло вообще не так
                await Dispatcher.Invoke(async () =>
                {
                    IsDataLOading = false;
                    await elapsedOp;
                    MessageBox.Show($"{ex.GetType().ToString()}: {ex.Message}\n{ex.StackTrace}");
                });
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary xml:lang="ru">
        ///     Метод загрузки целиком.
        /// </summary>
        private async Task GetAllCats()
        {
            DispatcherOperation? clearOp = null;
            DispatcherOperation? elapsedOp = null;
            try
            {
                IsDataLOading = true;
                DateTimeOffset started = DateTimeOffset.Now;
                ElapsedAll = DateTimeOffset.Now - started;

                // Тикают часики, пока идёт загрузка
                elapsedOp = Dispatcher.BeginInvoke(async () =>
                {
                    while (IsDataLOading)
                    {
                        bool willContinue = false;
                        lock (_lockIsDataLoading)
                        {
                            if (IsDataLOading)
                            {
                                willContinue = true;
                            }
                        }
                        if (willContinue)
                        {
                            ElapsedAll = DateTimeOffset.Now - started;
                            await Task.Delay(100);
                        }
                    }
                });

                // Чистим таблицы параллельно с подготовкой к получению кошек.
                clearOp = Dispatcher.BeginInvoke(() =>
                {
                    Cats.Clear();
                    Chunks.Clear();
                });

                using HttpClient _client = new HttpClient();
                if (HttpTimeout <= 0)
                {
                    HttpTimeout = _client.Timeout.TotalSeconds;
                }
                else
                {
                    _client.Timeout = TimeSpan.FromSeconds(HttpTimeout);
                }
                _client.BaseAddress = new Uri(Server);

                // Передаём запрос серверу в стиле REST
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{AllUri}/{Count}/{Delay}");
                HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

                // Кошки приехали, ждём на случай, если таблицы не дочистились
                await clearOp;

                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Обрабатываем данные в Dispatcher, чтобы не влезть в UI из левого потока.
                    await Dispatcher.BeginInvoke(async () =>
                    {
                        List<Cat>? list = await JsonSerializer.DeserializeAsync<List<Cat>>(response.Content.ReadAsStream(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            // Добавляем кошек в таблицу с кошками
                        foreach (Cat cat in list)
                        {
                            Cats.Add(cat);
                        }

                        // Данные пришли полностью.
                        lock (_lockIsDataLoading)
                        {
                            IsDataLOading = false;
                        }

                        // Добавляем инфу в таблицу с инфой.
                        Chunks.Add(new Chunk
                        {
                            Id = Chunks.Count + 1,
                            Count = list.Count,
                            CountAll = Cats.Count,
                            Elapsed = ElapsedAll,
                            ElapsedTotal = ElapsedAll
                        });
                        // Ждём, когда UI остановит часики
                        await elapsedOp;
                    });
                }
                else
                {
                    // Сервер сказал: "не ОК"
                    await Dispatcher.Invoke(async () =>
                    {
                        IsDataLOading = false;
                        await elapsedOp;
                        MessageBox.Show($"Сервер вернул {response.StatusCode}");
                    });
                }

            }
            catch (Exception ex)
            {
                // Что-то пошло вообще не так
                await Dispatcher.Invoke(async () =>
                {
                    IsDataLOading = false;
                    await elapsedOp;
                    MessageBox.Show($"{ex.GetType().ToString()}: {ex.Message}\n{ex.StackTrace}");
                });
            }
        }

    }
}
