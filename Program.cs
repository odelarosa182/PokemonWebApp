using PokemonWebApp.Models;
using PokemonWebApp.Services;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PokemonService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient<PokemonService>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
});

Log.Logger = new LoggerConfiguration().WriteTo.File("logs/pokeapp.txt", rollingInterval: RollingInterval.Day).
    CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
