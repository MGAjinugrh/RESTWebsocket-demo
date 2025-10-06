using EfAutoMigration;
using Entities;
using Entities.Objects;
using Entities.Queries;
using Library;
using Library.Data;
using Library.Facades;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

var b = WebApplication.CreateBuilder(args);


b.Services.AddControllers();

//Register DbContext
var connString = b.Configuration.GetConnectionString("Db")
                ?? throw new InvalidOperationException("ConnectionStrings:Db missing");
b.Services.AddDbContext<LibraryDbContext>(o =>
    o.UseMySql(connString, ServerVersion.AutoDetect(connString)));

// Register each interface mapping to the same instance
b.Services.AddScoped<IUserDbContext>(sp => sp.GetRequiredService<LibraryDbContext>());
b.Services.AddScoped<IBookDbContext>(sp => sp.GetRequiredService<LibraryDbContext>());

b.Services.AddEfAutoMigration<LibraryDbContext>("books");

b.Services.AddScoped<IBookFacade, BookFacade>();

b.Services.AddControllers();
b.Services.AddSingleton<SocketConnectionManager>();

var app = b.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var userIdStr = context.Request.Query["userId"].ToString();
    if (!uint.TryParse(userIdStr, out var userId))
    {
        context.Response.StatusCode = 401;
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var userQry = new UserQueries(db);
    Users user = await userQry.GetDetailByIdAsync(userId, CancellationToken.None);

    if (user == null)
    {
        context.Response.StatusCode = 403;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();

    var manager = app.Services.GetRequiredService<SocketConnectionManager>();
    manager.AddSocket(ws, userId);

    var buffer = new byte[1024 * 4];
    while (ws.State == WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await manager.RemoveSocket(ws);
            Console.WriteLine($"User {userId} disconnected.");
        }
    }
});

app.MapControllers();
app.Run();
