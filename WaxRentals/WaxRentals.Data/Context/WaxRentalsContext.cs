using System.Linq;
using Microsoft.EntityFrameworkCore;
using WaxRentals.Data.Entities;

namespace WaxRentals.Data.Context
{
    internal class WaxRentalsContext : DbContext
    {

        // dbo
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Rental> Rentals { get; set; }

        // welcome
        public DbSet<WelcomePackage> WelcomePackages { get; set; }
        public DbSet<WelcomePackageWithKeys> WelcomePackagesWithKeys { get; set; }

        // tracking
        public DbSet<WaxHistory> WaxHistory { get; set; }

        // logs
        public DbSet<Error> Errors { get; set; }
        public DbSet<Message> Messages { get; set; }

        #region " Setup "

        public WaxRentalsContext(DbContextOptions<WaxRentalsContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MonthlyStats>()
                        .HasKey(stats => new { stats.Year, stats.Month });

            // Set default precision on decimal properties.
            // https://stackoverflow.com/a/60260333/128217
            var properties = modelBuilder.Model.GetEntityTypes().SelectMany(
                t => t.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            );
            foreach (var property in properties)
            {
                property.SetColumnType("decimal(18,8)");
            }
        }

        #endregion

    }
}
