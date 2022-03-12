namespace Net.Leksi.PartialLoader;

using BigCatsDataContract;
using BigCatsDataServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
<<<<<<< HEAD
=======
using System.Text.Json;
>>>>>>> c65842fbef6ce5dc50269db07c9d631f5e483d10
using System.Threading;
using System.Threading.Tasks;

public class TestPartialLoader
{
    List<Cat> _catsList = new();

    /// <summary xml:lang="ru">
    /// Варианты неправильных последовательностей.
    /// </summary>
    public enum TestWorkflowCase
    {
        LoadNewState,
        LoadFullState,
        LoadStartedState,
        LoadContinuedState,
        InitializeInitializedState,
        InitializePartialState,
        InitializeFullState,
        InitializeStartedState,
        InitializeContinuedState,
    }

    /// <summary xml:lang="ru">
    /// Различные моменты, когда может настичь прерывание.
    /// </summary>
    public enum TestCancelationCase
    {
        CancelNewState,
        CancelInitilizedState,
        CancelStartedState,
        CancelPartialState,
        CancelContinuedState,
        CancelFullState,
    }

    private PartialLoader<Cat> _partialLoader = new();

    [OneTimeSetUp]
    public void StartTest()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }

    [OneTimeTearDown]
    public void EndTest()
    {
        Trace.Flush();
    }

    [SetUp]
    public void Setup()
    {
    }

    ///<summary xml:lang="ru">
    ///<para>Тестируем класс <see cref="Btcom.Server.PartialLoader{T}"/>. Получаем список котиков при различных комбинациях таймаута, 
    ///задержки и пейджинга.</para>
    ///<para>Ожидаемые результаты:</para>
    ///<para>1) Если установлен timeoutMs и не установлен paging, то каждый Сhunk имеет размер от 1 до timeoutMs / delay.</para>
    ///<para>2) Если не установлен timeoutMs и установлен paging, то каждый Сhunk имеет размер paging, кроме последнего, который содержит то, 
    /// что осталось.</para>
    ///<para>3) Если установлен timeoutMs и установлен paging, то каждый Сhunk имеет размер от 1 до paging.</para>
    ///<para>4) Также для всех случаях результирующий список котиков должен совпадать с полученным из всех порций и совпадать с исходным.</para>
    /// </summary>
    ///<param  xml:lang="ru" name="timeoutMs">Значение таймаута - времени ожидания очередной порции (chunk) котиков в миллисекундах.</param>
    ///<param xml:lang="ru" name="paging">Значение пейджинга - желаемого размера очередной порции (chunk) котиков в штуках.</param>
    ///<param xml:lang="ru" name="delay">Значение времени, необходимого для получения одного котика в миллисекундах.</param>
    ///<param xml:lang="ru" name="count">Общее количество котиков.</param>
    [Test]
    [TestCase(100, 0, 10, 1001, false)]
    [TestCase(-1, 4, 0, 1001, false)]
    [TestCase(100, 4, 10, 1001, false)]
    [TestCase(100, 11, 10, 1001, false)]
    [TestCase(100, 0, 10, 1001, true)]
    [TestCase(-1, 4, 0, 1001, true)]
    [TestCase(100, 4, 10, 1001, true)]
    [TestCase(100, 11, 10, 1001, true)]
    public void TestGetCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    {
        RunGetCats(timeoutMs, paging, delay, count, storeResult).Wait();
    }

    ///<summary xml:lang="ru">
    ///<para>Тестируем класс <see cref="Btcom.Server.PartialLoader{T}"/>. Получаем список котиков при различных комбинациях таймаута, 
    ///задержки и пейджинга.</para>
    ///<para>Ожидаемые результаты:</para>
    ///<para>1) Если установлен timeoutMs и не установлен paging, то каждый Сhunk имеет размер от 1 до timeoutMs / delay.</para>
    ///<para>2) Если не установлен timeoutMs и установлен paging, то каждый Сhunk имеет размер paging, кроме последнего, который содержит то, 
    /// что осталось.</para>
    ///<para>3) Если установлен timeoutMs и установлен paging, то каждый Сhunk имеет размер от 1 до paging.</para>
    ///<para>4) Также для всех случаях результирующий список котиков должен совпадать с полученным из всех порций и совпадать с исходным.</para>
    /// </summary>
    ///<param  xml:lang="ru" name="timeoutMs">Значение таймаута - времени ожидания очередной порции (chunk) котиков в миллисекундах.</param>
    ///<param xml:lang="ru" name="paging">Значение пейджинга - желаемого размера очередной порции (chunk) котиков в штуках.</param>
    ///<param xml:lang="ru" name="delay">Значение времени, необходимого для получения одного котика в миллисекундах.</param>
    ///<param xml:lang="ru" name="count">Общее количество котиков.</param>
    //[Test]
    //[TestCase(100, 0, 10, 1001, false)]
    //[TestCase(-1, 4, 0, 1001, false)]
    //[TestCase(100, 4, 10, 1001, false)]
    //[TestCase(100, 11, 10, 1001, false)]
    //[TestCase(100, 0, 10, 1001, true)]
    //[TestCase(-1, 4, 0, 1001, true)]
    //[TestCase(100, 4, 10, 1001, true)]
    //[TestCase(100, 11, 10, 1001, true)]
    //public void TestSerializeCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    //{
    //    RunSerializeCats(timeoutMs, paging, delay, count, storeResult).Wait();
    //}

    /// <summary xml:lang="ru">
    /// Тестируем класс <see cref="Btcom.Server.PartialLoader{T}"/>. Тестируем все варианты неправильных последовательностей. 
    /// В каждом ждём <see cref="System.InvalidOperationException"/> 
    /// с соответствующим сообщением.
    /// </summary>
    /// <param xml:lang="ru" name="testWorkflowCase"><see cref="TestWorkflowCase"/></param>
    [Test]
    [TestCase(TestWorkflowCase.LoadNewState)]
    [TestCase(TestWorkflowCase.LoadFullState)]
    [TestCase(TestWorkflowCase.LoadStartedState)]
    [TestCase(TestWorkflowCase.LoadContinuedState)]
    [TestCase(TestWorkflowCase.InitializeInitializedState)]
    [TestCase(TestWorkflowCase.InitializePartialState)]
    [TestCase(TestWorkflowCase.InitializeFullState)]
    [TestCase(TestWorkflowCase.InitializeStartedState)]
    [TestCase(TestWorkflowCase.InitializeContinuedState)]
    public void TestWorkflow(TestWorkflowCase testWorkflowCase)
    {
        var ex = Assert.Throws<AggregateException>(
            () => RunWorkflow(testWorkflowCase).Wait()
        );
        var ex1 = Assert.Catch(() => throw ex!.InnerException!);
        switch (testWorkflowCase)
        {
            case TestWorkflowCase.LoadNewState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Initialized or Partial, present: New"));
                break;
            case TestWorkflowCase.LoadFullState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Initialized or Partial, present: Full"));
                break;
            case TestWorkflowCase.LoadStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Initialized or Partial, present: Started"));
                break;
            case TestWorkflowCase.LoadContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Initialized or Partial, present: Continued"));
                break;
            case TestWorkflowCase.InitializeInitializedState:
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Initialized"));
                break;
            case TestWorkflowCase.InitializePartialState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Partial"));
                break;
            case TestWorkflowCase.InitializeFullState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Full"));
                break;
            case TestWorkflowCase.InitializeStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Started"));
                break;
            case TestWorkflowCase.InitializeContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Continued"));
                break;
            default:
                Console.WriteLine(ex);
                Console.WriteLine(ex1);
                break;
        }
    }

    /// <summary xml:lang="ru">
    /// Тестируем класс <see cref="Btcom.Server.PartialLoader{T}"/>. Тестируем все варианты прерываний. 
    /// Ждём, что <see cref="Btcom.Server.PartialLoader{T}"/> имеет состояние <see cref="Btcom.Server.PartialLoaderState.Canceled"/>.
    /// Пробуем выполнить запрещённые действия, ждём <see cref="System.InvalidOperationException"/> 
    /// с соответствующим сообщением.
    /// </summary>
    /// <param  xml:lang="ru" name="testCancelationCase"><see cref="TestCancelationCase"/></param>
    [Test]
    [TestCase(TestCancelationCase.CancelNewState)]
    [TestCase(TestCancelationCase.CancelInitilizedState)]
    [TestCase(TestCancelationCase.CancelStartedState)]
    [TestCase(TestCancelationCase.CancelPartialState)]
    [TestCase(TestCancelationCase.CancelContinuedState)]
    [TestCase(TestCancelationCase.CancelFullState)]
    public void TestCancelation(TestCancelationCase testCancelationCase)
    {
        switch (testCancelationCase)
        {
            case
                TestCancelationCase.CancelNewState or
                TestCancelationCase.CancelInitilizedState or
                TestCancelationCase.CancelStartedState or
                TestCancelationCase.CancelPartialState or
                TestCancelationCase.CancelContinuedState or
                TestCancelationCase.CancelFullState:
                RunCancelation(testCancelationCase).Wait();
                break;
        }
    }

    private async Task RunCancelation(TestCancelationCase testCancelationCase, int timeoutMs = -1, int paging = 4)
    {
        const int count = 1001;

        List<Cat> catList = new List<Cat>();
        int delay = testCancelationCase switch
        {
            TestCancelationCase.CancelFullState => 0,
            _ => 10
        };
        if (
            testCancelationCase == TestCancelationCase.CancelStartedState ||
            testCancelationCase == TestCancelationCase.CancelContinuedState
        )
        {
            timeoutMs = 1000;
            paging = 0;
        }
        _partialLoader.Reset();
        using (CancellationTokenSource cancellationTokenSource = new())
        {
            if (testCancelationCase == TestCancelationCase.CancelNewState)
            {
                cancellationTokenSource.Cancel();
                Assert.That(_partialLoader.State, Is.EqualTo(PartialLoaderState.New));
                return;
            }
            else
            {
                _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs),
                    Paging = paging,
                    CancellationToken = cancellationTokenSource.Token
                });
                if (testCancelationCase == TestCancelationCase.CancelInitilizedState)
                {
                    cancellationTokenSource.Cancel();
                    Assert.That(_partialLoader.State, Is.EqualTo(PartialLoaderState.Initialized));
                    return;
                }
                Task t = _partialLoader.LoadAsync();
                if (testCancelationCase == TestCancelationCase.CancelStartedState)
                {
                    cancellationTokenSource.Cancel();
                }
                await t;
                if (testCancelationCase == TestCancelationCase.CancelStartedState)
                {
                }
                while (_partialLoader.State != PartialLoaderState.Canceled)
                {
                    if (_partialLoader.State != PartialLoaderState.Full)
                    {
                        if (testCancelationCase == TestCancelationCase.CancelPartialState)
                        {
                            cancellationTokenSource.Cancel();
                        }
                        else if (testCancelationCase == TestCancelationCase.CancelContinuedState)
                        {
                            Task.Run(async () =>
                            {
                                Task t = Task.Run(() => _partialLoader.LoadAsync());
                                await Task.Delay(timeoutMs / 2);
                                cancellationTokenSource.Cancel();
                                await t;
                            }).Wait();
                        }
                        else
                        {
                            await _partialLoader.LoadAsync();
                        }
                        if (testCancelationCase == TestCancelationCase.CancelPartialState)
                        {
                            await _partialLoader.LoadAsync();
                            break;
                        }
                        if (testCancelationCase == TestCancelationCase.CancelContinuedState)
                        {
                            break;
                        }
                    }
                    else
                    {
                        switch (testCancelationCase)
                        {
                            case TestCancelationCase.CancelFullState:
                                cancellationTokenSource.Cancel();
                                Assert.That(_partialLoader.State == PartialLoaderState.Full);
                                return;
                        }
                        break;
                    }
                }
            }
            Assert.That(_partialLoader.State == PartialLoaderState.Canceled);
        }
    }

    private async Task RunGetCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    {
        if (timeoutMs != -1 && delay == 0)
        {
            Assert.Fail("Если установлен таймаут, задержка должна быть больше 0!");
        }
        List<Cat> cats = new List<Cat>();

        for (int i = _catsList.Count; i < count; i++)
        {
            _catsList.Add(new Cat { Name = $"{CatsGenerator.CatNamePrefix}{i + 1}" });
        }

        _partialLoader.Reset();


        List<Cat> result = new List<Cat>();
        List<Cat> chunk = new List<Cat>();

        PartialLoaderOptions options = new PartialLoaderOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging,
        };

        _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), options);
        _partialLoader.AddUtilizer(item =>
        {
            chunk.Add(item);
            return item;
        });
        if (storeResult)
        {
            _partialLoader.AddUtilizer(item =>
            {
                result.Add(item);
                return item;
            });
        }
        await _partialLoader.LoadAsync();

        while (true)
        {
            int chunkCount = chunk.Count;
            cats.AddRange(chunk);

            chunk.Clear();

            if (_partialLoader.State == PartialLoaderState.Partial)
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount, Is.EqualTo(paging));
                }
                await _partialLoader.LoadAsync();
            }
            else
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount == (count % paging == 0 ? paging : count % paging));
                }
                break;
            }
        }
        //4)
        Assert.That(cats.Count == count && ((storeResult && cats.Count == result.Count) || (!storeResult && result.Count == 0)));
        Assert.That(cats.Zip(result).Zip(_catsList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
    }

    //private async Task RunSerializeCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    //{
    //    if (timeoutMs != -1 && delay == 0)
    //    {
    //        Assert.Fail("Если установлен таймаут, задержка должна быть больше 0!");
    //    }
    //    List<Cat> cats = new List<Cat>();

    //    for (int i = _catsList.Count; i < count; i++)
    //    {
    //        _catsList.Add(new Cat { Name = $"{CatsGenerator.CatNamePrefix}{i + 1}" });
    //    }

    //    _partialLoader.Reset();

    //    JsonSerializerOptions jsonOptions = new();
    //    jsonOptions.Converters.Add(_partialLoader);
    //    jsonOptions.Converters.Add(new TransferJsonConverterFactory(null)
    //        .AddTransient<ICat>());
    //    _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
    //    {
    //        Timeout = TimeSpan.FromMilliseconds(timeoutMs),
    //        Paging = paging,
    //        StoreResult = storeResult
    //    });

    //    do
    //    {
    //        MemoryStream memoryStream = new MemoryStream();
    //        await JsonSerializer.SerializeAsync<StubForJson<Cat>>(memoryStream, StubForJson<Cat>.Instance, jsonOptions);

    //        memoryStream.Position = 0;
    //        using var reader = new StreamReader(memoryStream);
    //        string json = reader.ReadToEnd();

    //        List<Cat> chunk = JsonSerializer.Deserialize<List<Cat>>(json);
    //        if(chunk.Last() is null)
    //        {
    //            chunk.RemoveAt(chunk.Count - 1);
    //        }
    //        int chunkCount = chunk.Count;

    //        cats.AddRange(chunk);

    //        if (timeoutMs != -1 && (paging == 0 || paging > timeoutMs / delay))
    //        {
    //            // 1), 3)
    //            Assert.That(chunkCount > 0);
    //        }
    //        if (_partialLoader.State == PartialLoaderState.Partial)
    //        {
    //            if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
    //            {
    //                // 2), 3)
    //                Assert.That(chunkCount == paging);
    //            }
    //        }
    //        else
    //        {
    //            if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
    //            {
    //                // 2), 3)
    //                Assert.That(chunkCount == (count % paging == 0 ? paging : count % paging));
    //            }
    //        }
    //    }
    //    while (_partialLoader.State != PartialLoaderState.Full);

    //    Assert.That(cats.Count == count && ((storeResult && cats.Count == _partialLoader.Result.Count) || (!storeResult && _partialLoader.Result.Count == 0)));
    //    Assert.That(cats.Zip(_partialLoader.Result).Zip(_catsList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
    //}

    private async Task RunWorkflow(TestWorkflowCase testWorkflowCase, int timeoutMs = -1, int paging = 4)
    {
        const int count = 1001;

        int delay = testWorkflowCase switch
        {
            TestWorkflowCase.InitializeFullState => 0,
            TestWorkflowCase.LoadFullState => 0,
            _ => 10
        };
        List<Cat> cats = new List<Cat>();

        if (
            testWorkflowCase == TestWorkflowCase.LoadStartedState ||
            testWorkflowCase == TestWorkflowCase.LoadContinuedState ||
            testWorkflowCase == TestWorkflowCase.InitializeStartedState ||
            testWorkflowCase == TestWorkflowCase.InitializeContinuedState
        )
        {
            timeoutMs = 1000;
            paging = 0;
        }

        _partialLoader.Reset();

        switch (testWorkflowCase)
        {
            case TestWorkflowCase.LoadNewState:
                await _partialLoader.LoadAsync();
                break;
            case TestWorkflowCase.InitializeStartedState:
                Task.Run(async () =>
                {
                    _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                    Task t = Task.Run(() => _partialLoader.LoadAsync());
                    await Task.Delay(timeoutMs / 2);
                    _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                    await t;
                }).Wait();
                break;
            case TestWorkflowCase.InitializeInitializedState:
                _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                break;
            case TestWorkflowCase.LoadStartedState:
                _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs),
                });
                Task.Run(async () =>
                {
                    DateTime start = DateTime.Now;
                    Task t = Task.Run(() => _partialLoader.LoadAsync());
                    await Task.Delay(timeoutMs / 2);
                    await _partialLoader.LoadAsync();
                    await t;
                }).Wait();
                break;
        }
        _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging
        });
        await _partialLoader.LoadAsync();
        while (true)
        {
            if (_partialLoader.State != PartialLoaderState.Full)
            {
                switch (testWorkflowCase)
                {
                    case TestWorkflowCase.InitializePartialState:
                        _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                        break;
                    case TestWorkflowCase.LoadContinuedState:
                        Task.Run(async () =>
                        {
                            Task t = Task.Run(() => _partialLoader.LoadAsync());
                            await Task.Delay(timeoutMs / 2);
                            await _partialLoader.LoadAsync();
                            await t;
                        }).Wait();
                        break;
                    case TestWorkflowCase.InitializeContinuedState:
                        Task.Run(async () =>
                        {
                            Task t = Task.Run(() => _partialLoader.LoadAsync());
                            await Task.Delay(timeoutMs / 2);
                            _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                            await t;
                        }).Wait();
                        break;
                }
                await _partialLoader.LoadAsync();
            }
            else
            {
                switch (testWorkflowCase)
                {
                    case TestWorkflowCase.InitializeFullState:
                        _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                        break;
                    case TestWorkflowCase.LoadFullState:
                        await _partialLoader.LoadAsync();
                        break;
                }
                break;
            }
        }
        Assert.Fail();
    }
}
