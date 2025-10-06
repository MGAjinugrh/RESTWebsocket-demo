using Entities.Objects;

namespace Common.Data.Seeders;
public static class UserSeeder
{
    public static void Seed(ManagementDbContext db)
    {
        if (db.Users.Any(u => u.Username == "admin")) return;

        var adminRole = db.Roles.First(r => r.Name == "Admin");

        db.Users.Add(new Users
        {
            RoleId = adminRole.Id,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}
