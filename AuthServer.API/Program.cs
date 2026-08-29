using System.Text;
using AuthServer.API.Data;
using AuthServer.API.Models;
using AuthServer.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<GoogleOAuthService>();
builder.Services.AddScoped<GitHubOAuthService>();

builder.Services.AddScoped<IOAuthService>(sp => sp.GetRequiredService<GoogleOAuthService>());
builder.Services.AddScoped<IOAuthService>(sp => sp.GetRequiredService<GitHubOAuthService>());

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
})
.AddGoogle("google", googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    googleOptions.CallbackPath = "/signin-google";
    googleOptions.CorrelationCookie.SameSite = SameSiteMode.Lax;
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddGitHub("github", githubOptions =>
{
    githubOptions.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
    githubOptions.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
    githubOptions.SignInScheme = IdentityConstants.ExternalScheme;
    githubOptions.CallbackPath = "/signin-github";
    githubOptions.Scope.Add("user:email");
    githubOptions.CorrelationCookie.SameSite = SameSiteMode.Lax;
    githubOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();