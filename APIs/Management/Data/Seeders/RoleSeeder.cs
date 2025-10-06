using Entities.Objects;

namespace Common.Data.Seeders;
public static class RoleSeeder
{
    public static void Seed(ManagementDbContext db)
    {
        if (db.Roles.Any()) return;

        db.Roles.AddRange(
            new Roles { Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow },
            new Roles { Name = "Member", Description = "Regular Member", CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}
