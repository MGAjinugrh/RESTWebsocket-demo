using Entities;
using Entities.Objects;
using Microsoft.EntityFrameworkCore;

namespace Common.Data;

public class ManagementDbContext : DbContext, IUserDbContext, IRoleDbContext
{
    public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options) {}
    public DbSet<Users> Users => Set<Users>();

    public DbSet<Roles> Roles => Set <Roles>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ------- users ----------
        builder.Entity<Users>(e =>
        {
            e.Property(u => u.Id).UseMySqlIdentityColumn(); //Config Auto-Increment
            e.HasOne(u => u.Role)
             .WithMany()
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict); // will prevent deletion of a role if user are still linked
        });

        // -------- roles ----------
        builder.Entity<Roles>(e =>
        {
            e.Property(u => u.Id).UseMySqlIdentityColumn(); //Config Auto-Increment
        });
    }
}
