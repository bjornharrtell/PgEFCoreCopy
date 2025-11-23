using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MysticMind.PostgresEmbed;

namespace Wololo.PgEFCoreCopy.Tests;

[TestClass]
public class DbContextExtensionsTest
{
    private static PgServer pg = null!;
    private static ServiceProvider serviceProvider = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        IServiceCollection services = new ServiceCollection();
        pg = new PgServer("17.5.0", clearWorkingDirOnStart: true, clearInstanceDirOnStop: true);
        pg.Start();
        services.AddDbContext<TestDbContext>(options =>
        {
            options.UseNpgsql($"Host=localhost;Port={pg.PgPort};Username=postgres;Password=postgres;Database=postgres");
        });
        serviceProvider = services.BuildServiceProvider();
        var testContext = serviceProvider.GetRequiredService<TestDbContext>();
        testContext.Database.EnsureCreated();
    }

    [ClassCleanup()]
    public static void ClassCleanup()
    {
        pg.Stop();
        pg.Dispose();
    }

    [TestMethod]
    public async Task BasicTest()
    {
        var date = new DateOnly(2023, 10, 12);
        var dateTime = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var testContext = serviceProvider.GetRequiredService<TestDbContext>();
        List<TestEntity> testEntities = [
            new TestEntity { Id = 10, Name = "Test1", CreatedAt = dateTime, IsActive = true },
            new TestEntity { Id = 20, Name = "Test2", CreatedAt = dateTime, IsActive = false, ExampleDate = date },
        ];
        await testContext.ExecuteInsertRangeAsync(testEntities, new ExecuteInsertRangeOptions { IncludePrimaryKey = true });
        var count = testContext.TestEntities.Count();
        Assert.AreEqual(2, count, "Expected 2 entities to be inserted.");
        var a = testContext.TestEntities.ToArray();
        Assert.AreEqual(10, a[0].Id);
        Assert.AreEqual("Test1", a[0].Name);
        Assert.AreEqual(a[0].CreatedAt.ToString("o"), dateTime.ToString("o"));
        Assert.IsTrue(a[0].IsActive);
        Assert.AreEqual(20, a[1].Id);
        Assert.AreEqual("Test2", a[1].Name);
        Assert.AreEqual(a[1].CreatedAt.ToString("o"), dateTime.ToString("o"));
        Assert.IsFalse(a[1].IsActive);
        Assert.AreEqual(a[1].ExampleDate.ToString(), date.ToString());
    }

    [TestMethod]
    public async Task BasicTestAutoPK()
    {
        var dateTime = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var testContext = serviceProvider.GetRequiredService<TestDbContext>();
        List<TestEntity2> testEntities = [
            new TestEntity2 { Name = "Test1", CreatedAt = dateTime, IsActive = true },
            new TestEntity2 { Name = "Test2", CreatedAt = dateTime, IsActive = false }
        ];
        await testContext.ExecuteInsertRangeAsync(testEntities);
        var count = testContext.TestEntities2.Count();
        Assert.AreEqual(2, count, "Expected 2 entities to be inserted.");
        var a = testContext.TestEntities2.ToArray();
        Assert.AreEqual(1, a[0].Id);
        Assert.AreEqual("Test1", a[0].Name);
        Assert.AreEqual(a[0].CreatedAt.ToString("o"), dateTime.ToString("o"));
        Assert.IsTrue(a[0].IsActive);
        Assert.AreEqual(2, a[1].Id);
        Assert.AreEqual("Test2", a[1].Name);
        Assert.AreEqual(a[1].CreatedAt.ToString("o"), dateTime.ToString("o"));
        Assert.IsFalse(a[1].IsActive);
    }
}