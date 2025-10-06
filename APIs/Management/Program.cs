using EfAutoMigration;
using Entities;
using Common.Data;
using Common.Facades;
using Microsoft.EntityFrameworkCore;
using Common.StartupActions;

var b = WebApplication.CreateBuilder(args);

//Register DbContext
var connString = b.Configuration.GetConnectionString("Db")
                ?? throw new InvalidOperationException("ConnectionStrings:Db missing");
b.Services.AddDbContext<ManagementDbContext>(o =>
    o.UseMySql(connString, ServerVersion.AutoDetect(connString)));

// Register each interface mapping to the same instance
b.Services.AddScoped<IRoleDbContext>(sp => sp.GetRequiredService<ManagementDbContext>());
b.Services.AddScoped<IUserDbContext>(sp => sp.GetRequiredService<ManagementDbContext>());

b.Services.AddEfAutoMigration<ManagementDbContext>("roles","users"); // Using my custom EFAutoMigration to ensure DB and entities creations

b.Services.AddScoped<IUserManagementFacade, UserManagementFacade>(); //implement main facade

b.Services.AddControllers();

b.Services.AddHostedService<DataInitializer>();

var app = b.Build();
app.MapControllers();
app.Run();