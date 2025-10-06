using Entities.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Entities;

/// <summary>
/// Well actually this helps the main project to pick only needed entity/some group of entities
/// instead of including everyone. (Allow more flexibility)
/// Also allows for Mock test without the need to connect to real DB as well as swapping DB engines
/// </summary>
public interface IUserDbContext
{
    DbSet<Users> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IRoleDbContext
{
    DbSet<Roles> Roles { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IBookDbContext
{
    DbSet<Books> Books { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}