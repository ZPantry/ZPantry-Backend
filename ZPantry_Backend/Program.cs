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

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("Jwt:SecretKey is missing in authenticationconfig.json.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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
    await dbContext.Database.EnsureCreatedAsync();
}

await EnsureBootstrapAdminAsync(app.Services, builder.Configuration);

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
        PasswordHashed = new Microsoft.AspNet.Identity.PasswordHasher().HashPassword(password),
        IsEmailConfirmed = true,
        IsActive = true,
        Role = "admin",
        UpdatedAt = DateTime.UtcNow
    };

    await userRepository.AddUser(admin);
}
