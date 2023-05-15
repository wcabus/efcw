﻿using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCore.Workshop;

public class Program
{
    static async Task Main()
    {
        await using var db = new MyContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        //await QueryOnShadowProperties(db);
        //await QueryOnSharedType(db);
        await QuerySharedFilters(db);
    }

    private static async Task<List<Dog>> QueryOnShadowProperties(MyContext db)
    {
        return await db.Dogs
            .Where(x => EF.Property<DateTimeOffset>(x, "LastUpdated") <= DateTimeOffset.Now)
            .ToListAsync();
    }

    private static async Task<List<Dictionary<string, object?>>> QueryOnSharedType(MyContext db)
    {
        return await db.Set<Dictionary<string, object?>>("Foo")
            .Where(x => EF.Property<string>(x, "Name").StartsWith("Sar"))
            .ToListAsync();
    }

    private static async Task QuerySharedFilters(MyContext db)
    {
        await db.Dogs
            //.IgnoreQueryFilters()
            .Where(x => x.DateOfBirth.Year == 2000)
            .ToListAsync();
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

        modelBuilder.ApplyConfiguration(new OwnerConfiguration());
        modelBuilder.ApplyConfiguration(new DogConfiguration());
        modelBuilder.HasSequence("mysequence").IsCyclic().IncrementsBy(42);

        modelBuilder.SharedTypeEntity<Dictionary<string, object?>>("Foo", b =>
        {
            b.Property<int>("Id");
            b.Property<string>("Name");
            b.Property<int>("Age");
            b.Property<double>("Score");
        });
    }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Dog> Dogs => Set<Dog>();
    public DbSet<Cat> Cats => Set<Cat>();
}

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("People");
        builder.Property(x => x.LastName)
            .IsRequired()
            .IsFixedLength(false)
            .IsUnicode()
            .HasMaxLength(100)
            .HasColumnName("Surname");

        builder.HasMany(x => x.Dogs)
            .WithOne(x => x.Owner)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // builder.Property(x => x.Id).UseIdentityColumn(); // autogenerated, can't insert custom ID value except when toggling identity inserts
        // builder.Property(x => x.Id).UseSequence(); // use separate sequence for ID generation, but allows inserting custom ID values (sequence might throw an error then however)
        builder.Property(x => x.Id).UseHiLo(); // use cache of IDs to allow local ID generation.

        builder.OwnsOne(x => x.ShippingAddress);
        builder.OwnsOne(x => x.InvoicingAddress);
    }
}

public class Owner
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public ICollection<Dog>? Dogs { get; set; }
    public ICollection<Cat>? Cats { get; set; }

    public Address? ShippingAddress { get; set; }
    public Address? InvoicingAddress { get; set; }
}

public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

public class DogConfiguration : IEntityTypeConfiguration<Dog>
{
    public void Configure(EntityTypeBuilder<Dog> builder)
    {
        builder.Property(x => x.DateOfBirth)
            .UsePropertyAccessMode(PropertyAccessMode.FieldDuringConstruction)
            .HasField("_dateOfBirth");

        // Shadow properties
        // You can also add database-side properties that are not mapped to a property on the entity.
        builder.Property<DateTimeOffset>("LastUpdated");

        builder.HasQueryFilter(x => x.Active);
    }
}

public class Dog
{
    private DateTimeOffset _dateOfBirth;

    public int Id { get; set; }
    public string? Name { get; set; }

    public DateTimeOffset DateOfBirth
    {
        get => _dateOfBirth;
        set
        {
            Validate(value);
            _dateOfBirth = value;

            static void Validate(DateTimeOffset value)
            {
                if (value > DateTimeOffset.Now)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }
    }

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public bool Active { get; set; }
}

public class Cat
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset DateOfBirth { get; set; }
    public string? Title { get; set; }

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
}
