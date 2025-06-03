using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using MysticMind.PostgresEmbed;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;

namespace Wololo.PgEFCoreCopy.Benchmarks;

public class Program
{
    public class TraverseBenchmark
    {
        [Params(200000)]
        public int SampleCount;
        private PgServer pg;
        private ServiceProvider serviceProvider;
        readonly List<TestEntity> testEntities = [];

        [GlobalSetup]
        public void Setup()
        {
            IServiceCollection services = new ServiceCollection();
            pg = new PgServer("17.5.0", clearWorkingDirOnStart: true, clearInstanceDirOnStop: true);
            pg.Start();
            services.AddDbContext<TestDbContext>(options =>
                options.UseNpgsql($"Host=localhost;Port={pg.PgPort};Username=postgres;Password=postgres;Database=postgres")
            );
            serviceProvider = services.BuildServiceProvider();
            var testContext = serviceProvider.GetRequiredService<TestDbContext>();
            testContext.Database.EnsureCreated();
            var dateTime = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < SampleCount; i++)
                testEntities.Add(new TestEntity { Id = i + 1, Name = $"Test{i + 1}", Name2 = $"Test2{i + 1}", Name3 = $"Test3{i + 1}", Name4 = $"Test4{i + 1}", CreatedAt = dateTime, IsActive = i % 2 == 0 });
        }

        [IterationCleanup]
        public void Cleanup()
        {
            var testContext = serviceProvider.GetRequiredService<TestDbContext>();
            testContext.Database.ExecuteSql($"truncate table \"TestEntities\"");
        }

        [Benchmark]
        public async Task PgEFCoreCopy()
        {
            var testContext = serviceProvider.GetRequiredService<TestDbContext>();
            await testContext.ExecuteInsertRangeAsync(testEntities);
        }

        [Benchmark]
        public async Task EFCoreBulkExtensions()
        {
            var testContext = serviceProvider.GetRequiredService<TestDbContext>();
            await testContext.BulkInsertAsync(testEntities);
        }
    }

    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;
        config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}