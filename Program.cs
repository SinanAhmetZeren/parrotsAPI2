global using ParrotsAPI2.Models;
global using ParrotsAPI2.Services;
global using ParrotsAPI2.Dtos.Character;
global using ParrotsAPI2.Dtos.User;
global using AutoMapper;
global using ParrotsAPI2.Data;
global using Microsoft.EntityFrameworkCore;
global using ParrotsAPI2.Services.User;
global using Microsoft.AspNetCore.JsonPatch;





var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IUserService, UserService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



