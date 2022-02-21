using BigCatsDataContract;
using Net.Leksi;

namespace BigCatsDataServer
{
    /// <summary xml:lang="ru">
    ///     Сервер для демонстрации возможностей класса <see cref="PartialLoader"/>
    /// </summary>
    public class CatsGenerator
    {
        private const string CatNamePrefix = "Кошка №";

       /// <summary>
       ///  Параметризованный генератор кошек.
       /// </summary>
       /// <param name="count">Количество кошек, которое хотим получить.</param>
       /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
       /// <returns></returns>
        public static async IAsyncEnumerable<Cat> GenerateManyCats(int count, int delay)
        {
            int delayFactor = 10000;
            bool isDelayFactorCalculated = false;
            Random random = new Random();

            DateTimeOffset start = default(DateTimeOffset);

            for (int i = 0; i < count; i++)
            {
                if(delay > 0)
                {
                    // Если задали ненулевой delay, имитируем бурную деятельность продолжительностью примерно delay миллисекунд.
                    // Thread.Sleep(delay) или await Task.Delay(delay) использовать не получится, так как они дают минимальную задержку от ~200 ms
                    // Здесь мы подгоняем delayFactor, чтобы получить более точную задержку.
                    if (!isDelayFactorCalculated)
                    { 
                        start = DateTimeOffset.Now;
                    }
                    for(int j = 0; j < delay * delayFactor; j++)
                    {
                        Math.Sin(random.NextDouble() * 2 * Math.PI);
                    }
                    if (!isDelayFactorCalculated)
                    {
                        TimeSpan elapsed = DateTimeOffset.Now - start;
                        if (elapsed.TotalMilliseconds < delay)
                        {
                            delayFactor *= (int)Math.Ceiling(delay / elapsed.TotalMilliseconds);
                        }
                        else
                        {
                            delayFactor = (int)Math.Floor(delayFactor / elapsed.TotalMilliseconds * delay);
                            isDelayFactorCalculated = true;
                        }
                    }
                }
                yield return await Task.Run(() => new Cat { Name = $"{CatNamePrefix}{i + 1}" });
            }
        }

        /// <summary>
        ///     Метод, возвращающий всех кошек сразу.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="count">Количество кошек, которое хотим получить.</param>
        /// <param name="delay">Время в миллисекундах, требуемое для генерации одной кошки.</param>
        /// <returns></returns>
        public static async Task GetCats(HttpContext httpContext, int count, int delay)
        {
            List<Cat> cats = new();
            await foreach(Cat cat in GenerateManyCats(count, delay).ConfigureAwait(false))
            {
                cats.Add(cat);
            }
            await httpContext.Response.WriteAsJsonAsync<List<Cat>>(cats);
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
        public static async Task GetCatsChunks(HttpContext context, int count, int timeout, int paging, int delay)
        {
            PartialLoader<Cat> partialLoader;
            string key = null!;

            // Получаем хранилище через механизм внедрения зависимостей.
            CatsLoaderStorage loaderStorage = context.RequestServices.GetRequiredService<CatsLoaderStorage>();

            if (!context.Request.Headers.ContainsKey(Constants.PartialLoaderSessionKey))
            {
                // Если это первый запрос, то создаём PartialLoader и стартуем генерацию.
                partialLoader = new();
                await partialLoader.StartAsync(GenerateManyCats(count, delay), new PartialLoaderOptions { 
                    Timeout = TimeSpan.FromMilliseconds(timeout),
                    Paging = paging,
                }).ConfigureAwait(false);
            } 
            else
            {
                // Если это последующий запрос, то берём PartialLoader из хранилища и продолжаем генерацию.
                key = context.Request.Headers[Constants.PartialLoaderSessionKey];
                partialLoader = loaderStorage.Data[key];
                await partialLoader.ContinueAsync().ConfigureAwait(false);
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


            await context.Response.WriteAsJsonAsync<List<Cat>>(partialLoader.Chunk);
        }
    }
}
