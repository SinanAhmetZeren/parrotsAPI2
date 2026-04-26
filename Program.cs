

global using ParrotsAPI2.Models;
global using ParrotsAPI2.Dtos.User;
global using ParrotsAPI2.Dtos.VehicleDtos;
global using ParrotsAPI2.Dtos.VoyageDtos;
global using AutoMapper;
global using ParrotsAPI2.Data;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.JsonPatch;
global using ParrotsAPI2.Services.User;
global using ParrotsAPI2.Services.Vehicle;
global using ParrotsAPI2.Services.Voyage;
global using ParrotsAPI2.Services.Waypoint;
global using ParrotsAPI2.Services.Message;
global using ParrotsAPI2.Hubs;
global using Microsoft.AspNetCore.Identity;
global using ParrotsAPI2.Services.Token;
global using ParrotsAPI2.Services.Bid;
global using ParrotsAPI2.Services.Cleanup;
global using ParrotsAPI2.Services.Bookmark;
global using ParrotsAPI2.Services.Blob;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;


using Newtonsoft.Json;
using ParrotsAPI2.Services.EmailSender;
using ParrotsAPI2.Services.Alert;
using ParrotsAPI2.Helpers;
using Serilog;
// using ParrotsAPI2.Migrations;

DotNetEnv.Env.TraversePath().Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    var alertSink = new AlertEmailSink(
        smtpUser: Environment.GetEnvironmentVariable("Email__SmtpUser") ?? "",
        smtpPass: Environment.GetEnvironmentVariable("Email__SmtpPass") ?? "",
        adminEmail: Environment.GetEnvironmentVariable("Email__AdminAddress") ?? ""
    );
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Sink(alertSink)
        .CreateLogger();
}

builder.Host.UseSerilog();

// Bind Google settings from appsettings.json
builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection("Google")
);

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));


// Controllers + NewtonsoftJson
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Parrots API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] { }
        }
    });
});

// Services
builder.Services.AddSignalR();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVoyageService, VoyageService>();
builder.Services.AddScoped<IWaypointService, WaypointService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<ConversationPageTracker>();
builder.Services.AddHostedService<VehicleVoyageCleanupService>();
builder.Services.AddScoped<IBlobService, BlobService>();

builder.Services.AddMemoryCache();



// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
.AddEntityFrameworkStores<DataContext>()
.AddDefaultTokenProviders();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"] ?? throw new InvalidOperationException("TokenKey not configured"))),
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
});

//  Map your JSON to your CUSTOM class for use in your Controller
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("Google"));

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<DeviceRateLimitMiddleware>();


if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();


    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed");
        // do NOT crash
    }

    try
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        AppUser admin = null;

        if (!await userManager.Users.AnyAsync())
        {
            admin = new AppUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                EncryptionKey = "3423423425324562622ofofofofoffoo",
                IsAdmin = true,

            };

            await userManager.CreateAsync(admin, "Admin123!");
        }
        else
        {
            admin = await userManager.Users.FirstAsync(u => u.UserName == "admin");
        }

        // Seed Vehicles
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var existingVehicles = await context.Vehicles
            .Where(v => v.UserId == admin.Id)
            .ToListAsync();

        if (!existingVehicles.Any())
        {
            var now = DateTime.UtcNow;

            var vehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    Name = "Walk",
                    ProfileImageUrl = "Walk",
                    Type = VehicleType.Walk,
                    Capacity = 999999999,
                    Description = "Walk",
                    UserId = admin.Id,
                    Confirmed = true,
                    CreatedAt = now,
                    IsDeleted = false
                },
                new Vehicle
                {
                    Name = "Train",
                    ProfileImageUrl = "Train",
                    Type = VehicleType.Train,
                    Capacity = 999999999,
                    Description = "Train",
                    UserId = admin.Id,
                    Confirmed = true,
                    CreatedAt = now,
                    IsDeleted = false
                },
                new Vehicle
                {
                    Name = "Run",
                    ProfileImageUrl = "Run",
                    Type = VehicleType.Run,
                    Capacity = 999999999,
                    Description = "Run",
                    UserId = admin.Id,
                    Confirmed = true,
                    CreatedAt = now,
                    IsDeleted = false
                }
            };

            context.Vehicles.AddRange(vehicles);
            await context.SaveChangesAsync();
        }
    }


    catch (Exception ex)
    {
        logger.LogError(ex, "Admin user or vehicle seeding failed");
        // do NOT crash
    }
}

// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();


// --- Map endpoints ---
app.MapHub<ChatHub>("/chathub/81");
app.MapControllers();
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow:", "text/plain"));

app.Run();

public partial class Program { }
