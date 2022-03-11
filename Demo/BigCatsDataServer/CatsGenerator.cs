using BigCatsDataContract;
using Net.Leksi;
using Net.Leksi.PartialLoader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigCatsDataServer
{
    /// <summary xml:lang="ru">
    ///     Сервер для демонстрации возможностей класса <see cref="PartialLoader"/>
    /// </summary>
    public class CatsGenerator
    {
        public const string CatNamePrefix = "Кошка №";

        private const long MilliSecondTicks = 10000L;

       /// <summary>
       ///  Параметризованный генератор кошек.
       /// </summary>
       /// <param name="count">Количество кошек, которое хотим получить.</param>
       /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
       /// <returns></returns>
        public static async IAsyncEnumerable<Cat> GenerateManyCats(int count, double delay)
        {
            Random random = new Random();

            for (int i = 0; i < count; i++)
            {
                yield return await Task.Run(() => 
                {
                    if (delay > 0)
                    {
                        // Если задали ненулевой delay, имитируем бурную деятельность продолжительностью примерно delay миллисекунд.
                        int j = 0;
                        DateTimeOffset start = DateTimeOffset.Now;
                        while (true)
                        {
                            Math.Sin(random.NextDouble() * 2 * Math.PI);
                            if ((DateTimeOffset.Now - start).Ticks >= delay * MilliSecondTicks)
                            {
                                break;
                            }
                        }
                    }
                    return new Cat { Name = $"{CatNamePrefix}{i + 1}" }; 
                });
            }
            yield break;
        }

        /// <summary>
        ///     Метод, возвращающий всех кошек сразу.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="count">Количество кошек, которое хотим получить.</param>
        /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
        /// <returns></returns>
        public static async Task GetCats(HttpContext httpContext, int count, double delay)
        {
            List<Cat> cats = new();
            await foreach(Cat cat in GenerateManyCats(count, delay).ConfigureAwait(false))
            {
                cats.Add(cat);
            }
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            await httpContext.Response.WriteAsJsonAsync<List<Cat>>(cats, jsonOptions);
        }

        /// <summary>
        ///     Метод, возвращающий кошек партиями. 
        ///     1) Если установлен timeout (timeout != -1), и отключен paging (paging == 0), то возвращается партия, 
        ///     успевшая сгенерироваться за timeout миллисекунд. 
        ///     2) Если отключен timeout (timeout == -1), и установлен paging (paging } 0), то возвращается партия из
        ///     paging кошек, кроме, возможно, последней партии. 
        ///     3) Если оба параметра использованы, то возвращается партия, размер которой зависит от условия, которое выполнилось раньше.
        ///     Параметры применяютя при первом запросе, при последующих запросах - игнорируются, если <see cref="httpContext.Request"/> 
        ///     содержит заголовок с идентификатором данной серии запросов вплоть до возврата запрошенного количества кошек.
        ///     Важно обратить внимание на то, что генерация кошек происходит не только во время запроса, но и между запросами. Поэтому мы используем 
        ///     хранилище для <see cref="Net.Leksi.PartialLoader{T}"/> объекта.
        ///     В данном демо-сервере мы находим <see cref="Net.Leksi.PartialLoader{T}"/> объект в общем хранилище по ключу-идентификатору серии запросов. 
        ///     После получения всех запрошенных кошек этот объект освобождается, но если при неполном возврате клиент не запросит очередную партию,
        ///     то объект будет жить вечно. Предполагается, что в реальных условиях разработчик эту ситуацию обработает.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="count">Количество кошек, которое хотим получить.</param>
        /// <param name="timeout">Время в миллисекундах, по прошествии которого метод посылает в <see cref="httpContext.Response"/> кошек,
        /// которые успели сгенерироваться при условии paging == 0.</param>
        /// <param name="paging">Фиксированный размер партии кошек, которая возвращается при условии timeout == -1.</param>
        /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
        /// <returns></returns>
        public static async Task GetCatsChunks(HttpContext context, int count, int timeout, int paging, double delay)
        {
            ChunksPartialLoader<Cat> partialLoader;
            string key = null!;

            // Получаем хранилище через механизм внедрения зависимостей.
            CatsLoaderStorage loaderStorage = context.RequestServices.GetRequiredService<CatsLoaderStorage>();

            if (!context.Request.Headers.ContainsKey(Constants.PartialLoaderSessionKey))
            {
                // Если это первый запрос, то создаём PartialLoader и стартуем генерацию.
                partialLoader = context.RequestServices.GetRequiredService<ChunksPartialLoader<Cat>>();
                PartialLoaderOptions options = new PartialLoaderOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(timeout),
                    Paging = paging,
                };
                partialLoader.Initialize(GenerateManyCats(count, delay), options);
                await partialLoader.LoadAsync().ConfigureAwait(false);
            } 
            else
            {
                // Если это последующий запрос, то берём PartialLoader из хранилища и продолжаем генерацию.
                key = context.Request.Headers[Constants.PartialLoaderSessionKey];
                partialLoader = (ChunksPartialLoader<Cat>)loaderStorage.Data[key];
                await partialLoader.LoadAsync().ConfigureAwait(false);
            }

            // Добавляем заголовок ответа, сигнализирующий, последняя это партия или нет.
            context.Response.Headers.Add(Constants.PartialLoaderStateHeaderName, partialLoader.State.ToString());

            if(partialLoader.State == PartialLoaderState.Partial)
            {
                // Если партия не последняя, 
                if(key is null)
                {
                    // Если партия первая, придумываем идентификатор серии запросов и помещаем PartialLoader в хранилище.
                    key = Guid.NewGuid().ToString();
                    loaderStorage.Data[key] = partialLoader;
                }
                // Добавляем заголовок ответа с идентификатором серии запросов.
                context.Response.Headers.Add(Constants.PartialLoaderSessionKey, key);
            }
            else
            {
                // Если партия последняя, удаляем PartialLoader из хранилища.
                if (key is not null)
                {
                    loaderStorage.Data.Remove(key);
                }
            }

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            jsonOptions.Converters.Add(new TransferJsonConverterFactory(context.RequestServices)
                .AddTransient<ICat>());

            await context.Response.WriteAsJsonAsync<List<Cat>>(partialLoader.Chunks, jsonOptions);
        }

        /// <summary>
        ///     Метод, возвращающий кошек партиями. 
        ///     1) Если установлен timeout (timeout != -1), и отключен paging (paging == 0), то возвращается партия, 
        ///     успевшая сгенерироваться за timeout миллисекунд. 
        ///     2) Если отключен timeout (timeout == -1), и установлен paging (paging } 0), то возвращается партия из
        ///     paging кошек, кроме, возможно, последней партии. 
        ///     3) Если оба параметра использованы, то возвращается партия, размер которой зависит от условия, которое выполнилось раньше.
        ///     Параметры применяютя при первом запросе, при последующих запросах - игнорируются, если <see cref="httpContext.Request"/> 
        ///     содержит заголовок с идентификатором данной серии запросов вплоть до возврата запрошенного количества кошек.
        ///     Важно обратить внимание на то, что генерация кошек происходит не только во время запроса, но и между запросами. Поэтому мы используем 
        ///     хранилище для <see cref="Net.Leksi.PartialLoader{T}"/> объекта.
        ///     В данном демо-сервере мы находим <see cref="Net.Leksi.PartialLoader{T}"/> объект в общем хранилище по ключу-идентификатору серии запросов. 
        ///     После получения всех запрошенных кошек этот объект освобождается, но если при неполном возврате клиент не запросит очередную партию,
        ///     то объект будет жить вечно. Предполагается, что в реальных условиях разработчик эту ситуацию обработает.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="count">Количество кошек, которое хотим получить.</param>
        /// <param name="timeout">Время в миллисекундах, по прошествии которого метод посылает в <see cref="httpContext.Response"/> кошек,
        /// которые успели сгенерироваться при условии paging == 0.</param>
        /// <param name="paging">Фиксированный размер партии кошек, которая возвращается при условии timeout == -1.</param>
        /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
        /// <returns></returns>
        public static async Task GetCatsJson(HttpContext context, int count, int timeout, int paging, double delay)
        {
            //PartialLoader<Cat> partialLoader;
            //string key = null!;

            //// Получаем хранилище через механизм внедрения зависимостей.
            //CatsLoaderStorage loaderStorage = context.RequestServices.GetRequiredService<CatsLoaderStorage>();

            //if (!context.Request.Headers.ContainsKey(Constants.PartialLoaderSessionKey))
            //{
            //    // Если это первый запрос, то создаём PartialLoader и стартуем генерацию.
            //    partialLoader = context.RequestServices.GetRequiredService<PartialLoader<Cat>>();
            //    partialLoader.Initialize(GenerateManyCats(count, delay), new PartialLoaderOptions
            //    {
            //        Timeout = TimeSpan.FromMilliseconds(timeout),
            //        Paging = paging,
            //    });
            //    key = Guid.NewGuid().ToString();
            //    loaderStorage.Data[key] = partialLoader;
            //}
            //else
            //{
            //    // Если это последующий запрос, то берём PartialLoader из хранилища и продолжаем генерацию.
            //    key = context.Request.Headers[Constants.PartialLoaderSessionKey];
            //    partialLoader = loaderStorage.Data[key];
            //}

            //if(partialLoader.State != PartialLoaderState.Full)
            //{
            //    JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            //    jsonOptions.Converters.Add((JsonConverter<StubForJson<Cat>>)partialLoader);
            //    jsonOptions.Converters.Add(new TransferJsonConverterFactory(context.RequestServices)
            //        .AddTransient<ICat>());

            //    // Добавляем заголовок ответа с идентификатором серии запросов.
            //    context.Response.Headers.Add(Constants.PartialLoaderSessionKey, key);

            //    await context.Response.WriteAsJsonAsync<StubForJson<Cat>>(StubForJson<Cat>.Instance, jsonOptions);
            //} 
            //else
            //{
            //    if (key is not null)
            //    {
            //        loaderStorage.Data.Remove(key);
            //    }
            //    await Results.NoContent().ExecuteAsync(context);
            //}
            await Results.NotFound().ExecuteAsync(context);

        }
    }
}
