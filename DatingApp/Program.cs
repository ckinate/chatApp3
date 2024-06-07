using DatingApp.Controllers;
using DatingApp.Data;
using DatingApp.Entities;
using DatingApp.Extensions;
using DatingApp.Logger;
using DatingApp.Middleware;
using DatingApp.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NLog;
using System.Text.Json.Serialization;

//var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = "wwwroot/browser" });
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
LogManager.Setup().LoadConfigurationFromFile(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config.txt"));
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
}); ;
builder.Services.AddApplicationService(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerManager>();
app.ConfigureExceptionHandler(logger);
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<ExceptionMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);
//Configure the HTTP request pipeline.
app.UseCors(builder => builder.AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              .WithOrigins("http://localhost:4200"));

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PresenceHub>("hubs/presence");
app.MapHub<MessageHub>("hubs/message");
app.MapFallbackToController(nameof(FallBackController.Index), nameof(FallBackController).Replace("Controller", ""));
using var scope = app.Services.CreateScope();
var service = scope.ServiceProvider;

var context = service.GetRequiredService<DataContext>();
var userManager = service.GetRequiredService<UserManager<AppUser>>();
var roleManager = service.GetRequiredService<RoleManager<AppRole>>();
await context.Database.MigrateAsync();
await Seed.ClearConnections(context);
await Seed.SeedUsers(userManager, roleManager);
// try
// {

// }
// catch (Exception ex)
// {
//     var logging = service.GetService<ILogger<Program>>();
//     logging.LogError(ex, "An error occurred during migration");
// }

app.Run();
