using ITRockChallenge.Domain;
using Microsoft.EntityFrameworkCore;

namespace ITRockChallenge.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TodoTask> Tasks => Set<TodoTask>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones adicionales si hiciesen falta en el futuro
            modelBuilder.Entity<TodoTask>().HasIndex(t => t.UserId);

            modelBuilder.Entity<TodoTask>()
                .HasIndex(t => new { t.UserId, t.ExternalSourceId })
                .IsUnique()
                .HasFilter("\"ExternalSourceId\" IS NOT NULL");
        }
    }
}