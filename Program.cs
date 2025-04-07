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
using playground_check.Services;

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(AppConfig.Configuration)
            .CreateLogger();

try
{

    Log.Information("Starting playground service.");

    NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    string serviceDomain = AppConfig.Configuration.GetValue<string>("URL:ServiceDomain");
    string serviceBasePath = AppConfig.Configuration.GetValue<string>("URL:ServiceBasePath");
    string securityKey = AppConfig.Configuration.GetValue<string>("SecurityKey");

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddSingleton<SnackbarService>();

    // Add services for user login:
    builder.Services.AddAuthorizationCore();

    builder.Services.AddScoped<LoginController>();
    builder.Services.AddScoped<CustomAuthStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthStateProvider>());

    // Register services:
    builder.Services.AddScoped<AppStateProvider>();
    builder.Services.AddScoped<IPlaygroundService, PlaygroundService>();
    builder.Services.AddScoped<IInspectionService, InspectionService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        string serviceDescription = AppConfig.Configuration.GetValue<string>("ServiceDescription");
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Winterthur Playground Regular Inspection API - V1",
            Version = "v1",
            Description = serviceDescription
        });
        var commentsXmlFile = Path.Combine(System.AppContext.BaseDirectory,
                        "playground-check.xml");
        options.IncludeXmlComments(commentsXmlFile);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseSwagger(options =>
{
    if (!app.Environment.IsDevelopment())
    {
        options.PreSerializeFilters.Add((doc, httpRequest) =>
        {
            doc.Servers = new List<OpenApiServer> {
                    new OpenApiServer {
                        Url = serviceDomain + serviceBasePath
                        }
                    };

        });
    }
});
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        // endpoints.MapMetrics().RequireAuthorization("BasicAuthenticationForPrometheus");
    });

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