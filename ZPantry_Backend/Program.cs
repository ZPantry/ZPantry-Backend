using AuthenticationModule.Controllers;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Implementations;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Implementations;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load config
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "authenticationconfig.json"),
    optional: false,
    reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Options
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Gmail"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// DbContext
builder.Services.AddDbContext<ZpantryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

// Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

// JWT
var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "Jwt:SecretKey is missing in authenticationconfig.json.");
}

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer =
                    !string.IsNullOrWhiteSpace(jwtSettings.Issuer),

                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience =
                    !string.IsNullOrWhiteSpace(jwtSettings.Audience),

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
                var jti = context.Principal?
                    .FindFirst("jti")?
                    .Value;

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing token id.");
                    return;
                }

                var blacklistService =
                    context.HttpContext.RequestServices
                        .GetRequiredService<ITokenBlacklistService>();

                if (await blacklistService.IsRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly);

builder.Services.AddEndpointsApiExplorer();

// CORS Configuration
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

    c.SwaggerDoc("authentication", new OpenApiInfo
    {
        Title = "Authentication Module API",
        Version = "v1"
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "authentication")
        {
            return apiDesc.GroupName == "authentication";
        }

        if (docName == "v1")
        {
            return string.IsNullOrEmpty(apiDesc.GroupName);
        }

        return false;
    });

    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Token"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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

await EnsureDatabaseReadyAsync(
    builder.Configuration.GetConnectionString("DefaultConnection"));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ZpantryDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await EnsureUserSchemaAsync(dbContext);
}

await EnsureBootstrapAdminAsync(app.Services, builder.Configuration);

// Swagger
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        "/swagger/authentication/swagger.json",
        "Authentication Module API");

    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "ZPantry API");
});

// HTTPS
var httpsPorts =
    Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS");

if (!string.IsNullOrWhiteSpace(httpsPorts))
{
    app.UseHttpsRedirection();
}

// Middleware
app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Debug endpoint
app.MapGet("/", () => "API is running");

app.Run();

static async Task EnsureDatabaseReadyAsync(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("DefaultConnection is missing.");
    }

    var builder = new SqlConnectionStringBuilder(connectionString);
    var databaseName = builder.InitialCatalog;

    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("Database name is missing in DefaultConnection.");
    }

    builder.InitialCatalog = "master";

    await using var connection = new SqlConnection(builder.ConnectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = $@"
IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL
BEGIN
    CREATE DATABASE [{databaseName.Replace("]", "]]")}];
END";

    await command.ExecuteNonQueryAsync();
}

static async Task EnsureUserSchemaAsync(ZpantryDbContext dbContext)
{
    var sqlStatements = new[]
    {
        """
        IF COL_LENGTH('users', 'Role') IS NULL
            ALTER TABLE [users] ADD [Role] nvarchar(50) NOT NULL CONSTRAINT [DF_users_Role] DEFAULT('user');
        """,
        """
        IF COL_LENGTH('users', 'RefreshTokenHash') IS NULL
            ALTER TABLE [users] ADD [RefreshTokenHash] nvarchar(128) NULL;
        """,
        """
        IF COL_LENGTH('users', 'RefreshTokenExpiresAt') IS NULL
            ALTER TABLE [users] ADD [RefreshTokenExpiresAt] datetime2 NULL;
        """
    };

    foreach (var sql in sqlStatements)
    {
        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}

static async Task EnsureBootstrapAdminAsync(IServiceProvider services, IConfiguration configuration)
{
    var adminSection = configuration.GetSection("BootstrapAdmin");
    var email = adminSection["Email"];
    var password = adminSection["Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    using var scope = services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var existingAdmin = await userRepository.GetUserByEmail(email);
    if (existingAdmin != null)
    {
        if (!string.Equals(existingAdmin.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            existingAdmin.Role = "admin";
            existingAdmin.IsEmailConfirmed = true;
            existingAdmin.IsActive = true;
            existingAdmin.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateUser(existingAdmin);
        }

        return;
    }

    var admin = new User
    {
        FullName = adminSection["FullName"] ?? "Admin",
        Email = email,
        PasswordHashed = new PasswordHasher().HashPassword(password),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsEmailConfirmed = true,
        IsActive = true,
        Role = "admin"
    };

    await userRepository.AddUser(admin);
}
