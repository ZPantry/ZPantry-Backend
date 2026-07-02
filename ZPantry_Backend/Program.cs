using AuthenticationModule.Controllers;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Implementations;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Implementations;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZPantryModule.Controllers;
using ZPantryModule.Services.Implementations;
using ZPantryModule.Services.Interfaces;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "authenticationconfig.json"),
    optional: false,
    reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Gmail"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<ZpantryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IUserPantryService, PantryService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICloudinaryStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddHttpClient<IAIRecommendationClient, AIRecommendationClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var aiServiceUrl = configuration["AI_SERVICE_URL"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(aiServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("Jwt:SecretKey is missing in authenticationconfig.json.");
}

if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must be at least 32 bytes for HS256.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSettings.Issuer),
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtSettings.Audience),
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst("jti")?.Value;

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing token id.");
                    return;
                }

                var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                if (await blacklistService.IsRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(IngredientsController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ZPantry API",
        Version = "v1"
    });
    c.DocInclusionPredicate((_, _) => true);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ZpantryDbContext>();
    await ApplyDatabaseSchemaModeAsync(dbContext, builder.Configuration);
    await EnsureGradientColumnsAsync(dbContext);
}

await EnsureDemoAccountsAsync(app.Services, builder.Configuration);
await EnsureTestFoodDataAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZPantry API");
});

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS")))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task EnsureDemoAccountsAsync(IServiceProvider services, IConfiguration configuration)
{
    var demoAccounts = configuration.GetSection("DemoAccounts").GetChildren();

    foreach (var accountSection in demoAccounts)
    {
        var email = accountSection["Email"];
        var password = accountSection["Password"];
        var role = accountSection["Role"] ?? "user";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            continue;
        }

        using var scope = services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var existingUser = await userRepository.GetUserByEmail(email);
        if (existingUser != null)
        {
            continue;
        }

        var user = new User
        {
            FullName = accountSection["FullName"] ?? role,
            Email = email,
            PasswordHashed = new Microsoft.AspNet.Identity.PasswordHasher().HashPassword(password),
            IsEmailConfirmed = true,
            IsActive = true,
            Role = role.Trim().ToLowerInvariant(),
            UpdatedAt = DateTime.UtcNow
        };

        await userRepository.AddUser(user);
    }
}

