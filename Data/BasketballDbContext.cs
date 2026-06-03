using BAsketball_stats.Models;
using Microsoft.EntityFrameworkCore;

namespace BAsketball_stats.Data
{
    public class BasketballDbContext : DbContext
    {
        public BasketballDbContext(DbContextOptions<BasketballDbContext> options) : base(options)
        {
        }

        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<PlayerStat> PlayerStats => Set<PlayerStat>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }
    }
}