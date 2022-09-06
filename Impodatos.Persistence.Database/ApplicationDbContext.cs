using Impodatos.Domain;
using Impodatos.Persistence.Database.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Impodatos.Persistence.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<history> history { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            historyConfiguration.SetEntityBuilder(builder.Entity<history>());
            base.OnModelCreating(builder);
            
        }
    }
}
