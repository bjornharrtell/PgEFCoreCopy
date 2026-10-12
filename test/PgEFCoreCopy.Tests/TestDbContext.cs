using Microsoft.EntityFrameworkCore;

namespace Wololo.PgEFCoreCopy.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
    public DbSet<TestEntity2> TestEntities2 { get; set; }
    public DbSet<TestEntity3> TestEntities3 { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(o => o.UseNodaTime());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
            .Property(e => e.UnsignedLong)
            .HasColumnType("xid8");

        modelBuilder.Entity<TestEntity3>()
            .Property(e => e.Label)
            .HasComputedColumnSql("\"Number\"::text || CASE WHEN \"Suffix\" <> '' THEN '-' || \"Suffix\" ELSE '' END", stored: true);
    }
}