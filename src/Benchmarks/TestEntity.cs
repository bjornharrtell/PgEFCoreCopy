using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wololo.PgEFCoreCopy.Benchmarks;

public class TestEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Name2 { get; set; } = string.Empty;
    public string Name3 { get; set; } = string.Empty;
    public string Name4 { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}