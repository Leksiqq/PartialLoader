using Net.Leksi;
using System.Text.Json;

namespace BigCatsDataServer
{
    public class CatsGenerator
    {
        private const string Cat = "Кошка №";
        public static async IAsyncEnumerable<Cat> GenerateManyCats(int count, int delay)
        {
            int delayFactor = 10000;
            Random random = new Random();
            DateTimeOffset start = default(DateTimeOffset);
            for (int i = 0; i < count; i++)
            {
                Cat result = new Cat { Name = $"{Cat}{i}" };
                if(delay > 0)
                {
                    if(i == 0)
                    { 
                        start = DateTimeOffset.Now;
                    }
                    for(int j = 0; j < delay * delayFactor; j++)
                    {
                        Math.Sin(random.NextDouble() * 2 * Math.PI);
                    }
                    if (i == 0)
                    {
                        TimeSpan elapsed = DateTimeOffset.Now - start;
                        if (elapsed.TotalMilliseconds < delay)
                        {
                            delayFactor *= (int)Math.Ceiling(delay / elapsed.TotalMilliseconds);
                            i--;
                            continue;
                        }
                        delayFactor = (int)Math.Floor(delayFactor / elapsed.TotalMilliseconds * delay);
                    }
                }
                yield return result  
            }
        }

        public static async Task GetCats(HttpContext httpContext, int count, int delay)
        {
            List<Cat> cats = new();
            await foreach(Cat cat in GenerateManyCats(count, delay).ConfigureAwait(false))
            {
                cats.Add(cat);
            }
            await httpContext.Response.WriteAsJsonAsync<List<Cat>>(cats);
        }

        public static async Task GetCatsChunks(HttpContext context, int count, int timeout, int paging, int delay)
        {
            const string partialLoaderStateHeaderName = "X-CatsPartialLoaderState";
            const string partialLoaderSessionKey = "X-CatsPartialLoaderSessionKey";
            PartialLoader<Cat> partialLoader;
            string key = null;
            CatsLoaderStorage loaderStorage = context.RequestServices.GetRequiredService<CatsLoaderStorage>();
            if (!context.Request.Headers.ContainsKey(partialLoaderSessionKey))
            {
                partialLoader = new();
                await partialLoader.StartAsync(GenerateManyCats(count, delay), new PartialLoaderOptions { 
                    Timeout = TimeSpan.FromMilliseconds(timeout),
                    Paging = paging,
                }).ConfigureAwait(false);
            } 
            else
            {
                key = context.Request.Headers[partialLoaderSessionKey];
                partialLoader = loaderStorage.Data[key];
                await partialLoader.ContinueAsync().ConfigureAwait(false);
            }
            context.Response.Headers.Add(partialLoaderStateHeaderName, partialLoader.State.ToString());
            if(partialLoader.State == PartialLoaderState.Partial)
            {
                if(key is null)
                {
                    key = Guid.NewGuid().ToString();
                    loaderStorage.Data[key] = partialLoader;
                }
                context.Response.Headers.Add(partialLoaderSessionKey, key);
            }
            else
            {
                if (key is not null)
                {
                    loaderStorage.Data.Remove(key);
                }
            }
            await context.Response.WriteAsJsonAsync<List<Cat>>(partialLoader.Chunk);
        }
    }
}
