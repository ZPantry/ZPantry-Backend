using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Implementations;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Implementations;
using AuthenticationModule.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ä Äá» c cáº¥u hÃ¬nh riÃªng cá»§a AuthenticationModule (Ä‘Ã£ Ä‘Æ°á»£c copy vÃ o thÆ° má»¥c build)
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "authenticationconfig.json"), optional: false, reloadOnChange: true);

// Ä Äƒng kÃ½ DbContext
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Gmail"));

// Đăng ký DbContext
builder.Services.AddDbContext<ZpantryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Ä Äƒng kÃ½ cÃ¡c Services vÃ  Repositories cá»§a AuthenticationModule
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add services to the container.
// Thêm .AddApplicationPart() để đảm bảo API Controllers trong AuthenticationModule được nhận diện
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthenticationModule.Controllers.AuthController).Assembly);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ZPantry API", Version = "v1" });
    c.SwaggerDoc("authentication", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Authentication Module API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZPantry API v1");
        c.SwaggerEndpoint("/swagger/authentication/swagger.json", "Authentication Module API");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
