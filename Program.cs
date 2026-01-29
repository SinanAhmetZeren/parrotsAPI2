

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

var builder = WebApplication.CreateBuilder(args);

// Bind Google settings from appsettings.json
builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection("Google")
);

// Add DbContext with Azure SQL connection string
// builder.Services.AddDbContext<DataContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
//         sqlOptions => sqlOptions.EnableRetryOnFailure()
//     ));


builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        });
    options.AddInterceptors(new DbFailureInterceptor());
});


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
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ChatHub>();
builder.Services.AddHostedService<VehicleVoyageCleanupService>();
builder.Services.AddScoped<IBlobService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetValue<string>("AzureStorage:ConnectionString");
    var containerName = configuration.GetValue<string>("AzureStorage:ContainerName");
    return new BlobService(connectionString, containerName);
});

builder.Services.AddMemoryCache();



// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
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


// endpoints
app.MapGet("/robots.txt", () =>
{
    return Results.Text(
        "User-agent: *\nDisallow:",
        "text/plain"
    );
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed");
        // do NOT crash
    }

    try
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (!await userManager.Users.AnyAsync())
        {
            var admin = new AppUser
            {
                UserName = "admin",
                Email = "admin@example.com"
            };

            await userManager.CreateAsync(admin, "Admin123!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Admin user seeding failed");
        // do NOT crash
    }
}



// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map Hub and Controllers
app.MapHub<ChatHub>("/chathub/11");
app.MapControllers();

// // Serve static files
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(
//         Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
//     RequestPath = "/Uploads"
// });

app.Run();
