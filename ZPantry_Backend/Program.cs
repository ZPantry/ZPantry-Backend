using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Implementations;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Implementations;
using AuthenticationModule.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Äá»c cáº¥u hÃ¬nh riÃªng cá»§a AuthenticationModule (Ä‘Ã£ Ä‘Æ°á»£c copy vÃ o thÆ° má»¥c build)
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "authenticationconfig.json"), optional: false, reloadOnChange: true);

// ÄÄƒng kÃ½ DbContext
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Gmail"));

// Đăng ký DbContext
builder.Services.AddDbContext<ZpantryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ÄÄƒng kÃ½ cÃ¡c Services vÃ  Repositories cá»§a AuthenticationModule
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add services to the container.
// ThÃªm .AddApplicationPart() Ä‘á»ƒ Ä‘áº£m báº£o API Controllers trong AuthenticationModule Ä‘Æ°á»£c nháº­n diá»‡n
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthenticationModule.Controllers.AuthController).Assembly);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
