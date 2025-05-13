

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
using ParrotsAPI2.Services.Token;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using ParrotsAPI2.Services.Bid;
using Microsoft.Extensions.FileProviders;
using ParrotsAPI2.Services.Cleanup;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Parrots API", Version = "v1" });
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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVoyageService, VoyageService>();
builder.Services.AddScoped<IWaypointService, WaypointService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
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
builder.Services.AddHostedService<VehicleVoyageCleanupService>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{

    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Parrots API V1");
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

app.UseCors("AllowAll");
//app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.MapControllers();
app.UseRouting();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = "/Uploads"
});


app.Run();



