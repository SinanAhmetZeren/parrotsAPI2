/*
Install - Package Pomelo.EntityFrameworkCore.MySql

program.cs:
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

services.AddDbContext<DataContext>(options =>
    options.UseMySql(Configuration.GetConnectionString("DefaultConnection"), 
        new MySqlServerVersion(new Version(8, 0, 23)),
        mySqlOptions => mySqlOptions
            .CharSetBehavior(CharSetBehavior.NeverAppend)));

appsettings:
"DefaultConnection": "Server=localhost;Database=YourDatabaseName;User=YourUsername;Password=YourPassword;"

csproj:
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="5.0.4" />
*/



global using ParrotsAPI2.Models;
global using ParrotsAPI2.Services;
global using ParrotsAPI2.Dtos.Character;
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
global using ParrotsAPI2.Services.Character;
global using ParrotsAPI2.Hubs;
global using Microsoft.AspNetCore.Identity;
using ParrotsAPI2.Services.Token;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using ParrotsAPI2.Services.Bid;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        BearerFormat = "JWT",
        Description = "Enter 'Bearer' [space] and your token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    var securityRequirement = new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } },
    };
    c.AddSecurityRequirement(securityRequirement);
});



builder.Services.AddSignalR();
builder.Logging.AddConsole();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVoyageService, VoyageService>();
builder.Services.AddScoped<IWaypointService, WaypointService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ChatHub>();
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 3;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();




builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("http://localhost")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
        c.DocExpansion(DocExpansion.List);
        c.EnableDeepLinking();
        c.DisplayOperationId();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
        c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Head, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Patch, SubmitMethod.Delete);
    });





}
app.UseHttpsRedirection();
app.MapHub<ChatHub>("/chathub/11");
app.UseCors(builder =>
{
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("http://localhost");
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("ws://localhost");
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("https://localhost");
});
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.MapControllers();
app.UseRouting();
app.UseAuthorization();
app.Run();



