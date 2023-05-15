using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Test;

// https://github.com/FransBouma/RawDataAccessBencher

class Program
{
    static void Main()
    {
        using var db = new MyContext();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        //db.Owners.Add(new Owner() { LastName = "Test" });
        //db.SaveChanges();
        //db.Dogs.Where(x => EF.Property<DateTimeOffset>(x, "LastUpdated") == DateTimeOffset.Now).Load();
        //db.Set<Dictionary<string, object>>("Foo").Add(new Dictionary<string, object>());
        //db.SaveChanges();
        //db.Dogs
        //    //.IgnoreQueryFilters()
        //    .Where(x => x.DateOfBirth.Year == 2000)
        //    .ToList();
        //db.Owners.Where(x => x.Dogs.Any(y => y.DateOfBirth.Year == 2000)).Load();
        //var owner = new Owner() { LastName = "Test" };
        //db.Add(owner);
        //var dog = new Dog()
        //{
        //    DateOfBirth = DateTimeOffset.Now,
        //    Name = "Test",
        //    Owner = owner,
        //    Duration = new Duration(10),
        //};
        //db.Dogs.Add(dog);
        //db.SaveChanges();
        //dog.Duration = new Duration(10);
        //db.SaveChanges();
        //db.Set<Foo>().Load();
        //db.Database.ExecuteSqlRaw("create function ...");
        //db.Set<Foo>()
        //    .FromSqlRaw("select 1 as Bar, '2' as Baz")
        //    .OrderBy(x => MyContext.Foo(x.Bar))
        //    .Load();
        //var foo = db.Set<Order>()
        //    .TagWithCallSite()
        //    .Where(x => x.Id == 10)
        //    .First();
        //db.Entry(foo).Reference(x => x.DetailedOrder).Load();

        db.Set<Boat>().Add(new Boat() { Price = 120_000m, Length = 10f });
        db.Set<Plane>().Add(new Plane() { Price = 2_400_000m, MTOW = 10000 });
        db.SaveChanges();

        db.Set<Boat>().Load();
        db.Set<Plane>().Load();
        db.Set<Vehicle>().Load();
    }
}

class MyContext : DbContext
{
    [DbFunction()]
    public static int Foo(int i)
    {
        throw new InvalidOperationException();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(@"Server=.;Database=workshop;Trusted_Connection=true;ConnectRetryCount=0;TrustServerCertificate=true");
            optionsBuilder.LogTo(Console.WriteLine);
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OldConfig(modelBuilder);

        TypeInheritanceConfig(modelBuilder);
    }

    private static void TypeInheritanceConfig(ModelBuilder modelBuilder)
    {
        // table/entity inheritance
        //modelBuilder.Entity<Vehicle>().UseTphMappingStrategy();
        //modelBuilder.Entity<Boat>(b =>
        //{
        //    b.HasDiscriminator<string>("D").HasValue("bo");
        //    b.Property("D").HasMaxLength(2).IsFixedLength();
        //});
        //modelBuilder.Entity<Plane>(b =>
        //{
        //    b.HasDiscriminator<string>("D").HasValue("pl");
        //    b.Property("D").HasMaxLength(2).IsFixedLength();
        //});

        //modelBuilder.Entity<Vehicle>().UseTptMappingStrategy();
        //modelBuilder.Entity<Boat>(b =>
        //{
        //});
        //modelBuilder.Entity<Plane>(b =>
        //{
        //});

        modelBuilder.Entity<Vehicle>().UseTpcMappingStrategy();
        modelBuilder.Entity<Boat>(b => { });
        modelBuilder.Entity<Plane>(b => { });
    }

    private static void OldConfig(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OwnerConfiguration());
        modelBuilder.ApplyConfiguration(new DogConfiguration());
        modelBuilder.HasSequence("cdssdcds").IsCyclic()
            .IncrementsBy(7839);

