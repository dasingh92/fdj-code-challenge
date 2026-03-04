using Microsoft.EntityFrameworkCore;

namespace FdjCodeChallenge.Api.Database;

public class FdjDatabase : DbContext
{
    public FdjDatabase(DbContextOptions<FdjDatabase> options) : base(options)
    {
    }

    // public DbSet<Fixture> Fixtures { get; set; }
    // public DbSet<Bet> BetPlacedPayloads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // modelBuilder.Entity<Fixture>().HasKey(f => f.Id);
        // modelBuilder.Entity<Bet>().HasKey(b => new { b.FixtureId, b.OutcomeKey, b.Stake, b.Odds });
    }
}
