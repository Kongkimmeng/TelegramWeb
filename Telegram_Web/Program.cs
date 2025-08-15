using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Telegram_Web.Data;
using Telegram_Web.Services;
using Telegram_Web.Services.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddBlazorBootstrap();

var apiBase = builder.Configuration["ApiBaseUrl"];

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBase)
});



builder.Services.AddScoped<ITelegramService, TelegramService>();
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
