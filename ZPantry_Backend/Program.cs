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
builder.Services.AddScoped<ITodayMenuService, TodayMenuService>();
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
    await EnsureTodayMenuSchemaAsync(dbContext);
}

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

        default:
            throw new InvalidOperationException(
                $"Unsupported Database__SchemaMode '{schemaMode}'. Allowed value: update.");
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

static async Task EnsureTodayMenuSchemaAsync(ZpantryDbContext dbContext)
{
    var commands = new[]
    {
        @"CREATE TABLE IF NOT EXISTS today_menu_items (
            id uuid PRIMARY KEY,
            created_at timestamptz NOT NULL,
            created_by uuid NULL,
            updated_at timestamptz NULL,
            updated_by uuid NULL,
            deleted_at timestamptz NULL,
            deleted_by uuid NULL,
            is_deleted boolean NOT NULL DEFAULT false,
            user_id uuid NOT NULL,
            meal_id uuid NULL,
            recipe_id uuid NULL,
            meal_name varchar(200) NOT NULL,
            meal_type varchar(100) NULL,
            serving_size integer NULL,
            planned_date date NOT NULL,
            status varchar(20) NOT NULL DEFAULT 'Planned',
            note text NULL,
            cooked_at timestamptz NULL,
            image_url varchar(500) NULL,
            image_public_id varchar(200) NULL
        );",
        @"CREATE INDEX IF NOT EXISTS ix_today_menu_items_user_planned_date
            ON today_menu_items (user_id, planned_date);",
        @"CREATE INDEX IF NOT EXISTS ix_today_menu_items_recipe_id
            ON today_menu_items (recipe_id);",
        @"CREATE TABLE IF NOT EXISTS cooking_logs (
            id uuid PRIMARY KEY,
            created_at timestamptz NOT NULL,
            created_by uuid NULL,
            updated_at timestamptz NULL,
            updated_by uuid NULL,
            deleted_at timestamptz NULL,
            deleted_by uuid NULL,
            is_deleted boolean NOT NULL DEFAULT false,
            user_id uuid NOT NULL,
            today_menu_item_id uuid NOT NULL,
            meal_id uuid NULL,
            recipe_id uuid NULL,
            meal_name varchar(200) NOT NULL,
            image_url varchar(500) NULL,
            image_public_id varchar(200) NULL,
            cooked_at timestamptz NOT NULL,
            rating integer NULL,
            note text NULL
        );",
        @"CREATE INDEX IF NOT EXISTS ix_cooking_logs_user_cooked_at
            ON cooking_logs (user_id, cooked_at DESC);",
        @"CREATE INDEX IF NOT EXISTS ix_cooking_logs_today_menu_item_id
            ON cooking_logs (today_menu_item_id);",
        @"CREATE TABLE IF NOT EXISTS pantry_usage_logs (
            id uuid PRIMARY KEY,
            created_at timestamptz NOT NULL,
            created_by uuid NULL,
            updated_at timestamptz NULL,
            updated_by uuid NULL,
            deleted_at timestamptz NULL,
            deleted_by uuid NULL,
            is_deleted boolean NOT NULL DEFAULT false,
            user_id uuid NOT NULL,
            today_menu_item_id uuid NOT NULL,
            cooking_log_id uuid NOT NULL,
            ingredient_id uuid NOT NULL,
            ingredient_name varchar(200) NOT NULL,
            quantity_used numeric(18, 4) NULL,
            unit varchar(50) NULL,
            action_type varchar(50) NOT NULL DEFAULT 'consumed',
            warning text NULL
        );",
        @"CREATE INDEX IF NOT EXISTS ix_pantry_usage_logs_cooking_log_id
            ON pantry_usage_logs (cooking_log_id);",
        @"CREATE INDEX IF NOT EXISTS ix_pantry_usage_logs_today_menu_item_id
            ON pantry_usage_logs (today_menu_item_id);"
    };

    foreach (var command in commands)
    {
        await dbContext.Database.ExecuteSqlRawAsync(command);
    }
}


