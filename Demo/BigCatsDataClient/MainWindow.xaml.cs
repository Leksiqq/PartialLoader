using BigCatsDataServer;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const string Server = "https://localhost:7209";
        private const string AllUri = "/cats";

        private bool _getAllCanExecute = true;
        private DateTimeOffset _startedGetAll = DateTimeOffset.Now;
        private int _count = 1000;
        private int _delay = 0;
        private double _httpTimeout = 0;
        private TimeSpan _elapsed;

        private bool GetAllCanExecute
        {
            get => _getAllCanExecute;
            set
            {
                _getAllCanExecute = value;
                OnPropertyChanged(nameof(GetAllCommand));
            }
        }
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

        public TimeSpan Elapsed
        {
            get => _elapsed;
            private set
            {
                if (value.TotalMilliseconds >= 0)
                {
                    _elapsed = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Cat> Cats { get; init; } = new();
        public ObservableCollection<Chunk> Chunks { get; init; } = new();

        public ICommand GetAllCommand { get; init; }

        public MainWindow()
        {
            GetAllCommand = new Command(async _ => await GetAllCats(), _ => GetAllCanExecute);
            InitializeComponent();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async Task GetAllCats()
        {
            DispatcherOperation? op = null;
            DispatcherOperation? elapsedOp = null;
            try
            {
                GetAllCanExecute = false;
                _startedGetAll = DateTimeOffset.Now;
                Elapsed = DateTimeOffset.Now - _startedGetAll;
                elapsedOp = Dispatcher.BeginInvoke(async () =>
                {
                    while (!_getAllCanExecute)
                    {
                        await Task.Delay(100);
                        Elapsed = DateTimeOffset.Now - _startedGetAll;
                    }
                });
                op = Dispatcher.BeginInvoke(() =>
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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{AllUri}/{Count}/{Delay}");
                HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);
                List<Cat>? list = await JsonSerializer.DeserializeAsync<List<Cat>>(response.Content.ReadAsStream(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                await op;
                if (list is not null)
                {
                    await Dispatcher.BeginInvoke(() =>
                    {
                        Chunks.Add(new Chunk
                        {
                            Id = Chunks.Count + 1,
                            Count = list.Count,
                            CountAll = list.Count,
                            Elapsed = Elapsed,
                            ElapsedAll = Elapsed
                        });
                        foreach (Cat cat in list)
                        {
                            Cats.Add(cat);
                        }
                    });
                }
                GetAllCanExecute = true;
                await elapsedOp;
            }
            catch (Exception ex)
            {
                await Dispatcher.Invoke(async () =>
                {
                    GetAllCanExecute = true;
                    await elapsedOp;
                    MessageBox.Show($"{ex.GetType().ToString()}: {ex.Message}\n{ex.StackTrace}");
                });
            }
        }

    }
}
