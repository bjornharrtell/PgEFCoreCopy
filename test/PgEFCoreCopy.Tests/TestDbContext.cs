using Microsoft.EntityFrameworkCore;

namespace Wololo.PgEFCoreCopy.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
    public DbSet<TestEntity2> TestEntities2 { get; set; }
}