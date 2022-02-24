using BigCatsDataContract;
using BigCatsDataServer;
using Net.Leksi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatsLoaderStorage>();
builder.Services.AddTransient(typeof(IPartialLoader<>), typeof(PartialLoader<>));

var app = builder.Build();

app.MapGet($"{Constants.AllUri}/{{count=1001}}/{{delay=0}}",
    async (HttpContext context, int count, double delay) =>
    await Task.Run(() => CatsGenerator.GetCats(context, count, delay)).ConfigureAwait(false)
);

app.MapGet($"{Constants.ChunkslUri}/{{count=1001}}/{{timeout=100}}/{{paging=1000}}/{{delay=0}}",
    async (HttpContext context, int count, int timeout, int paging, double delay) =>
    await Task.Run(() => CatsGenerator.GetCatsChunks(context, count, timeout, paging, delay)).ConfigureAwait(false)
);


app.Run();
