using Entities;
using Entities.Objects;
using Microsoft.EntityFrameworkCore;

namespace Library.Data
{
    public class LibraryDbContext : DbContext, IBookDbContext, IUserDbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
                : base(options) { }

        public DbSet<Users> Users => Set<Users>();

        public DbSet<Books> Books => Set<Books>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ------- books ----------
            builder.Entity<Books>(e =>
            {
                e.Property(b => b.Id).UseMySqlIdentityColumn(); //Config Auto-Increment

                // Define case-insensitive collation on specific string fields (for filtering)
                e.Property(b => b.Title)
                 .UseCollation("utf8mb4_general_ci");

                e.Property(b => b.Author)
                 .UseCollation("utf8mb4_general_ci");

                e.Property(b => b.Summary)
                 .UseCollation("utf8mb4_general_ci");
            });
        }
    }
}
