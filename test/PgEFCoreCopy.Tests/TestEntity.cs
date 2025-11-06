using System.ComponentModel.DataAnnotations.Schema;

namespace Wololo.PgEFCoreCopy.Tests;

public class TestEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? ExampleDate { get; set; } 
  
    public bool IsActive { get; set; } = true;
}