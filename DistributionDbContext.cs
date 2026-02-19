using Microsoft.EntityFrameworkCore;
using DistributionAPI.Models;

namespace DistributionAPI.Data
{
    public class DistributionDbContext : DbContext
    {
        public DistributionDbContext(DbContextOptions<DistributionDbContext> options)
            : base(options)
        {
        }

        public DbSet<DistributorInventory> Inventories { get; set; }
        public DbSet<SellerOrder> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed inventory data
            modelBuilder.Entity<DistributorInventory>().HasData(
                new DistributorInventory
                {
                    Id = 1,
                    DistributorId = 1,
                    BlanketId = 1,
                    Quantity = 50
                },
                new DistributorInventory
                {
                    Id = 2,
                    DistributorId = 1,
                    BlanketId = 2,
                    Quantity = 75
                },
                new DistributorInventory
                {
                    Id = 3,
                    DistributorId = 1,
                    BlanketId = 3,
                    Quantity = 25
                }
            );
        }
    }
}