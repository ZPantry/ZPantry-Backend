using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace AuthenticationModule.Repositories.Entities;

public partial class ZpantryDbContext : DbContext
{
    public ZpantryDbContext()
    {
    }

    public ZpantryDbContext(DbContextOptions<ZpantryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfiguration config = new ConfigurationBuilder()
                 .SetBasePath(AppContext.BaseDirectory)
                 .AddJsonFile("authenticationconfig.json", true, true)
                 .Build();
            var strConn = config["ConnectionStrings:DefaultConnection"];
            if (!string.IsNullOrEmpty(strConn))
            {
                optionsBuilder.UseSqlServer(strConn);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("user");
            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(128)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
