using Microsoft.EntityFrameworkCore;

namespace EFCore.Workshop;

public class Program
{
    static void Main()
    {

    }
}

public class MyContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Owner> Owners { get; set; }
    public DbSet<Dog> Dogs { get; set; }
}

public class Owner
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public ICollection<Dog>? Dogs { get; set; }
}

public class Dog
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset DateOfBirth { get; set; }

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
}