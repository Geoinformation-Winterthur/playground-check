using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using playground_check.Configuration;
using playground_check.Data;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;
using Npgsql;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using playground_check.Controllers;

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(AppConfig.Configuration)
            .CreateLogger();

try
{

    Log.Information("Starting playground service.");

    NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddSingleton<WeatherForecastService>();

    // Add services for user login:
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<LoginController>();
    builder.Services.AddScoped<CustomAuthStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
            provider.GetRequiredService<CustomAuthStateProvider>());

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Playground service stopped with error.");
}
finally
{
    Log.CloseAndFlush();
}