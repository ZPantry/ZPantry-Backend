using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;

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

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientAlias> IngredientAliases { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<UserPantryItem> UserPantryItems { get; set; }

    public virtual DbSet<MealRecommendation> MealRecommendations { get; set; }

    public virtual DbSet<MealRecommendationItem> MealRecommendationItems { get; set; }

    public virtual DbSet<RecommendationFeedback> RecommendationFeedbacks { get; set; }

    public virtual DbSet<MediaAsset> MediaAssets { get; set; }

    public virtual DbSet<TodayMenuItem> TodayMenuItems { get; set; }

    public virtual DbSet<CookingLog> CookingLogs { get; set; }

    public virtual DbSet<PantryUsageLog> PantryUsageLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("authenticationconfig.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var strConn = config["ConnectionStrings:DefaultConnection"];
            if (!string.IsNullOrEmpty(strConn))
            {
                optionsBuilder.UseNpgsql(strConn, o =>
                {
                    o.UseVector();
                    o.MigrationsAssembly("ZPantry_Backend");
                }).UseSnakeCaseNamingConvention();
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("users");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.OtpCode).HasMaxLength(6);
            entity.Property(e => e.PasswordHashed).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("user");
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ingredients");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.NormalizedName).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.GradientFrom).HasMaxLength(32);
            entity.Property(e => e.GradientTo).HasMaxLength(32);
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)");
        });

        modelBuilder.Entity<IngredientAlias>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ingredient_aliases");
            entity.Property(e => e.AliasName).HasMaxLength(200);
            entity.Property(e => e.NormalizedAliasName).HasMaxLength(200);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("recipes");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.SourceType).HasMaxLength(100);
            entity.Property(e => e.GradientFrom).HasMaxLength(32);
            entity.Property(e => e.GradientTo).HasMaxLength(32);
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)");
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("recipe_ingredients");
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<UserPantryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("user_pantry_items");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.StorageLocation).HasMaxLength(100);
        });

        modelBuilder.Entity<MealRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("meal_recommendations");
            entity.Property(e => e.RecommendationType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(100);
        });

        modelBuilder.Entity<MealRecommendationItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("meal_recommendation_items");
            entity.Property(e => e.MissingIngredientNames).HasMaxLength(2000);
            entity.Property(e => e.Reason).HasMaxLength(2000);
        });

        modelBuilder.Entity<RecommendationFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("recommendation_feedbacks");
            entity.Property(e => e.FeedbackType).HasMaxLength(100);
            entity.Property(e => e.Comment).HasMaxLength(2000);
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("media_assets");
            entity.Property(e => e.PublicId).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.SecureUrl).HasMaxLength(500);
            entity.Property(e => e.ResourceType).HasMaxLength(50);
            entity.Property(e => e.Format).HasMaxLength(50);
        });

        modelBuilder.Entity<TodayMenuItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("today_menu_items");
            entity.Property(e => e.MealName).HasMaxLength(200);
            entity.Property(e => e.MealType).HasMaxLength(100);
            entity.Property(e => e.PlannedDate).HasColumnType("date");
            entity.Property(e => e.Note).HasColumnType("text");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagePublicId).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(TodayMenuStatus.Planned);
        });

        modelBuilder.Entity<CookingLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cooking_logs");
            entity.Property(e => e.MealName).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagePublicId).HasMaxLength(200);
            entity.Property(e => e.Note).HasColumnType("text");
        });

        modelBuilder.Entity<PantryUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("pantry_usage_logs");
            entity.Property(e => e.IngredientName).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.QuantityUsed).HasColumnType("numeric(18,4)");
            entity.Property(e => e.Warning).HasColumnType("text");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
