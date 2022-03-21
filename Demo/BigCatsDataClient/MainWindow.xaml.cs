using BigCatsDataContract;
using Net.Leksi;
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
using System.Windows.Controls;
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

        private bool _isDataLoading = false;
        private object _lockIsDataLoading = new();
        private int _count = 1000;
        private double _delay = 0;
        private double _httpTimeout = 0;
        private TimeSpan _elapsedChunks;
        private TimeSpan _elapsedAll;
        private int _timeout = -1;
        private int _paging = 0;
        private string _lastCommand = string.Empty;

        /// <summary xml:lang="ru">
        ///     Определяет, загружаются ли в данный момент данные. 
        ///     Также это сигнализирует, могут ли быть выполнены команды <see cref="GetAllCommand"/> и <see cref="GetChunksCommand"/>. 
        /// </summary>
        private bool IsDataLoading
        {
            get => _isDataLoading;
            set
            {
                _isDataLoading = value;
                GetAllCommand.Touch();
                GetChunksCommand.Touch();
                GetJsonCommand.Touch();
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
        public double Delay
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

        public string LastCommand
        {
            get
            {
                return _lastCommand;
            }
            set
            {
                _lastCommand = value;
                OnPropertyChanged();
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
        public Command GetAllCommand { get; init; }

        /// <summary xml:lang="ru">
        ///     Команда для связи метода загрузки частями с соответствующей кнопкой UI.
        /// </summary>
        public Command GetChunksCommand { get; init; }

        /// <summary xml:lang="ru">
        ///     Команда для связи метода загрузки частями с соответствующей кнопкой UI.
        /// </summary>
        public Command GetJsonCommand { get; init; }

        public MainWindow()
        {
            GetAllCommand = new Command(async _ => await GetAllCats(), _ => !IsDataLoading);
            GetChunksCommand = new Command(async _ => await GetChunksCats(Constants.ChunksUri), _ => !IsDataLoading);
            GetJsonCommand = new Command(async _ => await GetChunksCats(Constants.JsonUri), _ => !IsDataLoading);
            InitializeComponent();
        }

        /// <summary xml:lang="ru">
        ///     Метод загрузки частями.
        /// </summary>
        private async Task GetChunksCats(string path)
        {
            DispatcherOperation? clearOp = null;
            DispatcherOperation? elapsedOp = null;
            try
            {
                IsDataLoading = true;
                DateTimeOffset started = DateTimeOffset.Now;
                ElapsedChunks = DateTimeOffset.Now - started;

                // Тикают часики, пока идёт загрузка
                elapsedOp = Dispatcher.BeginInvoke(async () =>
                {
                    while (IsDataLoading)
                    {
                        bool willContinue = false;
                        lock (_lockIsDataLoading)
                        {
                            if (IsDataLoading)
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

                JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
                var converter = new TransferJsonConverterFactory(null)
                    .AddTransient<ICat, Cat>();
                jsonOptions.Converters.Add(converter);

                // Чистим таблицы параллельно с подготовкой к получению кошек.
                clearOp = Dispatcher.BeginInvoke(() =>
                {
                    converter.ObjectsPool[typeof(Cat)] = Cats.Select(it => (object)it).ToList();
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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{path}/{Count}/{Timeout}/{Paging}/{Delay.ToString().Replace(',', '.')}");
                HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

                // Кошки приехали, ждём на случай, если таблицы не дочистились
                await clearOp;

                while (response is { } && response.StatusCode == System.Net.HttpStatusCode.OK && IsDataLoading)
                {
                    // Обрабатываем данные в Dispatcher, чтобы не влезть в UI из левого потока.
                    await Dispatcher.BeginInvoke(async () =>
                    {
                        converter.Target = Cats;
                        int prevCount = Cats.Count;
                        try
                        {
                            await JsonSerializer.DeserializeAsync<AppendableList<ICat>>(response.Content.ReadAsStream(),
                                    jsonOptions);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        // Добавляем инфу в таблицу с инфой.
                        Chunks.Add(new Chunk
                        {
                            Id = Chunks.Count + 1,
                            Count = Cats.Count - prevCount,
                            CountAll = Cats.Count,
                            ElapsedTotal = ElapsedChunks,
                        });
                        Chunks.Last().Elapsed = Chunks.Last().ElapsedTotal;
                        if(Chunks.Count > 1)
                        {
                            Chunks.Last().Elapsed -= Chunks[^2].ElapsedTotal;
                        }

                    });
                    if (!converter.EndOfData && (!response.Headers.Contains(Constants.PartialLoaderStateHeaderName) 
                            || response.Headers.GetValues(Constants.PartialLoaderStateHeaderName).First() == Constants.Partial))
                    {
                        // Если данные пришли не полностью, повторяем запрос. Можно без параметров, так как сервер подставит значения по умолчанию,
                        // но они всё равно не будут использоваться, так как мы передаём заголовок с идентификатором запроса, который сервер
                        // вернул нам с неполными данными.
                        request = new HttpRequestMessage(HttpMethod.Get, $"{path}");
                        request.Headers.Add(Constants.PartialLoaderSessionKey, response.Headers.GetValues(Constants.PartialLoaderSessionKey).First());
                        response = await _client.SendAsync(request).ConfigureAwait(false);

                    }
                    else
                    {
                        break;
                    }
                }
                await Dispatcher.BeginInvoke(async () =>
                {
                    lock (_lockIsDataLoading)
                    {
                        IsDataLoading = false;
                    }
                    // Ждём, когда UI остановит часики
                    await elapsedOp;
                });
                if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    // Сервер сказал: "не ОК"
                    await Dispatcher.Invoke(async () =>
                    {
                        IsDataLoading = false;
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
                    IsDataLoading = false;
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
                IsDataLoading = true;
                DateTimeOffset started = DateTimeOffset.Now;
                ElapsedAll = DateTimeOffset.Now - started;

                // Тикают часики, пока идёт загрузка
                elapsedOp = Dispatcher.BeginInvoke(async () =>
                {
                    while (IsDataLoading)
                    {
                        bool willContinue = false;
                        lock (_lockIsDataLoading)
                        {
                            if (IsDataLoading)
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

                JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
                var converter = new TransferJsonConverterFactory(null)
                    .AddTransient<ICat, Cat>();
                jsonOptions.Converters.Add(converter);

                // Чистим таблицы параллельно с подготовкой к получению кошек.
                clearOp = Dispatcher.BeginInvoke(() =>
                {
                    converter.ObjectsPool[typeof(Cat)] = Cats.Select(it => (object)it).ToList();
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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Constants.AllUri}/{Count}/{Delay.ToString().Replace(',', '.')}");
                HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

                // Кошки приехали, ждём на случай, если таблицы не дочистились
                await clearOp;

                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Обрабатываем данные в Dispatcher, чтобы не влезть в UI из левого потока.
                    await Dispatcher.BeginInvoke(async () =>
                    {
                        converter.Target = Cats;
                        int prevCount = Cats.Count;
                        try
                        {
                            await JsonSerializer.DeserializeAsync<AppendableList<ICat>>(response.Content.ReadAsStream(),
                                    jsonOptions);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        // Данные пришли полностью.
                        lock (_lockIsDataLoading)
                        {
                            IsDataLoading = false;
                        }

                        // Добавляем инфу в таблицу с инфой.
                        Chunks.Add(new Chunk
                        {
                            Id = Chunks.Count + 1,
                            Count = Cats.Count - prevCount,
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
                        IsDataLoading = false;
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
                    IsDataLoading = false;
                    await elapsedOp;
                    MessageBox.Show($"{ex.GetType().ToString()}: {ex.Message}\n{ex.StackTrace}");
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                LastCommand = button.Content.ToString();
            }
        }
    }
}
