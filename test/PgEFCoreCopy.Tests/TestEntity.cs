using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace Wololo.PgEFCoreCopy.Tests;

public class TestEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? ExampleDate { get; set; }   
    public bool IsActive { get; set; } = true;
    public TimeSpan? Duration { get; set; }
    public Instant? NodaInstant { get; set; }
    public LocalDate? NodaLocalDate { get; set; }
    public LocalDateTime? NodaLocalDateTime { get; set; }
    public LocalTime? NodaLocalTime { get; set; }
    public NodaTime.Duration? NodaDuration { get; set; }
    public Interval? NodaInterval { get; set; }
    public ulong? UnsignedLong { get; set; }
}