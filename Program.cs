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
using ParrotsAPI2.Services.Character;
using ParrotsAPI2.Hubs;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVoyageService, VoyageService>();
builder.Services.AddScoped<IWaypointService, WaypointService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddSignalR();
builder.Logging.AddConsole();
builder.Services.AddScoped<ChatHub>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapHub<ChatHub>("/chathub/11");
app.UseCors(builder =>
{
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("http://localhost");
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("ws://localhost");
    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins("https://localhost");
});
app.MapControllers();
app.UseRouting();
app.UseAuthorization();
app.Run();



