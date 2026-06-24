using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace CuisineModule.Repositories.Entities;

public partial class ZpantryDbContext : DbContext
{
    public ZpantryDbContext()
    {
    }

    public ZpantryDbContext(DbContextOptions<ZpantryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(GetConnectionString());
        }
    }

    private string GetConnectionString() 

 { 

IConfiguration config = new ConfigurationBuilder() 

 	.SetBasePath(AppContext.BaseDirectory) 

            .AddJsonFile("cuisineconfig.json",true,true) 

            .Build(); 

var strConn = config["ConnectionStrings:DefaultConnection"]; 

 
return strConn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."); 

} 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PK__Ingredie__BEAEB25A2FAEC38A");

            entity.ToTable("ingredients");

            entity.Property(e => e.IngredientId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IngredientName).HasMaxLength(150);
            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Reservation).HasMaxLength(250);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ingredients_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__users__3214EC07530E79D6");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__A9D105342A37A83B").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.OtpCode)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHashed)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(128);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("user", "DF_users_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
