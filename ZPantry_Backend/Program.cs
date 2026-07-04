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
using Npgsql;
using Pgvector.EntityFrameworkCore;
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o =>
    {
        o.UseVector();
        o.MigrationsAssembly("ZPantry_Backend");
    }));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IUserPantryService, PantryService>();
builder.Services.AddScoped<ITodayMenuService, TodayMenuService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICloudinaryStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IEmbeddingBackfillService, EmbeddingBackfillService>();
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
    await ApplyDatabaseSchemaAsync(dbContext, builder.Configuration);
}

await RunEmbeddingBackfillAsync(app.Services, builder.Configuration);
await EnsureDemoAccountsAsync(app.Services, builder.Configuration);

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
    var enabled = bool.TryParse(configuration["BootstrapDemoAccountsEnabled"], out var bootstrapEnabled)
        && bootstrapEnabled;
    if (!enabled)
    {
        return;
    }

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

static async Task ApplyDatabaseSchemaAsync(ZpantryDbContext dbContext, IConfiguration configuration)
{
    await EnsureTargetDatabaseExistsAsync(configuration);
    await EnsureLegacyMigrationsHistoryCompatibilityAsync(dbContext);

    var schemaMode = (configuration["Database:SchemaMode"]
        ?? configuration["Database__SchemaMode"]
        ?? "update")
        .Trim()
        .ToLowerInvariant();

    switch (schemaMode)
    {
        case "update":
        case "migrate":
            await dbContext.Database.MigrateAsync();
            return;
        case "create":
        case "create-drop":
        case "createdrop":
        case "drop-create":
        case "dropcreate":
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
            return;
        default:
            throw new InvalidOperationException(
                $"Unsupported Database:SchemaMode value '{schemaMode}'. Use 'update' or 'create-drop'.");
    }
}

static async Task EnsureLegacyMigrationsHistoryCompatibilityAsync(ZpantryDbContext dbContext)
{
    var hasLegacyMigrationIdColumn = await ColumnExistsAsync(dbContext, "__EFMigrationsHistory", "migration_id");

    if (hasLegacyMigrationIdColumn)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "__EFMigrationsHistory"
                RENAME COLUMN migration_id TO "MigrationId";
            """);
    }

    var hasLegacyProductVersionColumn = await ColumnExistsAsync(dbContext, "__EFMigrationsHistory", "product_version");

    if (hasLegacyProductVersionColumn)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "__EFMigrationsHistory"
                RENAME COLUMN product_version TO "ProductVersion";
            """);
    }
}

static async Task<bool> ColumnExistsAsync(ZpantryDbContext dbContext, string tableName, string columnName)
{
    var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
    var wasClosed = connection.State != System.Data.ConnectionState.Open;

    if (wasClosed)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = current_schema()
              AND table_name = @table_name
              AND column_name = @column_name
            LIMIT 1
            """,
            connection);

        command.Parameters.AddWithValue("table_name", tableName);
        command.Parameters.AddWithValue("column_name", columnName);

        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }
    finally
    {
        if (wasClosed)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureTargetDatabaseExistsAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
    }

    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = builder.Database;
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection does not specify a database name.");
    }

    var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };

    await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
    await connection.OpenAsync();

    await using var checkCommand = new NpgsqlCommand(
        "SELECT 1 FROM pg_database WHERE datname = @db_name",
        connection);
    checkCommand.Parameters.AddWithValue("db_name", databaseName);

    var exists = await checkCommand.ExecuteScalarAsync();
    if (exists is not null)
    {
        return;
    }

    await using var createCommand = new NpgsqlCommand(
        $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\";",
        connection);
    await createCommand.ExecuteNonQueryAsync();
}

static async Task RunEmbeddingBackfillAsync(IServiceProvider services, IConfiguration configuration)
{
    var enabled = bool.TryParse(configuration["BootstrapReembedEmbeddingsEnabled"], out var shouldRun)
        && shouldRun;
    if (!enabled)
    {
        return;
    }

    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EmbeddingBackfill");
    var backfillService = scope.ServiceProvider.GetRequiredService<IEmbeddingBackfillService>();

    logger.LogInformation("Bootstrap embedding backfill is enabled.");
    var result = await backfillService.ReembedExistingDataAsync();
    logger.LogInformation(
        "Bootstrap embedding backfill completed. Ingredients updated: {IngredientsUpdated}, recipes updated: {RecipesUpdated}, failed: {FailedCount}.",
        result.IngredientsUpdated,
        result.RecipesUpdated,
        result.FailedCount);

    if (result.FailedCount > 0)
    {
        throw new InvalidOperationException(
            $"Embedding backfill completed with {result.FailedCount} failures. Check AI service and rerun with BootstrapReembedEmbeddingsEnabled=true.");
    }
}
