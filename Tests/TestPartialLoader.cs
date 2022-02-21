namespace Net.Leksi;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class TestPartialLoader
{
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

    private List<Cat> _catList = new();
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
    ///<para>3) Если установлен timeoutMs и установлен paging и paging <= timeoutMs / delay, то каждый Сhunk имеет размер от 1 до paging.</para>
    ///<para>4) Если установлен timeoutMs и установлен paging и paging > timeoutMs / delay, то каждый Сhunk имеет размер от 1 до timeoutMs / delay.</para>
    ///<para>5) Также для всех случаях результирующий список котиков должен совпадать с полученным из всех порций и совпадать с исходным.</para>
    /// </summary>
    ///<param  xml:lang="ru" name="timeoutMs">Значение таймаута - времени ожидания очередной порции (chunk) котиков в миллисекундах.</param>
    ///<param xml:lang="ru" name="paging">Значение пейджинга - желаемого размера очередной порции (chunk) котиков в штуках.</param>
    ///<param xml:lang="ru" name="delay">Значение времени, необходимого для получения одного котика в миллисекундах.</param>
    ///<param xml:lang="ru" name="count">Общее количество котиков.</param>
    [Test]
    [TestCase(100, 0, 10, 1001)]
    [TestCase(-1, 4, 0, 1001)]
    [TestCase(100, 4, 10, 1001)]
    [TestCase(100, 11, 10, 1001)]
    public void TestGetCats(int timeoutMs, int paging, int delay, int count)
    {
        RunGetCats(timeoutMs, paging, delay, count).Wait();
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
            await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs),
                Paging = paging,
                CancellationToken = cancellationTokenSource.Token
            });
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

            var ex = Assert.Throws<InvalidOperationException>(() => _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions()).Wait());
            Assert.That(ex!.Message, Is.EqualTo("Expected State: New, present: Canceled"));

            ex = Assert.Throws<InvalidOperationException>(() => _partialLoader.ContinueAsync().Wait());
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Partial, present: Canceled"));

            List<Cat> cats = new List<Cat>();

            ex = Assert.Throws<InvalidOperationException>(() => cats.AddRange(_partialLoader.Chunk));
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Full or Partial, present: Canceled"));

            ex = Assert.Throws<InvalidOperationException>(() => cats.AddRange(_partialLoader.Result));
            Assert.That(ex!.Message, Is.EqualTo("Expected State: Full, present: Canceled"));

        }
    }

    private async Task RunGetCats(int timeoutMs, int paging, int delay, int count)
    {
        if (timeoutMs != -1 && delay == 0)
        {
            Assert.Fail("Если установлен таймаут, задержка должна быть больше 0!");
        }
        List<Cat> cats = new List<Cat>();
        
        _partialLoader.Reset();

        await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions { 
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            Paging = paging
        });
        while (true)
        {
            int chunkCount = _partialLoader.Chunk.Count;
            cats.AddRange(_partialLoader.Chunk);
            if (timeoutMs != -1 && (paging == 0 || paging > timeoutMs / delay))
            {
                // 1), 4)
                Assert.That(chunkCount > 0 && chunkCount <= timeoutMs / delay);
            }
            if (_partialLoader.State == PartialLoaderState.Partial)
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount <= paging);
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
        //5)
        Assert.That(cats.Count == _catList.Count && cats.Count == _partialLoader.Result.Count);
        Assert.That(cats.Zip(_partialLoader.Result).Zip(_catList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
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
                    await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions());
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
        await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions
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
                        await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions());
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
                            await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions());
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
                        await _partialLoader.StartAsync(Data(delay, count), new PartialLoaderOptions());
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
    private async IAsyncEnumerable<Cat> Data(int delay, int count)
    {
        _catList.Clear();
        DateTime catEpoch = DateTime.Now - TimeSpan.FromDays(2000);
        for (int i = 0; i < count; i++)
        {
            if(delay > 0)
            {
                await Task.Delay(delay);
            }
            Cat result = new Cat { Name = $"Cat{i}", Birthday = catEpoch + TimeSpan.FromDays(i) };
            _catList.Add(result);
            yield return result;
        }
    }
}