static async Task ApplyDatabaseSchemaModeAsync(ZpantryDbContext dbContext, IConfiguration configuration)
{
    var schemaMode = (configuration["Database:SchemaMode"]
        ?? configuration["Database__SchemaMode"]
        ?? "update").Trim().ToLowerInvariant();

    switch (schemaMode)
    {
        case "update":
            var migrations = dbContext.Database.GetMigrations();
            if (migrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            break;

        case "create-drop":
        case "createdrop":
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            break;

        default:
            throw new InvalidOperationException(
                $"Unsupported Database__SchemaMode '{schemaMode}'. Allowed values: update, create-drop.");
    }
}

static async Task EnsureGradientColumnsAsync(ZpantryDbContext dbContext)
{
    var commands = new[]
    {
        @"ALTER TABLE IF EXISTS ""ingredients"" ADD COLUMN IF NOT EXISTS ""GradientFrom"" character varying(32);",
        @"ALTER TABLE IF EXISTS ""ingredients"" ADD COLUMN IF NOT EXISTS ""GradientTo"" character varying(32);",
        @"ALTER TABLE IF EXISTS ""recipes"" ADD COLUMN IF NOT EXISTS ""GradientFrom"" character varying(32);",
        @"ALTER TABLE IF EXISTS ""recipes"" ADD COLUMN IF NOT EXISTS ""GradientTo"" character varying(32);"
    };

    foreach (var command in commands)
    {
        await dbContext.Database.ExecuteSqlRawAsync(command);
    }
}

static async Task EnsureTestFoodDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ZpantryDbContext>();

    var seedIngredients = new[]
    {
        new IngredientSeed("Trứng gà", "trứng gà", "Protein", "piece", 70m, 6m, 5m, 1m),
        new IngredientSeed("Cà chua", "cà chua", "Vegetable", "g", 0.18m, 0.009m, 0.002m, 0.039m),
        new IngredientSeed("Thịt bò", "thịt bò", "Protein", "g", 2.5m, 0.26m, 0.15m, 0m),
        new IngredientSeed("Thịt gà", "thịt gà", "Protein", "g", 1.65m, 0.31m, 0.036m, 0m),
        new IngredientSeed("Thịt heo", "thịt heo", "Protein", "g", 2.42m, 0.27m, 0.14m, 0m),
        new IngredientSeed("Gạo", "gạo", "Grain", "g", 1.3m, 0.027m, 0.003m, 0.28m),
        new IngredientSeed("Đậu hũ", "đậu hũ", "Protein", "g", 0.76m, 0.08m, 0.048m, 0.019m),
        new IngredientSeed("Cà rốt", "cà rốt", "Vegetable", "g", 0.41m, 0.009m, 0.002m, 0.1m),
        new IngredientSeed("Hành lá", "hành lá", "Vegetable", "g", 0.32m, 0.018m, 0.002m, 0.073m),
        new IngredientSeed("Hành tím", "hành tím", "Vegetable", "g", 0.4m, 0.011m, 0.001m, 0.093m),
        new IngredientSeed("Tỏi", "tỏi", "Spice", "g", 1.49m, 0.064m, 0.005m, 0.33m),
        new IngredientSeed("Nước mắm", "nước mắm", "Condiment", "ml", 0.35m, 0.06m, 0m, 0.03m),
        new IngredientSeed("Dầu ăn", "dầu ăn", "Condiment", "ml", 8.84m, 0m, 1m, 0m)
    };

    var existingIngredientNames = await dbContext.Ingredients
        .Where(ingredient => !ingredient.IsDeleted)
        .Select(ingredient => ingredient.NormalizedName)
        .ToListAsync();

    var existingIngredientSet = existingIngredientNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var ingredientsToAdd = seedIngredients
        .Where(seed => !existingIngredientSet.Contains(seed.NormalizedName))
        .Select(seed => new Ingredient
        {
            Name = seed.Name,
            NormalizedName = seed.NormalizedName,
            Category = seed.Category,
            Unit = seed.Unit,
            CaloriesPerUnit = seed.CaloriesPerUnit,
            ProteinPerUnit = seed.ProteinPerUnit,
            FatPerUnit = seed.FatPerUnit,
            CarbPerUnit = seed.CarbPerUnit
        })
        .ToList();

    if (ingredientsToAdd.Count > 0)
    {
        dbContext.Ingredients.AddRange(ingredientsToAdd);
        await dbContext.SaveChangesAsync();
    }

    var ingredientIds = await dbContext.Ingredients
        .Where(ingredient => !ingredient.IsDeleted)
        .ToDictionaryAsync(ingredient => ingredient.NormalizedName, ingredient => ingredient.Id);

    var seedRecipes = new[]
    {
        new RecipeSeed(
            "Trứng xào cà chua",
            "Món nhanh với trứng và cà chua, phù hợp bữa sáng hoặc bữa tối nhẹ.",
            15,
            "easy",
            2,
            "Đánh trứng. Xào cà chua với hành tím, thêm trứng, nêm nước mắm và hành lá.",
            new[]
            {
                new RecipeIngredientSeed("trứng gà", 2m, "piece", true),
                new RecipeIngredientSeed("cà chua", 200m, "g", true),
                new RecipeIngredientSeed("hành tím", 10m, "g", false),
                new RecipeIngredientSeed("hành lá", 10m, "g", false),
                new RecipeIngredientSeed("nước mắm", 10m, "ml", false)
            }),
        new RecipeSeed(
            "Cơm rang thịt bò",
            "Cơm rang giàu đạm với thịt bò, cà rốt và trứng.",
            25,
            "medium",
            2,
            "Xào bò với tỏi. Thêm cơm, trứng, cà rốt, nêm nước mắm rồi đảo lửa lớn.",
            new[]
            {
                new RecipeIngredientSeed("gạo", 250m, "g", true),
                new RecipeIngredientSeed("thịt bò", 200m, "g", true),
                new RecipeIngredientSeed("trứng gà", 1m, "piece", false),
                new RecipeIngredientSeed("cà rốt", 80m, "g", false),
                new RecipeIngredientSeed("tỏi", 5m, "g", false)
            }),
        new RecipeSeed(
            "Cháo gà",
            "Món mềm, dễ ăn, dùng tốt khi cần bữa nhẹ.",
            45,
            "easy",
            3,
            "Nấu gạo với nhiều nước. Luộc gà, xé nhỏ, cho vào cháo và nêm vừa ăn.",
            new[]
            {
                new RecipeIngredientSeed("gạo", 150m, "g", true),
                new RecipeIngredientSeed("thịt gà", 250m, "g", true),
                new RecipeIngredientSeed("hành lá", 10m, "g", false),
                new RecipeIngredientSeed("nước mắm", 10m, "ml", false)
            }),
        new RecipeSeed(
            "Đậu hũ sốt cà chua",
            "Món chay đơn giản với đậu hũ và sốt cà chua.",
            20,
            "easy",
            2,
            "Áp chảo đậu hũ. Nấu sốt cà chua với hành tím, cho đậu hũ vào rim thấm.",
            new[]
            {
                new RecipeIngredientSeed("đậu hũ", 300m, "g", true),
                new RecipeIngredientSeed("cà chua", 250m, "g", true),
                new RecipeIngredientSeed("hành tím", 10m, "g", false),
                new RecipeIngredientSeed("hành lá", 10m, "g", false)
            }),
        new RecipeSeed(
            "Canh thịt heo cà rốt",
            "Canh gia đình cơ bản, dễ nấu với thịt heo và cà rốt.",
            30,
            "easy",
            3,
            "Xào sơ thịt heo với hành tím. Thêm nước và cà rốt, nấu mềm rồi nêm nước mắm.",
            new[]
            {
                new RecipeIngredientSeed("thịt heo", 200m, "g", true),
                new RecipeIngredientSeed("cà rốt", 150m, "g", true),
                new RecipeIngredientSeed("hành tím", 10m, "g", false),
                new RecipeIngredientSeed("nước mắm", 10m, "ml", false)
            })
    };

    var existingRecipeNames = await dbContext.Recipes
        .Where(recipe => !recipe.IsDeleted)
        .Select(recipe => recipe.Name)
        .ToListAsync();

    var existingRecipeSet = existingRecipeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var seed in seedRecipes.Where(seed => !existingRecipeSet.Contains(seed.Name)))
    {
        var recipe = new Recipe
        {
            Name = seed.Name,
            Description = seed.Description,
            CookingTimeMinutes = seed.CookingTimeMinutes,
            Difficulty = seed.Difficulty,
            ServingSize = seed.ServingSize,
            InstructionText = seed.InstructionText,
            SourceType = "seed"
        };

        dbContext.Recipes.Add(recipe);

        foreach (var ingredient in seed.Ingredients)
        {
            if (!ingredientIds.TryGetValue(ingredient.NormalizedName, out var ingredientId))
            {
                continue;
            }

            dbContext.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                IngredientId = ingredientId,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit,
                IsRequired = ingredient.IsRequired
            });
        }
    }

    await dbContext.SaveChangesAsync();
}

internal sealed record IngredientSeed(
    string Name,
    string NormalizedName,
    string Category,
    string Unit,
    decimal CaloriesPerUnit,
    decimal ProteinPerUnit,
    decimal FatPerUnit,
    decimal CarbPerUnit);

internal sealed record RecipeSeed(
    string Name,
    string Description,
    int CookingTimeMinutes,
    string Difficulty,
    int ServingSize,
    string InstructionText,
    IReadOnlyCollection<RecipeIngredientSeed> Ingredients);

internal sealed record RecipeIngredientSeed(
    string NormalizedName,
    decimal Quantity,
    string Unit,
    bool IsRequired);
