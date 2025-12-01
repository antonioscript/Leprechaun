using Leprechaun.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Context;

public class LeprechaunDbContext : DbContext
{
    public LeprechaunDbContext(DbContextOptions<LeprechaunDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("person");

            entity.HasKey(p => p.Id);

            // Cockroach/Postgres usa identity, mas EF já entende pelo convention.
            entity.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.IsActive)
                .HasColumnName("isactive")
                .IsRequired()
                .HasDefaultValue(true);
        });
    }
}