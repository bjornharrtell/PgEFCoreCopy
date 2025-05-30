using Microsoft.EntityFrameworkCore;

namespace Wololo.PgEFCoreCopy.Benchmarks;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
}