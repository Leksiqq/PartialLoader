namespace Net.Leksi.PartialLoader;

using BigCatsDataContract;
using BigCatsDataServer;
using Net.Leksi.TransferJsonConverter;
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
    /// ???????? ???????????? ???????????????????.
    /// </summary>
    public enum TestWorkflowCase
    {
        LoadFullState,
        LoadStartedState,
        LoadContinuedState,
    }

    /// <summary xml:lang="ru">
    /// ????????? ???????, ????? ????? ??????? ??????????.
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
    ///<para>????????? ????? <see cref="Btcom.Server.PartialLoader{T}"/>. ???????? ?????? ??????? ??? ????????? ??????????? ????????, 
    ///???????? ? ?????????.</para>
    ///<para>????????? ??????????:</para>
    ///<para>1) ???? ?????????? timeoutMs ? ?? ?????????? paging, ?? ?????? ?hunk ????? ?????? ?? 1 ?? timeoutMs / delay.</para>
    ///<para>2) ???? ?? ?????????? timeoutMs ? ?????????? paging, ?? ?????? ?hunk ????? ?????? paging, ????? ??????????, ??????? ???????? ??, 
    /// ??? ????????.</para>
    ///<para>3) ???? ?????????? timeoutMs ? ?????????? paging, ?? ?????? ?hunk ????? ?????? ?? 1 ?? paging.</para>
    ///<para>4) ????? ??? ???? ??????? ?????????????? ?????? ??????? ?????? ????????? ? ?????????? ?? ???? ?????? ? ????????? ? ????????.</para>
    /// </summary>
    ///<param  xml:lang="ru" name="timeoutMs">???????? ???????? - ??????? ???????? ????????? ?????? (chunk) ??????? ? ?????????????.</param>
    ///<param xml:lang="ru" name="paging">???????? ????????? - ????????? ??????? ????????? ?????? (chunk) ??????? ? ??????.</param>
    ///<param xml:lang="ru" name="delay">???????? ???????, ???????????? ??? ????????? ?????? ?????? ? ?????????????.</param>
    ///<param xml:lang="ru" name="count">????? ?????????? ???????.</param>
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
    ///<para>????????? ????? <see cref="Btcom.Server.PartialLoader{T}"/>. ???????? ?????? ??????? ??? ????????? ??????????? ????????, 
    ///???????? ? ?????????.</para>
    ///<para>????????? ??????????:</para>
    ///<para>1) ???? ?????????? timeoutMs ? ?? ?????????? paging, ?? ?????? ?hunk ????? ?????? ?? 1 ?? timeoutMs / delay.</para>
    ///<para>2) ???? ?? ?????????? timeoutMs ? ?????????? paging, ?? ?????? ?hunk ????? ?????? paging, ????? ??????????, ??????? ???????? ??, 
    /// ??? ????????.</para>
    ///<para>3) ???? ?????????? timeoutMs ? ?????????? paging, ?? ?????? ?hunk ????? ?????? ?? 1 ?? paging.</para>
    ///<para>4) ????? ??? ???? ??????? ?????????????? ?????? ??????? ?????? ????????? ? ?????????? ?? ???? ?????? ? ????????? ? ????????.</para>
    /// </summary>
    ///<param  xml:lang="ru" name="timeoutMs">???????? ???????? - ??????? ???????? ????????? ?????? (chunk) ??????? ? ?????????????.</param>
    ///<param xml:lang="ru" name="paging">???????? ????????? - ????????? ??????? ????????? ?????? (chunk) ??????? ? ??????.</param>
    ///<param xml:lang="ru" name="delay">???????? ???????, ???????????? ??? ????????? ?????? ?????? ? ?????????????.</param>
    ///<param xml:lang="ru" name="count">????? ?????????? ???????.</param>
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
    /// ????????? ????? <see cref="Btcom.Server.PartialLoader{T}"/>. ????????? ??? ???????? ???????????? ???????????????????. 
    /// ? ?????? ???? <see cref="System.InvalidOperationException"/> 
    /// ? ??????????????? ??????????.
    /// </summary>
    /// <param xml:lang="ru" name="testWorkflowCase"><see cref="TestWorkflowCase"/></param>
    [Test]
    [TestCase(TestWorkflowCase.LoadFullState)]
    [TestCase(TestWorkflowCase.LoadStartedState)]
    [TestCase(TestWorkflowCase.LoadContinuedState)]
    public void TestWorkflow(TestWorkflowCase testWorkflowCase)
    {
        var ex = Assert.Throws<AggregateException>(
            () => RunWorkflow(testWorkflowCase).Wait()
        );
        var ex1 = Assert.Catch(() => throw ex!.InnerException!);
        switch (testWorkflowCase)
        {
            case TestWorkflowCase.LoadFullState:
                Assert.Throws<InvalidOperationException>(() => throw ex1!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New or Partial, present: Full"));
                break;
            case TestWorkflowCase.LoadStartedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New or Partial, present: Started"));
                break;
            case TestWorkflowCase.LoadContinuedState:
                ex1 = Assert.Throws<AggregateException>(() => throw ex1!);
                ex1 = Assert.Throws<InvalidOperationException>(() => throw ex1!.InnerException!);
                Assert.That(ex1!.Message, Is.EqualTo("Expected State: New or Partial, present: Continued"));
                break;
            default:
                Console.WriteLine(ex);
                Console.WriteLine(ex1);
                break;
        }
    }

    /// <summary xml:lang="ru">
    /// ????????? ????? <see cref="Btcom.Server.PartialLoader{T}"/>. ????????? ??? ???????? ??????????. 
    /// ????, ??? <see cref="Btcom.Server.PartialLoader{T}"/> ????? ????????? <see cref="Btcom.Server.PartialLoaderState.Canceled"/>.
    /// ??????? ????????? ??????????? ????????, ???? <see cref="System.InvalidOperationException"/> 
    /// ? ??????????????? ??????????.
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
        switch (testCancelationCase)
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


    private class TestAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            for(int i = 0; i < 10; i++)
            {
                yield return Activator.CreateInstance<T>();
            }
        }
    }

    [Test]
    public void Test2()
    {
        TestAsyncEnumerable<Cat> catGen = new();
        Task.Run(async () => 
        { 
            await foreach(Cat cat in catGen)
            {
                Console.WriteLine(cat);
            }
        }).Wait();
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
                _partialLoader.SetDataProvider(CatsGenerator.GenerateManyCats(count, delay))
                    .SetTimeout(TimeSpan.FromMilliseconds(timeoutMs))
                    .SetPaging(paging)
                    .SetCancellationToken(cancellationTokenSource.Token)
                    ;
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
            Assert.Fail("???? ?????????? ???????, ???????? ?????? ???? ?????? 0!");
        }
        List<Cat> cats = new List<Cat>();

        for (int i = _catsList.Count; i < count; i++)
        {
            _catsList.Add(new Cat { Name = $"{CatsGenerator.CatNamePrefix}{i + 1}" });
        }

        _partialLoader.Reset();


        List<Cat> result = new List<Cat>();
        List<Cat> chunk = new List<Cat>();

        _partialLoader.SetDataProvider(CatsGenerator.GenerateManyCats(count, delay))
            .SetTimeout(TimeSpan.FromMilliseconds(timeoutMs))
            .SetPaging(paging)
            ;
        _partialLoader.AddUtilizer(item =>
        {
            chunk.Add(item);
        });
        if (storeResult)
        {
            _partialLoader.AddUtilizer(item =>
            {
                result.Add(item);
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
                _partialLoader.AddUtilizer(item =>
                {
                    chunk.Add(item);
                });
                if (storeResult)
                {
                    _partialLoader.AddUtilizer(item =>
                    {
                        result.Add(item);
                    });
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

    private async Task RunSerializeCats(int timeoutMs, int paging, int delay, int count, bool storeResult)
    {
        List<Cat> result = new List<Cat>();
        List<Cat> chunk = new List<Cat>();

        if (timeoutMs != -1 && delay == 0)
        {
            Assert.Fail("???? ?????????? ???????, ???????? ?????? ???? ?????? 0!");
        }
        List<Cat> cats = new List<Cat>();

        for (int i = _catsList.Count; i < count; i++)
        {
            _catsList.Add(new Cat { Name = $"{CatsGenerator.CatNamePrefix}{i + 1}" });
        }

        _partialLoader.Reset();

        JsonSerializerOptions jsonOptionsSerialize = new();
        JsonSerializerOptions jsonOptionsDeserialize = new();

        TransferJsonConverterFactory deserializer = new(null);
        deserializer.AddTransient<Cat>();
        deserializer.UseEndOfDataNull = true;

        jsonOptionsSerialize.Converters.Add(new PartialLoadingJsonSerializer<Cat>());
        jsonOptionsDeserialize.Converters.Add(deserializer);
        _partialLoader.SetDataProvider(CatsGenerator.GenerateManyCats(count, delay))
            .SetTimeout(TimeSpan.FromMilliseconds(timeoutMs))
            .SetPaging(paging);

        do
        {
            _partialLoader.AddUtilizer(item =>
            {
                chunk.Add(item);
            });
            if (storeResult)
            {
                _partialLoader.AddUtilizer(item =>
                {
                    result.Add(item);
                });
            }
            MemoryStream memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, _partialLoader, jsonOptionsSerialize);

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            string json = reader.ReadToEnd();

            List<Cat> chunk1 = new();
            deserializer.Target = chunk1;
            
            JsonSerializer.Deserialize(json, typeof(RewritableListStub<Cat>), jsonOptionsDeserialize);

            int chunkCount = chunk1.Count;

            cats.AddRange(chunk1);

            if (timeoutMs != -1 && (paging == 0 || paging > timeoutMs / delay))
            {
                // 1), 3)
                //Assert.That(chunkCount > 0);
            }
            if (_partialLoader.State == PartialLoaderState.Partial)
            {
                if (paging > 0 && (timeoutMs == -1 || paging <= timeoutMs / delay))
                {
                    // 2), 3)
                    Assert.That(chunkCount, Is.EqualTo(paging));
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

        Assert.That(cats.Count == count && ((storeResult && cats.Count == result.Count) || (!storeResult && result.Count == 0)));
        Assert.That(cats.Zip(result).Zip(_catsList, (x, y) => x.First.Name == y.Name && x.Second.Name == y.Name).All(x => x));
    }

    private async Task RunWorkflow(TestWorkflowCase testWorkflowCase, int timeoutMs = -1, int paging = 4)
    {
        const int count = 1001;

        int delay = testWorkflowCase switch
        {
            TestWorkflowCase.LoadFullState => 0,
            _ => 10
        };
        List<Cat> cats = new List<Cat>();

        if (
            testWorkflowCase == TestWorkflowCase.LoadStartedState ||
            testWorkflowCase == TestWorkflowCase.LoadContinuedState
        )
        {
            timeoutMs = 1000;
            paging = 0;
        }

        _partialLoader.Reset();

        switch (testWorkflowCase)
        {
            case TestWorkflowCase.LoadStartedState:
                _partialLoader.SetDataProvider(CatsGenerator.GenerateManyCats(count, delay))
                    .SetTimeout(TimeSpan.FromMilliseconds(timeoutMs))
                    ;
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
        _partialLoader.SetDataProvider(CatsGenerator.GenerateManyCats(count, delay))
            .SetTimeout(TimeSpan.FromMilliseconds(timeoutMs))
            .SetPaging(paging)
            ;
        await _partialLoader.LoadAsync();
        while (true)
        {
            if (_partialLoader.State != PartialLoaderState.Full)
            {
                switch (testWorkflowCase)
                {
                    case TestWorkflowCase.LoadContinuedState:
                        Task.Run(async () =>
                        {
                            Task t = Task.Run(() => _partialLoader.LoadAsync());
                            await Task.Delay(timeoutMs / 2);
                            await _partialLoader.LoadAsync();
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
