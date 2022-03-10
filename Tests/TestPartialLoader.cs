namespace Net.Leksi;

using BigCatsDataContract;
using BigCatsDataServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        ContunueNewState,
        ContunueFullState,
        ContinueStartedState,
        ContinueContinuedState,
        StartPartialState,
        StartFullState,
        StartStartedState,
        StartContinuedState,
        ChunkNewState,
        ChunkStartedState,
        ChunkContinuedState,
        ResultNewState,
        ResultStartedState,
        ResultContinuedState,
        ResultPartialState,
    }

    /// <summary xml:lang="ru">
    /// Различные моменты, когда может настичь прерывание.
    /// </summary>
    public enum TestCancelationCase
    {
        CancelNewState,
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
    [Test]
    [TestCase(100, 0, 10, 1001, false)]
    [TestCase(-1, 4, 0, 1001, false)]
    [TestCase(100, 4, 10, 1001, false)]
    [TestCase(100, 11, 10, 1001, false)]
    [TestCase(100, 0, 10, 1001, true)]
    [TestCase(-1, 4, 0, 1001, true)]
    [TestCase(100, 4, 10, 1001, true)]
    [TestCase(100, 11, 10, 1001, true)]
    public void TestSerializeCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    {
        RunSerializeCats(timeoutMs, paging, delay, count, storeResult).Wait();
    }

    /// <summary xml:lang="ru">
    /// Тестируем класс <see cref="Btcom.Server.PartialLoader{T}"/>. Тестируем все варианты неправильных последовательностей. 
    /// В каждом ждём <see cref="System.InvalidOperationException"/> 
    /// с соответствующим сообщением.
    /// </summary>
    /// <param xml:lang="ru" name="testWorkflowCase"><see cref="TestWorkflowCase"/></param>
    [Test]
    [TestCase(TestWorkflowCase.ContunueNewState)]
    [TestCase(TestWorkflowCase.ContunueFullState)]
    [TestCase(TestWorkflowCase.ContinueStartedState)]
    [TestCase(TestWorkflowCase.ContinueContinuedState)]
    [TestCase(TestWorkflowCase.StartPartialState)]
    [TestCase(TestWorkflowCase.StartFullState)]
    [TestCase(TestWorkflowCase.StartStartedState)]
    [TestCase(TestWorkflowCase.StartContinuedState)]
    [TestCase(TestWorkflowCase.ChunkNewState)]
    [TestCase(TestWorkflowCase.ChunkStartedState)]
    [TestCase(TestWorkflowCase.ChunkContinuedState)]
    [TestCase(TestWorkflowCase.ResultNewState)]
    [TestCase(TestWorkflowCase.ResultPartialState)]
    [TestCase(TestWorkflowCase.ResultStartedState)]
    [TestCase(TestWorkflowCase.ResultContinuedState)]
    public void TestWorkflow(TestWorkflowCase testWorkflowCase)
    {
        var ex = Assert.Throws<AggregateException>(
            () => RunWorkflow(testWorkflowCase).Wait()
        );
        var ex1 = Assert.Catch(() => throw ex!.InnerException!);
        switch (testWorkflowCase)
        {
            case TestWorkflowCase.ContunueNewState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Partial, present: New"));
                break;
            case TestWorkflowCase.ContunueFullState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Partial, present: Full"));
                break;
            case TestWorkflowCase.ContinueStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Partial, present: Started"));
                break;
            case TestWorkflowCase.ContinueContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Partial, present: Continued"));
                break;
            case TestWorkflowCase.StartPartialState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Partial"));
                break;
            case TestWorkflowCase.StartFullState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Full"));
                break;
            case TestWorkflowCase.StartStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Started"));
                break;
            case TestWorkflowCase.StartContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New, present: Continued"));
                break;
            case TestWorkflowCase.ChunkNewState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full or Partial, present: New"));
                break;
            case TestWorkflowCase.ChunkStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full or Partial, present: Started"));
                break;
            case TestWorkflowCase.ChunkContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full or Partial, present: Continued"));
                break;
            case TestWorkflowCase.ResultNewState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full, present: New"));
                break;
            case TestWorkflowCase.ResultPartialState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full, present: Partial"));
                break;
            case TestWorkflowCase.ResultStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full, present: Started"));
                break;
            case TestWorkflowCase.ResultContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: Full, present: Continued"));
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
    [TestCase(TestCancelationCase.CancelStartedState)]
    [TestCase(TestCancelationCase.CancelPartialState)]
    [TestCase(TestCancelationCase.CancelContinuedState)]
    [TestCase(TestCancelationCase.CancelFullState)]
    public void TestCancelation(TestCancelationCase testCancelationCase)
    {
        switch(testCancelationCase)
        {
            case
                TestCancelationCase.CancelNewState or
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

        Task? auxTask = null;
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
            switch (testCancelationCase)
            {
                case TestCancelationCase.CancelNewState:
                    cancellationTokenSource.Cancel();
                    break;
                case TestCancelationCase.CancelStartedState:
                    auxTask = Task.Run(async () =>
                    {
                        await Task.Delay(timeoutMs / 2);
                        cancellationTokenSource.Cancel();
                    });
                    break;
            }
            await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs),
                Paging = paging,
                CancellationToken = cancellationTokenSource.Token
            });
            Console.WriteLine(_partialLoader.CancelationTrace);
            switch (testCancelationCase)
            {
                case TestCancelationCase.CancelNewState:
                    Assert.That(_partialLoader.CancelationTrace == "1");
                    break;
                case TestCancelationCase.CancelStartedState:
                    Assert.That(_partialLoader.CancelationTrace == "2,3,4,5");
                    break;
            }
            while (_partialLoader.State != PartialLoaderState.Canceled)
            {
                if (auxTask is not null && auxTask.IsFaulted)
                {
                    throw auxTask.Exception!;
                }
                if (_partialLoader.State != PartialLoaderState.Full)
                {
                    switch (testCancelationCase)
                    {
                        case TestCancelationCase.CancelPartialState:
                            cancellationTokenSource.Cancel();
                            break;
                        case TestCancelationCase.CancelContinuedState:
                            auxTask = Task.Run(async () =>
                            {
                                await Task.Delay(timeoutMs / 2);
                                cancellationTokenSource.Cancel();
                            });
                            break;
                    }
                    await _partialLoader.ContinueAsync();
                    switch (testCancelationCase)
                    {
                        case TestCancelationCase.CancelPartialState:
                            Assert.That(_partialLoader.CancelationTrace == "2,3,4,5");
                            break;
                        case TestCancelationCase.CancelContinuedState:
                            Assert.That(_partialLoader.CancelationTrace == "2,3,4,5");
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
                            Assert.That(_partialLoader.CancelationTrace == string.Empty);
                            return;
                    }
                    break;
                }
            }
            if (auxTask is not null)
            {
                await auxTask;
            }
            Assert.That(_partialLoader.State == PartialLoaderState.Canceled);

            var ex0 = Assert.Catch<AggregateException>(() => _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions()).Wait());
            var ex = Assert.Throws<InvalidOperationException>(() => throw ex0!.InnerException!);

            Assert.That(ex!.Message, Is.EqualTo("Expected State: New, present: Canceled"));

            ex0 = Assert.Catch<AggregateException>(() => _partialLoader.ContinueAsync().Wait());
            ex = Assert.Throws<InvalidOperationException>(() => throw ex0!.InnerException!);
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Partial, present: Canceled"));

            List<Cat> cats = new List<Cat>();

            ex = Assert.Throws<InvalidOperationException>(() => cats.AddRange(_partialLoader.Chunk));
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Full or Partial, present: Canceled"));

            ex = Assert.Throws<InvalidOperationException>(() => cats.AddRange(_partialLoader.Result));
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Full, present: Canceled"));

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

        await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging,
            StoreResult = storeResult
        });

        while (true)
        {
            int chunkCount = _partialLoader.Chunk.Count;
            cats.AddRange(_partialLoader.Chunk);

            if (timeoutMs != -1 && (paging == 0 || paging > timeoutMs / delay))
            {
                // 1), 3)
                Assert.That(chunkCount > 0);
            }
            if (_partialLoader.State == PartialLoaderState.Partial)
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount == paging);
                }
                await _partialLoader.ContinueAsync();
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
        Assert.That(cats.Count == count && ((storeResult && cats.Count == _partialLoader.Result.Count) || (!storeResult && _partialLoader.Result.Count == 0)));
        Assert.That(cats.Zip(_partialLoader.Result).Zip(_catsList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
    }

    private async Task RunSerializeCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
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

        JsonSerializerOptions jsonOptions = new();
        jsonOptions.Converters.Add(_partialLoader);
        jsonOptions.Converters.Add(new TransferJsonConverterFactory(null)
            .AddTransient<ICat>());
        _partialLoader.Initialize(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging,
            StoreResult = storeResult
        });

        do
        {
            MemoryStream memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync<StubForJson<Cat>>(memoryStream, StubForJson<Cat>.Instance, jsonOptions);

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            string json = reader.ReadToEnd();

            List<Cat> chunk = JsonSerializer.Deserialize<List<Cat>>(json);
            if(chunk.Last() is null)
            {
                chunk.RemoveAt(chunk.Count - 1);
            }
            int chunkCount = chunk.Count;

            cats.AddRange(chunk);

            if (timeoutMs != -1 && (paging == 0 || paging > timeoutMs / delay))
            {
                // 1), 3)
                Assert.That(chunkCount > 0);
            }
            if (_partialLoader.State == PartialLoaderState.Partial)
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount == paging);
                }
            }
            else
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount == (count % paging == 0 ? paging : count % paging));
                }
            }
        }
        while (_partialLoader.State != PartialLoaderState.Full);

        Assert.That(cats.Count == count && ((storeResult && cats.Count == _partialLoader.Result.Count) || (!storeResult && _partialLoader.Result.Count == 0)));
        Assert.That(cats.Zip(_partialLoader.Result).Zip(_catsList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
    }

    private async Task RunWorkflow(TestWorkflowCase testWorkflowCase, int timeoutMs = -1, int paging = 4)
    {
        const int count = 1001;

        int delay = testWorkflowCase switch {
            TestWorkflowCase.StartFullState => 0,
            TestWorkflowCase.ContunueFullState => 0,
            _ => 10
        };
        Task? auxTask = null;
        List<Cat> cats = new List<Cat>();

        if (
            testWorkflowCase == TestWorkflowCase.ContinueStartedState ||
            testWorkflowCase == TestWorkflowCase.ContinueContinuedState ||
            testWorkflowCase == TestWorkflowCase.StartStartedState ||
            testWorkflowCase == TestWorkflowCase.StartContinuedState ||
            testWorkflowCase == TestWorkflowCase.ChunkStartedState ||
            testWorkflowCase == TestWorkflowCase.ChunkContinuedState ||
            testWorkflowCase == TestWorkflowCase.ResultStartedState ||
            testWorkflowCase == TestWorkflowCase.ResultContinuedState
        )
        {
            timeoutMs = 1000;
            paging = 0;
        }

        _partialLoader.Reset();

        switch(testWorkflowCase)
        {
            case TestWorkflowCase.ContunueNewState:
                await _partialLoader.ContinueAsync();
                break;
            case TestWorkflowCase.ChunkNewState:
                cats.AddRange(_partialLoader.Chunk);
                break;
            case TestWorkflowCase.ResultNewState:
                cats.AddRange(_partialLoader.Result);
                break;
            case TestWorkflowCase.StartStartedState:
                auxTask = Task.Run(async () =>
                {
                    await Task.Delay(timeoutMs / 2);
                    await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                });
                break;
            case TestWorkflowCase.ContinueStartedState:
                auxTask = Task.Run(async () =>
                {
                    await Task.Delay(timeoutMs / 2);
                    await _partialLoader.ContinueAsync();
                });
                break;
            case TestWorkflowCase.ChunkStartedState:
                auxTask = Task.Run(async () =>
                {
                    await Task.Delay(timeoutMs / 2);
                    cats.AddRange(_partialLoader.Chunk);
                });
                break;
            case TestWorkflowCase.ResultStartedState:
                auxTask = Task.Run(async () =>
                {
                    await Task.Delay(timeoutMs / 2);
                    cats.AddRange(_partialLoader.Result);
                });
                break;
        }
        await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging
        });
        while (true)
        {
            if(auxTask is not null && auxTask.IsFaulted)
            {
                throw auxTask.Exception!;
            }
            if (_partialLoader.State != PartialLoaderState.Full)
            {
                switch(testWorkflowCase)
                {
                    case TestWorkflowCase.StartPartialState:
                        await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                        break;
                    case TestWorkflowCase.ResultPartialState:
                        cats.AddRange(_partialLoader.Result);
                        break;
                    case TestWorkflowCase.ContinueContinuedState:
                        auxTask = Task.Run(async () =>
                        {
                            await Task.Delay(timeoutMs / 2);
                            await _partialLoader.ContinueAsync();
                        });
                        break;
                    case TestWorkflowCase.StartContinuedState:
                        auxTask = Task.Run(async () =>
                        {
                            await Task.Delay(timeoutMs / 2);
                            await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                        });
                        break;
                    case TestWorkflowCase.ChunkContinuedState:
                        auxTask = Task.Run(async () =>
                        {
                            await Task.Delay(timeoutMs / 2);
                            cats.AddRange(_partialLoader.Chunk);
                        });
                        break;
                    case TestWorkflowCase.ResultContinuedState:
                        auxTask = Task.Run(async () =>
                        {
                            await Task.Delay(timeoutMs / 2);
                            cats.AddRange(_partialLoader.Result);
                        });
                        break;
                }
                await _partialLoader.ContinueAsync();
            }
            else
            {
                switch(testWorkflowCase)
                {
                    case TestWorkflowCase.StartFullState:
                        await _partialLoader.StartAsync(CatsGenerator.GenerateManyCats(count, delay), new PartialLoaderOptions());
                        break;
                    case TestWorkflowCase.ContunueFullState:
                        await _partialLoader.ContinueAsync();
                        break;
                }
                break;
            }
        }
        if (auxTask is not null)
        {
            await auxTask;
        }
        Assert.Fail();
    }
}
