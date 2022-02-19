using BigCatsDataServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatsLoaderStorage>();

var app = builder.Build();

app.MapGet("/cats/{count=1001}/{delay=0}",
    async (HttpContext context, int count, int delay) =>
    await Task.Run(() => CatsGenerator.GetCats(context, count, delay)).ConfigureAwait(false)
);

app.MapGet("/catsChunks/{count=1001}/{timeout=100}/{paging=1000}/{delay=0}",
    async (HttpContext context, int count, int timeout, int paging, int delay) =>
    await Task.Run(() => CatsGenerator.GetCatsChunks(context, count, timeout, paging, delay)).ConfigureAwait(false)
);


app.Run();
