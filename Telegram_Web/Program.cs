using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Telegram_Web.Data;
using Telegram_Web.Services;
using Telegram_Web.Services.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();  //narort add for login dotnet add package Microsoft.AspNetCore.Authentication.Cookies
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddHttpClient();
builder.Services.AddSession();
builder.Services.AddSingleton<SignalRService>();


builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
    })
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

// 1. Register the user service as a singleton
builder.Services.AddSingleton<OnlineUserService>();

// 2. Register the circuit handler
builder.Services.AddScoped<CircuitHandler, TrackingCircuitHandler>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Denied";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();


var apiBase = builder.Configuration["ApiBaseUrl"];

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBase)
});


builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<UserStateService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();


app.MapRazorPages();// <--- This must be here
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
