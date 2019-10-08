using Microsoft.EntityFrameworkCore;

namespace YouZack.Entities
{
    public class YZDbContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Episode> Episodes { get; set; }

        private string connectionString { get; set; }

        public YZDbContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }

    }
}
