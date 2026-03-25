using ElectionPredictor.Components;
using ElectionPredictor.Data;
using ElectionPredictor.Data.Entities;
using ElectionPredictor.Services;
using ElectionPredictor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWikipediaApiService, WikipediaApiService>();
builder.Services.AddScoped<IPollImportService, PollImportService>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IPredictionService, PredictionService>(); 
builder.Services.AddScoped<IWikipediaParseService, WikipediaParseService>();
builder.Services.AddScoped<IElectionResultParser, ElectionResultParser>();
builder.Services.AddScoped<IElectionResultsService, ElectionResultsService>();

builder.Services.AddHttpClient("Wikipedia", client =>
{
    client.BaseAddress = new Uri("https://en.wikipedia.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ElectionPredictor/1.0 (contact: qnkovanikol29@gmail.com)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
