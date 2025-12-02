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
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinanceTransaction> FinanceTransactions => Set<FinanceTransaction>();
    
    public DbSet<ChatState> ChatStates { get; set; }

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
        
        
        // Institution
    modelBuilder.Entity<Institution>(entity =>
    {
        entity.ToTable("institution");

        entity.HasKey(i => i.Id);

        entity.Property(i => i.Id).HasColumnName("id");
        entity.Property(i => i.Name).HasColumnName("name");
        entity.Property(i => i.Type).HasColumnName("type");
        entity.Property(i => i.PersonId).HasColumnName("personid");
        entity.Property(i => i.Description).HasColumnName("description");
        entity.Property(i => i.StartDate).HasColumnName("startdate").HasColumnType("date");
        entity.Property(i => i.EndDate).HasColumnName("enddate").HasColumnType("date");
        entity.Property(i => i.IsActive).HasColumnName("isactive");

        entity.HasOne(i => i.Person)
            .WithMany()
            .HasForeignKey(i => i.PersonId);
    });

    // CostCenter
    modelBuilder.Entity<CostCenter>(entity =>
    {
        entity.ToTable("costcenter");

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.Name).HasColumnName("name");
        entity.Property(c => c.PersonId).HasColumnName("personid");
        entity.Property(c => c.Description).HasColumnName("description");
        entity.Property(c => c.IsActive).HasColumnName("isactive");

        entity.HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId);
    });

    // Category
    modelBuilder.Entity<Category>(entity =>
    {
        entity.ToTable("category");

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.Name).HasColumnName("name");
        entity.Property(c => c.Description).HasColumnName("description");
        entity.Property(c => c.IsActive).HasColumnName("isactive");
    });

    // FinanceTransaction
    modelBuilder.Entity<FinanceTransaction>(entity =>
    {
        entity.ToTable("financetransaction");

        entity.HasKey(f => f.Id);

        entity.Property(f => f.Id).HasColumnName("id");
        entity.Property(f => f.TransactionDate).HasColumnName("transactiondate");
        entity.Property(f => f.Amount).HasColumnName("amount");
        entity.Property(f => f.TransactionType).HasColumnName("transactiontype");

        entity.Property(f => f.SourceCostCenterId).HasColumnName("sourcecostcenterid");
        entity.Property(f => f.TargetCostCenterId).HasColumnName("targetcostcenterid");
        entity.Property(f => f.InstitutionId).HasColumnName("institutionid");
        entity.Property(f => f.PersonId).HasColumnName("personid");
        entity.Property(f => f.CategoryId).HasColumnName("categoryid");
        entity.Property(f => f.Description).HasColumnName("description");

        entity.HasOne(f => f.SourceCostCenter)
            .WithMany()
            .HasForeignKey(f => f.SourceCostCenterId);

        entity.HasOne(f => f.TargetCostCenter)
            .WithMany()
            .HasForeignKey(f => f.TargetCostCenterId);

        entity.HasOne(f => f.Institution)
            .WithMany()
            .HasForeignKey(f => f.InstitutionId);

        entity.HasOne(f => f.Person)
            .WithMany()
            .HasForeignKey(f => f.PersonId);

        entity.HasOne(f => f.Category)
            .WithMany()
            .HasForeignKey(f => f.CategoryId);
    });
    
    modelBuilder.Entity<ChatState>(entity =>
    {
        entity.ToTable("chatstate");

        entity.HasKey(c => c.ChatId);

        entity.Property(c => c.ChatId).HasColumnName("chatid");
        entity.Property(c => c.State).HasColumnName("state");
        entity.Property(c => c.TempInstitutionId).HasColumnName("tempinstitutionid");
        entity.Property(c => c.TempAmount).HasColumnName("tempamount");
        entity.Property(c => c.UpdatedAt).HasColumnName("updatedat");
    });
    
    }
    
    
}