        //modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Foo", b =>
        //{
        //    b.Property<int>("Id");
        //    b.Property<string>("Name");
        //    b.Property<int>("Age");
        //    b.Property<double>("Car");
        //});
        //modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Bar", b =>
        //{
        //    b.Property<int>("Id");
        //    b.Property<string>("Foo");
        //});

        modelBuilder.Entity<DetailedOrder>(dob =>
        {
            dob.ToTable("Orders");
            dob.Property(o => o.Status).HasColumnName("Status");
        });

        modelBuilder.Entity<Order>(ob =>
        {
            ob.ToTable("Orders");
            ob.Property(o => o.Status).HasColumnName("Status");
            ob.HasOne(o => o.DetailedOrder).WithOne()
                .HasForeignKey<DetailedOrder>(o => o.Id);
            ob.Navigation(o => o.DetailedOrder).IsRequired();
        });

        modelBuilder.Entity<Foo>()
            .HasNoKey()
            /*.ToView("MyView")*/;
    }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Dog> Dogs => Set<Dog>();
}

#region Foo

class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("People");
        builder.Property(x => x.LastName)
            .IsRequired()
            .IsFixedLength(false)
            .IsUnicode()
            .HasMaxLength(50)
            .HasColumnName("Surname")
            /*.HasDefaultValueSql()*/;
        builder.HasMany(x => x.Dogs)
            .WithOne(x => x.Owner)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.Id)/*.UseHiLo()*/;
        builder.OwnsOne(x => x.ShippingAddress);
        builder.OwnsOne(x => x.InvoicingAddress);
    }
}
class Owner
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Dog> Dogs { get; set; }
    public Address ShippingAddress { get; set; }
    public Address InvoicingAddress { get; set; }
}
//class Name
//{
//    public string FirstName { get; set; }
//    public string LastName { get; set; }
//}
class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

class DogConfiguration : IEntityTypeConfiguration<Dog>
{
    public void Configure(EntityTypeBuilder<Dog> builder)
    {
        builder.Property(x => x.DateOfBirth)
            .UsePropertyAccessMode(PropertyAccessMode.FieldDuringConstruction)
            .HasField("dob");
        builder.Property<DateTimeOffset>("LastUpdated");
        builder.HasQueryFilter(x => x.Active);
        builder.Property(x => x.Duration)
            .HasConversion(new DurationConverter()/*, comparer*/);
    }
}
class Dog
{
    private DateTimeOffset dob;

    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset DateOfBirth
    {
        get => dob;
        set
        {
            Validate(value);
            dob = value;

            static void Validate(DateTimeOffset dto) { }
        }
    }
    public Owner Owner { get; set; }
    public int OwnerId { get; set; }
    public bool Active { get; set; }
    public Duration Duration { get; set; }
}

class Duration
{
    private int _value;

    public Duration(int ms)
    {
        _value = ms;
    }

    public int Value => _value;
}
class DurationConverter : ValueConverter<Duration, int>
{
    public DurationConverter()
        : base(d => d.Value, x => new Duration(x), null)
    { }
}

public class Order
{
    public int Id { get; set; }
    public OrderStatus? Status { get; set; }
    public DetailedOrder DetailedOrder { get; set; }
}
public class DetailedOrder
{
    public int Id { get; set; }
    public OrderStatus? Status { get; set; }
    public string BillingAddress { get; set; }
    public string ShippingAddress { get; set; }
    public byte[] Version { get; set; }
}
public enum OrderStatus
{
    Pending,
    Shipped
}

class Foo
{
    public int Bar { get; set; }
    public string Baz { get; set; }
}

#endregion

abstract class Vehicle
{
    public int Id { get; set; }
    public decimal Price { get; set; }
}

class Boat : Vehicle
{
    public float Length { get; set; }
}

class Plane : Vehicle
{
    /// <summary>
    /// Maximum Take-Off Weight
    /// </summary>
    public int MTOW { get; set; }
}