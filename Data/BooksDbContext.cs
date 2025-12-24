using LibraryManagementBE.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementBE.Data
{
    public class BooksDbContext : DbContext
    {
        public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options) { }

        public DbSet<Books> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Books>().HasIndex(b => b.genre);
            modelBuilder.Entity<Books>().HasIndex(b => b.title);

            modelBuilder.Entity<Books>()
                .Property(b => b.price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
