using Common.Data;
using Common.Data.Seeders;

namespace Common.StartupActions;

public class DataInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    public DataInitializer(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
        // Perform seeding
        RoleSeeder.Seed(db);
        UserSeeder.Seed(db);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
