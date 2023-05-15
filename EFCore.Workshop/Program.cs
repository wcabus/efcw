using Microsoft.EntityFrameworkCore;

namespace EFCore.Workshop;

public class Program
{
    static async Task Main()
    {
        await using var db = new MyContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}

public class MyContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=.;Database=EFCore.Workshop;Trusted_Connection=True;TrustServerCertificate=true");
            optionsBuilder.LogTo(Console.WriteLine);
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Dog> Dogs => Set<Dog>();
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